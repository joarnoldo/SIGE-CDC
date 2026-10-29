using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<ResultadoNucleoConceptos> PrepararAsync(SolicitudNucleoConceptos solicitud, string actor, CancellationToken ct = default)
        => LeerTemporalAsync(actor, () => PrepararNucleoEnTransaccionAsync(solicitud, ct), ct);

    private async Task<ResultadoNucleoConceptos> PrepararNucleoEnTransaccionAsync(SolicitudNucleoConceptos solicitud, CancellationToken ct,
        IReadOnlyDictionary<long, IReadOnlyList<DateTime>>? fronterasNormativas = null)
        {
            solicitud.Periodo.Validar();
            var bloqueos = new List<BloqueoConcepto>(); var lineas = new List<LineaMonetaria>();
            void Falta(string codigo, string clave, DateTime fecha, long? persona = null) => bloqueos.Add(new(codigo, clave, fecha, persona));
            if (solicitud.IdPeriodoPlanilla is { } idPeriodo)
            {
                var p = await contexto.PeriodosPlanilla.AsNoTracking().SingleOrDefaultAsync(p => p.IdPeriodoPlanilla == idPeriodo, ct);
                if (p is null || p.FechaInicio != solicitud.Periodo.Inicio || p.FechaFin != solicitud.Periodo.Fin
                    || p.TipoPeriodo != solicitud.Periodo.Tipo || p.EstadoRegistro != Activo)
                    Falta("PERIODO_INCOMPATIBLE", "PERIODO", solicitud.Periodo.Inicio);
            }
            // Una sola transacción MVCC abarca D y toda la configuración de E.
            var temporal = await ResolverTemporalEnTransaccionAsync(solicitud.Periodo, solicitud.ReferenciaRelacionOrdinaria, ct);
            foreach (var p in temporal.Poblacion)
                foreach (var b in p.Bloqueos) Falta(b.Codigo, "TEMPORAL", b.Fecha, p.IdColaborador);
            if (temporal.Poblacion.Count == 0) Falta("POBLACION_FALTANTE", "TEMPORAL", solicitud.Periodo.Inicio);
            var configuracion = await LeerInternosAsync(solicitud.Internos.Select(h => h.CodigoParametro).Distinct().ToArray(), ct);
            var codigos = configuracion.Parametros.Select(p => p.ConceptoPlanilla?.Codigo).OfType<string>().Append("SALARIO_ORDINARIO").Distinct().ToArray();
            var conceptos = await LeerConceptosAsync(codigos, ct);
            foreach (var persona in temporal.Poblacion.Where(p => p.EsResoluble))
            {
                foreach (var segmento in persona.Segmentos)
                {
                    try
                    {
                        // Un cambio de versión de concepto es frontera explícita reutilizando el calendario de D.
                        var fronteras = conceptos.Where(c => c.Codigo == "SALARIO_ORDINARIO").SelectMany(c =>
                            c.Vigencia.Fin is { } fin && fin < segmento.Fin ? new[] { c.Vigencia.Inicio, fin.AddDays(1) } : new[] { c.Vigencia.Inicio });
                        var adicionales = fronterasNormativas?.GetValueOrDefault(persona.IdColaborador) ?? [];
                        foreach (var tramo in CalendarioSalarial.Dividir(new(segmento.Inicio, segmento.Fin), fronteras.Concat(adicionales)))
                        {
                            var s = segmento with { Inicio = tramo.Inicio, Fin = tramo.Fin!.Value,
                                DiasBase30 = CalendarioSalarial.DiasBase30(tramo.Inicio, tramo.Fin.Value) };
                            lineas.Add(ProductorLineasConceptos.Ordinario(s, ResolucionConceptos.Resolver("SALARIO_ORDINARIO", s.Inicio, conceptos)));
                        }
                    }
                    catch (ConfiguracionConceptoException e) { Falta(e.Codigo, "SALARIO_ORDINARIO", segmento.Inicio, persona.IdColaborador); }
                    catch (ArgumentException) { Falta("VIGENCIA_CONCEPTO_INVALIDA", "SALARIO_ORDINARIO", segmento.Inicio, persona.IdColaborador); }
                    catch (OverflowException) { Falta("DESBORDAMIENTO_MONETARIO", "SALARIO_ORDINARIO", segmento.Inicio, persona.IdColaborador); }
                }
            }
            foreach (var h in solicitud.Internos)
            {
                try
                {
                    if (!new VigenciaFinanciera(solicitud.Periodo.Inicio, solicitud.Periodo.Fin).Contiene(h.FechaHecho))
                        throw new ConfiguracionConceptoException("HECHO_FUERA_PERIODO");
                    var persona = temporal.Poblacion.SingleOrDefault(p => p.IdColaborador == h.IdColaborador);
                    if (persona is null || !persona.EsResoluble) throw new ConfiguracionConceptoException("TEMPORAL_BLOQUEADO");
                    var parametro = ResolucionParametrosInternos.Resolver(h.CodigoParametro, h.FechaHecho, h.IdColaborador,
                        solicitud.IdPeriodoPlanilla, configuracion.Versiones, configuracion.Overrides);
                    var codigo = configuracion.Parametros.Single(p => p.IdParametroPlanilla == parametro.Version.IdVersion).ConceptoPlanilla!.Codigo;
                    var concepto = ResolucionConceptos.Resolver(codigo, h.FechaHecho, conceptos);
                    SegmentoSalarialTemporal? segmento = null;
                    if (h.Segmento is not null)
                    {
                        segmento = persona.Segmentos.SingleOrDefault(s => ReferenciaSegmentoTemporal.De(s) == h.Segmento);
                        if (segmento is null || h.FechaHecho < segmento.Inicio || h.FechaHecho > segmento.Fin)
                            throw new ConfiguracionConceptoException("SEGMENTO_INCOMPATIBLE");
                    }
                    lineas.Add(ProductorLineasConceptos.Interno(parametro, concepto,
                        new(h.IdColaborador, h.ClaveHecho, "Parámetro", parametro.Version.IdVersion, h.FechaHecho,
                            h.Segmento, h.ReferenciaSoporte), segmento, h.ReferenciaAutorizacion));
                }
                catch (ConfiguracionConceptoException e) { Falta(e.Codigo, h.CodigoParametro, h.FechaHecho, h.IdColaborador); }
                catch (ArgumentException) { Falta("VIGENCIA_INTERNA_INVALIDA", h.CodigoParametro, h.FechaHecho, h.IdColaborador); }
                catch (OverflowException) { Falta("DESBORDAMIENTO_MONETARIO", h.CodigoParametro, h.FechaHecho, h.IdColaborador); }
            }
            ConciliacionLineas? conciliacion = null;
            if (bloqueos.Count == 0)
            {
                try { conciliacion = AgregadorLineasMonetarias.Conciliar(lineas); }
                catch (ConfiguracionConceptoException e) { Falta(e.Codigo, "CONCILIACION", solicitud.Periodo.Inicio); }
                catch (OverflowException) { Falta("DESBORDAMIENTO_MONETARIO", "CONCILIACION", solicitud.Periodo.Inicio); }
            }
            return new ResultadoNucleoConceptos(conciliacion,
                Array.AsReadOnly(bloqueos.Distinct().OrderBy(b => b.IdColaborador).ThenBy(b => b.Fecha)
                    .ThenBy(b => b.Clave, StringComparer.Ordinal).ThenBy(b => b.Codigo, StringComparer.Ordinal).ToArray()));
        }

    public Task<ParametroInternoResuelto> ResolverParametroAsync(string codigo, DateTime fecha, long idColaborador,
        long? idPeriodo, string actor, CancellationToken ct = default)
        => LeerTemporalAsync(actor, async () =>
        {
            new VigenciaFinanciera(fecha, fecha).Validar();
            var datos = await LeerInternosAsync([codigo], ct);
            var resultado = ResolucionParametrosInternos.Resolver(codigo, fecha, idColaborador, idPeriodo, datos.Versiones, datos.Overrides);
            var clave = datos.Parametros.Single(p => p.IdParametroPlanilla == resultado.Version.IdVersion).ConceptoPlanilla!.Codigo;
            ResolucionConceptos.Resolver(clave, fecha, await LeerConceptosAsync([clave], ct));
            return resultado;
        }, ct);

    private static T? LeerEvidencia<T>(string? json) where T : class
    {
        try { return json is null ? null : JsonSerializer.Deserialize<T>(json,
            new JsonSerializerOptions { RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true }); }
        catch (JsonException) { return null; }
    }

    private async Task<IReadOnlyList<ConceptoMonetarioVersion>> LeerConceptosAsync(string[] codigos, CancellationToken ct)
    {
        var catalogos = await FiltrarClaves(contexto.ConceptosPlanilla.AsNoTracking().Include(c => c.Versiones), c => c.Codigo, codigos).ToListAsync(ct);
        var ids = catalogos.Select(c => c.IdConceptoPlanilla.ToString(CultureInfo.InvariantCulture)).ToArray();
        var auditorias = await FiltrarClaves(contexto.BitacoraAuditoria.AsNoTracking(), b => b.IdRegistro, ids)
            .Where(b => b.Entidad == nameof(ConceptoPlanilla) && b.Accion == "VersionarConcepto" && b.IdUsuario != null).ToListAsync(ct);
        return catalogos.SelectMany(c => c.Versiones.Select(v =>
        {
            var clasificacion = new ClasificacionConcepto(v.Clasificacion, v.EsSalarial, v.EsContributivoCCSS, v.EsTributario,
                v.EsPatronal, v.TipoBase, v.UnidadPermitida, v.PermiteMontoManual, v.RequiereAutorizacion);
            var confirmada = auditorias.Where(a => a.IdRegistro == c.IdConceptoPlanilla.ToString(CultureInfo.InvariantCulture))
                .Select(a => LeerEvidencia<ConceptoCarga>(a.ValoresNuevos)).Any(d => d is not null && d.Codigo == c.Codigo && d.Nombre == c.Nombre
                    && d.Vigencia == Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin) && d.Clasificacion == clasificacion && !string.IsNullOrWhiteSpace(d.ReferenciaSoporte));
            return new ConceptoMonetarioVersion(c.IdConceptoPlanilla, v.IdConceptoPlanillaVersion, c.Codigo, c.Nombre,
                Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin), v.Clasificacion, v.EsSalarial, v.EsContributivoCCSS, v.EsTributario,
                v.EsPatronal, v.TipoBase, v.UnidadPermitida, v.PermiteMontoManual, v.RequiereAutorizacion,
                c.EstadoRegistro == Activo && v.EstadoRegistro == Activo, confirmada);
        })).ToArray();
    }

    private sealed record LecturaInternos(IReadOnlyList<ParametroPlanilla> Parametros, IReadOnlyList<VersionParametroInterno> Versiones,
        IReadOnlyList<OverrideParametroInterno> Overrides);
    private async Task<LecturaInternos> LeerInternosAsync(string[] codigos, CancellationToken ct)
    {
        var parametros = await FiltrarClaves(contexto.ParametrosPlanilla.AsNoTracking().Include(p => p.ConceptoPlanilla)
            .Include(p => p.AsignacionesColaborador).Include(p => p.AsignacionesPeriodo).AsSplitQuery(), p => p.Codigo, codigos).ToListAsync(ct);
        var clavesNormativas = await FiltrarClaves(contexto.ParametrosNormativos.AsNoTracking(), p => p.Codigo, codigos).Select(p => p.Codigo).ToListAsync(ct);
        var ids = parametros.Select(p => p.IdParametroPlanilla.ToString(CultureInfo.InvariantCulture)).ToArray();
        var auditorias = await FiltrarClaves(contexto.BitacoraAuditoria.AsNoTracking(), b => b.IdRegistro, ids)
            .Where(b => b.Entidad == nameof(ParametroPlanilla) && (b.Accion == "VersionarParametroInterno" || b.Accion == "RegistrarOverrideInterno") && b.IdUsuario != null).ToListAsync(ct);
        var versiones = parametros.Select(p =>
        {
            var evidencia = auditorias.Where(a => a.Accion == "VersionarParametroInterno" && a.IdUsuario == p.CreadoPor
                && a.IdRegistro == p.IdParametroPlanilla.ToString(CultureInfo.InvariantCulture)).Select(a => LeerEvidencia<ParametroInternoCarga>(a.ValoresNuevos))
                .Any(d => d is not null && d.Codigo == p.Codigo && d.Nombre == p.Nombre && d.IdConcepto == p.IdConceptoPlanilla
                    && d.Tipo == p.TipoParametro && d.Naturaleza == p.Naturaleza && d.ValorDecimal == p.ValorDecimal && d.ValorTexto == p.ValorTexto
                    && d.Vigencia.Inicio == p.FechaVigenciaInicio && d.Vigencia.Fin == p.FechaVigenciaFin
                    && d.ReferenciaSoporte == p.Descripcion && !string.IsNullOrWhiteSpace(d.ReferenciaSoporte));
            return new VersionParametroInterno(p.IdParametroPlanilla, p.Codigo, p.IdConceptoPlanilla ?? 0, p.TipoParametro, p.Naturaleza ?? "",
                p.ValorDecimal, p.ValorTexto, p.FechaVigenciaInicio is { } inicio ? Vigencia(inicio, p.FechaVigenciaFin) : null,
                p.EstadoRegistro == Activo, !p.EsEditable && evidencia && p.FechaModificacion is null,
                p.EsNormativo || clavesNormativas.Contains(p.Codigo) || ClavesOrdinarias.Contains(p.Codigo));
        }).ToList();
        // Una solicitud normativa sin fila interna se rechaza explícitamente; no se convierte en cero/faltante interno.
        versiones.AddRange(clavesNormativas.Where(k => parametros.All(p => p.Codigo != k))
            .Select(k => new VersionParametroInterno(0, k, 0, "", "", null, null, null, false, false, true)));
        var overrides = new List<OverrideParametroInterno>();
        foreach (var p in parametros)
        {
            void Agregar(long id, AmbitoParametroInterno ambito, long destino, decimal? valor, string? texto, string estado, string? actor)
            {
                var evidencia = auditorias.Where(a => a.Accion == "RegistrarOverrideInterno" && a.IdUsuario == actor
                    && a.IdRegistro == p.IdParametroPlanilla.ToString(CultureInfo.InvariantCulture)).Select(a => LeerEvidencia<EvidenciaOverride>(a.ValoresNuevos))
                    .Any(e => e is not null && e.IdOverride == id && e.Solicitud.IdParametro == p.IdParametroPlanilla && e.Solicitud.Ambito == ambito
                        && e.Solicitud.IdDestino == destino && e.Solicitud.ValorDecimal == valor && e.Solicitud.ValorTexto == texto
                        && !string.IsNullOrWhiteSpace(e.Solicitud.ReferenciaSoporte));
                overrides.Add(new(id, p.IdParametroPlanilla, ambito, destino, valor, texto, estado == Activo, evidencia));
            }
            foreach (var o in p.AsignacionesColaborador) Agregar(o.IdParametroPlanillaColaborador, AmbitoParametroInterno.Colaborador, o.IdColaborador, o.ValorDecimalOverride, o.ValorTextoOverride, o.EstadoRegistro, o.CreadoPor);
            foreach (var o in p.AsignacionesPeriodo) Agregar(o.IdParametroPlanillaPeriodo, AmbitoParametroInterno.Periodo, o.IdPeriodoPlanilla, o.ValorDecimalOverride, o.ValorTextoOverride, o.EstadoRegistro, o.CreadoPor);
        }
        return new(parametros, versiones, overrides);
    }

    private sealed record EvidenciaOverride(long IdOverride, SolicitudOverrideInterno Solicitud);
    public Task<long> RegistrarOverrideInternoAsync(SolicitudOverrideInterno solicitud, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            Texto(solicitud.ReferenciaSoporte, 300, "Soporte del override");
            var parametro = await contexto.ParametrosPlanilla.AsNoTracking().SingleAsync(p => p.IdParametroPlanilla == solicitud.IdParametro, ct);
            var datos = await LeerInternosAsync([parametro.Codigo], ct);
            var version = datos.Versiones.Single(p => p.IdVersion == solicitud.IdParametro);
            if (version.EsNormativo) throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
            if (!version.Gobernado || !version.Activo || version.Vigencia is null) throw new ConfiguracionConceptoException("PARAMETRO_NO_GOBERNADO");
            var codigoConcepto = datos.Parametros.Single(p => p.IdParametroPlanilla == solicitud.IdParametro).ConceptoPlanilla!.Codigo;
            var conceptos = await LeerConceptosAsync([codigoConcepto], ct);
            var c = ResolucionConceptos.Resolver(codigoConcepto, version.Vigencia.Inicio, conceptos);
            if (c.Clasificacion is "DeduccionLegal" or "CargaPatronal" || c.TipoBase == "REDUCCION_DEVENGO") throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
            ResolucionParametrosInternos.ValidarValor(version.Tipo, solicitud.ValorDecimal, solicitud.ValorTexto);
            if (solicitud.ValorDecimal is { } valor) DecimalExacto(valor, 4, 0, 99999999999999.9999m);
            if (solicitud.ValorTexto is { } texto) Texto(texto, 255, "Valor textual");
            long id;
            if (solicitud.Ambito == AmbitoParametroInterno.Colaborador)
            {
                if (!await contexto.Colaboradores.AnyAsync(c => c.IdColaborador == solicitud.IdDestino, ct)) throw new ConfiguracionConceptoException("COLABORADOR_FALTANTE");
                if (await contexto.ParametrosPlanillaColaborador.AnyAsync(o => o.IdParametroPlanilla == solicitud.IdParametro && o.IdColaborador == solicitud.IdDestino, ct))
                    throw new ConfiguracionConceptoException("OVERRIDE_EXISTENTE_CREAR_VERSION");
                var o = new ParametroPlanillaColaborador { IdParametroPlanilla = solicitud.IdParametro, IdColaborador = solicitud.IdDestino,
                    ValorDecimalOverride = solicitud.ValorDecimal, ValorTextoOverride = solicitud.ValorTexto, CreadoPor = actor, FechaCreacion = DateTime.UtcNow };
                contexto.ParametrosPlanillaColaborador.Add(o); await contexto.SaveChangesAsync(ct); id = o.IdParametroPlanillaColaborador;
            }
            else if (solicitud.Ambito == AmbitoParametroInterno.Periodo)
            {
                var periodo = await BloqueoPeriodoPlanilla.ObtenerAsync(contexto, solicitud.IdDestino, ct);
                if (periodo is null || periodo.EstadoRegistro != Activo || !version.Vigencia.Solapa(new(periodo.FechaInicio, periodo.FechaFin)))
                    throw new ConfiguracionConceptoException("PERIODO_INCOMPATIBLE");
                if (FlujoEstadosPlanilla.EstaBloqueada(periodo.EstadoPlanilla?.Nombre)) throw new ConfiguracionConceptoException("PERIODO_BLOQUEADO");
                if (await contexto.ParametrosPlanillaPeriodo.AnyAsync(o => o.IdParametroPlanilla == solicitud.IdParametro && o.IdPeriodoPlanilla == solicitud.IdDestino, ct))
                    throw new ConfiguracionConceptoException("OVERRIDE_EXISTENTE_CREAR_VERSION");
                var o = new ParametroPlanillaPeriodo { IdParametroPlanilla = solicitud.IdParametro, IdPeriodoPlanilla = solicitud.IdDestino,
                    ValorDecimalOverride = solicitud.ValorDecimal, ValorTextoOverride = solicitud.ValorTexto, CreadoPor = actor, FechaCreacion = DateTime.UtcNow };
                contexto.ParametrosPlanillaPeriodo.Add(o); await contexto.SaveChangesAsync(ct); id = o.IdParametroPlanillaPeriodo;
            }
            else throw new ConfiguracionConceptoException("AMBITO_OVERRIDE_INVALIDO");
            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria { IdUsuario = actor, FechaHora = DateTime.UtcNow,
                Entidad = nameof(ParametroPlanilla), IdRegistro = solicitud.IdParametro.ToString(CultureInfo.InvariantCulture), Accion = "RegistrarOverrideInterno",
                ValoresNuevos = JsonSerializer.Serialize(new EvidenciaOverride(id, solicitud)), Observacion = "F1-FIN-02E: override explícito sin editar versión; FIN-02 contenido." });
            return id;
        }, ct);
}

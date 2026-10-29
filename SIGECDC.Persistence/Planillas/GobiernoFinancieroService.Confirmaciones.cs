using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<long> CapturarIncidenciaFinancieraAsync(CapturaIncidenciaFinanciera solicitud,
        string referenciaRelacionOrdinaria, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            var periodo = await PeriodoIncidenciasEditableAsync(solicitud.IdPeriodo, ct);
            var h = new IncidenciaPlanilla { IdPeriodoPlanilla = solicitud.IdPeriodo, IdColaborador = solicitud.IdColaborador,
                IdTipoIncidenciaPlanilla = solicitud.IdTipoIncidencia, IdConceptoPlanillaVersion = solicitud.IdConceptoVersion,
                FechaIncidencia = solicitud.FechaHecho, Unidad = solicitud.Unidad, Cantidad = solicitud.Cantidad,
                Monto = solicitud.MontoManual, Descripcion = Texto(solicitud.ReferenciaHecho, 300, "Referencia del hecho"),
                RegistradoPor = actor, FechaRegistro = DateTime.UtcNow };
            h.TipoIncidenciaPlanilla = await contexto.TiposIncidenciaPlanilla.Include(t => t.ConceptoPlanilla)
                .SingleOrDefaultAsync(t => t.IdTipoIncidenciaPlanilla == h.IdTipoIncidenciaPlanilla, ct);
            var conceptos = await LeerConceptosAsync([h.TipoIncidenciaPlanilla?.ConceptoPlanilla?.Codigo ?? ""], ct);
            ValidarHechoFinanciero(h, periodo, conceptos);
            var temporal = await ResolverTemporalEnTransaccionAsync(periodo, referenciaRelacionOrdinaria, ct);
            SegmentoIncidencia(h, temporal);
            contexto.IncidenciasPlanilla.Add(h); await contexto.SaveChangesAsync(ct);
            AuditarIncidencia(actor, "CapturarIncidenciaFinanciera", nameof(IncidenciaPlanilla), h.IdIncidenciaPlanilla, solicitud);
            return h.IdIncidenciaPlanilla;
        }, ct, IsolationLevel.Serializable);

    public async Task<long> ConfirmarIncidenciaAsync(ConfirmacionIncidenciaSolicitud solicitud,
        string referenciaRelacionOrdinaria, string actor, CancellationToken ct = default)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Confirmar requiere una unidad de trabajo independiente sin cambios pendientes.");
        await AutorizarAsync(actor, ct);
        // Descubrimiento sin locks; la lectura financiera sucede después del lock de período.
        var idPeriodo = await contexto.IncidenciasPlanilla.AsNoTracking().Where(h => h.IdIncidenciaPlanilla == solicitud.IdIncidencia)
            .Select(h => (long?)h.IdPeriodoPlanilla).SingleOrDefaultAsync(ct)
            ?? throw new ConfiguracionConceptoException("INCIDENCIA_FALTANTE");
        return await EjecutarAsync(actor, async () =>
        {
            // La primera lectura solo identifica el período; se comprueba de nuevo tras bloquear la fila.
            // Una actualización legada que cambie de período provoca conflicto, nunca se usa un lock ajeno.
            var periodo = await PeriodoIncidenciasEditableAsync(idPeriodo, ct);
            await contexto.Database.ExecuteSqlInterpolatedAsync($"SELECT IdIncidenciaPlanilla FROM IncidenciaPlanilla WHERE IdIncidenciaPlanilla={solicitud.IdIncidencia} FOR UPDATE", ct);
            var h = await contexto.IncidenciasPlanilla.AsNoTracking().Include(h => h.TipoIncidenciaPlanilla).ThenInclude(t => t!.ConceptoPlanilla)
                .SingleAsync(h => h.IdIncidenciaPlanilla == solicitud.IdIncidencia, ct);
            if (h.IdPeriodoPlanilla != idPeriodo) throw new ConfiguracionConceptoException("CONFLICTO_INCIDENCIA_PERIODO");
            var ultima = await contexto.ConfirmacionesIncidencias.Where(c => c.IdIncidenciaPlanilla == h.IdIncidenciaPlanilla)
                .Select(c => (int?)c.NumeroVersion).MaxAsync(ct) ?? 0;
            if (solicitud.UltimaVersionEsperada != ultima) throw new ConfiguracionConceptoException("CONFLICTO_VERSION_CONFIRMACION");
            var conceptos = await LeerConceptosAsync([h.TipoIncidenciaPlanilla?.ConceptoPlanilla?.Codigo ?? ""], ct);
            var concepto = ValidarHechoFinanciero(h, periodo, conceptos);
            var temporal = await ResolverTemporalEnTransaccionAsync(periodo, referenciaRelacionOrdinaria, ct);
            var segmento = SegmentoIncidencia(h, temporal);
            ValidarTipoDia(concepto, solicitud.TipoDiaHE);
            var soporte = Texto(solicitud.ReferenciaSoporte, 500, "Soporte de confirmación");
            if (concepto.RequiereAutorizacion && !solicitud.RegistrarAutorizacion)
                throw new ConfiguracionConceptoException("AUTORIZACION_INCIDENCIA_NO_DEMOSTRABLE");
            if (!solicitud.RegistrarAutorizacion && solicitud.ReferenciaAutorizacion is not null)
                throw new ConfiguracionConceptoException("AUTORIZACION_PARCIAL");
            var autorizacion = solicitud.RegistrarAutorizacion ? Texto(solicitud.ReferenciaAutorizacion, 500, "Autorización explícita") : null;
            var parametro = await ParametroIncidenciaAsync(h, concepto, solicitud.IdParametroPlanilla, solicitud.BaseParametro, segmento, ct);
            var usaMonto = parametro is null && concepto.PermiteMontoManual && h.Unidad == "Monto";
            var ahora = DateTime.UtcNow;
            var c = new IncidenciaPlanillaConfirmacion { IdIncidenciaPlanilla = h.IdIncidenciaPlanilla, NumeroVersion = checked(ultima + 1),
                FechaHecho = h.FechaIncidencia, IdConceptoPlanillaVersion = concepto.IdVersion, Unidad = h.Unidad!, Cantidad = h.Cantidad,
                MontoManual = usaMonto ? h.Monto : null, TipoDiaHE = solicitud.TipoDiaHE, ReferenciaSoporte = soporte,
                ConfirmadoPor = actor, FechaConfirmacion = ahora, AutorizacionRequerida = concepto.RequiereAutorizacion,
                AutorizadoPor = solicitud.RegistrarAutorizacion ? actor : null, FechaAutorizacion = solicitud.RegistrarAutorizacion ? ahora : null,
                ReferenciaAutorizacion = autorizacion, IdParametroPlanilla = solicitud.IdParametroPlanilla,
                IdCondicionBase = solicitud.BaseParametro?.IdCondicion, FechaBaseInicio = solicitud.BaseParametro?.Inicio,
                FechaBaseFin = solicitud.BaseParametro?.Fin,
                HuellaIncidencia = HuellaIncidenciaConfirmada.Calcular(h, soporte, solicitud.TipoDiaHE, usaMonto,
                    solicitud.IdParametroPlanilla, solicitud.BaseParametro?.IdCondicion, solicitud.BaseParametro?.Inicio, solicitud.BaseParametro?.Fin) };
            contexto.ConfirmacionesIncidencias.Add(c); await contexto.SaveChangesAsync(ct);
            AuditarIncidencia(actor, "ConfirmarIncidencia", nameof(IncidenciaPlanillaConfirmacion), c.IdIncidenciaPlanillaConfirmacion,
                EvidenciaConfirmacion.De(c));
            return c.IdIncidenciaPlanillaConfirmacion;
        }, ct, IsolationLevel.Serializable);
    }

    private async Task<PeriodoSalarial> PeriodoIncidenciasEditableAsync(long idPeriodo, CancellationToken ct)
    {
        var p = await BloqueoPeriodoPlanilla.ObtenerAsync(contexto, idPeriodo, ct);
        if (p is null || p.EstadoRegistro != Activo) throw new ConfiguracionConceptoException("PERIODO_INCOMPATIBLE");
        if (p.EstadoPlanilla?.Nombre != "Borrador") throw new ConfiguracionConceptoException("PERIODO_BLOQUEADO");
        var periodo = new PeriodoSalarial(p.FechaInicio, p.FechaFin, p.TipoPeriodo); periodo.Validar(); return periodo;
    }

    private static ConceptoMonetarioVersion ValidarHechoFinanciero(IncidenciaPlanilla h, PeriodoSalarial periodo,
        IReadOnlyList<ConceptoMonetarioVersion> conceptos)
    {
        if (h.EstadoRegistro != Activo) throw new ConfiguracionConceptoException("INCIDENCIA_INACTIVA");
        if (h.FechaIncidencia != h.FechaIncidencia.Date || h.FechaIncidencia < periodo.Inicio || h.FechaIncidencia > periodo.Fin)
            throw new ConfiguracionConceptoException("INCIDENCIA_FUERA_PERIODO");
        if (h.TipoIncidenciaPlanilla is not { EstadoRegistro: Activo, ConceptoPlanilla: { } catalogo })
            throw new ConfiguracionConceptoException("TIPO_INCIDENCIA_NO_CLASIFICADO");
        if (h.IdConceptoPlanillaVersion is null) throw new ConfiguracionConceptoException("INCIDENCIA_SIN_VERSION_FIJADA");
        var c = ResolucionConceptos.Resolver(catalogo.Codigo, h.FechaIncidencia, conceptos);
        if (c.IdVersion != h.IdConceptoPlanillaVersion) throw new ConfiguracionConceptoException("VERSION_FIJADA_INCOMPATIBLE");
        if (c.Codigo is not ("HORA_EXTRA_ORDINARIA" or "AUSENCIA_SIN_GOCE") && c.Clasificacion is not ("IngresoSalarial" or "IngresoNoSalarial" or "DeduccionInterna"))
            throw new ConfiguracionConceptoException("CLASE_HECHO_NO_SOPORTADA");
        if (c.Codigo == "HORA_EXTRA_ORDINARIA" ? c.TipoBase != "HORA_ORDINARIA"
            : c.Codigo == "AUSENCIA_SIN_GOCE" ? c.TipoBase != "REDUCCION_DEVENGO"
            : c.TipoBase is not ("SALARIO_DEVENGADO" or "MONTO_AUTORIZADO"))
            throw new ConfiguracionConceptoException("TIPO_BASE_INCIDENCIA_INCOMPATIBLE");
        if (c.TipoBase == "REDUCCION_DEVENGO" ? h.Unidad is not ("Día" or "Hora") : h.Unidad != c.UnidadPermitida)
            throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        if (h.Unidad is "Hora" or "Día")
        {
            if (h.Cantidad is null or <= 0) throw new ConfiguracionConceptoException("CANTIDAD_INCIDENCIA_INVALIDA");
            DecimalExacto(h.Cantidad.Value, 2, 0.01m, 9999999999999999.99m);
            if (h.Unidad == "Día" && h.Cantidad > 1) throw new ConfiguracionConceptoException("DISTRIBUCION_TEMPORAL_NO_DEMOSTRABLE");
        }
        else if (h.Cantidad is not null) throw new ConfiguracionConceptoException("CANTIDAD_INCIDENCIA_INVALIDA");
        DecimalExacto(h.Monto, 2, 0, 9999999999999999.99m);
        return c;
    }

    private static SegmentoSalarialTemporal SegmentoIncidencia(IncidenciaPlanilla h, ResultadoTemporalSalarial temporal)
    {
        var persona = temporal.Poblacion.SingleOrDefault(p => p.IdColaborador == h.IdColaborador);
        var segmentos = persona?.Segmentos.Where(s => s.Inicio <= h.FechaIncidencia && h.FechaIncidencia <= s.Fin).ToArray() ?? [];
        if (persona is null || !persona.EsResoluble || segmentos.Length != 1) throw new ConfiguracionConceptoException("TEMPORAL_BLOQUEADO");
        return segmentos[0];
    }

    private static void ValidarTipoDia(ConceptoMonetarioVersion c, string? tipoDia)
    {
        if (c.Codigo == "HORA_EXTRA_ORDINARIA")
        {
            if (tipoDia is not ("Ordinario" or "Feriado" or "Descanso" or "Especial"))
                throw new ConfiguracionConceptoException("TIPO_DIA_HE_NO_DEMOSTRABLE");
        }
        else if (tipoDia is not null) throw new ConfiguracionConceptoException("TIPO_DIA_HE_INCOMPATIBLE");
    }

    private async Task<ParametroInternoResuelto?> ParametroIncidenciaAsync(IncidenciaPlanilla h, ConceptoMonetarioVersion c,
        int? idParametro, ReferenciaSegmentoTemporal? referenciaBase, SegmentoSalarialTemporal segmento, CancellationToken ct)
    {
        if (idParametro is null)
        {
            if (referenciaBase is not null) throw new ConfiguracionConceptoException("BASE_PARAMETRO_INCOMPATIBLE");
            if (c.TipoBase is not ("HORA_ORDINARIA" or "REDUCCION_DEVENGO") && !(c.PermiteMontoManual && c.TipoBase == "MONTO_AUTORIZADO" && h.Unidad == "Monto"))
                throw new ConfiguracionConceptoException("PARAMETRO_INTERNO_O_MODALIDAD_FALTANTE");
            return null;
        }
        if (c.Codigo is "HORA_EXTRA_ORDINARIA" or "AUSENCIA_SIN_GOCE") throw new ConfiguracionConceptoException("PARAMETRO_INCIDENCIA_INCOMPATIBLE");
        var codigo = await contexto.ParametrosPlanilla.AsNoTracking().Where(p => p.IdParametroPlanilla == idParametro).Select(p => p.Codigo).SingleOrDefaultAsync(ct)
            ?? throw new ConfiguracionConceptoException("PARAMETRO_INTERNO_FALTANTE");
        var datos = await LeerInternosAsync([codigo], ct);
        var p = ResolucionParametrosInternos.Resolver(codigo, h.FechaIncidencia, h.IdColaborador, h.IdPeriodoPlanilla, datos.Versiones, datos.Overrides);
        if (p.Version.IdVersion != idParametro || p.Version.IdConcepto != c.IdConcepto || p.Version.Tipo != h.Unidad)
            throw new ConfiguracionConceptoException("PARAMETRO_INCIDENCIA_INCOMPATIBLE");
        if (p.Version.Tipo == "Porcentaje" && referenciaBase != ReferenciaSegmentoTemporal.De(segmento)
            || referenciaBase is not null && referenciaBase != ReferenciaSegmentoTemporal.De(segmento))
            throw new ConfiguracionConceptoException("BASE_PORCENTUAL_NO_DEMOSTRADA");
        // Disponibilidad semántica, sin calcular dinero al confirmar el hecho.
        if (BasesConceptos.Familia(c.TipoBase) is not (FamiliaBaseConcepto.SalarioOrdinario or FamiliaBaseConcepto.MontoAutorizado))
            throw new ConfiguracionConceptoException("MOTOR_BASE_NO_DISPONIBLE");
        return p;
    }

    private sealed record EvidenciaConfirmacion(long IdIncidenciaPlanilla, int NumeroVersion, string HuellaIncidencia,
        string ConfirmadoPor, string? AutorizadoPor, string ReferenciaSoporte, string? ReferenciaAutorizacion)
    {
        public static EvidenciaConfirmacion De(IncidenciaPlanillaConfirmacion c) => new(c.IdIncidenciaPlanilla, c.NumeroVersion,
            c.HuellaIncidencia, c.ConfirmadoPor, c.AutorizadoPor, c.ReferenciaSoporte, c.ReferenciaAutorizacion);
    }

    private void AuditarIncidencia(string actor, string accion, string entidad, long id, object datos)
        => contexto.BitacoraAuditoria.Add(new BitacoraAuditoria { IdUsuario = actor, FechaHora = DateTime.UtcNow,
            Accion = accion, Entidad = entidad, IdRegistro = id.ToString(CultureInfo.InvariantCulture), ValoresNuevos = JsonSerializer.Serialize(datos),
            Observacion = "F1-FIN-02F: evidencia explícita del hecho; FIN-02 contenido. No es un snapshot financiero." });
}

using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    // Identidades de obligaciones, nunca cifras anuales ni una tasa financiera agregada.
    public static IReadOnlyList<string> ClavesOrdinarias => CatalogoCuotasSociales.Claves;

    public Task<ConfiguracionFinancieraResuelta> ResolverAsync(RequisitosFinancieros solicitud, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            solicitud.Periodo.Validar();
            if (solicitud.Periodo.Fin is null) throw new ArgumentException("La consulta requiere un período finito.");
            Opcion(solicitud.PeriodicidadMinimo, "Diario", "Mensual"); Clave(solicitud.ClaveAplicacion);
            var faltantes = new List<FaltanteFinanciero>();
            var datos = new List<DatoFinancieroResuelto>();
            var contratos = await contexto.Contratos.AsNoTracking().Include(c => c.Colaborador).ThenInclude(c => c!.Puesto)
                .Include(c => c.JornadaLaboral).Include(c => c.ConfiguracionRiesgoTrabajo)
                .Where(c => solicitud.IdContratos.Contains(c.IdContrato)).ToListAsync(ct);
            foreach (var contrato in contratos.Where(c => !Vigencia(c.FechaInicio, c.FechaFin).Solapa(solicitud.Periodo)))
                faltantes.Add(new("CONTRATO_FUERA_PERIODO", contrato.IdContrato.ToString(), solicitud.Periodo.Inicio, contrato.IdContrato));
            var normas = await contexto.VersionesNormativas.AsNoTracking().ToListAsync(ct);
            var evidenciaValida = new HashSet<long>();
            foreach (var norma in normas.Where(NormativaUtilizable))
            {
                var huella = await HuellaRevisionAsync(norma.IdVersionNormativa, ct);
                if (huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "CargarNormativa", ct)
                    && huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "ActivarNormativa", ct))
                    evidenciaValida.Add(norma.IdVersionNormativa);
            }
            var valores = await contexto.ValoresParametrosNormativos.AsNoTracking().Include(v => v.ParametroNormativo).ToListAsync(ct);
            var minimos = await contexto.SalariosMinimosVersion.AsNoTracking().Include(m => m.OcupacionSalarioMinimo).ToListAsync(ct);
            var jornadas = await contexto.JornadasLaborales.AsNoTracking().ToListAsync(ct);
            var riesgos = await contexto.ConfiguracionesRiesgoTrabajo.AsNoTracking().ToListAsync(ct);
            var conceptos = await contexto.ConceptosPlanilla.AsNoTracking().Include(c => c.Versiones).ToListAsync(ct);
            var internos = await contexto.ParametrosPlanilla.AsNoTracking().ToListAsync(ct);
            var tramos = await contexto.TramosImpuestoSalario.AsNoTracking().ToListAsync(ct);
            var creditos = await contexto.CreditosFiscalesNormativos.AsNoTracking().ToListAsync(ct);
            var colaboradores = contratos.Select(c => c.IdColaborador).ToArray();
            var asignaciones = await contexto.CreditosFiscalesColaborador.AsNoTracking()
                .Where(c => colaboradores.Contains(c.IdColaborador)).ToListAsync(ct);

            // Evaluar fronteras de configuración detecta huecos dentro del período; no genera segmentos salariales.
            var fechas = new SortedSet<DateTime> { solicitud.Periodo.Inicio, solicitud.Periodo.Fin.Value };
            void Frontera(DateTime inicio, DateTime? fin)
            {
                if (solicitud.Periodo.Contiene(inicio)) fechas.Add(inicio);
                if (fin is { } f && f < solicitud.Periodo.Fin.Value && f >= solicitud.Periodo.Inicio) fechas.Add(f.AddDays(1));
            }
            foreach (var v in normas) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in valores) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in minimos) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in jornadas) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in riesgos) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in conceptos.SelectMany(c => c.Versiones)) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in internos) Frontera(v.FechaVigenciaInicio ?? solicitud.Periodo.Inicio, v.FechaVigenciaFin);
            foreach (var v in creditos) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in asignaciones) Frontera(v.FechaVigenciaInicio, v.FechaVigenciaFin);
            foreach (var v in contratos) Frontera(v.FechaInicio, v.FechaFin);
            foreach (var fecha in fechas)
            {
                void Falta(string codigo, string clave, long? contrato = null) => faltantes.Add(new(codigo, clave, fecha, contrato));
                bool Norma(long id)
                {
                    var v = normas.SingleOrDefault(n => n.IdVersionNormativa == id);
                    if (!NormativaUtilizable(v) || !evidenciaValida.Contains(id) || !Vigencia(v!.FechaVigenciaInicio, v.FechaVigenciaFin).Contiene(fecha))
                    { Falta("NORMATIVA_INACTIVA_O_NO_REVISADA", id.ToString()); return false; }
                    return true;
                }
                T? Unico<T>(IEnumerable<T> candidatos, string tipo, string clave, long? contrato = null) where T : class
                {
                    var encontrados = candidatos.ToArray();
                    if (encontrados.Length == 1) return encontrados[0];
                    Falta(encontrados.Length == 0 ? tipo + "_FALTANTE" : tipo + "_AMBIGUO", clave, contrato);
                    return null;
                }
                bool EnFecha(DateTime inicio, DateTime? fin) => Vigencia(inicio, fin).Contiene(fecha);
                if (string.IsNullOrWhiteSpace(solicitud.ReferenciaAplicabilidadOrdinaria) || solicitud.ClaveAplicacion != "ORDINARIO")
                    Falta("APLICABILIDAD_EMPRESARIAL_FALTANTE", solicitud.ClaveAplicacion);
                if (solicitud.IdContratos.Count == 0) Falta("CONTRATOS_FALTANTES", "EMPRESA");
                foreach (var clave in ClavesOrdinarias.Concat(valores.Where(v => v.ParametroNormativo!.EsObligatorio && v.ClaveAplicacion == solicitud.ClaveAplicacion)
                    .Select(v => v.ParametroNormativo!.Codigo)).Distinct())
                {
                    var valor = Unico(valores.Where(v => v.ParametroNormativo!.Codigo == clave && v.ClaveAplicacion == solicitud.ClaveAplicacion
                        && EnFecha(v.FechaVigenciaInicio, v.FechaVigenciaFin)), "PARAMETRO_NORMATIVO", clave);
                    if (valor is null) continue;
                    if (valor.ParametroNormativo!.EstadoRegistro != Activo) Falta("PARAMETRO_NORMATIVO_INACTIVO", clave);
                    else if (Norma(valor.IdVersionNormativa)) datos.Add(new("Normativo", clave, fecha,
                        valor.IdValorParametroNormativo, valor.IdVersionNormativa, valor.ValorDecimal,
                        valor.ParametroNormativo.Unidad, valor.ParametroNormativo.Sujeto, valor.ParametroNormativo.CodigoBase));
                }
                foreach (var clave in solicitud.Conceptos.Append("SALARIO_ORDINARIO").Distinct())
                {
                    var concepto = conceptos.SingleOrDefault(c => c.Codigo == clave);
                    var version = Unico(concepto?.Versiones.Where(v => EnFecha(v.FechaVigenciaInicio, v.FechaVigenciaFin)) ?? [], "CONCEPTO_SIN_CLASIFICAR", clave);
                    if (version is null) continue;
                    if (concepto!.EstadoRegistro != Activo || version.EstadoRegistro != Activo) Falta("CONCEPTO_INACTIVO", clave);
                    else datos.Add(new("Concepto", clave, fecha, version.IdConceptoPlanillaVersion));
                }
                foreach (var clave in solicitud.ParametrosInternos ?? [])
                {
                    var version = Unico(internos.Where(p => p.Codigo == clave
                        && EnFecha(p.FechaVigenciaInicio ?? DateTime.MinValue, p.FechaVigenciaFin)), "PARAMETRO_INTERNO", clave);
                    if (version is null) continue;
                    if (version.EsNormativo || version.EsEditable || version.FechaVigenciaInicio is null
                        || version.IdConceptoPlanilla is null || version.EstadoRegistro != Activo)
                        Falta("PARAMETRO_INTERNO_NO_GOBERNADO", clave);
                    else
                    {
                        var concepto = conceptos.SingleOrDefault(c => c.IdConceptoPlanilla == version.IdConceptoPlanilla);
                        var clasificacion = Unico(concepto?.Versiones.Where(v => EnFecha(v.FechaVigenciaInicio, v.FechaVigenciaFin)) ?? [], "CONCEPTO_INTERNO", clave);
                        if (concepto?.EstadoRegistro != Activo || clasificacion?.EstadoRegistro != Activo) Falta("CONCEPTO_INTERNO_NO_DISPONIBLE", clave);
                        else datos.Add(new("Interno", clave, fecha, version.IdParametroPlanilla, Valor: version.ValorDecimal));
                    }
                }
                var renta = Unico(normas.Where(n => EnFecha(n.FechaVigenciaInicio, n.FechaVigenciaFin)
                    && tramos.Any(t => t.IdVersionNormativa == n.IdVersionNormativa)), "TRAMOS_RENTA", "SALARIOS");
                if (renta is not null && Norma(renta.IdVersionNormativa))
                {
                    var filas = tramos.Where(t => t.IdVersionNormativa == renta.IdVersionNormativa).ToArray();
                    try { ValidarTramos(filas.Select(t => new TramoRentaCarga(t.NumeroOrden, t.LimiteInferior, t.LimiteSuperior, t.Tasa)).ToArray()); }
                    catch (ArgumentException) { Falta("TRAMOS_RENTA_INVALIDOS", renta.Codigo); }
                    datos.AddRange(filas.Select(t => new DatoFinancieroResuelto("TramoRenta", t.NumeroOrden.ToString(), fecha, t.IdTramoImpuestoSalario, t.IdVersionNormativa)));
                }
                foreach (var clave in new[] { "HIJO", "CONYUGE" })
                {
                    var credito = Unico(creditos.Where(c => c.CodigoTipo == clave && EnFecha(c.FechaVigenciaInicio, c.FechaVigenciaFin)), "CREDITO_NORMATIVO", clave);
                    if (credito is null) continue;
                    if (credito.EstadoRegistro != Activo) Falta("CREDITO_NORMATIVO_INACTIVO", clave);
                    else if (Norma(credito.IdVersionNormativa)) datos.Add(new("CreditoNormativo", clave, fecha, credito.IdCreditoFiscalNormativo, credito.IdVersionNormativa, credito.MontoUnitario));
                }
                foreach (var id in solicitud.IdContratos.Distinct())
                {
                    var contrato = contratos.SingleOrDefault(c => c.IdContrato == id);
                    if (contrato is null) { Falta("CONTRATO_FALTANTE", id.ToString(), id); continue; }
                    if (!EnFecha(contrato.FechaInicio, contrato.FechaFin)) continue;
                    if (contrato.EstadoRegistro != Activo || contrato.Colaborador?.EstadoRegistro != Activo || contrato.Colaborador.Puesto?.EstadoRegistro != Activo)
                        Falta("RELACION_NO_DISPONIBLE", id.ToString(), id);
                    var jornada = Unico(jornadas.Where(j => j.Codigo == contrato.JornadaLaboral?.Codigo
                        && EnFecha(j.FechaVigenciaInicio, j.FechaVigenciaFin)), "JORNADA", id.ToString(), id);
                    if (jornada is not null)
                    {
                        if (jornada.EstadoRegistro != Activo || jornada.TipoJornada is not ("Diurna" or "Mixta" or "Nocturna")) Falta("JORNADA_NO_DISPONIBLE", jornada.Codigo, id);
                        else datos.Add(new("Jornada", jornada.Codigo, fecha, jornada.IdJornadaLaboral));
                    }
                    var ocupacion = contrato.Colaborador?.Puesto?.IdOcupacionSalarioMinimo;
                    if (ocupacion is null) Falta("OCUPACION_NO_HOMOLOGADA", id.ToString(), id);
                    else if (jornada is not null)
                    {
                        var minimo = Unico(minimos.Where(m => m.IdOcupacionSalarioMinimo == ocupacion
                            && m.PeriodicidadReferencia == solicitud.PeriodicidadMinimo
                            && (m.TipoJornadaAplicable is null || m.TipoJornadaAplicable == jornada.TipoJornada)
                            && EnFecha(m.FechaVigenciaInicio, m.FechaVigenciaFin)), "SALARIO_MINIMO", ocupacion.ToString()!, id);
                        if (minimo is not null)
                        {
                            if (minimo.OcupacionSalarioMinimo?.EstadoRegistro != Activo) Falta("OCUPACION_INACTIVA", ocupacion.ToString()!, id);
                            else if (Norma(minimo.IdVersionNormativa)) datos.Add(new("Minimo", ocupacion.ToString()!, fecha, minimo.IdSalarioMinimoVersion, minimo.IdVersionNormativa, minimo.Monto));
                        }
                    }
                    var rt = Unico(riesgos.Where(r => r.NumeroPoliza == contrato.ConfiguracionRiesgoTrabajo?.NumeroPoliza
                        && r.CodigoActividad == contrato.ConfiguracionRiesgoTrabajo?.CodigoActividad
                        && EnFecha(r.FechaVigenciaInicio, r.FechaVigenciaFin)), "RT", id.ToString(), id);
                    if (rt is not null)
                    {
                        if (rt.EstadoRegistro != Activo || string.IsNullOrWhiteSpace(rt.ReferenciaDocumento) || string.IsNullOrWhiteSpace(rt.CreadoPor))
                            Falta("RT_SIN_EVIDENCIA", rt.NumeroPoliza, id);
                        else if (rt.IdVersionNormativa is null || Norma(rt.IdVersionNormativa.Value))
                            datos.Add(new("RT", rt.CodigoActividad, fecha, rt.IdConfiguracionRiesgoTrabajo, rt.IdVersionNormativa, rt.Tarifa));
                    }
                    var personales = asignaciones.Where(a => a.IdColaborador == contrato.IdColaborador && EnFecha(a.FechaVigenciaInicio, a.FechaVigenciaFin)).ToArray();
                    foreach (var grupo in personales.GroupBy(a => creditos.SingleOrDefault(c => c.IdCreditoFiscalNormativo == a.IdCreditoFiscalNormativo)?.CodigoTipo))
                    {
                        var personal = Unico(grupo, "CREDITO_PERSONAL", grupo.Key ?? "HUERFANO", id);
                        if (personal is null) continue;
                        var norma = creditos.SingleOrDefault(c => c.IdCreditoFiscalNormativo == personal.IdCreditoFiscalNormativo);
                        if (personal.EstadoRegistro != Activo || string.IsNullOrWhiteSpace(personal.ReferenciaSoporte)
                            || string.IsNullOrWhiteSpace(personal.AprobadoPor) || norma?.EstadoRegistro != Activo
                            || !EnFecha(norma.FechaVigenciaInicio, norma.FechaVigenciaFin)) Falta("CREDITO_PERSONAL_NO_ACREDITADO", grupo.Key ?? "HUERFANO", id);
                        else if (Norma(norma.IdVersionNormativa)) datos.Add(new("CreditoPersonal", grupo.Key!, fecha, personal.IdCreditoFiscalColaborador, norma.IdVersionNormativa, personal.CantidadAcreditada));
                    }
                }
            }
            return new ConfiguracionFinancieraResuelta(faltantes.Distinct().ToArray(), datos.Distinct().ToArray());
        }, ct);
}

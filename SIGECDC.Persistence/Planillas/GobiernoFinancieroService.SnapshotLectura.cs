using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    private async Task<IReadOnlyDictionary<long, LineaMonetaria>> LineasSnapshotAsync(EjecucionPlanilla e, CancellationToken ct)
    {
        var filas = e.Detalles.SelectMany(d => d.Conceptos).ToArray();
        // Solo versiones exactas referenciadas. Ninguna resolución por fecha ni proyección laboral actual.
        var conceptos = await FiltrarClaves(contexto.ConceptosPlanillaVersion.AsNoTracking().Include(v => v.ConceptoPlanilla),
            v => v.IdConceptoPlanillaVersion, filas.Select(l => l.IdConceptoPlanillaVersion)).ToDictionaryAsync(v => v.IdConceptoPlanillaVersion, ct);
        var confirmaciones = await FiltrarClaves(contexto.ConfirmacionesIncidencias.AsNoTracking(), c => c.IdIncidenciaPlanillaConfirmacion,
            filas.Select(l => l.IdIncidenciaPlanillaConfirmacion).OfType<long>()).ToDictionaryAsync(c => c.IdIncidenciaPlanillaConfirmacion, ct);
        var parametros = await FiltrarClaves(contexto.ParametrosPlanilla.AsNoTracking(), p => p.IdParametroPlanilla,
            filas.Select(l => l.IdParametroPlanilla).OfType<int>()).ToDictionaryAsync(p => p.IdParametroPlanilla, ct);
        var resultado = new Dictionary<long, LineaMonetaria>();
        foreach (var d in e.Detalles)
        foreach (var l in d.Conceptos.OrderBy(l => l.NumeroOrden))
        {
            var c = conceptos[l.IdConceptoPlanillaVersion];
            if (c.EsSalarial != l.EsSalarialSnapshot || c.EsContributivoCCSS != l.EsContributivoCCSSSnapshot
                || c.EsTributario != l.EsTributarioSnapshot || c.EsPatronal != l.EsPatronalSnapshot)
                throw new ConfiguracionConceptoException("SNAPSHOT_CONCEPTO_ALTERADO");
            var concepto = new ConceptoMonetarioVersion(c.IdConceptoPlanilla, c.IdConceptoPlanillaVersion, c.ConceptoPlanilla!.Codigo,
                l.DescripcionSnapshot, new(c.FechaVigenciaInicio, c.FechaVigenciaFin), c.Clasificacion, l.EsSalarialSnapshot,
                l.EsContributivoCCSSSnapshot, l.EsTributarioSnapshot, l.EsPatronalSnapshot, c.TipoBase, c.UnidadPermitida,
                c.PermiteMontoManual, c.RequiereAutorizacion, true, true);
            var segmento = l.IdSegmentoCalculoPlanilla is { } sid ? d.Segmentos.Single(s => s.IdSegmentoCalculoPlanilla == sid) : null;
            var referencia = segmento is null ? null : new ReferenciaSegmentoTemporal(segmento.IdContratoOrigen!.Value,
                segmento.IdCondicionLaboralVersion!.Value, segmento.FechaInicio, segmento.FechaFin);
            if (l.IdCondicionLaboralBase is { } condicionBase)
            {
                var originales = d.Segmentos.Where(s => s.IdCondicionLaboralVersion == condicionBase
                    && s.FechaInicio >= l.FechaBaseInicio && s.FechaFin <= l.FechaBaseFin).OrderBy(s => s.FechaInicio).ToArray();
                if (originales.Length == 0 || originales[0].FechaInicio != l.FechaBaseInicio || originales[^1].FechaFin != l.FechaBaseFin
                    || originales.Select(s => s.IdContratoOrigen).Distinct().Count() != 1
                    || originales.Zip(originales.Skip(1)).Any(p => p.First.FechaFin.AddDays(1) != p.Second.FechaInicio))
                    throw new ConfiguracionConceptoException("BASE_HECHO_SNAPSHOT_NO_DEMOSTRABLE");
                referencia = new(originales[0].IdContratoOrigen!.Value, condicionBase, l.FechaBaseInicio!.Value, l.FechaBaseFin!.Value);
            }
            var fuente = new FuenteLineaMonetaria(d.IdColaborador, l.ClaveHecho!, l.TipoFuente, l.IdFuente, l.FechaHecho!.Value,
                referencia,
                l.ReferenciaSoporte!, l.IdIncidenciaPlanillaConfirmacion,
                l.IdIncidenciaPlanillaConfirmacion is { } cid ? confirmaciones[cid].NumeroVersion : null);
            ParametroInternoResuelto? parametro = null;
            if (l.IdParametroPlanilla is { } pid)
            {
                var p = parametros[pid]; var ambito = Enum.Parse<AmbitoParametroInterno>(l.AmbitoParametro!);
                parametro = new(new(p.IdParametroPlanilla, p.Codigo, p.IdConceptoPlanilla!.Value, p.TipoParametro, p.Naturaleza!,
                    p.ValorDecimal, p.ValorTexto, new(p.FechaVigenciaInicio!.Value, p.FechaVigenciaFin), true, true, p.EsNormativo),
                    ambito, l.IdParametroPlanillaColaborador ?? l.IdParametroPlanillaPeriodo, l.ValorParametroAplicado, null);
            }
            var usaSalario = l.TipoFuente == "Contrato" || c.TipoBase is "HORA_ORDINARIA" or "REDUCCION_DEVENGO" || parametro is not null && c.TipoBase == "SALARIO_DEVENGADO";
            var jornada = l.TipoFuente == "Incidencia" && c.TipoBase is "HORA_ORDINARIA" or "REDUCCION_DEVENGO" && segmento is not null
                ? new JornadaTemporal(segmento.IdJornadaLaboral!.Value, segmento.CodigoJornada, segmento.TipoJornada!, segmento.HorasOrdinariasDia,
                    segmento.HorasOrdinariasSemana!.Value, new(segmento.FechaJornadaInicio!.Value, segmento.FechaJornadaFin), true) : null;
            var factores = new FactoresLineaMonetaria(l.Cantidad, l.Unidad!, l.BaseCalculo, l.Tasa, l.Multiplicador, l.MontoSinRedondear,
                usaSalario ? segmento?.SalarioMensual : null, l.UnidadCantidad, jornada);
            resultado.Add(l.IdConceptoDetallePlanilla, LineaMonetaria.DesdeSnapshot(concepto, fuente, factores, l.Monto, l.ReferenciaAutorizacion, parametro));
        }
        return resultado;
    }

    private async Task ConciliarSnapshotAsync(EjecucionPlanilla e, CancellationToken ct)
    {
        static void Exigir(bool condicion) { if (!condicion) throw new ConfiguracionConceptoException("SNAPSHOT_NO_CONCILIA"); }
        Exigir(e.SnapshotCompleto && e.EstadoReproducibilidad == "REPRODUCIBLE" && e.VersionMotor == VersionMotorSnapshot && e.Detalles.Count > 0);
        var lineas = await LineasSnapshotAsync(e, ct);
        foreach (var d in e.Detalles)
        {
            Exigir(d.SnapshotCompleto && d.IdPlanilla == e.IdPlanilla && d.IdEjecucionPlanilla == e.IdEjecucionPlanilla
                && !string.IsNullOrWhiteSpace(d.NombreCompleto) && !string.IsNullOrWhiteSpace(d.CodigoColaborador) && d.Segmentos.Count > 0);
            var ls = d.Conceptos.Select(l => lineas[l.IdConceptoDetallePlanilla]).ToArray();
            var t = AgregadorLineasMonetarias.Conciliar(ls).Totales;
            Exigir(CatalogoCuotasSociales.Componentes.Where(c => c.Sujeto != "Estado").Select(c => c.Codigo).ToHashSet(StringComparer.Ordinal)
                .SetEquals(ls.Where(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.Cuotas).Select(l => l.Concepto.Codigo)));
            Exigir(ls.Count(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.Renta) == 1
                && ls.Any(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.RiesgosTrabajo));
            Exigir(t.BrutoSalarial == d.SalarioBruto && t.IngresosNoSalariales == d.IngresosNoSalariales && t.Deducciones == d.TotalDeducciones
                && t.SaldoAlgebraico == d.SalarioNeto && t.CargasPatronales == d.CargasPatronales && t.CostoAlgebraico == d.CostoLaboralTotal
                && t.ReduccionesDevengo == d.TotalAusencias && d.CostoPatronalEstimado == d.CargasPatronales
                && d.CuotasObreras + d.ImpuestoSalario + d.OtrasDeducciones == d.TotalDeducciones);
            Exigir(ls.Where(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.Cuotas && l.Efecto == EfectoMonetario.DeduccionLegal).Sum(l => l.Monto) == d.CuotasObreras);
            Exigir(ls.Where(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.Renta).Sum(l => l.Monto) == d.ImpuestoSalario);
            Exigir(ls.Where(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.RiesgosTrabajo).Sum(l => l.Monto) == d.RiesgosTrabajo);
            Exigir(t.CargasPatronales == ls.Where(l => BasesConceptos.Familia(l.Concepto.TipoBase) == FamiliaBaseConcepto.Cuotas && l.Efecto == EfectoMonetario.CargaPatronal).Sum(l => l.Monto) + d.RiesgosTrabajo + d.OtrosCostosPatronales);
            var segmentos = d.Segmentos.OrderBy(s => s.FechaInicio).ToArray();
            Exigir(segmentos.Sum(s => s.MontoOrdinario) == d.SalarioProporcional);
            for (var i = 0; i < segmentos.Length; i++)
            {
                var s = segmentos[i];
                Exigir(s.IdCondicionLaboralVersion > 0 && s.IdJornadaLaboral > 0 && s.IdSalarioMinimoVersion > 0
                    && s.FechaInicio >= e.FechaInicioSnapshot && s.FechaFin <= e.FechaFinSnapshot && s.FechaInicio <= s.FechaFin
                    && (i == 0 || segmentos[i - 1].FechaFin < s.FechaInicio));
                var ordinario = ls.Single(l => l.Fuente.TipoFuente == "Contrato" && l.Fuente.Segmento?.Inicio == s.FechaInicio);
                Exigir(ordinario.Factores.Cantidad == s.DiasBase30 && ordinario.Factores.MontoSinRedondear == s.MontoOrdinarioSinRedondear && ordinario.Monto == s.MontoOrdinario);
            }
            foreach (var l in d.Conceptos)
            {
                Exigir(!string.IsNullOrWhiteSpace(l.ClaveHecho) && !string.IsNullOrWhiteSpace(l.ReferenciaSoporte));
                if (l.EsPatronalSnapshot && l.TipoFuente == "Derivado" && l.IdParametroPlanilla is null
                    && BasesConceptos.Familia(lineas[l.IdConceptoDetallePlanilla].Concepto.TipoBase) is FamiliaBaseConcepto.MontoAutorizado or FamiliaBaseConcepto.SalarioOrdinario)
                    Exigir(!string.IsNullOrWhiteSpace(l.RegistradoPorHecho)
                        && (!lineas[l.IdConceptoDetallePlanilla].Concepto.RequiereAutorizacion || !string.IsNullOrWhiteSpace(l.AutorizadoPorHecho) && !string.IsNullOrWhiteSpace(l.ReferenciaAutorizacion)));
                if (l.TipoFuente == "Incidencia") Exigir(l.IdIncidenciaPlanillaConfirmacion > 0);
                var familia = BasesConceptos.Familia(lineas[l.IdConceptoDetallePlanilla].Concepto.TipoBase);
                if (familia is FamiliaBaseConcepto.Cuotas or FamiliaBaseConcepto.Renta or FamiliaBaseConcepto.RiesgosTrabajo)
                    Exigir(l.AplicacionesNormativas.Count > 0);
                foreach (var a in l.AplicacionesNormativas)
                {
                    Exigir(a.MontoAplicadoSinRedondear is not null && a.FechaAplicacionInicio is not null && a.FechaAplicacionFin >= a.FechaAplicacionInicio);
                    if (a.IdTramoImpuestoSalario is not null) Exigir(a.MontoAplicado is null && a.MontoAplicadoSinRedondear == a.BaseAplicada * a.ValorAplicado);
                    if (a.IdCreditoFiscalNormativo is not null) Exigir(a.IdCreditoFiscalColaborador > 0 && a.CantidadAplicada > 0
                        && a.MontoAplicado is null && a.MontoAplicadoSinRedondear == a.CantidadAplicada * a.ValorAplicado);
                    if (a.IdConfiguracionRiesgoTrabajo is not null) Exigir(a.MontoAplicado == l.Monto && a.MontoAplicadoSinRedondear == l.MontoSinRedondear && a.ModalidadRT == "RemuneracionSalarialOrdinaria");
                    if (a.AlcanceAplicacion == "AcumuladoPrevio") Exigir(a.IdDetallePlanillaPrevio > 0);
                }
                decimal Normativa(string alcance) => l.AplicacionesNormativas.Where(a => a.AlcanceAplicacion == alcance && a.IdValorParametroNormativo is not null).Sum(a => a.MontoAplicado ?? 0);
                if (familia == FamiliaBaseConcepto.Cuotas) Exigir(l.Monto == Normativa("CorteActual") + Normativa("MensualDefinitiva") - Normativa("AcumuladoPrevio"));
                if (familia == FamiliaBaseConcepto.Renta)
                {
                    foreach (var alcance in l.AplicacionesNormativas.GroupBy(a => a.AlcanceAplicacion))
                    {
                        var ids = alcance.Where(a => a.IdTramoImpuestoSalario != null).Select(a => a.IdTramoImpuestoSalario!.Value).ToArray();
                        var tramos = await FiltrarClaves(contexto.TramosImpuestoSalario.AsNoTracking(), t => t.IdTramoImpuestoSalario, ids).ToArrayAsync(ct);
                        var versiones = tramos.Select(t => t.IdVersionNormativa).Distinct().ToArray();
                        Exigir(versiones.Length == 1 && tramos.Length == ids.Length);
                        var idVersion = versiones.Single();
                        Exigir(await contexto.TramosImpuestoSalario.CountAsync(t => t.IdVersionNormativa == idVersion, ct) == ids.Length);
                        foreach (var a in alcance.Where(a => a.IdTramoImpuestoSalario != null))
                            Exigir(tramos.Single(t => t.IdTramoImpuestoSalario == a.IdTramoImpuestoSalario).Tasa == a.ValorAplicado);
                    }
                    decimal Renta(string alcance) => PrecisionMonetaria.R2(Math.Max(0, l.AplicacionesNormativas.Where(a => a.AlcanceAplicacion == alcance)
                        .Sum(a => (a.IdCreditoFiscalNormativo is null ? 1 : -1) * (a.MontoAplicadoSinRedondear ?? 0))));
                    Exigir(l.Monto == Math.Max(0, Renta("CorteActual") + Renta("MensualDefinitiva") - Renta("AcumuladoPrevio")));
                }
            }
        }
        Exigir(e.SalarioBrutoTotal == e.Detalles.Sum(d => d.SalarioBruto) && e.IngresosNoSalarialesTotal == e.Detalles.Sum(d => d.IngresosNoSalariales)
            && e.DeduccionesTotal == e.Detalles.Sum(d => d.TotalDeducciones) && e.SalarioNetoTotal == e.Detalles.Sum(d => d.SalarioNeto)
            && e.CargasPatronalesTotal == e.Detalles.Sum(d => d.CargasPatronales) && e.RiesgosTrabajoTotal == e.Detalles.Sum(d => d.RiesgosTrabajo)
            && e.OtrosCostosPatronalesTotal == e.Detalles.Sum(d => d.OtrosCostosPatronales) && e.CostoLaboralTotal == e.Detalles.Sum(d => d.CostoLaboralTotal));
    }
}

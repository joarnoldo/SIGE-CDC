using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    private async Task<AcumuladoMensualRenta?> AcumuladoSnapshotAsync(long? id, PeriodoSalarial corte, CancellationToken ct)
    {
        var q2 = corte.Tipo == TiposPeriodoPlanilla.Quincenal && corte.Inicio.Day == 16;
        if (!q2)
        {
            if (id is not null) throw new ConfiguracionConceptoException("ACUMULADO_FUERA_CORTE");
            return null;
        }
        if (id is null) throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_RENTA_NO_DEMOSTRABLE");
        var e = await CargarSnapshotAsync(id.Value, ct);
        var mes = new DateTime(corte.Inicio.Year, corte.Inicio.Month, 1);
        if (!e.SnapshotCompleto || e.EstadoReproducibilidad != "REPRODUCIBLE" || e.EstadoEjecucion is not ("Calculada" or "Autorizada")
            || e.FechaInicioSnapshot != mes || e.FechaFinSnapshot != corte.Inicio.AddDays(-1) || e.TipoPeriodoSnapshot != TiposPeriodoPlanilla.Quincenal
            || !await contexto.Planillas.AnyAsync(p => p.IdPlanilla == e.IdPlanilla && p.IdEjecucionPlanillaVigente == id, ct))
            throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_RENTA_NO_DEMOSTRABLE");
        await ConciliarSnapshotAsync(e, ct);
        var restauradas = await LineasSnapshotAsync(e, ct);
        var lineas = AgregadorLineasMonetarias.Conciliar(restauradas.Values).Lineas;
        var previasEF = lineas.Where(l => BasesConceptos.Familia(l.Concepto.TipoBase) is not (FamiliaBaseConcepto.Cuotas or FamiliaBaseConcepto.Renta or FamiliaBaseConcepto.RiesgosTrabajo)
            && l.Efecto != EfectoMonetario.CargaPatronal).ToArray();
        var valores = await LeerValoresCuotasAsync(new(mes, corte.Fin, TiposPeriodoPlanilla.Mensual), ct);
        var fiscales = await LeerVersionesFiscalesAsync(mes, corte.Fin, ct);
        var cuotas = new List<AplicacionCuota>(); var retenciones = new List<AplicacionRenta>();
        static decimal Signo(LineaMonetaria l) => l.Efecto == EfectoMonetario.ReduccionDevengo ? -l.Monto : l.Monto;
        foreach (var d in e.Detalles.OrderBy(d => d.IdColaborador))
        foreach (var l in d.Conceptos.OrderBy(l => l.NumeroOrden))
        {
            var linea = restauradas[l.IdConceptoDetallePlanilla]; var apps = l.AplicacionesNormativas.ToArray();
            if (BasesConceptos.Familia(linea.Concepto.TipoBase) == FamiliaBaseConcepto.Cuotas)
            {
                var tasa = apps.Single(a => a.MontoAplicado is not null && a.IdValorParametroNormativo is not null);
                var valor = valores.Single(v => v.IdValorParametroNormativo == tasa.IdValorParametroNormativo);
                var minimo = apps.SingleOrDefault(a => a.MontoAplicado is null && a.IdValorParametroNormativo is not null);
                var rango = new VigenciaFinanciera(tasa.FechaAplicacionInicio!.Value, tasa.FechaAplicacionFin);
                var aportes = previasEF.Where(l => l.Fuente.IdColaborador == d.IdColaborador && l.Concepto.EsContributivoCCSS == true)
                    .Where(l => rango.Contiene(l.Fuente.TipoFuente == "Contrato"
                        ? new VigenciaFinanciera(l.Fuente.Segmento!.Inicio, l.Fuente.Segmento.Fin) : new(l.Fuente.FechaHecho, l.Fuente.FechaHecho)))
                    .Select(l => new AporteBaseCuota(l.Fuente, l.Concepto.IdVersion, Signo(l))).ToArray();
                if (valor.Valor != tasa.ValorAplicado || tasa.MontoAplicado != linea.Monto || aportes.Sum(a => a.MontoConSigno) != tasa.BaseContributiva)
                    throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_CCSS_INCONSISTENTE");
                cuotas.Add(new(linea, valor, minimo is null ? null : valores.Single(v => v.IdValorParametroNormativo == minimo.IdValorParametroNormativo),
                    rango, tasa.BaseContributiva!.Value, tasa.BaseAplicada!.Value,
                    new(d.IdColaborador, valor.Codigo, valor.ClaveAplicacion, new(tasa.FechaAplicabilidadInicio!.Value, tasa.FechaAplicabilidadFin),
                        Enum.Parse<ReglaBaseCuota>(tasa.ReglaAplicabilidad!), tasa.ReferenciaSoporte!, tasa.ReferenciaRegla!, tasa.AcreditadoPor!), aportes));
            }
            if (BasesConceptos.Familia(linea.Concepto.TipoBase) == FamiliaBaseConcepto.Renta)
            {
                var fiscalesUsadas = fiscales.Where(v => apps.Any(a => v.Tramos.Any(t => t.IdTramo == a.IdTramoImpuestoSalario))).ToArray();
                if (fiscalesUsadas.Length != 1) throw new ConfiguracionConceptoException("ACUMULADO_RENTA_VERSION_INCOMPATIBLE");
                var fiscal = fiscalesUsadas[0];
                var tramos = apps.Where(a => a.IdTramoImpuestoSalario is not null).Select(a => new AplicacionTramoRenta(
                    fiscal.Tramos.Single(t => t.IdTramo == a.IdTramoImpuestoSalario), a.BaseAplicada!.Value, a.MontoAplicadoSinRedondear!.Value)).OrderBy(t => t.Tramo.Orden).ToArray();
                var creditos = new List<AplicacionCreditoRenta>();
                foreach (var a in apps.Where(a => a.IdCreditoFiscalNormativo is not null))
                {
                    var p = await contexto.CreditosFiscalesColaborador.AsNoTracking().SingleAsync(p => p.IdCreditoFiscalColaborador == a.IdCreditoFiscalColaborador, ct);
                    if (p.IdColaborador != d.IdColaborador || p.IdCreditoFiscalNormativo != a.IdCreditoFiscalNormativo || p.CantidadAcreditada != a.CantidadAplicada)
                        throw new ConfiguracionConceptoException("ACUMULADO_RENTA_CREDITO_INCOMPATIBLE");
                    creditos.Add(new(fiscal.Creditos.Single(c => c.IdCreditoNormativo == a.IdCreditoFiscalNormativo),
                        new(p.IdCreditoFiscalColaborador, p.IdColaborador, p.IdCreditoFiscalNormativo, p.CantidadAcreditada,
                            new(p.FechaVigenciaInicio, p.FechaVigenciaFin), p.ReferenciaSoporte, p.AprobadoPor, p.FechaAprobacion,
                            p.EstadoRegistro == Activo, await AcreditacionRentaDemostradaAsync(p, ct)), a.MontoAplicadoSinRedondear!.Value));
                }
                var aportes = lineas.Where(l => l.Fuente.IdColaborador == d.IdColaborador && l.Concepto.EsTributario == true)
                    .Select(l => new AporteBaseRenta(l.Fuente, l.Concepto, Signo(l))).ToArray();
                var evaluacion = new EvaluacionRenta(d.IdColaborador, new(mes, e.FechaFinSnapshot), fiscal, linea.Factores.BaseSinRedondear!.Value,
                    aportes, tramos, creditos.OrderBy(c => c.Normativo.CodigoTipo, StringComparer.Ordinal).ToArray(), linea);
                retenciones.Add(new(linea, evaluacion, null));
            }
        }
        var personas = e.Detalles.Select(d => d.IdColaborador).Order().ToArray();
        var soporte = FormattableString.Invariant($"EjecucionPlanilla:{e.IdEjecucionPlanilla}:V{e.NumeroVersion}");
        var g = new AcumuladoMensualCuotas(mes, e.FechaFinSnapshot, previasEF, cuotas, soporte, personas);
        return new(mes, e.FechaFinSnapshot, personas, previasEF.Concat(cuotas.Select(a => a.Linea)).ToArray(), retenciones, soporte, g);
    }
}

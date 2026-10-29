using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    private static bool MismaLineaSnapshot(LineaMonetaria a, LineaMonetaria b)
        => a.Fuente.IdColaborador == b.Fuente.IdColaborador && a.Fuente.TipoFuente == b.Fuente.TipoFuente && a.Fuente.ClaveHecho == b.Fuente.ClaveHecho;

    private static void AgregarAplicacionesSnapshot(ConceptoDetallePlanilla fila, LineaMonetaria linea, ResultadoCostoLaboral r, long? previo)
    {
        AplicacionNormativaDetallePlanilla Nueva(string alcance, VigenciaFinanciera rango)
        {
            var a = new AplicacionNormativaDetallePlanilla { AlcanceAplicacion = alcance, FechaAplicacionInicio = rango.Inicio,
                FechaAplicacionFin = rango.Fin, IdDetallePlanillaPrevio = alcance == "AcumuladoPrevio" ? previo
                    ?? throw new ConfiguracionConceptoException("ACUMULADO_SNAPSHOT_FALTANTE") : null };
            fila.AplicacionesNormativas.Add(a); return a;
        }
        void Cuota(AplicacionCuota c, string alcance)
        {
            var a = Nueva(alcance, c.Rango); a.IdValorParametroNormativo = c.Tasa.IdValorParametroNormativo;
            a.BaseAplicada = c.BaseAplicada; a.BaseContributiva = c.BaseContributiva; a.ValorAplicado = c.Tasa.Valor;
            a.MontoAplicadoSinRedondear = c.Linea.Factores.MontoSinRedondear; a.MontoAplicado = c.Linea.Monto;
            a.ReglaAplicabilidad = c.Aplicabilidad.Regla.ToString(); a.FechaAplicabilidadInicio = c.Aplicabilidad.Vigencia.Inicio;
            a.FechaAplicabilidadFin = c.Aplicabilidad.Vigencia.Fin; a.ReferenciaSoporte = c.Aplicabilidad.ReferenciaSoporte;
            a.ReferenciaRegla = c.Aplicabilidad.ReferenciaRegla; a.AcreditadoPor = c.Aplicabilidad.ConfirmadoPor;
            if (c.Minimo is { } minimo)
            {
                var b = Nueva(alcance, c.Rango); b.IdValorParametroNormativo = minimo.IdValorParametroNormativo;
                b.BaseAplicada = c.BaseAplicada; b.BaseContributiva = c.BaseContributiva; b.ValorAplicado = minimo.Valor;
                // La BMC es una base, no otra cuota materializada.
                b.MontoAplicadoSinRedondear = 0; b.MontoAplicado = null;
                b.ReglaAplicabilidad = a.ReglaAplicabilidad; b.FechaAplicabilidadInicio = a.FechaAplicabilidadInicio;
                b.FechaAplicabilidadFin = a.FechaAplicabilidadFin; b.ReferenciaSoporte = a.ReferenciaSoporte;
                b.ReferenciaRegla = a.ReferenciaRegla; b.AcreditadoPor = a.AcreditadoPor;
            }
        }
        void Renta(EvaluacionRenta evaluacion, string alcance)
        {
            foreach (var tramo in evaluacion.Tramos)
            {
                var a = Nueva(alcance, evaluacion.RangoAcumulado); a.IdTramoImpuestoSalario = tramo.Tramo.IdTramo;
                a.BaseAplicada = tramo.BaseGravada; a.ValorAplicado = tramo.Tramo.Tasa;
                a.MontoAplicadoSinRedondear = tramo.ImpuestoSinRedondear; a.MontoAplicado = null;
            }
            foreach (var credito in evaluacion.Creditos)
            {
                var a = Nueva(alcance, evaluacion.RangoAcumulado); a.IdCreditoFiscalNormativo = credito.Normativo.IdCreditoNormativo;
                a.IdCreditoFiscalColaborador = credito.Personal.IdCreditoColaborador; a.CantidadAplicada = credito.Personal.Cantidad;
                a.ValorAplicado = credito.Normativo.MontoUnitario; a.MontoAplicadoSinRedondear = credito.Monto; a.MontoAplicado = null;
            }
        }
        var g = r.Renta!.CuotasSociales!;
        var regularizacion = g.Regularizaciones.SingleOrDefault(a => MismaLineaSnapshot(a.Linea, linea));
        if (regularizacion is not null)
        {
            foreach (var c in regularizacion.ObligacionMensual) Cuota(c, "MensualDefinitiva");
            foreach (var c in regularizacion.CuotasPrevias) Cuota(c, "AcumuladoPrevio");
        }
        else foreach (var c in g.Aplicaciones.Where(a => MismaLineaSnapshot(a.Linea, linea))) Cuota(c, "CorteActual");
        foreach (var h in r.Renta.Aplicaciones.Where(a => MismaLineaSnapshot(a.Linea, linea)))
        {
            Renta(h.Evaluacion, h.EvaluacionPrevia is null ? "CorteActual" : "MensualDefinitiva");
            if (h.EvaluacionPrevia is { } p) Renta(p, "AcumuladoPrevio");
        }
        foreach (var rt in r.AplicacionesRT.Where(a => MismaLineaSnapshot(a.Linea, linea)))
        {
            var a = Nueva("CorteActual", rt.Rango); a.IdConfiguracionRiesgoTrabajo = rt.Configuracion.IdConfiguracion;
            a.BaseAplicada = rt.BaseAplicada; a.ValorAplicado = rt.Configuracion.Tarifa;
            a.MontoAplicadoSinRedondear = rt.Linea.Factores.MontoSinRedondear; a.MontoAplicado = rt.Linea.Monto;
            a.ModalidadRT = rt.Aplicabilidad.Modalidad.ToString(); a.IdContratoRT = rt.Asignacion.IdContrato;
            a.IdConfiguracionRTAncla = rt.Asignacion.IdConfiguracionAncla; a.FechaAplicabilidadInicio = rt.Aplicabilidad.Vigencia.Inicio;
            a.FechaAplicabilidadFin = rt.Aplicabilidad.Vigencia.Fin; a.ReferenciaSoporte = rt.Aplicabilidad.ReferenciaSoporte;
            a.AcreditadoPor = rt.Aplicabilidad.ConfirmadoPor; a.ReferenciaAsignacionRT = rt.Asignacion.ReferenciaSoporte;
            a.AsociadoPorRT = rt.Asignacion.AsociadoPor;
        }
    }
}

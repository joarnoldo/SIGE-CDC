namespace SIGECDC.Domain.Forecast;

// Evidencia proyectada propia de Forecast; no crea relaciones RRHH ni snapshots de Planilla.
public sealed class ForecastAplicacionFinanciera
{
    public long IdForecastAplicacionFinanciera { get; set; }
    public long IdForecastDetalle { get; set; }
    public int NumeroOrden { get; set; }
    public long? IdForecastSegmentoProyectado { get; set; }
    public long? IdForecastParametro { get; set; }
    public long? IdForecastObservacionHistorica { get; set; }
    public string FuenteEfectiva { get; set; } = string.Empty;
    public string AlcanceAplicacion { get; set; } = string.Empty;
    public string Componente { get; set; } = string.Empty;
    public string? Sujeto { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public long? IdForecastDetallePrevio { get; set; }
    public long? IdValorParametroNormativo { get; set; }
    public long? IdTramoImpuestoSalario { get; set; }
    public long? IdCreditoFiscalNormativo { get; set; }
    public long? IdCreditoFiscalColaborador { get; set; }
    public long? IdConfiguracionRiesgoTrabajo { get; set; }
    public string? Unidad { get; set; }
    public string? TipoBase { get; set; }
    public decimal? BaseAplicada { get; set; }
    public decimal? ValorAplicado { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? Multiplicador { get; set; }
    public decimal MontoSinRedondear { get; set; }
    public decimal? MontoMaterializado { get; set; }
    public string? ReglaAplicabilidad { get; set; }
    public DateTime? FechaAplicabilidadInicio { get; set; }
    public DateTime? FechaAplicabilidadFin { get; set; }
    public string? ReferenciaSoporte { get; set; }
    public string? ReferenciaRegla { get; set; }
    public string? AcreditadoPor { get; set; }
    public string? ModalidadRT { get; set; }
    public string? NumeroPoliza { get; set; }
    public string? CodigoActividad { get; set; }
    public decimal? DescuentoRT { get; set; }
    public decimal? RecargoRT { get; set; }
    public bool EsSalarial { get; set; }
    public bool EsContributivoCCSS { get; set; }
    public bool EsTributario { get; set; }
    public bool EsPatronal { get; set; }
    public string Efecto { get; set; } = string.Empty;
    public string DescripcionConcepto { get; set; } = string.Empty;
}


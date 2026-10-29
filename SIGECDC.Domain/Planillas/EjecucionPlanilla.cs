namespace SIGECDC.Domain.Planillas;

public sealed class EjecucionPlanilla
{
    public long IdEjecucionPlanilla { get; set; }
    public long IdPlanilla { get; set; }
    public int NumeroVersion { get; set; }
    public string EstadoEjecucion { get; set; } = "Calculada";
    public string EstadoReproducibilidad { get; set; } = "REPRODUCIBLE";
    public bool SnapshotCompleto { get; set; }
    public string Moneda { get; set; } = "CRC";
    public string? VersionMotor { get; set; }
    public DateTime FechaInicioSnapshot { get; set; }
    public DateTime FechaFinSnapshot { get; set; }
    public string TipoPeriodoSnapshot { get; set; } = string.Empty;
    public DateTime FechaCalculo { get; set; }
    public string? CalculadoPor { get; set; }
    public decimal SalarioBrutoTotal { get; set; }
    public decimal IngresosNoSalarialesTotal { get; set; }
    public decimal DeduccionesTotal { get; set; }
    public decimal SalarioNetoTotal { get; set; }
    public decimal CargasPatronalesTotal { get; set; }
    public decimal RiesgosTrabajoTotal { get; set; }
    public decimal OtrosCostosPatronalesTotal { get; set; }
    public decimal CostoLaboralTotal { get; set; }
    public string? MotivoInvalidacion { get; set; }
    public DateTime? FechaInvalidacion { get; set; }
    public string? InvalidadoPor { get; set; }

    public Planilla? Planilla { get; set; }
    public ICollection<DetallePlanilla> Detalles { get; set; } = [];
}

public sealed class SegmentoCalculoPlanilla
{
    public long IdSegmentoCalculoPlanilla { get; set; }
    public long IdDetallePlanilla { get; set; }
    public int NumeroOrden { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public long? IdContratoOrigen { get; set; }
    // Evidencia J: nullable exclusivamente para compatibilidad con snapshots legados.
    public long? IdCondicionLaboralVersion { get; set; }
    public long? IdJornadaLaboral { get; set; }
    public long? IdSalarioMinimoVersion { get; set; }
    public string? NombrePuesto { get; set; }
    public string? NombreDepartamento { get; set; }
    public string? TipoContrato { get; set; }
    public string? PeriodicidadPago { get; set; }
    public string? CodigoOcupacion { get; set; }
    public string? NombreOcupacion { get; set; }
    public string? TipoJornada { get; set; }
    public decimal? HorasOrdinariasSemana { get; set; }
    public DateTime? FechaJornadaInicio { get; set; }
    public DateTime? FechaJornadaFin { get; set; }
    public decimal? MontoMinimo { get; set; }
    public string? PeriodicidadMinimo { get; set; }
    public string? TipoJornadaMinimo { get; set; }
    public DateTime? FechaMinimoInicio { get; set; }
    public DateTime? FechaMinimoFin { get; set; }
    public string? ReferenciaRelacionOrdinaria { get; set; }
    public decimal SalarioMensual { get; set; }
    public string CodigoJornada { get; set; } = string.Empty;
    public decimal HorasOrdinariasDia { get; set; }
    public decimal DiasBase30 { get; set; }
    public decimal MontoOrdinarioSinRedondear { get; set; }
    public decimal MontoOrdinario { get; set; }

    public DetallePlanilla? DetallePlanilla { get; set; }
}

public sealed class ConceptoDetallePlanilla
{
    public long IdConceptoDetallePlanilla { get; set; }
    public long IdDetallePlanilla { get; set; }
    public long? IdSegmentoCalculoPlanilla { get; set; }
    public long IdConceptoPlanillaVersion { get; set; }
    public int NumeroOrden { get; set; }
    public string TipoFuente { get; set; } = string.Empty;
    public long? IdFuente { get; set; }
    public DateTime? FechaHecho { get; set; }
    public decimal? Cantidad { get; set; }
    public string? Unidad { get; set; }
    public decimal? BaseCalculo { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? Multiplicador { get; set; }
    public decimal MontoSinRedondear { get; set; }
    public decimal Monto { get; set; }
    public bool EsSalarialSnapshot { get; set; }
    public bool EsContributivoCCSSSnapshot { get; set; }
    public bool EsTributarioSnapshot { get; set; }
    public bool EsPatronalSnapshot { get; set; }
    public string DescripcionSnapshot { get; set; } = string.Empty;
    // Evidencia J: nullable exclusivamente para compatibilidad con snapshots legados.
    public long? IdIncidenciaPlanillaConfirmacion { get; set; }
    public int? IdParametroPlanilla { get; set; }
    public string? AmbitoParametro { get; set; }
    public long? IdParametroPlanillaPeriodo { get; set; }
    public long? IdParametroPlanillaColaborador { get; set; }
    public decimal? ValorParametroAplicado { get; set; }
    public string? UnidadParametroAplicada { get; set; }
    public string? ClaveHecho { get; set; }
    public string? UnidadCantidad { get; set; }
    public string? ReferenciaSoporte { get; set; }
    public string? ReferenciaAutorizacion { get; set; }
    public string? RegistradoPorHecho { get; set; }
    public string? AutorizadoPorHecho { get; set; }
    // Base temporal de un hecho patronal I que abarca varios subsegmentos normativos E/G/RT.
    public long? IdCondicionLaboralBase { get; set; }
    public DateTime? FechaBaseInicio { get; set; }
    public DateTime? FechaBaseFin { get; set; }

    public DetallePlanilla? DetallePlanilla { get; set; }
    public SegmentoCalculoPlanilla? SegmentoCalculoPlanilla { get; set; }
    public ConceptoPlanillaVersion? ConceptoPlanillaVersion { get; set; }
    public ICollection<AplicacionNormativaDetallePlanilla> AplicacionesNormativas { get; set; } = [];
}

public sealed class AplicacionNormativaDetallePlanilla
{
    public long IdAplicacionNormativaDetalle { get; set; }
    public long IdConceptoDetallePlanilla { get; set; }
    public long? IdValorParametroNormativo { get; set; }
    public long? IdTramoImpuestoSalario { get; set; }
    public long? IdCreditoFiscalNormativo { get; set; }
    public long? IdConfiguracionRiesgoTrabajo { get; set; }
    public decimal? BaseAplicada { get; set; }
    public decimal? ValorAplicado { get; set; }
    public decimal? MontoAplicado { get; set; }
    // Evidencia J: nullable exclusivamente para compatibilidad con snapshots legados.
    public decimal? MontoAplicadoSinRedondear { get; set; }
    public long? IdCreditoFiscalColaborador { get; set; }
    public decimal? CantidadAplicada { get; set; }
    public long? IdDetallePlanillaPrevio { get; set; }
    public string? AlcanceAplicacion { get; set; }
    public DateTime? FechaAplicacionInicio { get; set; }
    public DateTime? FechaAplicacionFin { get; set; }
    public decimal? BaseContributiva { get; set; }
    public string? ReglaAplicabilidad { get; set; }
    public DateTime? FechaAplicabilidadInicio { get; set; }
    public DateTime? FechaAplicabilidadFin { get; set; }
    public string? ReferenciaSoporte { get; set; }
    public string? ReferenciaRegla { get; set; }
    public string? AcreditadoPor { get; set; }
    public string? ModalidadRT { get; set; }
    public long? IdContratoRT { get; set; }
    public long? IdConfiguracionRTAncla { get; set; }
    public string? ReferenciaAsignacionRT { get; set; }
    public string? AsociadoPorRT { get; set; }

    public ConceptoDetallePlanilla? ConceptoDetallePlanilla { get; set; }
    public ValorParametroNormativo? ValorParametroNormativo { get; set; }
    public TramoImpuestoSalario? TramoImpuestoSalario { get; set; }
    public CreditoFiscalNormativo? CreditoFiscalNormativo { get; set; }
    public ConfiguracionRiesgoTrabajo? ConfiguracionRiesgoTrabajo { get; set; }
}

public sealed class DetallePlanillaDistribucionProyecto
{
    public string? CodigoProyectoSnapshot { get; set; }
    public string? NombreProyectoSnapshot { get; set; }
    public long IdDetallePlanillaDistribucionProyecto { get; set; }
    public long IdDetallePlanilla { get; set; }
    public long? IdProyecto { get; set; }
    public bool EsSinAsignar { get; set; }
    public long IdProyectoNormalizado { get; private set; }
    public decimal Porcentaje { get; set; }
    public decimal MontoReal { get; set; }
    public decimal MontoSinRedondear { get; set; }
    public int NumeroOrden { get; set; }

    public DetallePlanilla? DetallePlanilla { get; set; }
}

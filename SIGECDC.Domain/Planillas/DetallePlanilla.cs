using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Domain.Planillas;

public sealed class DetallePlanilla
{
    public long IdDetallePlanilla { get; set; }

    public long IdPlanilla { get; set; }

    public long? IdEjecucionPlanilla { get; set; }

    public long IdColaborador { get; set; }

    public decimal SalarioBase { get; set; }

    public decimal SalarioProporcional { get; set; }

    public decimal TotalHorasExtra { get; set; }

    public decimal TotalBonos { get; set; }

    public decimal TotalBeneficiosConfigurables { get; set; }

    public decimal TotalAusencias { get; set; }

    public decimal SalarioBruto { get; set; }

    public decimal TotalDeducciones { get; set; }

    public decimal SalarioNeto { get; set; }

    public decimal CostoPatronalEstimado { get; set; }

    public string? CodigoColaborador { get; set; }
    public string? NombreCompleto { get; set; }
    public string? NombrePuesto { get; set; }
    public string? NombreDepartamento { get; set; }
    public string? TipoContrato { get; set; }
    public string? PeriodicidadPago { get; set; }
    public string? CodigoJornada { get; set; }
    public decimal? HorasOrdinariasDia { get; set; }
    public decimal? SalarioContractual { get; set; }
    public string? FuenteDimensionesSnapshot { get; set; }
    public bool SnapshotCompleto { get; set; }
    public decimal IngresosNoSalariales { get; set; }
    public decimal CuotasObreras { get; set; }
    public decimal ImpuestoSalario { get; set; }
    public decimal OtrasDeducciones { get; set; }
    public decimal CargasPatronales { get; set; }
    public decimal RiesgosTrabajo { get; set; }
    public decimal OtrosCostosPatronales { get; set; }
    public decimal CostoLaboralTotal { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public Planilla? Planilla { get; set; }

    public Colaborador? Colaborador { get; set; }

    public ColillaPago? ColillaPago { get; set; }

    public EjecucionPlanilla? EjecucionPlanilla { get; set; }
    public ICollection<SegmentoCalculoPlanilla> Segmentos { get; set; } = [];
    public ICollection<ConceptoDetallePlanilla> Conceptos { get; set; } = [];
    public ICollection<DetallePlanillaDistribucionProyecto> DistribucionesProyecto { get; set; } = [];
}

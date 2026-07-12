namespace SIGECDC.Domain.Planillas;

public sealed class Planilla
{
    public long IdPlanilla { get; set; }

    public long IdPeriodoPlanilla { get; set; }

    public int IdEstadoPlanilla { get; set; }

    public DateTime? FechaCalculo { get; set; }

    public decimal SalarioBrutoTotal { get; set; }

    public decimal DeduccionesTotal { get; set; }

    public decimal SalarioNetoTotal { get; set; }

    public decimal CostoPatronalEstimadoTotal { get; set; }

    public string? AprobadoPor { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public string? CerradoPor { get; set; }

    public DateTime? FechaCierre { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public PeriodoPlanilla? PeriodoPlanilla { get; set; }

    public EstadoPlanilla? EstadoPlanilla { get; set; }

    public ICollection<DetallePlanilla> Detalles { get; set; } = [];
}

using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Domain.Planillas;

public sealed class DetallePlanilla
{
    public long IdDetallePlanilla { get; set; }

    public long IdPlanilla { get; set; }

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

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public Planilla? Planilla { get; set; }

    public Colaborador? Colaborador { get; set; }

    public ColillaPago? ColillaPago { get; set; }
}

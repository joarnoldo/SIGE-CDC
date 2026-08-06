namespace SIGECDC.Application.Planillas;

public sealed class DetalleReportePlanilla
{
    public string CodigoColaborador { get; set; } = string.Empty;

    public string NombreColaborador { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public decimal SalarioProporcional { get; set; }

    public decimal TotalHorasExtra { get; set; }

    public decimal TotalBonos { get; set; }

    public decimal TotalBeneficiosConfigurables { get; set; }

    public decimal TotalAusencias { get; set; }

    public decimal SalarioBruto { get; set; }

    public decimal TotalDeducciones { get; set; }

    public decimal SalarioNeto { get; set; }
}

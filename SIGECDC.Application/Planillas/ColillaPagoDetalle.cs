namespace SIGECDC.Application.Planillas;

public sealed class ColillaPagoDetalle
{
    public long IdColillaPago { get; set; }

    public string CodigoColilla { get; set; } = string.Empty;

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string NombrePeriodo { get; set; } = string.Empty;

    public string TipoPeriodo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoPlanilla { get; set; } = string.Empty;

    public DateTime FechaGeneracion { get; set; }

    public string CodigoColaborador { get; set; } = string.Empty;

    public string NombreColaborador { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public string Puesto { get; set; } = string.Empty;

    public decimal SalarioBase { get; set; }

    public decimal SalarioProporcional { get; set; }

    public decimal TotalHorasExtra { get; set; }

    public decimal TotalBonos { get; set; }

    public decimal TotalBeneficiosConfigurables { get; set; }

    public decimal TotalAusencias { get; set; }

    public decimal SalarioBruto { get; set; }

    public decimal TotalDeducciones { get; set; }

    public decimal SalarioNeto { get; set; }
}

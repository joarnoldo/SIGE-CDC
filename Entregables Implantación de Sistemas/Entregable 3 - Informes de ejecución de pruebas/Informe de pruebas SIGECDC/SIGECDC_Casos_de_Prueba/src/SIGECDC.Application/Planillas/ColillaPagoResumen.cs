namespace SIGECDC.Application.Planillas;

public sealed class ColillaPagoResumen
{
    public long IdColillaPago { get; set; }

    public long IdPeriodoPlanilla { get; set; }

    public string CodigoColilla { get; set; } = string.Empty;

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string NombrePeriodo { get; set; } = string.Empty;

    public string TipoPeriodo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoPlanilla { get; set; } = string.Empty;

    public DateTime FechaGeneracion { get; set; }

    public decimal SalarioBruto { get; set; }

    public decimal TotalDeducciones { get; set; }

    public decimal SalarioNeto { get; set; }
}

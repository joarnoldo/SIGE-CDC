namespace SIGECDC.Application.RecursosHumanos;

public sealed class ContratoDetalle
{
    public long IdContrato { get; set; }

    public long IdColaborador { get; set; }

    public string TipoContrato { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public decimal SalarioBase { get; set; }

    public string? Jornada { get; set; }

    public string PeriodicidadPago { get; set; } = string.Empty;

    public string EstadoContrato { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}

namespace SIGECDC.Application.Activos;

public sealed class PeriodoFueraServicioResumen
{
    public long IdMantenimiento { get; set; }

    public string TipoMantenimiento { get; set; } = string.Empty;

    public string EstadoMantenimiento { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public decimal? TiempoFueraServicioHoras { get; set; }
}

namespace SIGECDC.Application.Activos;

public sealed class MantenimientoCostoReporteResumen
{
    public long IdMantenimiento { get; set; }

    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string Activo { get; set; } = string.Empty;

    public long? IdProyecto { get; set; }

    public string? CodigoProyecto { get; set; }

    public string? Proyecto { get; set; }

    public string TipoMantenimiento { get; set; } = string.Empty;

    public string EstadoMantenimiento { get; set; } = string.Empty;

    public DateTime FechaEfectiva { get; set; }

    public DateTime FechaProgramada { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? Descripcion { get; set; }

    public decimal? CostoEstimado { get; set; }

    public decimal? CostoReal { get; set; }
}

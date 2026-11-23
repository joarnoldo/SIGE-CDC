namespace SIGECDC.Application.Activos;

public sealed class AsignacionActivoProyectoResumen
{
    public long IdAsignacionActivoProyecto { get; set; }

    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public string EstadoActivo { get; set; } = string.Empty;

    public long IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = string.Empty;

    public string NombreProyecto { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string? Observaciones { get; set; }

    public string? AsignadoPor { get; set; }

    public DateTime FechaAsignacion { get; set; }
}

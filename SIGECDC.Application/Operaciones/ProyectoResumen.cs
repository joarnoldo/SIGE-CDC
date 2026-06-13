namespace SIGECDC.Application.Operaciones;

public sealed class ProyectoResumen
{
    public long IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = string.Empty;

    public string NombreProyecto { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFinEstimada { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public string? Responsable { get; set; }

    public string? Ubicacion { get; set; }

    public int IdEstadoProyecto { get; set; }

    public string EstadoProyecto { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}

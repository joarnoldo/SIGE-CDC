namespace SIGECDC.Application.Activos;

public sealed class DisponibilidadActivoResumen
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public string TipoActivo { get; set; } = string.Empty;

    public string CategoriaActivo { get; set; } = string.Empty;

    public int IdEstadoActivo { get; set; }

    public string EstadoActivo { get; set; } = string.Empty;

    public string? UbicacionActual { get; set; }

    public bool EstaDisponible { get; set; }

    public string MotivoDisponibilidad { get; set; } = string.Empty;
}

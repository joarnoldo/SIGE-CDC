using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudActualizacionEstadoUbicacion
{
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un activo.")]
    public long IdActivo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estado.")]
    public int IdEstadoActivo { get; set; }

    [StringLength(150, ErrorMessage = "La ubicación no puede superar 150 caracteres.")]
    public string? UbicacionActual { get; set; }
}

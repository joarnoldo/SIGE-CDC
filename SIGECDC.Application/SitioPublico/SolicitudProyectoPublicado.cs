using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.SitioPublico;

public sealed class SolicitudProyectoPublicado
{
    public long IdProyectoPublicado { get; set; }

    public long? IdProyecto { get; set; }

    [Required(ErrorMessage = "El titulo es obligatorio.")]
    [StringLength(200, ErrorMessage = "El titulo no puede superar 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripcion no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }

    [StringLength(50, ErrorMessage = "El estado visible no puede superar 50 caracteres.")]
    public string? EstadoVisual { get; set; }

    public bool EstaPublicado { get; set; }
}

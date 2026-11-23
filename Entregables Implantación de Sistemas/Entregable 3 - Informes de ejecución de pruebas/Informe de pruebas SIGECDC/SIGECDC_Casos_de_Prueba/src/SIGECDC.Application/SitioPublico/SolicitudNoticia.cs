using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.SitioPublico;

public sealed class SolicitudNoticia
{
    [Required(ErrorMessage = "El titulo es obligatorio.")]
    [StringLength(200, ErrorMessage = "El titulo no debe superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "El resumen no debe superar los 500 caracteres.")]
    public string? Resumen { get; set; }

    public string? Contenido { get; set; }
}
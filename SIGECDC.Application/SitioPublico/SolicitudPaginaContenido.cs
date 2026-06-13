using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.SitioPublico;

public sealed class SolicitudPaginaContenido
{
    public long IdPaginaContenido { get; set; }

    [Required(ErrorMessage = "El codigo de pagina es obligatorio.")]
    [StringLength(100, ErrorMessage = "El codigo no puede superar 100 caracteres.")]
    public string CodigoPagina { get; set; } = string.Empty;

    [Required(ErrorMessage = "El titulo es obligatorio.")]
    [StringLength(200, ErrorMessage = "El titulo no puede superar 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    public string? Contenido { get; set; }

    public bool EstaPublicado { get; set; }
}

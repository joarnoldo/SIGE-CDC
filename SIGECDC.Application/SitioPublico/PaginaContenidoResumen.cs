namespace SIGECDC.Application.SitioPublico;

public sealed class PaginaContenidoResumen
{
    public long IdPaginaContenido { get; set; }

    public string CodigoPagina { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Contenido { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }
}

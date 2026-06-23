namespace SIGECDC.Application.SitioPublico;

public sealed class NoticiaResumen
{
    public long IdNoticia { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Resumen { get; set; }

    public string? Contenido { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
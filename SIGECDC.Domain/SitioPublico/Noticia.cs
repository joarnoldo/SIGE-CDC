namespace SIGECDC.Domain.SitioPublico;

public class Noticia
{
    public long IdNoticia { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Resumen { get; set; }

    public string? Contenido { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}
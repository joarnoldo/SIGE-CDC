namespace SIGECDC.Application.SitioPublico;

public sealed class ProyectoPublicadoResumen
{
    public long IdProyectoPublicado { get; set; }

    public long? IdProyecto { get; set; }

    public string? CodigoProyecto { get; set; }

    public string? NombreProyecto { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? EstadoVisual { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime FechaCreacion { get; set; }
}

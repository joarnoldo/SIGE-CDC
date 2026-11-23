namespace SIGECDC.Application.SitioPublico;

public sealed class ProyectoPortafolioResumen
{
    public long IdProyectoPublicado { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? EstadoVisual { get; set; }
}

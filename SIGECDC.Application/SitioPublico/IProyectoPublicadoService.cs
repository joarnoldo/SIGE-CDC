namespace SIGECDC.Application.SitioPublico;

public interface IProyectoPublicadoService
{
    Task<IReadOnlyList<ProyectoPublicadoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProyectoPortafolioResumen>> ObtenerProyectosPublicadosAsync(
        string? estadoVisual = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ObtenerEstadosPublicadosAsync(CancellationToken cancellationToken = default);

    Task GuardarProyectoPublicadoAsync(SolicitudProyectoPublicado solicitud, CancellationToken cancellationToken = default);

    Task CambiarPublicacionAsync(long idProyectoPublicado, bool estaPublicado, CancellationToken cancellationToken = default);
}

namespace SIGECDC.Application.SitioPublico;

public interface IProyectoPublicadoService
{
    Task<IReadOnlyList<ProyectoPublicadoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProyectoPublicadoResumen>> ObtenerProyectosPublicadosAsync(CancellationToken cancellationToken = default);

    Task GuardarProyectoPublicadoAsync(SolicitudProyectoPublicado solicitud, CancellationToken cancellationToken = default);

    Task CambiarPublicacionAsync(long idProyectoPublicado, bool estaPublicado, CancellationToken cancellationToken = default);
}

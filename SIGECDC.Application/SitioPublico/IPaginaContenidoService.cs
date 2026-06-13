namespace SIGECDC.Application.SitioPublico;

public interface IPaginaContenidoService
{
    Task<IReadOnlyList<PaginaContenidoResumen>> ObtenerPaginasAsync(CancellationToken cancellationToken = default);

    Task<PaginaContenidoResumen?> ObtenerPaginaPorIdAsync(long idPaginaContenido, CancellationToken cancellationToken = default);

    Task<PaginaContenidoResumen?> ObtenerPaginaPublicadaAsync(string codigoPagina, CancellationToken cancellationToken = default);

    Task GuardarPaginaAsync(SolicitudPaginaContenido solicitud, CancellationToken cancellationToken = default);

    Task CambiarPublicacionAsync(long idPaginaContenido, bool estaPublicado, CancellationToken cancellationToken = default);
}

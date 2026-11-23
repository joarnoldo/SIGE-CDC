namespace SIGECDC.Application.SitioPublico;

public interface INoticiaService
{
    Task<IReadOnlyList<NoticiaResumen>> ObtenerTodasAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoticiaResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default);

    Task<NoticiaResumen?> ObtenerPorIdAsync(long idNoticia, CancellationToken cancellationToken = default);

    Task<long> RegistrarAsync(SolicitudNoticia solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(long idNoticia, SolicitudNoticia solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task PublicarAsync(long idNoticia, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task DespublicarAsync(long idNoticia, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task DesactivarAsync(long idNoticia, string? idUsuarioActual = null, CancellationToken cancellationToken = default);
}
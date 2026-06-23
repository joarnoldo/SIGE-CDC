namespace SIGECDC.Application.SitioPublico;

public interface IGaleriaService
{
    Task<IReadOnlyList<GaleriaResumen>> ObtenerTodasAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GaleriaResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default);

    Task<GaleriaResumen?> ObtenerPorIdAsync(long idGaleria, CancellationToken cancellationToken = default);

    Task<long> RegistrarAsync(SolicitudGaleria solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(long idGaleria, SolicitudGaleria solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task PublicarAsync(long idGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task DespublicarAsync(long idGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task DesactivarAsync(long idGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default);
}
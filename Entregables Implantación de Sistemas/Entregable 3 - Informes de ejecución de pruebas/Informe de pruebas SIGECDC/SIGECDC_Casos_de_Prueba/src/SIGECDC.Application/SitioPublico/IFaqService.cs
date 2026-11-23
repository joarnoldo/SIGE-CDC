namespace SIGECDC.Application.SitioPublico;

public interface IFaqService
{
    Task RegistrarAsync(SolicitudRegistrarFaq solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(SolicitudActualizarFaq solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task PublicarAsync(long idFaq, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task DespublicarAsync(long idFaq, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task DesactivarAsync(long idFaq, string? idUsuarioActual = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqResumen>> ObtenerTodosAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default);
}
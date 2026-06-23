namespace SIGECDC.Application.RecursosHumanos;

public interface IPuestoService
{
    Task RegistrarAsync(SolicitudRegistrarPuesto solicitud, CancellationToken cancellationToken = default);

    Task ActualizarAsync(SolicitudActualizarPuesto solicitud, CancellationToken cancellationToken = default);

    Task ActivarAsync(int idPuesto, CancellationToken cancellationToken = default);

    Task DesactivarAsync(int idPuesto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PuestoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    Task<PuestoResumen?> ObtenerPorIdAsync(int idPuesto, CancellationToken cancellationToken = default);
}
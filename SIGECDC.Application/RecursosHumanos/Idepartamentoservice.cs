namespace SIGECDC.Application.RecursosHumanos;

public interface IDepartamentoService
{
    Task RegistrarAsync(SolicitudRegistrarDepartamento solicitud, CancellationToken cancellationToken = default);

    Task ActualizarAsync(SolicitudActualizarDepartamento solicitud, CancellationToken cancellationToken = default);

    Task DesactivarAsync(long idDepartamento, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartamentoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    Task<DepartamentoResumen?> ObtenerPorIdAsync(long idDepartamento, CancellationToken cancellationToken = default);
}
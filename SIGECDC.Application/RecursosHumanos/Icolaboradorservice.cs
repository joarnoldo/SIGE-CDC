namespace SIGECDC.Application.RecursosHumanos;

public interface IColaboradorService
{
    Task AsignarPuestoYDepartamentoAsync(SolicitudAsignarColaborador solicitud, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ColaboradorResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    Task<ColaboradorResumen?> ObtenerPorIdAsync(long idColaborador, CancellationToken cancellationToken = default);
}
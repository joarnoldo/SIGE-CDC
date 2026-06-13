namespace SIGECDC.Application.Operaciones;

public interface IProyectoService
{
    Task<IReadOnlyList<ProyectoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstadoProyectoOpcion>> ObtenerEstadosAsync(CancellationToken cancellationToken = default);

    Task<ProyectoResumen?> ObtenerProyectoPorIdAsync(long idProyecto, CancellationToken cancellationToken = default);

    Task GuardarProyectoAsync(SolicitudProyecto solicitud, CancellationToken cancellationToken = default);
}

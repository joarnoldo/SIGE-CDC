namespace SIGECDC.Application.Activos;

public interface IMantenimientoService
{
	Task<IReadOnlyList<MantenimientoResumen>> ObtenerMantenimientosAsync(
		CancellationToken cancellationToken = default);

	Task<MantenimientoResumen?> ObtenerMantenimientoPorIdAsync(
		long idMantenimiento,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<EstadoMantenimientoOpcion>> ObtenerEstadosMantenimientoAsync(
		CancellationToken cancellationToken = default);

	Task<long> RegistrarMantenimientoAsync(
		SolicitudRegistroMantenimiento solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default);

	Task ActualizarMantenimientoAsync(
		SolicitudActualizarMantenimiento solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default);
}

namespace SIGECDC.Application.Activos;

public interface IMantenimientoService
{
	Task<IReadOnlyList<MantenimientoResumen>> ObtenerMantenimientosAsync(
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<MantenimientoResumen>> ObtenerMantenimientosPreventivosPendientesAsync(
		CancellationToken cancellationToken = default);

	Task<MantenimientoResumen?> ObtenerMantenimientoPorIdAsync(
		long idMantenimiento,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<EstadoMantenimientoOpcion>> ObtenerEstadosMantenimientoAsync(
		CancellationToken cancellationToken = default);

	Task<long> DefinirMantenimientoPreventivoAsync(
		SolicitudDefinirMantenimientoPreventivo solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default);

	Task<long> CrearOrdenCorrectivaAsync(
		SolicitudCrearOrdenMantenimiento solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default);

	Task IniciarOrdenAsync(
		long idMantenimiento,
		string idUsuarioActual,
		CancellationToken cancellationToken = default);

	Task FinalizarOrdenAsync(
		SolicitudCerrarOrdenMantenimiento solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<MantenimientoResumen>> ObtenerHistorialActivoAsync(
		long idActivo,
		CancellationToken cancellationToken = default);

	Task<SIGECDC.Application.Archivos.ArchivoDescarga?> AbrirEvidenciaAsync(
		long idDocumentoArchivo,
		CancellationToken cancellationToken = default);
}

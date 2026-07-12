namespace SIGECDC.Application.Activos;

public interface IAsignacionActivoProyectoService
{
    Task<IReadOnlyList<AsignacionActivoProyectoResumen>> ObtenerAsignacionesAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        long? idProyecto = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivoAsignacionOpcion>> ObtenerActivosDisponiblesAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);

    Task<long> CrearAsignacionAsync(
        SolicitudAsignacionActivoProyecto solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}

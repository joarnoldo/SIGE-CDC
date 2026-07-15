namespace SIGECDC.Application.Activos;

public interface IActivoService
{
    Task<IReadOnlyList<ActivoResumen>> ObtenerActivosAsync(CancellationToken cancellationToken = default);

    Task<ActivoResumen?> ObtenerActivoPorIdAsync(long idActivo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoActivoOpcion>> ObtenerTiposActivoAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoriaActivoOpcion>> ObtenerCategoriasPorTipoAsync(int idTipoActivo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstadoActivoOpcion>> ObtenerEstadosActivoAsync(CancellationToken cancellationToken = default);

    Task<long> RegistrarActivoAsync(
        SolicitudRegistroActivo solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task ActualizarEstadoUbicacionAsync(
        SolicitudActualizacionEstadoUbicacion solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrazabilidadActivoResumen>> ObtenerTrazabilidadActivoAsync(
        long idActivo,
        CancellationToken cancellationToken = default);
}

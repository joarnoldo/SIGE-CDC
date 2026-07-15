namespace SIGECDC.Application.Activos;

public interface IActivoService
{
    Task<IReadOnlyList<ActivoResumen>> ObtenerActivosAsync(CancellationToken cancellationToken = default);

    Task<ActivoResumen?> ObtenerActivoPorIdAsync(long idActivo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoActivoOpcion>> ObtenerTiposActivoAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoriaActivoOpcion>> ObtenerCategoriasPorTipoAsync(int idTipoActivo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstadoActivoOpcion>> ObtenerEstadosActivoAsync(CancellationToken cancellationToken = default);

    Task GuardarActivoAsync(SolicitudActivo solicitud, CancellationToken cancellationToken = default);
}

namespace SIGECDC.Application.Planillas;

public interface IParametroPlanillaService
{
    Task<IReadOnlyList<ParametroPlanillaResumen>> ObtenerParametrosAsync(
        string? busqueda = null,
        string? tipoParametro = null,
        string? estadoRegistro = null,
        CancellationToken cancellationToken = default);

    Task<ParametroPlanillaDetalle?> ObtenerParametroPorIdAsync(int idParametroPlanilla, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoParametroPlanillaOpcion>> ObtenerTiposParametroAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NaturalezaParametroPlanillaOpcion>> ObtenerNaturalezasParametroAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AsignacionParametroPlanillaResumen>> ObtenerAsignacionesAsync(
        int idParametroPlanilla,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DestinoAsignacionParametroPlanillaOpcion>> ObtenerColaboradoresAsignacionAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DestinoAsignacionParametroPlanillaOpcion>> ObtenerPeriodosAsignacionAsync(
        CancellationToken cancellationToken = default);

    Task<int> RegistrarParametroAsync(
        SolicitudParametroPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task ActualizarParametroAsync(
        int idParametroPlanilla,
        SolicitudParametroPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task DesactivarParametroAsync(
        int idParametroPlanilla,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task<long> GuardarAsignacionAsync(
        int idParametroPlanilla,
        SolicitudAsignacionParametroPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task DesactivarAsignacionAsync(
        string ambito,
        long idAsignacion,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);
}

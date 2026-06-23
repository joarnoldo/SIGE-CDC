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
}

namespace SIGECDC.Application.Planillas;

public interface IIncidenciaPlanillaService
{
    Task<IReadOnlyList<IncidenciaPlanillaResumen>> ObtenerIncidenciasAsync(
        long? idPeriodoPlanilla = null,
        long? idColaborador = null,
        int? idTipoIncidenciaPlanilla = null,
        string? estadoRegistro = null,
        CancellationToken cancellationToken = default);

    Task<IncidenciaPlanillaDetalle?> ObtenerIncidenciaPorIdAsync(long idIncidenciaPlanilla, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PeriodoPlanillaOpcion>> ObtenerPeriodosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ColaboradorIncidenciaOpcion>> ObtenerColaboradoresAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoIncidenciaPlanillaOpcion>> ObtenerTiposIncidenciaAsync(CancellationToken cancellationToken = default);

    Task<long> RegistrarIncidenciaAsync(
        SolicitudIncidenciaPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task ActualizarIncidenciaAsync(
        long idIncidenciaPlanilla,
        SolicitudIncidenciaPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task DesactivarIncidenciaAsync(
        long idIncidenciaPlanilla,
        CancellationToken cancellationToken = default);
}

namespace SIGECDC.Application.Planillas;

public interface IPlanillaService
{
    Task<IReadOnlyList<PeriodoPlanillaResumen>> ObtenerPeriodosAsync(
        CancellationToken cancellationToken = default);

    Task<long> CrearPeriodoAsync(
        SolicitudPeriodoPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task<PeriodoPlanillaDetalle?> ObtenerDetallePeriodoAsync(
        long idPeriodoPlanilla,
        CancellationToken cancellationToken = default);

    Task CalcularPlanillaAsync(
        long idPeriodoPlanilla,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task AprobarPlanillaAsync(
        long idPeriodoPlanilla,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task CerrarPlanillaAsync(
        long idPeriodoPlanilla,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}

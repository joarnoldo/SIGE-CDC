namespace SIGECDC.Application.Planillas;

public interface IReportePlanillaService
{
    Task<IReadOnlyList<PeriodoReportePlanillaOpcion>> ObtenerPeriodosDisponiblesAsync(
        CancellationToken cancellationToken = default);

    Task<ReportePlanilla?> ObtenerReporteAsync(
        long idPeriodoPlanilla,
        CancellationToken cancellationToken = default);
}

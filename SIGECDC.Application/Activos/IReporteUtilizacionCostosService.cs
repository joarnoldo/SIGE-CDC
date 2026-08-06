namespace SIGECDC.Application.Activos;

public interface IReporteUtilizacionCostosService
{
    Task<ReporteUtilizacionCostosResumen> GenerarPorProyectoAsync(
        long idProyecto,
        FiltroPeriodoReporte filtro,
        CancellationToken cancellationToken = default);

    Task<ReporteUtilizacionCostosResumen> GenerarPorActivoAsync(
        long idActivo,
        FiltroPeriodoReporte filtro,
        CancellationToken cancellationToken = default);
}

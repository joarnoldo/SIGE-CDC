using SIGECDC.Application.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class ReportePlanillaService(ApplicationDbContext contexto) : IReportePlanillaService
{
    public async Task<IReadOnlyList<PeriodoReportePlanillaOpcion>> ObtenerPeriodosDisponiblesAsync(
        CancellationToken cancellationToken = default, string actor = "")
    {
        var lector = new LectorPublicacionPlanilla(contexto);
        var resultado = new List<PeriodoReportePlanillaOpcion>();
        foreach (var id in await lector.PeriodosAsync(actor, false, cancellationToken))
        {
            var r = await lector.ConsultarAsync(id, actor, false, cancellationToken);
            if (r is not null) resultado.Add(new() { IdPeriodoPlanilla = id, CodigoPeriodo = r.CodigoPeriodo,
                NombrePeriodo = r.NombrePeriodo, TipoPeriodo = r.TipoPeriodo, FechaInicio = r.FechaInicio,
                FechaFin = r.FechaFin, EstadoPlanilla = r.EstadoPlanilla });
        }
        return resultado;
    }

    public Task<ReportePlanilla?> ObtenerReporteAsync(long idPeriodoPlanilla,
        CancellationToken cancellationToken = default, string actor = "")
        => new LectorPublicacionPlanilla(contexto).ConsultarAsync(idPeriodoPlanilla, actor, false, cancellationToken);
}

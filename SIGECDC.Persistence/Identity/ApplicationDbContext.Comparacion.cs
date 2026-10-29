using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Planillas;

namespace SIGECDC.Persistence.Identity;

public partial class ApplicationDbContext
{
    // Solo el caso de uso M puede materializar la transición después de conciliar.
    internal bool PermitirTransicionComparacionM { get; set; }
    private IEnumerable<EntityEntry> CandidatosComparacion()=>ChangeTracker.Entries()
        .Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Where(e=>e.Entity is ForecastComparacionReal or ForecastComparacionResultado or ForecastComparacionResultadoFuente
            or DetallePlanillaDistribucionProyecto);

    private async Task ProtegerComparacionAsync(CancellationToken ct)
    {
        if(!Database.IsRelational() || !CandidatosComparacion().Any())return;
        await using var command=Database.GetDbConnection().CreateCommand();
        command.Transaction=Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText="SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastComparacionReal'";
        if(Convert.ToInt32(await command.ExecuteScalarAsync(ct))!=1)return;
        var consumidas=from c in ForecastComparacionesReales join p in ForecastPeriodos on c.IdForecastPeriodo equals p.IdForecastPeriodo
            join e in ForecastEscenarios on p.IdForecastEscenario equals e.IdForecastEscenario
            where e.EstadoEscenario==EstadosEscenarioForecast.Comparado select c;
        foreach(var entrada in CandidatosComparacion().ToArray())
        {
            if(entrada.Entity is DetallePlanillaDistribucionProyecto d)
            {
                var ids=new HashSet<long>{d.IdDetallePlanilla};
                if(entrada.State!=EntityState.Added)
                {
                    var original=await DistribucionesProyectoDetallePlanilla.AsNoTracking().Where(x=>x.IdDetallePlanillaDistribucionProyecto==d.IdDetallePlanillaDistribucionProyecto)
                        .Select(x=>(long?)x.IdDetallePlanilla).SingleOrDefaultAsync(ct);
                    if(original is {} id)ids.Add(id);
                }
                var periodos=await DetallesPlanilla.Where(x=>ids.Contains(x.IdDetallePlanilla)).Select(x=>x.Planilla!.IdPeriodoPlanilla).ToArrayAsync(ct);
                await BloqueoPeriodoPlanilla.ObtenerAsync(this,periodos,ct);
                // Periodo -> evidencia M; no adquirir aquí el lock del escenario en orden inverso a comparar.
                if(await DetallesPlanilla.AnyAsync(x=>ids.Contains(x.IdDetallePlanilla) && ForecastComparacionesReales.Any(c=>c.IdEjecucionPlanillaReal==x.IdEjecucionPlanilla
                    && ForecastComparacionResultados.Any(r=>r.IdForecastComparacionReal==c.IdForecastComparacionReal && r.Granularidad=="Proyecto")),ct))
                    throw new InvalidOperationException("DISTRIBUCION_REAL_CONSUMIDA: conserve la evidencia comparada.");
                continue;
            }
            long? cabecera=entrada.Entity switch
            {
                ForecastComparacionReal c=>c.IdForecastComparacionReal,
                ForecastComparacionResultado r=>r.IdForecastComparacionReal,
                ForecastComparacionResultadoFuente f=>await ForecastComparacionResultados.Where(r=>r.IdForecastComparacionResultado==f.IdForecastComparacionResultado)
                    .Select(r=>(long?)r.IdForecastComparacionReal).SingleOrDefaultAsync(ct),
                _=>null
            };
            long? periodo=entrada.Entity is ForecastComparacionReal cab?cab.IdForecastPeriodo:
                await ForecastComparacionesReales.Where(c=>c.IdForecastComparacionReal==cabecera).Select(c=>(long?)c.IdForecastPeriodo).SingleOrDefaultAsync(ct);
            var escenario=await ForecastPeriodos.Where(p=>p.IdForecastPeriodo==periodo).Select(p=>(long?)p.IdForecastEscenario).SingleOrDefaultAsync(ct);
            if(escenario is null)continue;
            command.CommandText="SELECT IdForecastEscenario FROM ForecastEscenario WHERE IdForecastEscenario=@id FOR UPDATE";
            command.Parameters.Clear();var par=command.CreateParameter();par.ParameterName="@id";par.Value=escenario;command.Parameters.Add(par);
            await command.ExecuteScalarAsync(ct);
            if(await ForecastEscenarios.AnyAsync(e=>e.IdForecastEscenario==escenario && e.EstadoEscenario==EstadosEscenarioForecast.Comparado,ct)
                || cabecera is not null && await consumidas.AnyAsync(c=>c.IdForecastComparacionReal==cabecera,ct))
                throw new InvalidOperationException("COMPARACION_INMUTABLE: conserve cabecera, resultados y procedencia.");
            // Un cambio de FK no permite sacar evidencia de una comparación consumida.
            if(entrada.State!=EntityState.Added)
            {
                var protegida=entrada.Entity switch
                {
                    ForecastComparacionResultado r=>await ForecastComparacionResultados.AnyAsync(x=>x.IdForecastComparacionResultado==r.IdForecastComparacionResultado
                        && consumidas.Any(c=>c.IdForecastComparacionReal==x.IdForecastComparacionReal),ct),
                    ForecastComparacionResultadoFuente f=>await ForecastComparacionFuentes.AnyAsync(x=>x.IdForecastComparacionResultadoFuente==f.IdForecastComparacionResultadoFuente
                        && ForecastComparacionResultados.Any(r=>r.IdForecastComparacionResultado==x.IdForecastComparacionResultado
                            && consumidas.Any(c=>c.IdForecastComparacionReal==r.IdForecastComparacionReal)),ct),
                    _=>false
                };
                if(protegida)throw new InvalidOperationException("COMPARACION_INMUTABLE");
            }
        }
    }
}

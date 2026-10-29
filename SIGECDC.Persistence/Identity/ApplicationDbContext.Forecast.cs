using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Identity;

public partial class ApplicationDbContext
{
    private IEnumerable<EntityEntry> CandidatosForecast()=>ChangeTracker.Entries()
        .Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Where(e=>e.Entity is ForecastEscenario or ForecastPeriodo or ForecastParticipante or ForecastFuenteHistorica
            or ForecastParametro or ForecastAsignacionProyecto or ForecastObservacionHistorica
            or ForecastSegmentoProyectado or ForecastDetalle or ForecastAplicacionFinanciera);

    private async Task ProtegerForecastAsync(CancellationToken ct)
    {
        if(!Database.IsRelational())return;
        await using var command=Database.GetDbConnection().CreateCommand();
        command.Transaction=Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText="SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAplicacionFinanciera'";
        if(Convert.ToInt32(await command.ExecuteScalarAsync(ct))!=1)return; // QA anterior al upgrade: no contiene evidencia L.
        foreach(var e in CandidatosForecast().ToArray())
        {
            long? escenario=e.Entity switch
            {
                ForecastEscenario f=>f.IdForecastEscenario,
                ForecastPeriodo f=>f.IdForecastEscenario,
                ForecastParticipante f=>f.IdForecastEscenario,
                ForecastFuenteHistorica f=>f.IdForecastEscenario,
                ForecastParametro f=>f.IdForecastEscenario,
                ForecastAsignacionProyecto f=>f.IdForecastEscenario,
                ForecastObservacionHistorica f=>f.IdForecastEscenario,
                ForecastSegmentoProyectado f=>f.IdForecastEscenario,
                ForecastDetalle f=>f.IdForecastEscenario,
                ForecastAplicacionFinanciera f=>await ForecastDetalles.Where(d=>d.IdForecastDetalle==f.IdForecastDetalle).Select(d=>(long?)d.IdForecastEscenario).SingleOrDefaultAsync(ct),
                _=>null
            };
            if(escenario is null or <=0)continue;
            command.CommandText="SELECT IdForecastEscenario FROM ForecastEscenario WHERE IdForecastEscenario=@id FOR UPDATE";
            command.Parameters.Clear();var id=command.CreateParameter();id.ParameterName="@id";id.Value=escenario.Value;command.Parameters.Add(id);
            await command.ExecuteScalarAsync(ct);
            var previo=await ForecastEscenarios.AsNoTracking().SingleOrDefaultAsync(f=>f.IdForecastEscenario==escenario,ct);
            if(previo?.VersionMotor!=ProyeccionFinanciera.VersionMotor || previo.EstadoEscenario==EstadosEscenarioForecast.Borrador)continue;
            var guardar=e.Entity is ForecastEscenario actual && e.State==EntityState.Modified
                && previo.EstadoEscenario==EstadosEscenarioForecast.Calculado && actual.EstadoEscenario==EstadosEscenarioForecast.Guardado
                && e.Properties.Where(p=>p.IsModified).All(p=>p.Metadata.Name is nameof(ForecastEscenario.EstadoEscenario)
                    or nameof(ForecastEscenario.FechaModificacion) or nameof(ForecastEscenario.ModificadoPor));
            var comparar=PermitirTransicionComparacionM && e.Entity is ForecastEscenario comparado && e.State==EntityState.Modified
                && previo.EstadoEscenario==EstadosEscenarioForecast.Guardado && comparado.EstadoEscenario==EstadosEscenarioForecast.Comparado
                && e.Properties.Where(p=>p.IsModified).All(p=>p.Metadata.Name is nameof(ForecastEscenario.EstadoEscenario)
                    or nameof(ForecastEscenario.FechaModificacion) or nameof(ForecastEscenario.ModificadoPor));
            if(!guardar && !comparar)throw new InvalidOperationException("EVIDENCIA_FORECAST_INMUTABLE: conserve el snapshot calculado/guardado.");
        }
        // Los valores/versiones consumidos no pueden cambiar después de materializar L.
        var congelados=ForecastEscenarios.Where(f=>f.VersionMotor==ProyeccionFinanciera.VersionMotor && f.EstadoEscenario!=EstadosEscenarioForecast.Borrador).Select(f=>f.IdForecastEscenario);
        var detalles=ForecastDetalles.Where(d=>congelados.Contains(d.IdForecastEscenario));
        var segmentos=ForecastSegmentosProyectados.Where(s=>congelados.Contains(s.IdForecastEscenario));
        var idsDetalles=detalles.Select(d=>d.IdForecastDetalle);
        var aplicaciones=ForecastAplicacionesFinancieras.Where(a=>idsDetalles.Contains(a.IdForecastDetalle));
        foreach(var e in CandidatosSnapshot().Where(e=>e.State!=EntityState.Added).ToArray())
        {
            var usada=e.Entity switch
            {
                ConceptoPlanillaVersion c=>await detalles.AnyAsync(d=>d.IdConceptoPlanillaVersion==c.IdConceptoPlanillaVersion,ct),
                ConceptoPlanilla c when e.State==EntityState.Deleted || e.Property(nameof(c.Codigo)).IsModified
                    =>await detalles.AnyAsync(d=>ConceptosPlanillaVersion.Any(v=>v.IdConceptoPlanillaVersion==d.IdConceptoPlanillaVersion && v.IdConceptoPlanilla==c.IdConceptoPlanilla),ct),
                CondicionLaboralVersion c=>await CondicionForecastProtegidaAsync(c,e,segmentos,ct),
                JornadaLaboral j=>await ForecastSegmentosProyectados.AnyAsync(s=>s.IdJornadaLaboral==j.IdJornadaLaboral && congelados.Contains(s.IdForecastEscenario),ct),
                SalarioMinimoVersion m=>await ForecastSegmentosProyectados.AnyAsync(s=>s.IdSalarioMinimoVersion==m.IdSalarioMinimoVersion && congelados.Contains(s.IdForecastEscenario),ct),
                ConfiguracionRiesgoTrabajo r=>await aplicaciones.AnyAsync(a=>a.IdConfiguracionRiesgoTrabajo==r.IdConfiguracionRiesgoTrabajo,ct),
                ValorParametroNormativo v=>await aplicaciones.AnyAsync(a=>a.IdValorParametroNormativo==v.IdValorParametroNormativo,ct),
                ParametroNormativo p=>await aplicaciones.AnyAsync(a=>ValoresParametrosNormativos.Any(v=>v.IdValorParametroNormativo==a.IdValorParametroNormativo && v.IdParametroNormativo==p.IdParametroNormativo),ct),
                VersionNormativa v=>await aplicaciones.AnyAsync(a=>
                    ValoresParametrosNormativos.Any(n=>n.IdValorParametroNormativo==a.IdValorParametroNormativo && n.IdVersionNormativa==v.IdVersionNormativa)
                    || TramosImpuestoSalario.Any(t=>t.IdTramoImpuestoSalario==a.IdTramoImpuestoSalario && t.IdVersionNormativa==v.IdVersionNormativa)
                    || CreditosFiscalesNormativos.Any(c=>c.IdCreditoFiscalNormativo==a.IdCreditoFiscalNormativo && c.IdVersionNormativa==v.IdVersionNormativa)
                    || ConfiguracionesRiesgoTrabajo.Any(r=>r.IdConfiguracionRiesgoTrabajo==a.IdConfiguracionRiesgoTrabajo && r.IdVersionNormativa==v.IdVersionNormativa),ct)
                    || await segmentos.AnyAsync(s=>SalariosMinimosVersion.Any(m=>m.IdSalarioMinimoVersion==s.IdSalarioMinimoVersion && m.IdVersionNormativa==v.IdVersionNormativa),ct),
                TramoImpuestoSalario t=>await aplicaciones.AnyAsync(a=>a.IdTramoImpuestoSalario==t.IdTramoImpuestoSalario,ct),
                CreditoFiscalNormativo c=>await aplicaciones.AnyAsync(a=>a.IdCreditoFiscalNormativo==c.IdCreditoFiscalNormativo,ct),
                CreditoFiscalColaborador c=>await aplicaciones.AnyAsync(a=>a.IdCreditoFiscalColaborador==c.IdCreditoFiscalColaborador,ct),
                _=>false
            };
            if(usada)throw new InvalidOperationException("EVIDENCIA_FORECAST_INMUTABLE: conserve la versión consumida.");
        }
    }

    private static async Task<bool> CondicionForecastProtegidaAsync(CondicionLaboralVersion condicion,EntityEntry entrada,
        IQueryable<ForecastSegmentoProyectado> segmentos,CancellationToken ct)
    {
        var ultimo=await segmentos.Where(s=>s.IdCondicionLaboralVersion==condicion.IdCondicionLaboralVersion).MaxAsync(s=>(DateTime?)s.FechaFin,ct);
        // Igual que J: cerrar después del último intervalo consumido permite una versión futura sin alterar historia.
        var soloCierre=entrada.State==EntityState.Modified && entrada.Properties.Where(p=>p.IsModified)
            .All(p=>p.Metadata.Name==nameof(CondicionLaboralVersion.FechaVigenciaFin));
        return ultimo is not null && (!soloCierre || condicion.FechaVigenciaFin is null || condicion.FechaVigenciaFin<ultimo);
    }
}

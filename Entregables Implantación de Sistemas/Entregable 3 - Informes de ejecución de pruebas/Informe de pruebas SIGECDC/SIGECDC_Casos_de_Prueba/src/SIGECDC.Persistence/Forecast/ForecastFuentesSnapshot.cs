using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;

namespace SIGECDC.Persistence.Forecast;

// Se utiliza dentro del lock del escenario y la misma transacción Serializable del cálculo.
internal static class ForecastFuentesSnapshot
{
    internal static async Task<IReadOnlyList<ForecastObservacionHistorica>> ObservarAsync(ApplicationDbContext db,
        ForecastEscenario escenario,IReadOnlyList<ForecastPeriodo> periodos,IReadOnlyList<ForecastParticipante> participantes,CancellationToken ct)
    {
        var tipos=periodos.Select(p=>p.TipoPeriodo).Distinct().ToArray();
        if(tipos.Length!=1)throw new ConfiguracionConceptoException("PERIODOS_FORECAST_NO_COMPARABLES");
        var tipo=tipos[0];
        var candidatas=await db.Planillas.AsNoTracking().Include(p=>p.PeriodoPlanilla).ThenInclude(p=>p!.EstadoPlanilla)
            .Include(p=>p.EstadoPlanilla).Include(p=>p.EjecucionVigente)
            .Where(p=>p.EstadoRegistro=="Activo" && p.PeriodoPlanilla!.EstadoRegistro=="Activo"
                && (p.EstadoPlanilla!.Nombre==EstadosPlanilla.Aprobada || p.EstadoPlanilla.Nombre==EstadosPlanilla.Cerrada)
                && (p.PeriodoPlanilla.EstadoPlanilla!.Nombre==EstadosPlanilla.Aprobada || p.PeriodoPlanilla.EstadoPlanilla.Nombre==EstadosPlanilla.Cerrada)
                && p.PeriodoPlanilla.TipoPeriodo==tipo && p.PeriodoPlanilla.FechaFin<escenario.FechaInicioProyeccion
                && p.EjecucionVigente!=null && p.EjecucionVigente.SnapshotCompleto && p.EjecucionVigente.EstadoEjecucion=="Autorizada"
                && p.EjecucionVigente.EstadoReproducibilidad=="REPRODUCIBLE" && p.EjecucionVigente.IdPlanilla==p.IdPlanilla)
            .OrderByDescending(p=>p.PeriodoPlanilla!.FechaFin).ThenByDescending(p=>p.IdPlanilla).ToArrayAsync(ct);
        var fuentes=await db.ForecastFuentesHistoricas.Where(f=>f.IdForecastEscenario==escenario.IdForecastEscenario).OrderBy(f=>f.NumeroOrden).ToListAsync(ct);
        if(fuentes.Count==0)
        {
            foreach(var p in candidatas.Take(3))fuentes.Add(new(){IdForecastEscenario=escenario.IdForecastEscenario,IdPlanilla=p.IdPlanilla,NumeroOrden=fuentes.Count+1});
            db.ForecastFuentesHistoricas.AddRange(fuentes);await db.SaveChangesAsync(ct);
        }
        var seleccion=candidatas.Where(p=>fuentes.Any(f=>f.IdPlanilla==p.IdPlanilla)).ToArray();
        if(seleccion.Length!=fuentes.Count || seleccion.Select(p=>p.IdPeriodoPlanilla).Distinct().Count()!=seleccion.Length)
            throw new ConfiguracionConceptoException("FUENTE_FORECAST_NO_ELEGIBLE");
        await BloqueoPeriodoPlanilla.ObtenerAsync(db,seleccion.Select(p=>p.IdPeriodoPlanilla),ct);
        foreach(var p in seleccion)
        {
            var e=p.EjecucionVigente!;var periodo=p.PeriodoPlanilla!;
            if(e.FechaInicioSnapshot!=periodo.FechaInicio || e.FechaFinSnapshot!=periodo.FechaFin || e.TipoPeriodoSnapshot!=tipo)
                throw new ConfiguracionConceptoException("FUENTE_FORECAST_SNAPSHOT_INCOHERENTE");
            // Aprobada/Cerrada no pueden reabrirse en J: este FK alcanza una ejecución exacta e inmutable.
            if(!await db.Planillas.AnyAsync(x=>x.IdPlanilla==p.IdPlanilla && x.IdEjecucionPlanillaVigente==e.IdEjecucionPlanilla,ct))
                throw new ConfiguracionConceptoException("FUENTE_FORECAST_DESPLAZADA");
        }
        var previas=await db.ForecastObservacionesHistoricas.Where(o=>o.IdForecastEscenario==escenario.IdForecastEscenario).ToArrayAsync(ct);
        if(previas.Length!=0)throw new ConfiguracionConceptoException("OBSERVACIONES_FORECAST_REQUIEREN_REGENERACION_ATOMICA");
        var observaciones=new List<ForecastObservacionHistorica>();
        var versionesProyectadas=await db.ConceptosPlanillaVersion.AsNoTracking()
            .Where(v=>v.FechaVigenciaInicio<=escenario.FechaFinProyeccion && (v.FechaVigenciaFin==null || v.FechaVigenciaFin>=escenario.FechaInicioProyeccion))
            .ToArrayAsync(ct);
        foreach(var fuente in fuentes)
        {
            var ejecucion=seleccion.Single(p=>p.IdPlanilla==fuente.IdPlanilla).IdEjecucionPlanillaVigente!.Value;
            var detalles=await db.DetallesPlanilla.AsNoTracking().Include(d=>d.Conceptos).ThenInclude(l=>l.ConceptoPlanillaVersion)
                .Where(d=>d.IdEjecucionPlanilla==ejecucion).ToArrayAsync(ct);
            if(detalles.Any(d=>!d.SnapshotCompleto || d.Conceptos.Count==0) || detalles.GroupBy(d=>d.IdColaborador).Any(g=>g.Count()!=1))
                throw new ConfiguracionConceptoException("FUENTE_FORECAST_SNAPSHOT_INCOMPLETO");
            foreach(var persona in participantes.Where(p=>p.IdColaborador is not null))
            {
                var d=detalles.SingleOrDefault(d=>d.IdColaborador==persona.IdColaborador);
                if(d is null)continue; // Ausente no crea observación cero.
                foreach(var grupo in d.Conceptos.GroupBy(l=>l.IdConceptoPlanillaVersion))
                {
                    var version=grupo.First().ConceptoPlanillaVersion
                        ??throw new ConfiguracionConceptoException("CONCEPTO_HISTORICO_NO_DEMOSTRABLE");
                    if(grupo.Any(l=>l.EsSalarialSnapshot!=version.EsSalarial || l.EsContributivoCCSSSnapshot!=version.EsContributivoCCSS
                        || l.EsTributarioSnapshot!=version.EsTributario || l.EsPatronalSnapshot!=version.EsPatronal))
                        throw new ConfiguracionConceptoException("FUENTE_FORECAST_CLASIFICACION_INCOHERENTE");
                    var futuras=versionesProyectadas.Where(v=>v.IdConceptoPlanilla==version.IdConceptoPlanilla).ToArray();
                    var comparable=futuras.Length>0 && futuras.All(v=>v.Clasificacion==version.Clasificacion && v.TipoBase==version.TipoBase
                        && v.UnidadPermitida==version.UnidadPermitida && v.EsSalarial==version.EsSalarial
                        && v.EsContributivoCCSS==version.EsContributivoCCSS && v.EsTributario==version.EsTributario && v.EsPatronal==version.EsPatronal);
                    observaciones.Add(new(){IdForecastEscenario=escenario.IdForecastEscenario,IdForecastParticipante=persona.IdForecastParticipante,
                        IdForecastFuenteHistorica=fuente.IdForecastFuenteHistorica,IdDetallePlanilla=d.IdDetallePlanilla,
                        IdConceptoPlanillaVersion=grupo.Key,MontoObservado=grupo.Sum(l=>l.Monto),EsComparable=comparable,
                        MotivoNoComparable=comparable?null:"La clasificación/base/unidad histórica difiere de la versión proyectada."});
                }
            }
        }
        db.ForecastObservacionesHistoricas.AddRange(observaciones);escenario.TieneHistoricoLimitado=fuentes.Count<3;
        await db.SaveChangesAsync(ct);return observaciones;
    }

    internal static async Task<IReadOnlyList<AutorizacionCostoHistorico>> AutorizacionHistoricaAsync(ApplicationDbContext db,
        IReadOnlyList<ForecastObservacionHistorica> observaciones,bool requerida,CancellationToken ct)
    {
        if(!requerida)return [];
        var evidencias=new List<AutorizacionCostoHistorico>();
        foreach(var o in observaciones.Where(o=>o.EsComparable))
        {
            var lineas=await db.ConceptosDetallePlanilla.AsNoTracking().Where(l=>l.IdDetallePlanilla==o.IdDetallePlanilla
                && l.IdConceptoPlanillaVersion==o.IdConceptoPlanillaVersion).ToArrayAsync(ct);
            if(lineas.Length==0)throw new ConfiguracionConceptoException("AUTORIZACION_HISTORICA_FORECAST_NO_DEMOSTRABLE");
            foreach(var l in lineas)
            {
                var actor=l.AutorizadoPorHecho;var soporte=l.ReferenciaAutorizacion;
                if(l.IdIncidenciaPlanillaConfirmacion is {} id)
                {
                    var f=await db.ConfirmacionesIncidencias.AsNoTracking().SingleAsync(f=>f.IdIncidenciaPlanillaConfirmacion==id,ct);
                    if(!f.AutorizacionRequerida || f.FechaAutorizacion is null || f.IdConceptoPlanillaVersion!=l.IdConceptoPlanillaVersion)
                        throw new ConfiguracionConceptoException("AUTORIZACION_HISTORICA_FORECAST_NO_DEMOSTRABLE");
                    actor=f.AutorizadoPor;soporte=f.ReferenciaAutorizacion;
                }
                if(string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(soporte))
                    throw new ConfiguracionConceptoException("AUTORIZACION_HISTORICA_FORECAST_NO_DEMOSTRABLE");
                evidencias.Add(new(l.IdConceptoDetallePlanilla,actor,soporte));
            }
        }
        // No atribuir a quien genera el escenario la autorización del hecho histórico.
        // La evidencia completa continúa alcanzable por cada observación y detalle J.
        if(evidencias.Count==0)throw new ConfiguracionConceptoException("AUTORIZACION_HISTORICA_FORECAST_NO_DEMOSTRABLE");
        return evidencias.OrderBy(a=>a.IdConceptoDetallePlanilla).ToArray();
    }
}

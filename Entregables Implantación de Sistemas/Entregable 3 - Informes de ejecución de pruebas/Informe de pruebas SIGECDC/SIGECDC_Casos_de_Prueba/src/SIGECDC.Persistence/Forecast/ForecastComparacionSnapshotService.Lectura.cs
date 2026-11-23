using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastComparacionSnapshotService
{
    public async Task<ConsultaComparacionPersistida> LeerAsync(long idEscenario,string actor,CancellationToken ct=default)
    {
        await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var escenario=await BloquearEscenarioAsync(idEscenario,ct);
        var salida=await LeerInternoAsync(escenario,ct);await tx.CommitAsync(ct);return salida;
    }

    // Lee FK ya fijadas, sin buscar otra ejecución por fechas ni consultar dimensiones vivas.
    private async Task<ConsultaComparacionPersistida> LeerInternoAsync(ForecastEscenario escenario,CancellationToken ct)
    {
        var periodos=await db.ForecastPeriodos.AsNoTracking().Where(p=>p.IdForecastEscenario==escenario.IdForecastEscenario).OrderBy(p=>p.NumeroOrden).ToArrayAsync(ct);
        var ids=periodos.Select(p=>p.IdForecastPeriodo).ToArray();
        var cabeceras=await db.ForecastComparacionesReales.AsNoTracking().Where(c=>ids.Contains(c.IdForecastPeriodo)).ToArrayAsync(ct);
        var salida=new List<PeriodoComparacionPersistida>();
        foreach(var periodo in periodos)
        {
            var cab=cabeceras.SingleOrDefault(c=>c.IdForecastPeriodo==periodo.IdForecastPeriodo);
            if(cab is null)
            {
                if(escenario.EstadoEscenario=="Comparado")throw Error("COMPARACION_INCOMPLETA");
                salida.Add(new(0,periodo.IdForecastPeriodo,periodo.TipoPeriodo,periodo.FechaInicio,periodo.FechaFin,null,null,null,"SinEvaluar","ComparacionPendiente",null,default,[]));continue;
            }
            var resultados=await db.ForecastComparacionResultados.AsNoTracking().Where(r=>r.IdForecastComparacionReal==cab.IdForecastComparacionReal).OrderBy(r=>r.NumeroOrden).ToArrayAsync(ct);
            if(cab.Disponibilidad=="SinRealComparable")
            {
                if(cab.IdEjecucionPlanillaReal!=null || resultados.Length!=0 || escenario.EstadoEscenario=="Comparado")throw Error("COMPARACION_INCOHERENTE");
                salida.Add(new(cab.IdForecastComparacionReal,periodo.IdForecastPeriodo,periodo.TipoPeriodo,periodo.FechaInicio,periodo.FechaFin,null,null,null,cab.Disponibilidad,cab.MotivoNoComparable,null,cab.FechaEvaluacion,[]));continue;
            }
            var ejecucion=await db.EjecucionesPlanilla.AsNoTracking().SingleOrDefaultAsync(e=>e.IdEjecucionPlanilla==cab.IdEjecucionPlanillaReal,ct);
            if(escenario.EstadoEscenario!="Comparado" || ejecucion is null || !ejecucion.SnapshotCompleto || ejecucion.EstadoReproducibilidad!="REPRODUCIBLE"
                || resultados.Length==0 || resultados.Count(r=>r.Granularidad=="Total")!=1)throw Error("COMPARACION_INCOMPLETA");
            var total=resultados.Single(r=>r.Granularidad=="Total");
            if(total.MontoReal!=ejecucion.CostoLaboralTotal)throw Error("COMPARACION_NO_CONCILIA");
            foreach(var dimension in new[]{"Participante","Proyecto"})
                if(resultados.Where(r=>r.Granularidad==dimension).Sum(r=>r.MontoForecast??0)!=total.MontoForecast
                    || resultados.Where(r=>r.Granularidad==dimension).Sum(r=>r.MontoReal??0)!=total.MontoReal)throw Error("COMPARACION_NO_CONCILIA");
            var rids=resultados.Select(r=>r.IdForecastComparacionResultado).ToArray();
            var fuentes=await db.ForecastComparacionFuentes.AsNoTracking().Where(f=>rids.Contains(f.IdForecastComparacionResultado)).ToArrayAsync(ct);
            var fs=await db.ForecastDetalles.AsNoTracking().Where(d=>d.IdForecastPeriodo==periodo.IdForecastPeriodo).ToDictionaryAsync(d=>d.IdForecastDetalle,ct);
            var fids=fs.Keys.ToArray();
            var apps=await db.ForecastAplicacionesFinancieras.AsNoTracking().Where(a=>fids.Contains(a.IdForecastDetalle) && a.Componente=="DISTRIBUCION").ToDictionaryAsync(a=>a.IdForecastDetalle,ct);
            var personasIds=fs.Values.Select(d=>d.IdForecastParticipante).Distinct().ToArray();
            var personas=await db.ForecastParticipantes.AsNoTracking().Where(p=>personasIds.Contains(p.IdForecastParticipante)).ToDictionaryAsync(p=>p.IdForecastParticipante,ct);
            var asignacionesIds=fs.Values.Select(d=>d.IdForecastAsignacionProyecto).Distinct().ToArray();
            var asignaciones=await db.ForecastAsignacionesProyecto.AsNoTracking().Where(a=>asignacionesIds.Contains(a.IdForecastAsignacionProyecto)).ToDictionaryAsync(a=>a.IdForecastAsignacionProyecto,ct);
            var ds=await db.DetallesPlanilla.AsNoTracking().Where(d=>d.IdEjecucionPlanilla==ejecucion.IdEjecucionPlanilla).ToDictionaryAsync(d=>d.IdDetallePlanilla,ct);
            var dids=ds.Keys.ToArray();
            var ls=await db.ConceptosDetallePlanilla.AsNoTracking().Where(l=>dids.Contains(l.IdDetallePlanilla)).ToDictionaryAsync(l=>l.IdConceptoDetallePlanilla,ct);
            var destinos=await db.DistribucionesProyectoDetallePlanilla.AsNoTracking().Where(d=>dids.Contains(d.IdDetallePlanilla)).ToDictionaryAsync(d=>d.IdDetallePlanillaDistribucionProyecto,ct);
            var versionesIds=fs.Values.Select(d=>d.IdConceptoPlanillaVersion).Concat(ls.Values.Select(l=>(long?)l.IdConceptoPlanillaVersion)).Distinct().ToArray();
            var conceptos=await db.ConceptosPlanillaVersion.AsNoTracking().Where(v=>versionesIds.Contains(v.IdConceptoPlanillaVersion))
                .ToDictionaryAsync(v=>v.IdConceptoPlanillaVersion,v=>v.IdConceptoPlanilla,ct);
            // Cada granularidad conserva el conjunto completo de fuentes, incluso importes cero.
            // Una suma correcta no demuestra identidad ni evita omisiones/duplicados compensados.
            static bool MismoConjunto(IEnumerable<long> esperado,IEnumerable<long> observado)
                =>esperado.Order().SequenceEqual(observado.Order());
            foreach(var dimension in new[]{"Total","Participante","Concepto","Proyecto"})
            {
                var propios=resultados.Where(r=>r.Granularidad==dimension).Select(r=>r.IdForecastComparacionResultado).ToHashSet();
                var evidencias=fuentes.Where(f=>propios.Contains(f.IdForecastComparacionResultado)).ToArray();
                if(!MismoConjunto(fids,evidencias.Where(f=>f.IdForecastDetalle!=null).Select(f=>f.IdForecastDetalle!.Value)))throw Error("COMPARACION_PROCEDENCIA_INCOMPLETA");
                IEnumerable<long> esperados=dimension=="Concepto"?ls.Keys:dimension=="Proyecto"?destinos.Keys:ds.Keys;
                var observados=dimension=="Concepto"?evidencias.Where(f=>f.IdConceptoDetallePlanilla!=null).Select(f=>f.IdConceptoDetallePlanilla!.Value):
                    dimension=="Proyecto"?evidencias.Where(f=>f.IdDetallePlanillaDistribucionProyecto!=null).Select(f=>f.IdDetallePlanillaDistribucionProyecto!.Value):
                    evidencias.Where(f=>f.IdDetallePlanilla!=null).Select(f=>f.IdDetallePlanilla!.Value);
                if(!MismoConjunto(esperados,observados))throw Error("COMPARACION_PROCEDENCIA_INCOMPLETA");
            }
            var segmentosF=await db.ForecastSegmentosProyectados.AsNoTracking().Where(s=>s.IdForecastPeriodo==periodo.IdForecastPeriodo).OrderBy(s=>s.FechaInicio).ToArrayAsync(ct);
            var segmentosR=await db.SegmentosCalculoPlanilla.AsNoTracking().Where(s=>dids.Contains(s.IdDetallePlanilla)).OrderBy(s=>s.FechaInicio).ToArrayAsync(ct);
            string Unir(IEnumerable<string?> etiquetas)=>string.Join(" / ",etiquetas.Where(s=>!string.IsNullOrWhiteSpace(s)).Distinct().Order(StringComparer.Ordinal));
            var filas=new List<FilaComparacionPersistida>();
            foreach(var r in resultados)
            {
                var ff=fuentes.Where(f=>f.IdForecastComparacionResultado==r.IdForecastComparacionResultado).OrderBy(f=>f.Lado,StringComparer.Ordinal)
                    .ThenBy(f=>f.IdForecastDetalle??f.IdDetallePlanilla??f.IdConceptoDetallePlanilla??f.IdDetallePlanillaDistribucionProyecto).ToArray();
                if(r.TieneForecast!=ff.Any(f=>f.Lado=="Forecast") || r.TieneReal!=ff.Any(f=>f.Lado=="Real"))throw Error("COMPARACION_PROCEDENCIA_INCOMPLETA");
                var fd=ff.Where(f=>f.IdForecastDetalle!=null).Select(f=>fs.GetValueOrDefault(f.IdForecastDetalle!.Value)??throw Error("COMPARACION_FUENTE_INVALIDA")).ToArray();
                var rd=ff.Where(f=>f.IdDetallePlanilla!=null).Select(f=>ds.GetValueOrDefault(f.IdDetallePlanilla!.Value)??throw Error("COMPARACION_FUENTE_INVALIDA")).ToArray();
                var rl=ff.Where(f=>f.IdConceptoDetallePlanilla!=null).Select(f=>ls.GetValueOrDefault(f.IdConceptoDetallePlanilla!.Value)??throw Error("COMPARACION_FUENTE_INVALIDA")).ToArray();
                var rp=ff.Where(f=>f.IdDetallePlanillaDistribucionProyecto!=null).Select(f=>destinos.GetValueOrDefault(f.IdDetallePlanillaDistribucionProyecto!.Value)??throw Error("COMPARACION_FUENTE_INVALIDA")).ToArray();
                if(fd.Any(d=>d.IdForecastPeriodo!=periodo.IdForecastPeriodo) || fd.Any(d=>!apps.ContainsKey(d.IdForecastDetalle)))throw Error("COMPARACION_FUENTE_INVALIDA");
                var identidadValida=r.Granularidad switch
                {
                    "Total"=>rl.Length==0 && rp.Length==0,
                    "Participante"=>rl.Length==0 && rp.Length==0 && rd.Length<=1
                        && fd.All(d=>d.IdForecastParticipante==r.IdForecastParticipante && personas[d.IdForecastParticipante].IdColaborador==r.IdColaborador)
                        && rd.All(d=>d.IdColaborador==r.IdColaborador),
                    "Concepto"=>rd.Length==0 && rp.Length==0
                        && fd.All(d=>d.IdConceptoPlanillaVersion is long id && conceptos.GetValueOrDefault(id)==r.IdConceptoPlanilla)
                        && rl.All(l=>conceptos.GetValueOrDefault(l.IdConceptoPlanillaVersion)==r.IdConceptoPlanilla),
                    "Proyecto"=>rd.Length==0 && rl.Length==0
                        && fd.All(d=>d.IdForecastAsignacionProyecto is long id && asignaciones.TryGetValue(id,out var a) && a.IdProyecto==r.IdProyecto && a.EsSinAsignar==r.EsSinAsignar)
                        && rp.All(d=>d.IdProyecto==r.IdProyecto && d.EsSinAsignar==r.EsSinAsignar),
                    _=>false
                };
                if(!identidadValida)throw Error("COMPARACION_IDENTIDAD_FUENTE_INCOHERENTE");
                var montoF=fd.Sum(d=>d.MontoProyectado*(r.Granularidad=="Concepto"?1:ForecastSnapshotEscritura.Signo(apps[d.IdForecastDetalle].Efecto)));
                var montoR=rd.Sum(d=>d.CostoLaboralTotal)+rl.Sum(l=>l.Monto)+rp.Sum(d=>d.MontoReal);
                if(r.TieneForecast && montoF!=r.MontoForecast || r.TieneReal && montoR!=r.MontoReal)throw Error("COMPARACION_NO_CONCILIA");
                var fc="";var fn="";var rc="";var rn="";
                switch(r.Granularidad)
                {
                    case "Total":fc=rc="CTL";fn=rn="Costo laboral total";break;
                    case "Participante":fc=Unir(fd.Select(d=>personas[d.IdForecastParticipante].CodigoParticipante));fn=Unir(fd.Select(d=>personas[d.IdForecastParticipante].Etiqueta));rc=Unir(rd.Select(d=>d.CodigoColaborador));rn=Unir(rd.Select(d=>d.NombreCompleto));break;
                    case "Concepto":fc=Unir(fd.Select(d=>d.Concepto));fn=Unir(fd.Select(d=>apps[d.IdForecastDetalle].DescripcionConcepto));rc=rl.Length>0?"Concepto "+r.IdConceptoPlanilla:"";rn=Unir(rl.Select(l=>l.DescripcionSnapshot));break;
                    case "Proyecto":fc=Unir(fd.Select(d=>asignaciones[d.IdForecastAsignacionProyecto!.Value] is var a && a.EsSinAsignar?"SIN_ASIGNAR":a.CodigoProyectoSnapshot));fn=Unir(fd.Select(d=>asignaciones[d.IdForecastAsignacionProyecto!.Value].NombreProyectoSnapshot));rc=Unir(rp.Select(d=>d.CodigoProyectoSnapshot));rn=Unir(rp.Select(d=>d.NombreProyectoSnapshot));break;
                    default:throw Error("COMPARACION_DIMENSION_INVALIDA");
                }
                var fpIds=fd.Select(d=>d.IdForecastParticipante).ToHashSet();var rpIds=rd.Select(d=>d.IdDetallePlanilla).ToHashSet();
                filas.Add(new(r.IdForecastComparacionResultado,r.Granularidad,fc,fn,rc,rn,r.TieneForecast,r.TieneReal,r.MontoForecast,r.MontoReal,r.DiferenciaFirmada,r.DiferenciaAbsoluta,r.DiferenciaPorcentual,r.MotivoNoComparable,
                    ff.Select(f=>new FuenteComparacion(f.IdForecastDetalle,f.IdDetallePlanilla,f.IdConceptoDetallePlanilla,f.IdDetallePlanillaDistribucionProyecto,f.Lado)).ToArray())
                {Dimensiones=r.Granularidad!="Participante"?[]:segmentosF.Where(s=>fpIds.Contains(s.IdForecastParticipante)).Select(s=>new DimensionComparacion("Forecast",s.FechaInicio,s.FechaFin,s.NombrePuesto,s.NombreDepartamento,s.NombreJornada,s.SalarioMensual))
                    .Concat(segmentosR.Where(s=>rpIds.Contains(s.IdDetallePlanilla)).Select(s=>new DimensionComparacion("Real",s.FechaInicio,s.FechaFin,s.NombrePuesto!,s.NombreDepartamento!,s.CodigoJornada,s.SalarioMensual))).ToArray()});
            }
            salida.Add(new(cab.IdForecastComparacionReal,periodo.IdForecastPeriodo,periodo.TipoPeriodo,periodo.FechaInicio,periodo.FechaFin,ejecucion.IdEjecucionPlanilla,ejecucion.NumeroVersion,ejecucion.VersionMotor,
                cab.Disponibilidad,cab.MotivoNoComparable,cab.EstadoPlanillaObservado,cab.FechaEvaluacion,filas));
        }
        if(escenario.EstadoEscenario=="Comparado" && salida.Sum(p=>p.Filas.Single(f=>f.Granularidad=="Total").MontoForecast??0)!=escenario.MontoProyectadoTotal)throw Error("COMPARACION_NO_CONCILIA");
        return new(escenario.IdForecastEscenario,escenario.Nombre,escenario.EstadoEscenario,salida);
    }
}

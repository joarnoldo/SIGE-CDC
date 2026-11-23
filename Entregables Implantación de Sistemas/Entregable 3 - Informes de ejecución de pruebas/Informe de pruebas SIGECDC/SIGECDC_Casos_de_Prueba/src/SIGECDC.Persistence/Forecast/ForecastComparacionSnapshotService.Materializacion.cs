using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Planillas;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastComparacionSnapshotService
{
    private sealed record DatosComparacion(ForecastDetalle[] Forecast,Dictionary<long,ForecastAplicacionFinanciera> Aplicaciones,
        Dictionary<long,ForecastParticipante> Personas,Dictionary<long,ForecastAsignacionProyecto> Destinos,
        Dictionary<long,ConceptoPlanillaVersion> Versiones,EjecucionPlanilla Real,DetallePlanillaDistribucionProyecto[] Distribuciones);

    private async Task<DatosComparacion> CargarFuentesAsync(ForecastPeriodo periodo,long idReal,string actor,CancellationToken ct)
    {
        var real=await new GobiernoFinancieroService(db).LeerSnapshotAsync(idReal,actor,ct);
        if(!real.SnapshotCompleto || real.EstadoReproducibilidad!="REPRODUCIBLE" || real.EstadoEjecucion!="Autorizada"
            || real.TipoPeriodoSnapshot!=periodo.TipoPeriodo || real.FechaInicioSnapshot!=periodo.FechaInicio || real.FechaFinSnapshot!=periodo.FechaFin)
            throw Error("REAL_SNAPSHOT_INCOMPATIBLE");
        PublicacionPlanilla.Conciliar(real);
        if(real.Detalles.GroupBy(d=>d.IdColaborador).Any(g=>g.Count()!=1))throw Error("IDENTIDAD_REAL_AMBIGUA");
        var forecast=await db.ForecastDetalles.AsNoTracking().Where(d=>d.IdForecastPeriodo==periodo.IdForecastPeriodo).ToArrayAsync(ct);
        var ids=forecast.Select(d=>d.IdForecastDetalle).ToArray();
        var apps=await db.ForecastAplicacionesFinancieras.AsNoTracking().Where(a=>ids.Contains(a.IdForecastDetalle) && a.Componente=="DISTRIBUCION").ToArrayAsync(ct);
        if(forecast.Length==0 || apps.Length!=forecast.Length || apps.GroupBy(a=>a.IdForecastDetalle).Any(g=>g.Count()!=1)
            || forecast.Any(d=>d.IdConceptoPlanillaVersion is null || d.IdForecastAsignacionProyecto is null || d.VersionMotor!=ProyeccionFinanciera.VersionMotor))throw Error("FORECAST_SNAPSHOT_INCOMPLETO");
        var personas=await db.ForecastParticipantes.AsNoTracking().Where(p=>p.IdForecastEscenario==periodo.IdForecastEscenario).ToDictionaryAsync(p=>p.IdForecastParticipante,ct);
        var usados=forecast.Select(d=>d.IdForecastParticipante).ToHashSet();
        if(personas.Values.Where(p=>usados.Contains(p.IdForecastParticipante) && p.IdColaborador!=null).GroupBy(p=>p.IdColaborador).Any(g=>g.Count()!=1))throw Error("IDENTIDAD_FORECAST_AMBIGUA");
        var destinos=await db.ForecastAsignacionesProyecto.AsNoTracking().Where(a=>a.IdForecastPeriodo==periodo.IdForecastPeriodo).ToDictionaryAsync(a=>a.IdForecastAsignacionProyecto,ct);
        foreach(var grupo in forecast.GroupBy(d=>d.IdForecastParticipante))
        {
            var propios=destinos.Values.Where(a=>a.IdForecastParticipante==grupo.Key).ToArray();
            if(propios.Length==0 || propios.Sum(a=>a.Porcentaje)!=100 || propios.Any(a=>string.IsNullOrWhiteSpace(a.NombreProyectoSnapshot)
                || !a.EsSinAsignar && string.IsNullOrWhiteSpace(a.CodigoProyectoSnapshot)) || grupo.Any(d=>!destinos.ContainsKey(d.IdForecastAsignacionProyecto!.Value)
                    || destinos[d.IdForecastAsignacionProyecto!.Value].IdForecastParticipante!=d.IdForecastParticipante))throw Error("COMPARACION_DISTRIBUCION_NO_CONCILIA");
        }
        var versionesIds=forecast.Select(d=>d.IdConceptoPlanillaVersion!.Value).Concat(real.Detalles.SelectMany(d=>d.Conceptos).Select(l=>l.IdConceptoPlanillaVersion)).Distinct().ToArray();
        var versiones=await db.ConceptosPlanillaVersion.AsNoTracking().Where(v=>versionesIds.Contains(v.IdConceptoPlanillaVersion)).ToDictionaryAsync(v=>v.IdConceptoPlanillaVersion,ct);
        if(versiones.Count!=versionesIds.Length)throw Error("CONCEPTO_COMPARABLE_NO_DEMOSTRABLE");
        // Reutiliza la conciliación K sobre importes persistidos; no invoca cálculo D–I ni genera metadata.
        foreach(var d in real.Detalles)PublicacionPlanilla.Proyectar(real,d,versiones,"Aprobada",new ColillaPago{IdDetallePlanilla=d.IdDetallePlanilla},true);
        var realesIds=real.Detalles.Select(d=>d.IdDetallePlanilla).ToArray();
        var distribuciones=await db.DistribucionesProyectoDetallePlanilla.AsNoTracking().Where(d=>realesIds.Contains(d.IdDetallePlanilla)).ToArrayAsync(ct);
        foreach(var detalle in real.Detalles)
        {
            var ds=distribuciones.Where(d=>d.IdDetallePlanilla==detalle.IdDetallePlanilla).OrderBy(d=>d.NumeroOrden).ToArray();
            if(ds.Length==0 || ds.Sum(d=>d.Porcentaje)!=100 || ds.Sum(d=>d.MontoReal)!=detalle.CostoLaboralTotal
                || ds.Any(d=>string.IsNullOrWhiteSpace(d.CodigoProyectoSnapshot) || string.IsNullOrWhiteSpace(d.NombreProyectoSnapshot)
                    || d.EsSinAsignar && (d.IdProyecto!=null || d.CodigoProyectoSnapshot!="SIN_ASIGNAR" || d.NombreProyectoSnapshot!="Sin asignar")))throw Error("COMPARACION_DISTRIBUCION_NO_CONCILIA");
            var comprobacion=ProyeccionFinanciera.Distribuir(detalle.CostoLaboralTotal,ds.Select(d=>new DestinoCostoForecast(d.IdDetallePlanillaDistribucionProyecto,d.IdProyecto,d.EsSinAsignar,d.Porcentaje)).ToArray());
            if(comprobacion.Any(v=>ds.Single(d=>d.IdDetallePlanillaDistribucionProyecto==v.Destino.IdAsignacion) is var d
                && (d.MontoReal!=v.Monto || d.MontoSinRedondear!=v.SinRedondear)))throw Error("COMPARACION_DISTRIBUCION_NO_CONCILIA");
        }
        return new(forecast,apps.ToDictionary(a=>a.IdForecastDetalle),personas,destinos,versiones,real,distribuciones);
    }

    private async Task MaterializarAsync(ForecastComparacionReal cab,ForecastPeriodo periodo,EjecucionPlanilla real,string actor,CancellationToken ct)
    {
        var datos=await CargarFuentesAsync(periodo,real.IdEjecucionPlanilla,actor,ct);
        var pendientes=new List<(ForecastComparacionResultado R,ForecastComparacionResultadoFuente[] Fuentes)>();
        decimal Costo(IEnumerable<ForecastDetalle> ds)=>ds.Sum(d=>d.MontoProyectado*ForecastSnapshotEscritura.Signo(datos.Aplicaciones[d.IdForecastDetalle].Efecto));
        ForecastComparacionResultadoFuente F(ForecastDetalle d)=>new(){Lado="Forecast",IdForecastDetalle=d.IdForecastDetalle};
        ForecastComparacionResultadoFuente R(DetallePlanilla d)=>new(){Lado="Real",IdDetallePlanilla=d.IdDetallePlanilla};
        void Agregar(string tipo,decimal? f,decimal? r,IEnumerable<ForecastComparacionResultadoFuente> fuentes,
            long? participante=null,long? colaborador=null,long? concepto=null,long? proyecto=null,bool sinAsignar=false)
        {
            var diferencia=ComparacionFinanciera.Comparar(f,r);
            pendientes.Add((new(){IdForecastComparacionReal=cab.IdForecastComparacionReal,Granularidad=tipo,NumeroOrden=pendientes.Count+1,
                IdForecastParticipante=participante,IdColaborador=colaborador,IdConceptoPlanilla=concepto,IdProyecto=proyecto,EsSinAsignar=sinAsignar,
                TieneForecast=f.HasValue,TieneReal=r.HasValue,MontoForecast=f,MontoReal=r,DiferenciaFirmada=diferencia.Firmada,DiferenciaAbsoluta=diferencia.Absoluta,
                DiferenciaPorcentual=diferencia.Porcentual,MotivoNoComparable=diferencia.Motivo},fuentes.ToArray()));
        }
        Agregar("Total",Costo(datos.Forecast),real.CostoLaboralTotal,datos.Forecast.Select(F).Concat(datos.Real.Detalles.Select(R)));
        var emparejados=new HashSet<long>();
        foreach(var grupo in datos.Forecast.GroupBy(d=>d.IdForecastParticipante).OrderBy(g=>g.Key))
        {
            var persona=datos.Personas[grupo.Key];var d=datos.Real.Detalles.SingleOrDefault(d=>d.IdColaborador==persona.IdColaborador);
            if(d is not null)emparejados.Add(d.IdDetallePlanilla);
            Agregar("Participante",Costo(grupo),d?.CostoLaboralTotal,grupo.Select(F).Concat(d is null?[]:new[]{R(d)}),grupo.Key,d?.IdColaborador??persona.IdColaborador);
        }
        foreach(var d in datos.Real.Detalles.Where(d=>!emparejados.Contains(d.IdDetallePlanilla)).OrderBy(d=>d.IdColaborador))
            Agregar("Participante",null,d.CostoLaboralTotal,[R(d)],colaborador:d.IdColaborador);
        var lineas=datos.Real.Detalles.SelectMany(d=>d.Conceptos).ToArray();
        var conceptos=datos.Forecast.Select(d=>datos.Versiones[d.IdConceptoPlanillaVersion!.Value].IdConceptoPlanilla)
            .Concat(lineas.Select(l=>datos.Versiones[l.IdConceptoPlanillaVersion].IdConceptoPlanilla)).Distinct().Order();
        foreach(var id in conceptos)
        {
            var fs=datos.Forecast.Where(d=>datos.Versiones[d.IdConceptoPlanillaVersion!.Value].IdConceptoPlanilla==id).ToArray();
            var rs=lineas.Where(l=>datos.Versiones[l.IdConceptoPlanillaVersion].IdConceptoPlanilla==id).ToArray();
            Agregar("Concepto",fs.Length==0?null:fs.Sum(d=>d.MontoProyectado),rs.Length==0?null:rs.Sum(l=>l.Monto),
                fs.Select(F).Concat(rs.Select(l=>new ForecastComparacionResultadoFuente{Lado="Real",IdConceptoDetallePlanilla=l.IdConceptoDetallePlanilla})),concepto:id);
        }
        var proyectos=datos.Forecast.Select(d=>datos.Destinos[d.IdForecastAsignacionProyecto!.Value].IdProyecto??0)
            .Concat(datos.Distribuciones.Select(d=>d.IdProyecto??0)).Distinct().Order();
        foreach(var id in proyectos)
        {
            var fs=datos.Forecast.Where(d=>(datos.Destinos[d.IdForecastAsignacionProyecto!.Value].IdProyecto??0)==id).ToArray();
            var rs=datos.Distribuciones.Where(d=>(d.IdProyecto??0)==id).ToArray();
            Agregar("Proyecto",fs.Length==0?null:Costo(fs),rs.Length==0?null:rs.Sum(d=>d.MontoReal),
                fs.Select(F).Concat(rs.Select(d=>new ForecastComparacionResultadoFuente{Lado="Real",IdDetallePlanillaDistribucionProyecto=d.IdDetallePlanillaDistribucionProyecto})),proyecto:id==0?null:id,sinAsignar:id==0);
        }
        var total=pendientes[0].R;
        foreach(var dimension in new[]{"Participante","Proyecto"})
            if(pendientes.Where(p=>p.R.Granularidad==dimension).Sum(p=>p.R.MontoForecast??0)!=total.MontoForecast
                || pendientes.Where(p=>p.R.Granularidad==dimension).Sum(p=>p.R.MontoReal??0)!=total.MontoReal)throw Error("COMPARACION_DISTRIBUCION_NO_CONCILIA");
        db.ForecastComparacionResultados.AddRange(pendientes.Select(p=>p.R));await db.SaveChangesAsync(ct);
        foreach(var (r,fuentes) in pendientes)foreach(var f in fuentes){f.IdForecastComparacionResultado=r.IdForecastComparacionResultado;db.ForecastComparacionFuentes.Add(f);}
        await db.SaveChangesAsync(ct);
        Auditar("FIJAR_REAL_FORECAST",actor,cab.IdForecastComparacionReal,new{Actor=actor,periodo.IdForecastEscenario,periodo.IdForecastPeriodo,cab.IdForecastComparacionReal,
            real.IdEjecucionPlanilla,real.NumeroVersion,cab.EstadoPlanillaObservado,Resultados=pendientes.Count,Fuentes=pendientes.Sum(p=>p.Fuentes.Length),
            ForecastCTL=total.MontoForecast,RealCTL=total.MontoReal,Destinos=pendientes.Where(p=>p.R.Granularidad=="Proyecto").Select(p=>new{p.R.IdProyecto,p.R.EsSinAsignar,p.R.MontoForecast,p.R.MontoReal}).ToArray()});
    }
}

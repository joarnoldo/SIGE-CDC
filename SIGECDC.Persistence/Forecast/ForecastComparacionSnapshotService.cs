using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastComparacionSnapshotService(ApplicationDbContext db):IForecastComparacionSnapshotService
{
    private static ConfiguracionConceptoException Error(string codigo)=>new(codigo);
    private async Task<ForecastEscenario> BloquearEscenarioAsync(long id,CancellationToken ct)
    {
        var e=(await db.ForecastEscenarios.FromSqlInterpolated($"SELECT * FROM ForecastEscenario WHERE IdForecastEscenario={id} FOR UPDATE").AsNoTracking().ToArrayAsync(ct)).SingleOrDefault()
            ??throw Error("FORECAST_NO_DISPONIBLE");
        if(e.EstadoRegistro!="Activo" || e.VersionMotor!=ProyeccionFinanciera.VersionMotor || e.EstadoEscenario is not ("Guardado" or "Comparado"))
            throw Error("FORECAST_COMPARACION_NO_ELEGIBLE");
        return e;
    }
    private void Auditar(string accion,string actor,long id,object evidencia)=>db.BitacoraAuditoria.Add(new(){IdUsuario=actor,FechaHora=DateTime.UtcNow,
        Accion=accion,Entidad="ForecastComparacionReal",IdRegistro=id.ToString(CultureInfo.InvariantCulture),ValoresNuevos=JsonSerializer.Serialize(evidencia),Observacion="Actor: "+actor+"; FIN-02 contenido."});

    public async Task<DistribucionRealConsulta> ConsultarDistribucionAsync(long idDetalle,string actor,CancellationToken ct=default)
    {
        await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var d=await db.DetallesPlanilla.AsNoTracking().SingleOrDefaultAsync(d=>d.IdDetallePlanilla==idDetalle,ct)??throw Error("REAL_NO_ELEGIBLE");
        var p=await db.Planillas.AsNoTracking().Include(p=>p.EstadoPlanilla).Include(p=>p.PeriodoPlanilla).ThenInclude(p=>p!.EstadoPlanilla).SingleAsync(p=>p.IdPlanilla==d.IdPlanilla,ct);
        var e=await db.EjecucionesPlanilla.AsNoTracking().SingleAsync(e=>e.IdEjecucionPlanilla==d.IdEjecucionPlanilla,ct);ExigirReal(p,e);
        if(!d.SnapshotCompleto)throw Error("REAL_SNAPSHOT_INCOMPLETO");
        var consumida=await db.ForecastComparacionesReales.AnyAsync(c=>c.IdEjecucionPlanillaReal==e.IdEjecucionPlanilla && c.Disponibilidad=="Disponible",ct);
        var destinos=await db.DistribucionesProyectoDetallePlanilla.AsNoTracking().Where(x=>x.IdDetallePlanilla==idDetalle).OrderBy(x=>x.NumeroOrden).ToArrayAsync(ct);
        var opciones=consumida?[]:await db.Proyectos.AsNoTracking().Where(x=>x.EstadoRegistro=="Activo").OrderBy(x=>x.CodigoProyecto).Select(x=>new ProyectoDistribucionReal(x.IdProyecto,x.CodigoProyecto,x.NombreProyecto)).ToListAsync(ct);
        // Si está consumida se muestran exclusivamente las etiquetas conservadas; nunca sustituciones de nombres vivos.
        if(consumida)opciones=destinos.Where(x=>x.IdProyecto!=null).Select(x=>new ProyectoDistribucionReal(x.IdProyecto!.Value,x.CodigoProyectoSnapshot!,x.NombreProyectoSnapshot!)).ToList();
        else foreach(var x in destinos.Where(x=>x.IdProyecto!=null && opciones.All(o=>o.Id!=x.IdProyecto)))opciones.Add(new(x.IdProyecto!.Value,x.CodigoProyectoSnapshot!,x.NombreProyectoSnapshot!));
        var r=new DistribucionRealConsulta(d.IdDetallePlanilla,e.IdEjecucionPlanilla,e.NumeroVersion,d.NombreCompleto!,p.EstadoPlanilla!.Nombre,d.CostoLaboralTotal,consumida,
            destinos.Select(x=>new DestinoDistribucionReal(x.IdProyecto,x.EsSinAsignar,x.Porcentaje)).ToArray(),opciones);
        await tx.CommitAsync(ct);return r;
    }

    public async Task RegistrarDistribucionAsync(SolicitudDistribucionReal s,string actor,CancellationToken ct=default)
    {
        actor=await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(s.Destinos);
        if(string.IsNullOrWhiteSpace(s.ReferenciaSoporte) || s.ReferenciaSoporte.Length>500)throw Error("DISTRIBUCION_REAL_SIN_SOPORTE");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var periodo=await db.DetallesPlanilla.Where(d=>d.IdDetallePlanilla==s.IdDetallePlanilla).Select(d=>d.Planilla!.IdPeriodoPlanilla).SingleAsync(ct);
            await BloqueoPeriodoPlanilla.ObtenerAsync(db,periodo,ct);
            var detalle=(await db.DetallesPlanilla.FromSqlInterpolated($"SELECT * FROM DetallePlanilla WHERE IdDetallePlanilla={s.IdDetallePlanilla} FOR UPDATE").AsNoTracking().ToArrayAsync(ct)).Single();
            var planilla=await db.Planillas.AsNoTracking().Include(p=>p.EstadoPlanilla).Include(p=>p.PeriodoPlanilla).ThenInclude(p=>p!.EstadoPlanilla).SingleAsync(p=>p.IdPlanilla==detalle.IdPlanilla,ct);
            var ejecucion=await db.EjecucionesPlanilla.AsNoTracking().SingleAsync(e=>e.IdEjecucionPlanilla==detalle.IdEjecucionPlanilla,ct);
            ExigirReal(planilla,ejecucion);
            if(!detalle.SnapshotCompleto)throw Error("REAL_SNAPSHOT_INCOMPLETO");
            if(await db.ForecastComparacionesReales.AnyAsync(c=>c.IdEjecucionPlanillaReal==ejecucion.IdEjecucionPlanilla && c.Disponibilidad=="Disponible",ct))throw Error("DISTRIBUCION_REAL_CONSUMIDA");
            var destinos=s.Destinos.OrderBy(d=>d.IdProyecto??0).Select((d,i)=>new DestinoCostoForecast(i+1,d.IdProyecto,d.EsSinAsignar,d.Porcentaje)).ToArray();
            var valores=ProyeccionFinanciera.Distribuir(detalle.CostoLaboralTotal,destinos);
            var ids=destinos.Where(d=>d.IdProyecto.HasValue).Select(d=>d.IdProyecto!.Value).ToArray();
            var proyectos=await db.Proyectos.AsNoTracking().Where(p=>ids.Contains(p.IdProyecto) && p.EstadoRegistro=="Activo").ToDictionaryAsync(p=>p.IdProyecto,ct);
            if(proyectos.Count!=ids.Length)throw Error("PROYECTO_REAL_NO_DISPONIBLE");
            var anteriores=await db.DistribucionesProyectoDetallePlanilla.Where(d=>d.IdDetallePlanilla==detalle.IdDetallePlanilla).ToArrayAsync(ct);
            db.DistribucionesProyectoDetallePlanilla.RemoveRange(anteriores);await db.SaveChangesAsync(ct);
            foreach(var v in valores)db.DistribucionesProyectoDetallePlanilla.Add(new(){IdDetallePlanilla=detalle.IdDetallePlanilla,IdProyecto=v.Destino.IdProyecto,EsSinAsignar=v.Destino.SinAsignar,
                Porcentaje=v.Destino.Porcentaje,MontoSinRedondear=v.SinRedondear,MontoReal=v.Monto,NumeroOrden=Array.IndexOf(destinos,v.Destino)+1,
                CodigoProyectoSnapshot=v.Destino.IdProyecto is {} id?proyectos[id].CodigoProyecto:"SIN_ASIGNAR",NombreProyectoSnapshot=v.Destino.IdProyecto is {} pid?proyectos[pid].NombreProyecto:"Sin asignar"});
            Auditar(anteriores.Length==0?"AUTORIZAR_DISTRIBUCION_REAL":"CORREGIR_DISTRIBUCION_REAL",actor,detalle.IdDetallePlanilla,
                new{Actor=actor,detalle.IdDetallePlanilla,ejecucion.IdEjecucionPlanilla,detalle.CostoLaboralTotal,s.ReferenciaSoporte,Destinos=s.Destinos,SinAsignar=valores.Where(v=>v.Destino.SinAsignar).Sum(v=>v.Monto)});
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        }
        catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
    }

    private static void ExigirReal(Planilla p,EjecucionPlanilla e)
    {
        if(p.EstadoRegistro!="Activo" || p.PeriodoPlanilla?.EstadoRegistro!="Activo" || p.EstadoPlanilla?.Nombre is not ("Aprobada" or "Cerrada")
            || p.PeriodoPlanilla.EstadoPlanilla?.Nombre!=p.EstadoPlanilla.Nombre || p.IdEjecucionPlanillaVigente!=e.IdEjecucionPlanilla || e.IdPlanilla!=p.IdPlanilla
            || e.EstadoEjecucion!="Autorizada" || !e.SnapshotCompleto || e.EstadoReproducibilidad!="REPRODUCIBLE" || e.VersionMotor!="FIN-DI-V1")throw Error("REAL_NO_ELEGIBLE");
    }

    public async Task<ConsultaComparacionPersistida> CompararAsync(long idEscenario,string actor,CancellationToken ct=default)
    {
        actor=await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var escenario=await BloquearEscenarioAsync(idEscenario,ct);
            if(escenario.EstadoEscenario=="Comparado"){var existente=await LeerInternoAsync(escenario,ct);await tx.CommitAsync(ct);return existente;}
            await ForecastSnapshotEscritura.ConciliarAsync(db,idEscenario,escenario.MontoProyectadoTotal,ct);
            var periodos=await db.ForecastPeriodos.AsNoTracking().Where(p=>p.IdForecastEscenario==idEscenario && p.EstadoRegistro=="Activo").OrderBy(p=>p.NumeroOrden).ToArrayAsync(ct);
            if(periodos.Length==0)throw Error("FORECAST_SIN_PERIODOS");
            var reales=new Dictionary<long,(Planilla P,EjecucionPlanilla E)>();
            foreach(var periodo in periodos)
            {
                var candidatos=await db.Planillas.AsNoTracking().Include(p=>p.EstadoPlanilla).Include(p=>p.PeriodoPlanilla).ThenInclude(p=>p!.EstadoPlanilla).Include(p=>p.EjecucionVigente)
                    .Where(p=>p.PeriodoPlanilla!.TipoPeriodo==periodo.TipoPeriodo && p.PeriodoPlanilla.FechaInicio==periodo.FechaInicio && p.PeriodoPlanilla.FechaFin==periodo.FechaFin).ToArrayAsync(ct);
                await BloqueoPeriodoPlanilla.ObtenerAsync(db,candidatos.Select(p=>p.IdPeriodoPlanilla),ct);
                var elegibles=candidatos.Where(p=>p.EstadoRegistro=="Activo" && p.PeriodoPlanilla!.EstadoRegistro=="Activo" && p.EstadoPlanilla!.Nombre is "Aprobada" or "Cerrada")
                    .Where(p=>p.PeriodoPlanilla!.EstadoPlanilla!.Nombre==p.EstadoPlanilla!.Nombre && p.EjecucionVigente is {EstadoEjecucion:"Autorizada",SnapshotCompleto:true,EstadoReproducibilidad:"REPRODUCIBLE",VersionMotor:"FIN-DI-V1"} e && e.IdPlanilla==p.IdPlanilla).ToArray();
                if(elegibles.Length>1)throw Error("REAL_COMPARABLE_AMBIGUO");
                if(elegibles.Length==1)reales.Add(periodo.IdForecastPeriodo,(elegibles[0],elegibles[0].EjecucionVigente!));
            }
            if(reales.Count!=periodos.Length)
            {
                foreach(var periodo in periodos.Where(p=>!reales.ContainsKey(p.IdForecastPeriodo)))
                {
                    var c=await db.ForecastComparacionesReales.SingleOrDefaultAsync(c=>c.IdForecastPeriodo==periodo.IdForecastPeriodo,ct);
                    if(c is null){c=new(){IdForecastPeriodo=periodo.IdForecastPeriodo};db.ForecastComparacionesReales.Add(c);}
                    if(c.IdEjecucionPlanillaReal is not null)throw Error("COMPARACION_PREVIA_INCOHERENTE");
                    c.FechaEvaluacion=DateTime.UtcNow;c.EvaluadoPor=actor;c.Disponibilidad="SinRealComparable";c.MotivoNoComparable="SinRealComparable";
                }
                Auditar("COMPARAR_FORECAST_SIN_REAL",actor,idEscenario,new{Actor=actor,IdEscenario=idEscenario,Estado="Guardado",PeriodosSinReal=periodos.Length-reales.Count});
            }
            else
            {
                foreach(var periodo in periodos)
                {
                    var (p,e)=reales[periodo.IdForecastPeriodo];ExigirReal(p,e);
                    var c=await db.ForecastComparacionesReales.SingleOrDefaultAsync(c=>c.IdForecastPeriodo==periodo.IdForecastPeriodo,ct);
                    if(c is null){c=new(){IdForecastPeriodo=periodo.IdForecastPeriodo};db.ForecastComparacionesReales.Add(c);}
                    if(c.IdEjecucionPlanillaReal is not null)throw Error("COMPARACION_PREVIA_INCOHERENTE");
                    c.IdEjecucionPlanillaReal=e.IdEjecucionPlanilla;c.FechaEvaluacion=DateTime.UtcNow;c.EvaluadoPor=actor;c.Disponibilidad="Disponible";c.MotivoNoComparable=null;
                    c.EstadoPlanillaObservado=p.EstadoPlanilla!.Nombre;c.EstadoPeriodoObservado=p.PeriodoPlanilla!.EstadoPlanilla!.Nombre;c.EstadoEjecucionObservado=e.EstadoEjecucion;c.SnapshotCompletoObservado=e.SnapshotCompleto;
                    await db.SaveChangesAsync(ct);await MaterializarAsync(c,periodo,e,actor,ct);
                }
                db.PermitirTransicionComparacionM=true;
                var local=db.ChangeTracker.Entries<ForecastEscenario>().SingleOrDefault(e=>e.Entity.IdForecastEscenario==idEscenario);
                if(local is not null){if(local.State!=EntityState.Unchanged)throw Error("FORECAST_CAMBIO_PENDIENTE");local.State=EntityState.Detached;}
                escenario.EstadoEscenario="Comparado";escenario.FechaModificacion=DateTime.UtcNow;escenario.ModificadoPor=actor;db.ForecastEscenarios.Update(escenario);
                // Attach solo la transición; nunca marcar columnas monetarias del snapshot como modificadas.
                foreach(var prop in db.Entry(escenario).Properties)prop.IsModified=prop.Metadata.Name is nameof(ForecastEscenario.EstadoEscenario) or nameof(ForecastEscenario.FechaModificacion) or nameof(ForecastEscenario.ModificadoPor);
                Auditar("COMPARAR_FORECAST",actor,idEscenario,new{Actor=actor,IdEscenario=idEscenario,Anterior="Guardado",Nuevo="Comparado",Periodos=periodos.Length,ForecastCTL=escenario.MontoProyectadoTotal,RealCTL=reales.Values.Sum(x=>x.E.CostoLaboralTotal),Ejecuciones=reales.Values.Select(x=>x.E.IdEjecucionPlanilla).ToArray()});
            }
            await db.SaveChangesAsync(ct);var salida=await LeerInternoAsync(escenario,ct);await tx.CommitAsync(ct);return salida;
        }
        catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
        finally{db.PermitirTransicionComparacionM=false;}
    }
}

using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastCalculoService(ApplicationDbContext db) : IForecastCalculoService
{
    // La ruta compatible no presume aplicabilidad empresarial por omisión.
    public Task<ResultadoGeneracionForecast> GenerarForecastAsync(long idForecastEscenario,string idUsuarioActual,CancellationToken cancellationToken=default)
        => GenerarForecastAsync(new SolicitudCalculoFinancieroForecast(idForecastEscenario,[]),idUsuarioActual,cancellationToken);

    public async Task<ResultadoGeneracionForecast> GenerarForecastAsync(SolicitudCalculoFinancieroForecast solicitud,string idUsuarioActual,CancellationToken cancellationToken=default)
    {
        var ct=cancellationToken;
        var actor=await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,idUsuarioActual,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var e=await BloqueoEscenarioForecast.ObtenerAsync(db,solicitud.IdEscenario,ct);
            if(e.EstadoEscenario!=EstadosEscenarioForecast.Borrador)throw new InvalidOperationException("Solo se genera un Forecast Borrador.");
            var periodos=await db.ForecastPeriodos.AsNoTracking().Where(p=>p.IdForecastEscenario==e.IdForecastEscenario && p.EstadoRegistro=="Activo")
                .OrderBy(p=>p.FechaInicio).ThenBy(p=>p.NumeroOrden).ToArrayAsync(ct);
            if(periodos.Length==0)throw new ConfiguracionConceptoException("PERIODO_FORECAST_FALTANTE");
            foreach(var p in periodos)
            {
                new PeriodoSalarial(p.FechaInicio,p.FechaFin,p.TipoPeriodo).Validar();
                if(p.FechaInicio<e.FechaInicioProyeccion || p.FechaFin>e.FechaFinProyeccion)throw new ConfiguracionConceptoException("PERIODO_FORECAST_FUERA_ESCENARIO");
            }
            if(periodos.Zip(periodos.Skip(1)).Any(x=>x.First.FechaFin>=x.Second.FechaInicio))throw new ConfiguracionConceptoException("PERIODOS_FORECAST_SOLAPADOS");
            var personas=await db.ForecastParticipantes.AsNoTracking().Where(p=>p.IdForecastEscenario==e.IdForecastEscenario && p.EstadoRegistro=="Activo" && p.EstaIncluido)
                .OrderBy(p=>p.CodigoParticipante).ThenBy(p=>p.IdForecastParticipante).ToArrayAsync(ct);
            if(personas.Length==0)throw new ConfiguracionConceptoException("POBLACION_FORECAST_VACIA");
            // Exclusivamente resultados transitorios del borrador bloqueado. Nunca toca legado ajeno.
            // ExecuteDelete del proveedor emite DELETE FROM tabla AS alias, incompatible con MariaDB 10.11.
            // Mantener parámetros, filtros y orden de dependencias dentro de la misma transacción.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE `a` FROM `ForecastAplicacionFinanciera` AS `a`
                WHERE `a`.`IdForecastDetalle` IN (
                    SELECT `d`.`IdForecastDetalle` FROM `ForecastDetalle` AS `d`
                    WHERE `d`.`IdForecastEscenario` = {e.IdForecastEscenario}
                )
                """,ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM `ForecastDetalle` WHERE `IdForecastEscenario` = {e.IdForecastEscenario}
                """,ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM `ForecastObservacionHistorica` WHERE `IdForecastEscenario` = {e.IdForecastEscenario}
                """,ct);
            var observaciones=await ForecastFuentesSnapshot.ObservarAsync(db,e,periodos,personas,ct);
            e.PeriodosHistoricosConsiderados=await db.ForecastFuentesHistoricas.CountAsync(f=>f.IdForecastEscenario==e.IdForecastEscenario,ct);
            var anteriores=new Dictionary<(long Persona,int Anio,int Mes),(AcumuladoMensualRenta Acumulado,IReadOnlyList<ForecastDetalle> Detalles)>();
            decimal total=0;var cantidad=0;
            foreach(var p in periodos)
            foreach(var persona in personas.Where(x=>x.FechaInicioAplicacion<=p.FechaFin && (x.FechaSalidaPrevista==null || x.FechaSalidaPrevista>=p.FechaInicio)))
            {
                var asignaciones=await db.ForecastAsignacionesProyecto.Where(a=>a.IdForecastEscenario==e.IdForecastEscenario && a.IdForecastPeriodo==p.IdForecastPeriodo
                    && a.IdForecastParticipante==persona.IdForecastParticipante && a.EstadoRegistro=="Activo").OrderBy(a=>a.IdProyectoNormalizado).ToArrayAsync(ct);
                foreach(var a in asignaciones)
                {
                    if(a.EsSinAsignar && a.IdProyecto is null){a.CodigoProyectoSnapshot=null;a.NombreProyectoSnapshot="Sin asignar";}
                    else
                    {
                        var proyecto=await db.Proyectos.AsNoTracking().SingleOrDefaultAsync(x=>x.IdProyecto==a.IdProyecto && x.EstadoRegistro=="Activo",ct)
                            ??throw new ConfiguracionConceptoException("DESTINO_FORECAST_NO_DEMOSTRABLE");
                        a.CodigoProyectoSnapshot=proyecto.CodigoProyecto;a.NombreProyectoSnapshot=proyecto.NombreProyecto;
                    }
                }
                var destinos=asignaciones.Select(a=>new DestinoCostoForecast(a.IdForecastAsignacionProyecto,a.IdProyecto,a.EsSinAsignar,a.Porcentaje)).ToArray();
                _=ProyeccionFinanciera.Distribuir(0,destinos);
                var clave=(persona.IdForecastParticipante,p.FechaInicio.Year,p.FechaInicio.Month);
                var final=p.TipoPeriodo==TiposPeriodoPlanilla.Quincenal && p.FechaInicio.Day==16;
                anteriores.TryGetValue(clave,out var previo);
                if(final && previo.Acumulado is null)throw new ConfiguracionConceptoException("ACUMULADO_FORECAST_NO_DEMOSTRABLE");
                var resultado=await ForecastMotorPreparacion.PrepararAsync(db,p,persona,observaciones,solicitud.Aplicabilidades,actor,final?previo.Acumulado:null,ct);
                var detalles=await ForecastSnapshotEscritura.PersistirAsync(db,p,persona,resultado,destinos,observaciones,final?previo.Detalles:[],ct);
                total+=resultado.Costo.CostoLaboralTotal!.Value;cantidad+=detalles.Count;
                if(p.TipoPeriodo==TiposPeriodoPlanilla.Quincenal && p.FechaInicio.Day==1)
                {
                    var h=resultado.Costo.Renta!;var g=h.CuotasSociales!;
                    var soporte=FormattableString.Invariant($"Forecast {e.IdForecastEscenario}, período {p.IdForecastPeriodo}, participante {persona.IdForecastParticipante}");
                    var cuotas=new AcumuladoMensualCuotas(p.FechaInicio,p.FechaFin,g.Conciliacion!.Lineas.Where(l=>l.Fuente.TipoFuente!="Normativa").ToArray(),g.Aplicaciones,soporte,[persona.IdForecastParticipante]);
                    var renta=new AcumuladoMensualRenta(p.FechaInicio,p.FechaFin,[persona.IdForecastParticipante],h.Conciliacion!.Lineas.Where(l=>l.Concepto.TipoBase!="RENTA_SALARIAL").ToArray(),h.Aplicaciones,soporte,cuotas);
                    anteriores.Add(clave,(renta,detalles));
                }
            }
            if(cantidad==0)throw new ConfiguracionConceptoException("POBLACION_FORECAST_VACIA");
            e.VersionMotor=ProyeccionFinanciera.VersionMotor;e.MontoProyectadoTotal=total;e.MontoRealTotal=null;e.DiferenciaTotal=null;
            e.EstadoEscenario=EstadosEscenarioForecast.Calculado;e.FechaCalculo=DateTime.UtcNow;e.FechaModificacion=e.FechaCalculo;e.ModificadoPor=actor;
            db.BitacoraAuditoria.Add(new(){IdUsuario=actor,FechaHora=e.FechaCalculo.Value,Accion="GENERAR_FORECAST",Entidad=nameof(ForecastEscenario),
                IdRegistro=e.IdForecastEscenario.ToString(CultureInfo.InvariantCulture),ValoresNuevos=JsonSerializer.Serialize(new{e.VersionMotor,e.MontoProyectadoTotal,Detalles=cantidad,e.PeriodosHistoricosConsiderados}),Observacion="Forecast proyectado; FIN-02 contenido; actor: "+actor});
            await db.SaveChangesAsync(ct);
            await ForecastSnapshotEscritura.ConciliarAsync(db,e.IdForecastEscenario,total,ct);
            await tx.CommitAsync(ct);
            return new(){IdForecastEscenario=e.IdForecastEscenario,EstadoEscenario=e.EstadoEscenario,FechaCalculo=e.FechaCalculo.Value,MontoProyectadoTotal=total,
                CantidadPeriodos=periodos.Length,CantidadParticipantes=personas.Length,CantidadDetalles=cantidad};
        }
        catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
    }
}

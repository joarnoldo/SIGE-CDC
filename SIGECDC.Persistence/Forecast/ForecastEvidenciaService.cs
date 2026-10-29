using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastEvidenciaService(ApplicationDbContext db):IForecastEvidenciaService
{
    public async Task<IReadOnlyList<long>> RegistrarCondicionAsync(SolicitudCondicionForecast s,string actor,CancellationToken ct=default)
    {
        actor=await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var e=await BloqueoEscenarioForecast.ObtenerAsync(db,s.IdEscenario,ct);ExigirBorrador(e);
            var p=await db.ForecastPeriodos.SingleOrDefaultAsync(p=>p.IdForecastPeriodo==s.IdPeriodo && p.IdForecastEscenario==s.IdEscenario && p.EstadoRegistro=="Activo",ct)
                ?? throw new ConfiguracionConceptoException("PERIODO_FORECAST_INVALIDO");
            var persona=await db.ForecastParticipantes.SingleOrDefaultAsync(p=>p.IdForecastParticipante==s.IdParticipante && p.IdForecastEscenario==s.IdEscenario && p.EstadoRegistro=="Activo",ct)
                ?? throw new ConfiguracionConceptoException("PARTICIPANTE_FORECAST_INVALIDO");
            var periodo=new PeriodoSalarial(p.FechaInicio,p.FechaFin,p.TipoPeriodo);periodo.Validar();
            var rango=new VigenciaFinanciera(s.Inicio,s.Fin);rango.Validar();
            if(!new VigenciaFinanciera(periodo.Inicio,periodo.Fin).Contiene(rango) || s.Inicio<persona.FechaInicioAplicacion
                || persona.FechaSalidaPrevista is {} salida && s.Fin>salida || s.SalarioMensual<=0
                || string.IsNullOrWhiteSpace(s.Soporte) || s.Soporte.Length>500 || string.IsNullOrWhiteSpace(s.TipoContrato)
                || s.TipoContrato.Length>100 || s.Periodicidad is not ("Mensual" or "Quincenal"))
                throw new ConfiguracionConceptoException("CONDICION_FORECAST_INVALIDA");
            if(s.Origen=="CondicionExistente")
            {
                if(s.IdContrato is null || s.IdCondicion is null || persona.IdColaborador is null)
                    throw new ConfiguracionConceptoException("CONDICION_FORECAST_SIN_ORIGEN");
            }
            else if(s.Origen is not ("ContratacionFutura" or "CambioFuturo") || s.IdContrato is not null || s.IdCondicion is not null
                || s.Origen=="ContratacionFutura" && persona.IdColaborador is not null)
                throw new ConfiguracionConceptoException("CONDICION_FORECAST_ORIGEN_INCOMPATIBLE");
            if(await db.ForecastSegmentosProyectados.AnyAsync(x=>x.IdForecastParticipante==s.IdParticipante && x.IdForecastPeriodo==s.IdPeriodo
                && x.FechaInicio<=s.Fin && x.FechaFin>=s.Inicio,ct))throw new ConfiguracionConceptoException("CONDICIONES_FORECAST_SOLAPADAS");
            var jornada=await db.JornadasLaborales.AsNoTracking().SingleOrDefaultAsync(j=>j.IdJornadaLaboral==s.IdJornada,ct)
                ?? throw new ConfiguracionConceptoException("JORNADA_FALTANTE");
            var ocupacion=await db.OcupacionesSalarioMinimo.AsNoTracking().SingleOrDefaultAsync(o=>o.IdOcupacionSalarioMinimo==s.IdOcupacion && o.EstadoRegistro=="Activo",ct)
                ?? throw new ConfiguracionConceptoException("OCUPACION_FALTANTE");
            var puesto=await db.Puestos.AsNoTracking().SingleOrDefaultAsync(x=>x.IdPuesto==s.IdPuesto,ct)
                ?? throw new ConfiguracionConceptoException("PUESTO_FALTANTE");
            var depto=await db.Departamentos.AsNoTracking().SingleOrDefaultAsync(x=>x.IdDepartamento==s.IdDepartamento,ct)
                ?? throw new ConfiguracionConceptoException("DEPARTAMENTO_FALTANTE");
            var gobierno=new GobiernoFinancieroService(db);
            var cat=await gobierno.LeerCatalogosForecastAsync(periodo,[jornada.Codigo],[s.IdOcupacion],actor,ct);
            ResultadoTemporalSalarial temporal;
            if(s.Origen=="CondicionExistente")
            {
                temporal=await gobierno.LeerCondicionesForecastAsync(periodo,s.Soporte,actor,ct);
                var real=temporal.Poblacion.SingleOrDefault(x=>x.IdColaborador==persona.IdColaborador);
                if(real?.EsResoluble!=true || !real.Segmentos.Any(x=>x.IdContrato==s.IdContrato && x.IdCondicionLaboralVersion==s.IdCondicion
                    && x.Inicio<=s.Inicio && x.Fin>=s.Fin && x.SalarioMensual==s.SalarioMensual && x.IdPuesto==s.IdPuesto && x.IdDepartamento==s.IdDepartamento
                    && x.Jornada.IdJornada==s.IdJornada && x.Ocupacion.IdOcupacion==s.IdOcupacion))
                    throw new ConfiguracionConceptoException("CONDICION_EXISTENTE_NO_DEMOSTRABLE");
            }
            temporal=ResolvedorTemporalSalarial.Resolver(periodo,[new(s.IdParticipante,rango,
                [new(s.IdContrato??0,rango,s.SalarioMensual,jornada.Codigo,s.Soporte,true,s.IdCondicion,s.IdPuesto,s.IdDepartamento,s.IdJornada)],
                [new(s.IdOcupacion,ocupacion.CodigoOficial,rango,s.Soporte,s.IdContrato??0)],[])],cat.Jornadas,cat.Minimos);
            if(!temporal.EsResoluble)throw new ConfiguracionConceptoException(temporal.Poblacion.SelectMany(x=>x.Bloqueos).FirstOrDefault()?.Codigo??"CONDICION_FORECAST_NO_RESOLUBLE");
            if(s.IdConfiguracionRT is not null)
            {
                if(string.IsNullOrWhiteSpace(s.SoporteRT) || s.SoporteRT.Length>500 || s.ModalidadRT==ModalidadBaseRiesgo.Desconocida)
                    throw new ConfiguracionConceptoException("APLICABILIDAD_RT_NO_DEMOSTRABLE");
                var config=await gobierno.LeerConfiguracionForecastAsync(periodo,[],[s.IdConfiguracionRT.Value],actor,ct);
                var rt=config.Riesgos.SingleOrDefault(r=>r.IdConfiguracion==s.IdConfiguracionRT);
                if(rt is null || !rt.Activo || !rt.EvidenciaDemostrada || !rt.Vigencia.Contiene(rango.Inicio))
                    throw new ConfiguracionConceptoException("CONFIGURACION_RT_NO_DEMOSTRABLE");
            }
            var orden=await db.ForecastSegmentosProyectados.Where(x=>x.IdForecastPeriodo==s.IdPeriodo && x.IdForecastParticipante==s.IdParticipante)
                .MaxAsync(x=>(int?)x.NumeroOrden,ct)??0;
            var filas=new List<ForecastSegmentoProyectado>();
            foreach(var tramo in temporal.Poblacion.Single().Segmentos)
            {
                filas.Add(new(){IdForecastEscenario=s.IdEscenario,IdForecastPeriodo=s.IdPeriodo,IdForecastParticipante=s.IdParticipante,NumeroOrden=++orden,
                    FechaInicio=tramo.Inicio,FechaFin=tramo.Fin,DiasBase30=tramo.DiasBase30,OrigenCondicion=s.Origen,SalarioMensual=s.SalarioMensual,
                    IdContrato=s.IdContrato,IdCondicionLaboralVersion=s.IdCondicion,IdJornadaLaboral=tramo.Jornada.IdJornada,CodigoJornada=jornada.Codigo,NombreJornada=jornada.Nombre,
                    TipoJornada=tramo.Jornada.Tipo,HorasOrdinariasDia=tramo.Jornada.HorasDia,HorasOrdinariasSemana=tramo.Jornada.HorasSemana,
                    FechaJornadaInicio=tramo.Jornada.Vigencia.Inicio,FechaJornadaFin=tramo.Jornada.Vigencia.Fin,
                    IdOcupacionSalarioMinimo=s.IdOcupacion,CodigoOcupacion=ocupacion.CodigoOficial,NombreOcupacion=ocupacion.Nombre,
                    IdSalarioMinimoVersion=tramo.Minimo.IdMinimo,MontoMinimo=tramo.Minimo.Monto,PeriodicidadMinimo=tramo.Minimo.Periodicidad,
                    IdPuesto=s.IdPuesto,NombrePuesto=puesto.Nombre,IdDepartamento=s.IdDepartamento,NombreDepartamento=depto.Nombre,
                    TipoContrato=s.TipoContrato,PeriodicidadPago=s.Periodicidad,ReferenciaSoporte=s.Soporte.Trim(),RegistradoPor=actor,FechaRegistro=DateTime.UtcNow,
                    IdConfiguracionRiesgoTrabajo=s.IdConfiguracionRT,ModalidadRT=s.IdConfiguracionRT is null?null:s.ModalidadRT.ToString(),
                    ReferenciaAsignacionRT=s.IdConfiguracionRT is null?null:s.SoporteRT,AsociadoPorRT=s.IdConfiguracionRT is null?null:actor,
                    FechaRTInicio=s.IdConfiguracionRT is null?null:s.Inicio,FechaRTFin=s.IdConfiguracionRT is null?null:s.Fin});
            }
            db.ForecastSegmentosProyectados.AddRange(filas);await db.SaveChangesAsync(ct);
            Auditar(e,actor,"REGISTRAR_CONDICION_FORECAST",new{s.IdParticipante,s.IdPeriodo,s.Origen,Segmentos=filas.Select(f=>f.IdForecastSegmentoProyectado).ToArray()});
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return filas.Select(f=>f.IdForecastSegmentoProyectado).ToArray();
        }
        catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
    }

    public async Task<long> RegistrarOverrideAsync(SolicitudOverrideForecast s,string actor,CancellationToken ct=default)
    {
        actor=await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var e=await BloqueoEscenarioForecast.ObtenerAsync(db,s.IdEscenario,ct);ExigirBorrador(e);
            var periodo=await db.ForecastPeriodos.Where(p=>p.IdForecastEscenario==s.IdEscenario).OrderBy(p=>p.NumeroOrden).FirstAsync(ct);
            var cat=await new GobiernoFinancieroService(db).LeerConfiguracionForecastAsync(new(periodo.FechaInicio,periodo.FechaFin,periodo.TipoPeriodo),[],[],actor,ct);
            var c=cat.Conceptos.SingleOrDefault(c=>c.IdVersion==s.IdConceptoVersion)??throw new ConfiguracionConceptoException("CONCEPTO_VERSION_FALTANTE");
            if(!c.Vigencia.Contiene(new VigenciaFinanciera(e.FechaInicioProyeccion,e.FechaFinProyeccion)))throw new ConfiguracionConceptoException("CONCEPTO_FUERA_VIGENCIA");
            if(decimal.Round(s.Valor,4)!=s.Valor || s.Valor>SolicitudGuardarParametrosForecast.MontoMaximo || s.Soporte?.Length>500 || s.ReferenciaAutorizacion?.Length>500)
                throw new ConfiguracionConceptoException("PRECISION_OVERRIDE_INVALIDA");
            var ahora=DateTime.UtcNow;
            ProyeccionFinanciera.ValidarOverride(new(1,c.IdVersion,s.Tipo,s.Unidad,s.Valor,s.Soporte!,actor,c.RequiereAutorizacion,
                c.RequiereAutorizacion?actor:null,c.RequiereAutorizacion?ahora:null,s.ReferenciaAutorizacion),c);
            var p=await db.ForecastParametros.SingleOrDefaultAsync(p=>p.IdForecastEscenario==s.IdEscenario && p.Codigo==c.Codigo,ct);
            if(p is null){p=new(){IdForecastEscenario=s.IdEscenario,Codigo=c.Codigo};db.ForecastParametros.Add(p);}
            p.Nombre=c.Nombre;p.TipoParametro=s.Tipo;p.ValorDecimal=s.Valor;p.ValorTexto=null;p.IdConceptoPlanillaVersion=c.IdVersion;
            p.Unidad=s.Unidad;p.ReferenciaSoporte=s.Soporte!.Trim();p.RegistradoPor=actor;p.FechaRegistro=ahora;p.AutorizacionRequerida=c.RequiereAutorizacion;
            p.AutorizadoPor=c.RequiereAutorizacion?actor:null;p.FechaAutorizacion=c.RequiereAutorizacion?ahora:null;
            p.ReferenciaAutorizacion=c.RequiereAutorizacion?s.ReferenciaAutorizacion:null;p.EstadoRegistro="Activo";
            await db.SaveChangesAsync(ct);Auditar(e,actor,"REGISTRAR_OVERRIDE_FORECAST",new{p.IdForecastParametro,p.IdConceptoPlanillaVersion,p.TipoParametro,p.ValorDecimal});
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return p.IdForecastParametro;
        }
        catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
    }

    private static void ExigirBorrador(ForecastEscenario e)
    {if(e.EstadoEscenario!=EstadosEscenarioForecast.Borrador)throw new InvalidOperationException("Solo se modifica evidencia de un Forecast Borrador.");}
    private void Auditar(ForecastEscenario e,string actor,string accion,object evidencia)
        =>db.BitacoraAuditoria.Add(new BitacoraAuditoria{IdUsuario=actor,FechaHora=DateTime.UtcNow,Accion=accion,Entidad=nameof(ForecastEscenario),
            IdRegistro=e.IdForecastEscenario.ToString(CultureInfo.InvariantCulture),ValoresNuevos=JsonSerializer.Serialize(evidencia),Observacion="Actor: "+actor+"; Forecast, sin modificación RRHH."});
}

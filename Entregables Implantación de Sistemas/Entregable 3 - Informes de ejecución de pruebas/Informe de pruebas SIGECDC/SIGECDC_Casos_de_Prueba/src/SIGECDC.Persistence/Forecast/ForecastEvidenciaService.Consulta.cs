using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastEvidenciaService
{
    // Opciones para captura explícita. No asigna, homologa ni convierte el maestro en historia.
    // RegistrarCondicion/Override vuelve a validar C/D bajo lock antes de escribir.
    public async Task<ConfiguracionEvidenciaForecast> ConsultarConfiguracionAsync(long escenario,string actor,CancellationToken ct=default)
    {
        await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(db,actor,ct);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var e=await BloqueoEscenarioForecast.ObtenerAsync(db,escenario,ct);
        var personas=await db.ForecastParticipantes.AsNoTracking().Where(p=>p.IdForecastEscenario==escenario && p.EstadoRegistro=="Activo" && p.EstaIncluido).OrderBy(p=>p.CodigoParticipante).ToArrayAsync(ct);
        var periodos=await db.ForecastPeriodos.AsNoTracking().Where(p=>p.IdForecastEscenario==escenario && p.EstadoRegistro=="Activo").OrderBy(p=>p.FechaInicio)
            .Select(p=>new PeriodoForecastResumen(p.IdForecastPeriodo,p.NumeroOrden,p.TipoPeriodo,p.FechaInicio,p.FechaFin,p.EstadoRegistro)).ToArrayAsync(ct);
        var puestos=await db.Puestos.AsNoTracking().Where(p=>p.EstadoRegistro=="Activo").OrderBy(p=>p.Nombre).Select(p=>new OpcionEvidenciaForecast(p.IdPuesto,p.Nombre)).ToArrayAsync(ct);
        var departamentos=await db.Departamentos.AsNoTracking().Where(p=>p.EstadoRegistro=="Activo").OrderBy(p=>p.Nombre).Select(p=>new OpcionEvidenciaForecast(p.IdDepartamento,p.Nombre)).ToArrayAsync(ct);
        var jornadas=await db.JornadasLaborales.AsNoTracking().Where(j=>j.EstadoRegistro=="Activo" && j.FechaVigenciaInicio<=e.FechaFinProyeccion && (j.FechaVigenciaFin==null || j.FechaVigenciaFin>=e.FechaInicioProyeccion)).OrderBy(j=>j.Codigo).ThenBy(j=>j.FechaVigenciaInicio).ToArrayAsync(ct);
        var ocupaciones=await db.OcupacionesSalarioMinimo.AsNoTracking().Where(o=>o.EstadoRegistro=="Activo").OrderBy(o=>o.CodigoOficial).Select(o=>new OpcionEvidenciaForecast(o.IdOcupacionSalarioMinimo,o.CodigoOficial+" — "+o.Nombre)).ToArrayAsync(ct);
        var polizas=await db.ConfiguracionesRiesgoTrabajo.AsNoTracking().Where(r=>r.EstadoRegistro=="Activo" && r.FechaVigenciaInicio<=e.FechaFinProyeccion && (r.FechaVigenciaFin==null || r.FechaVigenciaFin>=e.FechaInicioProyeccion)).OrderBy(r=>r.NumeroPoliza).ThenBy(r=>r.FechaVigenciaInicio).ToArrayAsync(ct);
        var conceptos=await db.ConceptosPlanillaVersion.AsNoTracking().Include(v=>v.ConceptoPlanilla).Where(v=>v.EstadoRegistro=="Activo" && v.ConceptoPlanilla!.EstadoRegistro=="Activo"
            && v.FechaVigenciaInicio<=e.FechaInicioProyeccion && (v.FechaVigenciaFin==null || v.FechaVigenciaFin>=e.FechaFinProyeccion)).OrderBy(v=>v.ConceptoPlanilla!.Codigo).ToArrayAsync(ct);
        var propuestas=new List<CondicionDisponibleForecast>();
        var ids=personas.Select(p=>p.IdColaborador).OfType<long>().ToArray();
        var condiciones=await db.CondicionesLaboralesVersion.AsNoTracking().Include(c=>c.Contrato)
            .Where(c=>ids.Contains(c.Contrato!.IdColaborador) && c.EstadoRegistro=="Activo"
                && c.FechaVigenciaInicio<=e.FechaFinProyeccion && (c.FechaVigenciaFin==null || c.FechaVigenciaFin>=e.FechaInicioProyeccion)).ToArrayAsync(ct);
        foreach(var persona in personas.Where(p=>p.IdColaborador!=null))
        foreach(var periodo in periodos)
        foreach(var c in condiciones.Where(c=>c.Contrato!.IdColaborador==persona.IdColaborador))
        {
            var contrato=c.Contrato!;
            var inicio=new[]{periodo.FechaInicio,c.FechaVigenciaInicio,contrato.FechaInicio,persona.FechaInicioAplicacion}.Max();
            var fin=new[]{periodo.FechaFin,c.FechaVigenciaFin??periodo.FechaFin,contrato.FechaFin??periodo.FechaFin,persona.FechaSalidaPrevista??periodo.FechaFin}.Min();
            if(inicio>fin)continue;
            propuestas.Add(new($"{persona.Etiqueta}: {inicio:dd/MM/yyyy}–{fin:dd/MM/yyyy}",new(escenario,periodo.IdForecastPeriodo,persona.IdForecastParticipante,
                inicio,fin,"CondicionExistente",c.SalarioBase,c.IdPuesto,c.IdDepartamento,c.IdJornadaLaboral,c.IdOcupacionSalarioMinimo,
                contrato.TipoContrato,contrato.PeriodicidadPago,"",contrato.IdContrato,c.IdCondicionLaboralVersion)));
        }
        var filas=await db.ForecastSegmentosProyectados.AsNoTracking().Where(s=>s.IdForecastEscenario==escenario).OrderBy(s=>s.IdForecastParticipante).ThenBy(s=>s.FechaInicio).ToArrayAsync(ct);
        var parametros=await db.ForecastParametros.AsNoTracking().Where(p=>p.IdForecastEscenario==escenario && p.EstadoRegistro=="Activo" && p.IdConceptoPlanillaVersion!=null).OrderBy(p=>p.Codigo).ToArrayAsync(ct);
        var resultado=new ConfiguracionEvidenciaForecast(e.EstadoEscenario==EstadosEscenarioForecast.Borrador,e.FechaInicioProyeccion,e.FechaFinProyeccion,
            personas.Select(p=>new OpcionEvidenciaForecast(p.IdForecastParticipante,p.CodigoParticipante+" — "+p.Etiqueta)).ToArray(),periodos,puestos,departamentos,
            jornadas.Select(j=>new OpcionEvidenciaForecast(j.IdJornadaLaboral,$"{j.Codigo} — {j.Nombre} ({j.FechaVigenciaInicio:dd/MM/yyyy})")).ToArray(),ocupaciones,
            polizas.Select(p=>new OpcionEvidenciaForecast(p.IdConfiguracionRiesgoTrabajo,$"{p.NumeroPoliza} — {p.CodigoActividad} ({p.FechaVigenciaInicio:dd/MM/yyyy})")).ToArray(),
            conceptos.Where(c=>c.Clasificacion!=null && c.UnidadPermitida!=null && c.TipoBase is "MONTO_AUTORIZADO" or "SALARIO_DEVENGADO" or "HORA_ORDINARIA" or "REDUCCION_DEVENGO")
                .Select(c=>new ConceptoEvidenciaForecast(c.IdConceptoPlanillaVersion,c.ConceptoPlanilla!.Codigo,c.ConceptoPlanilla.Nombre,c.UnidadPermitida!,c.TipoBase!,c.PermiteMontoManual,c.RequiereAutorizacion)).ToArray(),
            propuestas,filas.Select(s=>new SolicitudCondicionForecast(escenario,s.IdForecastPeriodo,s.IdForecastParticipante,s.FechaInicio,s.FechaFin,s.OrigenCondicion,s.SalarioMensual,
                s.IdPuesto,s.IdDepartamento,s.IdJornadaLaboral,s.IdOcupacionSalarioMinimo,s.TipoContrato,s.PeriodicidadPago,s.ReferenciaSoporte,s.IdContrato,s.IdCondicionLaboralVersion,
                s.IdConfiguracionRiesgoTrabajo,Enum.TryParse<ModalidadBaseRiesgo>(s.ModalidadRT,out var m)?m:ModalidadBaseRiesgo.Desconocida,s.ReferenciaAsignacionRT)).ToArray(),
            parametros.Select(p=>new SolicitudOverrideForecast(escenario,p.IdConceptoPlanillaVersion!.Value,p.TipoParametro,p.Unidad??"",p.ValorDecimal??0,p.ReferenciaSoporte??"",p.ReferenciaAutorizacion)).ToArray());
        await tx.CommitAsync(ct);return resultado;
    }
}

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

// Proyección del snapshot FIN-FOR-V1. No navega Colaborador/Contrato/Puesto/Departamento/
// Proyecto/Concepto/Normativa, ni invoca los motores. Totales: algebra de importes persistidos.
internal static class ForecastSnapshotConsulta
{
    internal static async Task<ConsultaResultadosForecast?> LeerAsync(ApplicationDbContext db,long id,FiltroResultadosForecast filtro,CancellationToken ct)
    {
        var e=await db.ForecastEscenarios.AsNoTracking().SingleOrDefaultAsync(e=>e.IdForecastEscenario==id && e.EstadoRegistro=="Activo",ct);
        if(e is null)return null;
        if(e.VersionMotor!=ProyeccionFinanciera.VersionMotor || e.EstadoEscenario is not (EstadosEscenarioForecast.Calculado or EstadosEscenarioForecast.Guardado or EstadosEscenarioForecast.Comparado))
            throw new ConfiguracionConceptoException("FORECAST_SNAPSHOT_NO_PUBLICABLE");
        await ForecastSnapshotEscritura.ConciliarAsync(db,id,e.MontoProyectadoTotal,ct);
        var periodos=await db.ForecastPeriodos.AsNoTracking().Where(p=>p.IdForecastEscenario==id && p.EstadoRegistro=="Activo").OrderBy(p=>p.FechaInicio).ToArrayAsync(ct);
        if(filtro.IdForecastPeriodo is {} seleccionado && !periodos.Any(p=>p.IdForecastPeriodo==seleccionado))throw new ValidationException("El período no pertenece al escenario.");
        var personas=await db.ForecastParticipantes.AsNoTracking().Where(p=>p.IdForecastEscenario==id).ToDictionaryAsync(p=>p.IdForecastParticipante,ct);
        var destinos=await db.ForecastAsignacionesProyecto.AsNoTracking().Where(a=>a.IdForecastEscenario==id).ToDictionaryAsync(a=>a.IdForecastAsignacionProyecto,ct);
        var segmentos=await db.ForecastSegmentosProyectados.AsNoTracking().Where(s=>s.IdForecastEscenario==id && (filtro.IdForecastPeriodo==null || s.IdForecastPeriodo==filtro.IdForecastPeriodo))
            .OrderBy(s=>s.IdForecastParticipante).ThenBy(s=>s.FechaInicio).ToArrayAsync(ct);
        var ds=await db.ForecastDetalles.AsNoTracking().Where(d=>d.IdForecastEscenario==id && (filtro.IdForecastPeriodo==null || d.IdForecastPeriodo==filtro.IdForecastPeriodo)).ToArrayAsync(ct);
        var ids=ds.Select(d=>d.IdForecastDetalle).ToArray();
        var apps=await db.ForecastAplicacionesFinancieras.AsNoTracking().Where(a=>ids.Contains(a.IdForecastDetalle) && a.Componente=="DISTRIBUCION").ToArrayAsync(ct);
        var filas=new List<(string Clave,string Codigo,string Etiqueta,string Detalle,long Persona,ForecastDetalle D,ForecastAplicacionFinanciera A)>();
        foreach(var d in ds)
        {
            var persona=personas[d.IdForecastParticipante];var p=periodos.Single(p=>p.IdForecastPeriodo==d.IdForecastPeriodo);
            var destino=destinos[d.IdForecastAsignacionProyecto!.Value];var s=segmentos.Where(s=>s.IdForecastParticipante==persona.IdForecastParticipante && s.IdForecastPeriodo==p.IdForecastPeriodo).ToArray();
            if(s.Length==0 || string.IsNullOrWhiteSpace(destino.NombreProyectoSnapshot))throw new ConfiguracionConceptoException("FORECAST_SNAPSHOT_INCOMPLETO");
            var departamentos=string.Join(" / ",s.Select(s=>s.NombreDepartamento).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
            var puestos=string.Join(" / ",s.Select(s=>s.NombrePuesto).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
            var dims=departamentos+" · "+puestos;
            var key=persona.IdForecastParticipante.ToString(CultureInfo.InvariantCulture);var codigo=persona.CodigoParticipante;var etiqueta=persona.Etiqueta;var detalle=dims;
            switch(filtro.Dimension)
            {
                case DimensionesResultadoForecast.Proyecto:key=(destino.IdProyecto??0).ToString(CultureInfo.InvariantCulture);codigo=destino.CodigoProyectoSnapshot??"SIN_ASIGNAR";etiqueta=destino.NombreProyectoSnapshot;detalle="Destino conservado en snapshot";break;
                case DimensionesResultadoForecast.Periodo:key=p.IdForecastPeriodo.ToString(CultureInfo.InvariantCulture);codigo=p.NumeroOrden.ToString(CultureInfo.InvariantCulture);etiqueta=FormattableString.Invariant($"{p.FechaInicio:yyyy-MM-dd} — {p.FechaFin:yyyy-MM-dd}");detalle=p.TipoPeriodo;break;
                case DimensionesResultadoForecast.Departamento:key=string.Join(",",s.Select(s=>s.IdDepartamento).Distinct().Order());codigo=s.Select(s=>s.IdDepartamento).Distinct().Count()>1?"MULTIPLE":key;etiqueta=departamentos;detalle=codigo=="MULTIPLE"?"Dimensiones múltiples; ver segmentos, sin asignación arbitraria a un departamento.":departamentos;break;
                case DimensionesResultadoForecast.Colaborador:break;
                default:throw new ValidationException("Dimensión no válida.");
            }
            filas.Add((key,codigo,etiqueta,detalle,persona.IdForecastParticipante,d,apps.Single(a=>a.IdForecastDetalle==d.IdForecastDetalle)));
        }
        var salida=filas.GroupBy(f=>f.Clave).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>
        {
            var primero=g.First();
            decimal S(string efecto)=>g.Where(f=>f.A.Efecto==efecto).Sum(f=>f.D.MontoProyectado);
            var bruto=S(nameof(EfectoMonetario.IngresoSalarial))-S(nameof(EfectoMonetario.ReduccionDevengo));
            var ded=S(nameof(EfectoMonetario.DeduccionLegal))+S(nameof(EfectoMonetario.DeduccionInterna));var patronal=S(nameof(EfectoMonetario.CargaPatronal));
            return new FilaResultadoForecast{Clave=filtro.Dimension+":"+g.Key,Codigo=primero.Codigo,Etiqueta=primero.Etiqueta,
                Detalle=string.Join("; ",g.Select(f=>f.Detalle).Distinct().Order(StringComparer.Ordinal)),CantidadParticipantes=g.Select(f=>f.Persona).Distinct().Count(),
                SalarioBrutoProyectado=bruto,DeduccionesProyectadas=ded,SalarioNetoProyectado=bruto+S(nameof(EfectoMonetario.IngresoNoSalarial))-ded,
                CargasPatronalesProyectadas=patronal,CostoLaboralTotalProyectado=bruto+patronal,
                Conceptos=g.GroupBy(f=>f.D.IdConceptoPlanillaVersion).OrderBy(c=>c.First().D.Concepto,StringComparer.Ordinal).ThenBy(c=>c.Key).Select(c=>
                    new ConceptoResultadoForecast(c.First().D.Concepto,c.First().A.DescripcionConcepto,c.Sum(f=>f.D.MontoBase),c.Sum(f=>f.D.MontoAjuste),c.Sum(f=>f.D.MontoProyectado))
                    {IdConceptoVersion=c.Key,Efecto=c.First().A.Efecto,
                        ObservacionesMinimas=c.Min(f=>f.D.CantidadObservaciones),ObservacionesMaximas=c.Max(f=>f.D.CantidadObservaciones),
                        HistoricoLimitado=c.Any(f=>f.D.HistoricoLimitado)}).ToArray()};
        }).ToArray();
        var total=salida.Sum(f=>f.CostoLaboralTotalProyectado);
        if(filtro.IdForecastPeriodo is null && total!=e.MontoProyectadoTotal)throw new ConfiguracionConceptoException("FORECAST_SNAPSHOT_NO_CONCILIA");
        return new(){IdForecastEscenario=id,NombreEscenario=e.Nombre,EstadoEscenario=e.EstadoEscenario,FechaCalculo=e.FechaCalculo,VersionMotor=e.VersionMotor,
            CantidadFuentesHistoricas=e.PeriodosHistoricosConsiderados,
            HistoricoLimitado=e.TieneHistoricoLimitado || ds.Any(d=>d.HistoricoLimitado),Dimension=filtro.Dimension,IdForecastPeriodo=filtro.IdForecastPeriodo,MontoProyectadoTotalEscenario=e.MontoProyectadoTotal,
            MontoProyectadoTotalConsulta=total,BrutoSalarialTotalConsulta=salida.Sum(f=>f.SalarioBrutoProyectado),CargasPatronalesTotalConsulta=salida.Sum(f=>f.CargasPatronalesProyectadas),
            DeduccionesTotalConsulta=salida.Sum(f=>f.DeduccionesProyectadas),SalarioNetoTotalConsulta=salida.Sum(f=>f.SalarioNetoProyectado),EsConsistenteConEscenario=filtro.IdForecastPeriodo is null?true:null,
            Periodos=periodos.Select(p=>new PeriodoResultadoForecastOpcion(p.IdForecastPeriodo,p.NumeroOrden,p.TipoPeriodo,p.FechaInicio,p.FechaFin)).ToArray(),Filas=salida,
            Segmentos=segmentos.Select(s=>new SegmentoResultadoForecast(s.IdForecastParticipante,s.IdForecastPeriodo,s.FechaInicio,s.FechaFin,s.NombrePuesto,s.NombreDepartamento,s.NombreJornada,s.SalarioMensual,s.DiasBase30,s.OrigenCondicion)).ToArray()};
    }
}

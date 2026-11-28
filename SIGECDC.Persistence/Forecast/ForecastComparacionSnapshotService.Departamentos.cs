using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastComparacionSnapshotService
{
    private sealed record AporteDepartamento(int Id,string Lado,string Nombre,decimal Monto,FuenteComparacion[] Fuentes);

    private async Task<(IReadOnlyList<DesviacionDepartamentoComparacion> Filas,string? Motivo)> AgruparDepartamentosAsync(
        IReadOnlyList<FilaComparacionPersistida> filas,IReadOnlyDictionary<long,ForecastDetalle> detallesForecast,ForecastSegmentoProyectado[] proyectados,
        SegmentoCalculoPlanilla[] reales,DateTime inicio,DateTime fin,CancellationToken ct)
    {
        const string incompleta="No se puede agrupar por departamento: falta identidad o cobertura histórica suficiente. Los demás resultados permanecen disponibles.";
        const string heterogenea="No se puede agrupar por departamento: un participante cambia de departamento dentro del período y el snapshot no conserva el reparto de su costo total. No se distribuyen importes estimados.";
        // J fija la versión de condición, que contiene la identidad del departamento.
        // Las etiquetas se leen de J/L; nunca del colaborador ni del departamento actual.
        var ids=reales.Where(s=>s.IdCondicionLaboralVersion.HasValue).Select(s=>s.IdCondicionLaboralVersion!.Value).Distinct().ToArray();
        var condiciones=await db.CondicionesLaboralesVersion.AsNoTracking().Where(c=>ids.Contains(c.IdCondicionLaboralVersion))
            .Select(c=>new {c.IdCondicionLaboralVersion,c.IdContrato,c.IdDepartamento,c.FechaVigenciaInicio,c.FechaVigenciaFin})
            .ToDictionaryAsync(c=>c.IdCondicionLaboralVersion,ct);
        var aportes=new List<AporteDepartamento>();
        foreach(var fila in filas.Where(f=>f.Granularidad=="Participante"))
        {
            foreach(var lado in new[]{"Forecast","Real"})
            {
                var monto=lado=="Forecast"?fila.MontoForecast:fila.MontoReal;
                if(monto is null)continue; // Ausencia no equivale a cero.
                var fuentes=fila.Fuentes.Where(f=>f.Lado==lado).ToArray();
                var segmentos=new List<(int Id,string Nombre,DateTime Inicio,DateTime Fin)>();
                if(lado=="Forecast")
                {
                    // Las dimensiones corresponden al participante de esta fila, no a maestros vivos.
                    var dimensiones=fila.Dimensiones.Where(d=>d.Lado==lado).ToArray();
                    // Identificar por las fuentes exactas ya validadas en LeerInternoAsync.
                    var detalleIds=fuentes.Where(f=>f.IdForecastDetalle.HasValue).Select(f=>f.IdForecastDetalle!.Value).ToArray();
                    var participantes=detalleIds.Select(id=>detallesForecast[id].IdForecastParticipante).Distinct().ToArray();
                    if(participantes.Length!=1)return([],incompleta);
                    segmentos.AddRange(proyectados.Where(s=>s.IdForecastParticipante==participantes[0])
                        .Select(s=>(s.IdDepartamento,s.NombreDepartamento,s.FechaInicio,s.FechaFin)));
                    if(segmentos.Count!=dimensiones.Length)return([],incompleta);
                }
                else
                {
                    var detalles=fuentes.Where(f=>f.IdDetallePlanilla.HasValue).Select(f=>f.IdDetallePlanilla!.Value).ToHashSet();
                    foreach(var s in reales.Where(s=>detalles.Contains(s.IdDetallePlanilla)))
                    {
                        if(s.IdCondicionLaboralVersion is not {} id || !condiciones.TryGetValue(id,out var c)
                            || c.IdContrato!=s.IdContratoOrigen || c.FechaVigenciaInicio>s.FechaInicio
                            || c.FechaVigenciaFin is {} hasta && hasta<s.FechaFin)return([],incompleta);
                        segmentos.Add((c.IdDepartamento,s.NombreDepartamento??"",s.FechaInicio,s.FechaFin));
                    }
                }
                if(segmentos.Count==0 || segmentos.Any(s=>s.Id<=0 || string.IsNullOrWhiteSpace(s.Nombre)
                    || s.Inicio>s.Fin || s.Inicio<inicio || s.Fin>fin))return([],incompleta);
                var ordenados=segmentos.OrderBy(s=>s.Inicio).ToArray();
                // Altas y salidas parciales son válidas. Un hueco entre segmentos no demuestra historia completa.
                if(ordenados.Zip(ordenados.Skip(1)).Any(p=>p.Second.Inicio!=p.First.Fin.AddDays(1)))return([],incompleta);
                if(segmentos.Select(s=>s.Id).Distinct().Count()!=1)return([],heterogenea);
                var nombre=string.Join(" / ",segmentos.Select(s=>s.Nombre.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
                aportes.Add(new(segmentos[0].Id,lado,nombre,monto.Value,fuentes));
            }
        }
        var total=filas.Single(f=>f.Granularidad=="Total");
        if(aportes.Count==0 || aportes.Where(a=>a.Lado=="Forecast").Sum(a=>a.Monto)!=total.MontoForecast
            || aportes.Where(a=>a.Lado=="Real").Sum(a=>a.Monto)!=total.MontoReal)
            return([],"No se puede agrupar por departamento: los importes históricos no concilian con el total.");
        var salida=new List<DesviacionDepartamentoComparacion>();
        foreach(var grupo in aportes.GroupBy(a=>a.Id).OrderBy(g=>g.Key))
        {
            var forecast=grupo.Where(a=>a.Lado=="Forecast").ToArray();var real=grupo.Where(a=>a.Lado=="Real").ToArray();
            decimal? f=forecast.Length==0?null:forecast.Sum(a=>a.Monto),r=real.Length==0?null:real.Sum(a=>a.Monto);
            var diferencia=ComparacionFinanciera.Comparar(f,r); // Misma fórmula M, aplicada a las bases agregadas.
            string N(IEnumerable<AporteDepartamento> valores)=>string.Join(" / ",valores.Select(a=>a.Nombre).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
            salida.Add(new(grupo.Key,N(forecast),N(real),f,r,diferencia.Firmada,diferencia.Absoluta,diferencia.Porcentual,
                diferencia.Motivo,grupo.SelectMany(a=>a.Fuentes).ToArray()));
        }
        return(salida,null);
    }
}

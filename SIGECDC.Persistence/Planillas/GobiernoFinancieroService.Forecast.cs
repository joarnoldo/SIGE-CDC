using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Planillas;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Persistence.Planillas;

internal sealed record ConfiguracionFinancieraForecast(IReadOnlyList<ConceptoMonetarioVersion> Conceptos,
    IReadOnlyList<ValorCuotaVersionado> Cuotas, IReadOnlyList<VersionFiscalRenta> Fiscales,
    IReadOnlyList<AcreditacionRenta> Creditos, IReadOnlyList<RiesgoTrabajoVersionado> Riesgos);

public sealed partial class GobiernoFinancieroService
{
    // Adaptador dentro de la transacción del escenario; comparte las mismas comprobaciones C
    // de carga/revisión/huella que Planilla. No abre una segunda vista ni modifica normativa.
    internal async Task<ConfiguracionFinancieraForecast> LeerConfiguracionForecastAsync(PeriodoSalarial periodo,
        IReadOnlyList<long> colaboradores, IReadOnlyList<long> riesgos, string actor, CancellationToken ct)
    {
        ExigirTransaccionForecast(); await AutorizarAsync(actor,ct); periodo.Validar();
        var inicio=new DateTime(periodo.Inicio.Year,periodo.Inicio.Month,1); var fin=inicio.AddMonths(1).AddDays(-1);
        var codigos=await contexto.ConceptosPlanilla.AsNoTracking().Select(c=>c.Codigo).ToArrayAsync(ct);
        var conceptos=await LeerConceptosAsync(codigos,ct);
        var cuotas=await LeerValoresCuotasAsync(new(inicio,fin,TiposPeriodoPlanilla.Mensual),ct);
        var fiscales=await LeerVersionesFiscalesAsync(inicio,fin,ct);
        var personales=await FiltrarClaves(contexto.CreditosFiscalesColaborador.AsNoTracking(),c=>c.IdColaborador,colaboradores)
            .Where(c=>c.FechaVigenciaInicio<=fin && (c.FechaVigenciaFin==null || c.FechaVigenciaFin>=inicio)).ToArrayAsync(ct);
        var acreditaciones=new List<AcreditacionRenta>();
        foreach(var c in personales)
            acreditaciones.Add(new(c.IdCreditoFiscalColaborador,c.IdColaborador,c.IdCreditoFiscalNormativo,c.CantidadAcreditada,
                new(c.FechaVigenciaInicio,c.FechaVigenciaFin),c.ReferenciaSoporte,c.AprobadoPor,c.FechaAprobacion,
                c.EstadoRegistro==Activo,await AcreditacionRentaDemostradaAsync(c,ct)));
        var anclas=await FiltrarClaves(contexto.ConfiguracionesRiesgoTrabajo.AsNoTracking(),r=>r.IdConfiguracionRiesgoTrabajo,riesgos).ToArrayAsync(ct);
        var configuraciones=new List<RiesgoTrabajoVersionado>();
        foreach(var familia in anclas.Select(a=>(a.NumeroPoliza,a.CodigoActividad)).Distinct())
        {
            var filas=await contexto.ConfiguracionesRiesgoTrabajo.AsNoTracking().Include(r=>r.VersionNormativa)
                .Where(r=>r.NumeroPoliza==familia.NumeroPoliza && r.CodigoActividad==familia.CodigoActividad).ToArrayAsync(ct);
            foreach(var r in filas)
                configuraciones.Add(new(r.IdConfiguracionRiesgoTrabajo,r.NumeroPoliza,r.CodigoActividad,r.DescripcionActividad,
                    r.Tarifa,r.Descuento,r.Recargo,new(r.FechaVigenciaInicio,r.FechaVigenciaFin),r.ReferenciaDocumento,
                    r.IdVersionNormativa,r.CreadoPor??"",r.FechaCreacion,r.EstadoRegistro==Activo,await RiesgoDemostradoAsync(r,ct)));
        }
        return new(conceptos,cuotas,fiscales,acreditaciones,configuraciones);
    }

    internal async Task<ResultadoTemporalSalarial> LeerCondicionesForecastAsync(PeriodoSalarial periodo,string soporte,string actor,CancellationToken ct)
    {
        ExigirTransaccionForecast();await AutorizarAsync(actor,ct);
        return await ResolverTemporalEnTransaccionAsync(periodo,soporte,ct);
    }

    private void ExigirTransaccionForecast()
    {
        if(contexto.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel!=IsolationLevel.Serializable)
            throw new InvalidOperationException("Forecast requiere la composición Serializable del escenario.");
    }

    internal async Task<CatalogosTemporalesSalariales> LeerCatalogosForecastAsync(PeriodoSalarial periodo,
        string[] jornadas,long[] ocupaciones,string actor,CancellationToken ct)
    {
        ExigirTransaccionForecast();await AutorizarAsync(actor,ct);
        return await CatalogosTemporalesAsync(periodo,jornadas,ocupaciones,ct);
    }
}

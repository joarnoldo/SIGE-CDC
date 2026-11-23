using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<ResultadoCuotasSociales> PrepararCuotasEnTransaccionAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, string actor, AcumuladoMensualCuotas? acumulado = null, CancellationToken ct = default)
        => PrepararCuotasConFronterasAsync(idPeriodo, referenciaRelacionOrdinaria, aplicabilidades, actor, acumulado, null, ct);

    private async Task<ResultadoCuotasSociales> PrepararCuotasConFronterasAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, string actor, AcumuladoMensualCuotas? acumulado,
        IReadOnlyDictionary<long, IReadOnlyList<DateTime>>? fronterasAdicionales, CancellationToken ct)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
            throw new InvalidOperationException("G requiere la transacción Serializable del consumidor FIN-01, sin cambios pendientes.");
        aplicabilidades = Array.AsReadOnly(aplicabilidades.ToArray());
        if (acumulado is not null) acumulado = acumulado with
        {
            Lineas = Array.AsReadOnly(acumulado.Lineas.ToArray()),
            CuotasPrevias = Array.AsReadOnly(acumulado.CuotasPrevias.Select(a => a with { Aportes = Array.AsReadOnly(a.Aportes.ToArray()) }).ToArray()),
            Colaboradores = acumulado.Colaboradores is null ? null : Array.AsReadOnly(acumulado.Colaboradores.ToArray())
        };
        contexto.ChangeTracker.Clear();
        var rango = await PeriodoIncidenciasEditableAsync(idPeriodo, ct);
        await AutorizarAsync(actor, ct);
        // No se concede autoridad a un actor escrito por el caller en la evidencia.
        foreach (var a in aplicabilidades)
            if (a.ConfirmadoPor != actor)
                return ProductorCuotasSociales.Fallido([new("ACTOR_APLICABILIDAD_INCOMPATIBLE", a.Componente, rango.Inicio, a.IdColaborador)]);
        // El corte final verifica también las versiones anteriores del mes bajo el mismo lock/transacción.
        var rangoNormativo = rango.Tipo == TiposPeriodoPlanilla.Quincenal && rango.Inicio.Day == 16
            ? new PeriodoSalarial(new(rango.Inicio.Year, rango.Inicio.Month, 1), rango.Fin, TiposPeriodoPlanilla.Mensual) : rango;
        var valores = await LeerValoresCuotasAsync(rangoNormativo, ct);
        var conceptos = await LeerConceptosAsync(CatalogoCuotasSociales.Componentes.Where(c => c.Sujeto != "Estado").Select(c => c.Codigo).ToArray(), ct);
        var fronteras = new Dictionary<long, IReadOnlyList<DateTime>>();
        foreach (var persona in aplicabilidades.GroupBy(a => a.IdColaborador))
        {
            var pertinentes = valores.Where(v => persona.Any(a => a.ClaveAplicacion == v.ClaveAplicacion
                && CatalogoCuotasSociales.Componentes.Any(c => c.Sujeto != "Estado" && c.Codigo == a.Componente
                    && (v.Codigo == c.Codigo || v.Codigo == c.CodigoMinimo))));
            fronteras.Add(persona.Key, ResolucionValoresCuotas.Fronteras(pertinentes.SelectMany(v => new[] { v.Vigencia, v.VigenciaNormativa })
                .Concat(conceptos.Select(c => c.Vigencia)).Concat(persona.Select(a => a.Vigencia)), rango.Fin).Distinct().Order().ToArray());
        }
        if (fronterasAdicionales is not null)
            foreach (var (persona, fechas) in fronterasAdicionales)
                fronteras[persona] = (fronteras.TryGetValue(persona, out var previas) ? previas : [])
                    .Concat(fechas).Distinct().Order().ToArray();
        // Una sola transacción: normativa C, D, E y confirmaciones F. E produce ordinario usando
        // fronteras de D; G no reparte una línea materializada ni vuelve a calcular salario/HE.
        var hechos = await PrepararIncidenciasConFronterasAsync(idPeriodo, referenciaRelacionOrdinaria, actor, fronteras, ct);
        return ProductorCuotasSociales.Producir(rango, hechos.Conciliacion?.Lineas ?? [], valores, conceptos,
            aplicabilidades, hechos.Bloqueos, acumulado);
    }

    private async Task<IReadOnlyList<ValorCuotaVersionado>> LeerValoresCuotasAsync(PeriodoSalarial periodo, CancellationToken ct)
    {
        var filas = await FiltrarClaves(contexto.ValoresParametrosNormativos.AsNoTracking()
            .Include(v => v.ParametroNormativo).Include(v => v.VersionNormativa), v => v.ParametroNormativo!.Codigo,
            CatalogoCuotasSociales.Claves)
            .Where(v => v.FechaVigenciaInicio <= periodo.Fin && (v.FechaVigenciaFin == null || v.FechaVigenciaFin >= periodo.Inicio))
            .OrderBy(v => v.IdValorParametroNormativo).ToArrayAsync(ct);
        var revisadas = new HashSet<long>();
        foreach (var norma in filas.Select(v => v.VersionNormativa).Where(NormativaUtilizable).DistinctBy(v => v!.IdVersionNormativa))
        {
            var huella = await HuellaRevisionAsync(norma!.IdVersionNormativa, ct);
            if (huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "CargarNormativa", ct)
                && huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "ActivarNormativa", ct)) revisadas.Add(norma.IdVersionNormativa);
        }
        return filas.Select(v =>
        {
            var p = v.ParametroNormativo!; var n = v.VersionNormativa!;
            return new ValorCuotaVersionado(p.IdParametroNormativo, v.IdValorParametroNormativo, n.IdVersionNormativa,
                p.Codigo, p.Institucion, p.Sujeto, p.Unidad, p.CodigoBase, v.ClaveAplicacion,
                new(v.FechaVigenciaInicio, v.FechaVigenciaFin), new(n.FechaVigenciaInicio, n.FechaVigenciaFin), v.ValorDecimal,
                p.EstadoRegistro == Activo && n.EstadoRegistro == Activo, revisadas.Contains(n.IdVersionNormativa)
                    && p.EsObligatorio && n.Institucion == p.Institucion,
                n.FuenteOficialUrl, n.HuellaContenido ?? "");
        }).ToArray();
    }
}

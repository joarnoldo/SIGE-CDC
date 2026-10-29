using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public interface ICostosPatronalesService
{
    // El consumidor conserva la transacción Serializable/lock FIN-01. No se escriben snapshots ni estados.
    // Aplicabilidad RT y hechos internos requieren evidencia empresarial real del adaptador autorizado.
    Task<ResultadoCostoLaboral> PrepararCostoLaboralEnTransaccionAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> cuotas, IReadOnlyList<AplicabilidadRiesgoTrabajo> riesgos,
        IReadOnlyList<HechoCostoPatronal> otrosCostos, string actor, AcumuladoMensualRenta? acumulado = null,
        CancellationToken ct = default);
}

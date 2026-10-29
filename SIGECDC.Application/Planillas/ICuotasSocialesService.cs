using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public interface ICuotasSocialesService
{
    // El adaptador servidor aporta evidencia empresarial explícita y el consumidor conserva
    // la misma transacción Serializable de FIN-01 hasta terminar. No aprueba ni persiste snapshots.
    // La ausencia de aplicabilidad acreditada bloquea; no existe un default empresarial ordinario.
    // Q2 requiere líneas y cuotas Q1 por colaborador/mes con evidencia explícita del adaptador.
    // Se contrasta el detalle previo con normativa C en la misma transacción; no reconstruye un snapshot J.
    // Aplicaciones conserva la obligación mensual; LineasCuotasCorte/Regularizaciones expresan solo el corte.
    Task<ResultadoCuotasSociales> PrepararCuotasEnTransaccionAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, string actor,
        AcumuladoMensualCuotas? acumulado = null, CancellationToken ct = default);
}

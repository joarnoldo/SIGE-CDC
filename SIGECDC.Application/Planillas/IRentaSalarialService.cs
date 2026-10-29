using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public interface IRentaSalarialService
{
    // El consumidor conserva su transacción Serializable y el lock FIN-01 hasta terminar.
    // Q2 aporta detalle autorizado previo de H y G, contrastado como una misma procedencia.
    // No persiste snapshots, modifica estados ni autoriza pago.
    Task<ResultadoRentaSalarial> PrepararRentaEnTransaccionAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, string actor,
        AcumuladoMensualRenta? acumulado = null, CancellationToken ct = default);
}

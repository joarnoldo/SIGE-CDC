using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

// Entradas empresariales explícitas; el acumulado solo puede proceder de una ejecución persistida exacta.
public sealed record SolicitudSnapshotPlanilla(long IdPeriodo, string ReferenciaRelacionOrdinaria,
    IReadOnlyList<AplicabilidadCuota> Cuotas, IReadOnlyList<AplicabilidadRiesgoTrabajo> Riesgos,
    IReadOnlyList<HechoCostoPatronal> OtrosCostos, long? IdEjecucionPrevia = null);

public interface ISnapshotPlanillaService
{
    Task<long> CalcularSnapshotAsync(SolicitudSnapshotPlanilla solicitud, string actor, CancellationToken ct = default);
    Task ReabrirSnapshotAsync(long idPeriodo, long idEjecucionEsperada, string motivo, string actor, CancellationToken ct = default);
    Task AprobarSnapshotAsync(long idPeriodo, long idEjecucionEsperada, string actor, CancellationToken ct = default);
    Task CerrarSnapshotAsync(long idPeriodo, long idEjecucionEsperada, string actor, CancellationToken ct = default);
    // Solo snapshot separado del tracking; no completa legado ni navega maestros vivos. No habilita colillas K.
    Task<EjecucionPlanilla> LeerSnapshotAsync(long idEjecucion, string actor, CancellationToken ct = default);
}

using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public sealed record CatalogosTemporalesSalariales(IReadOnlyList<JornadaTemporal> Jornadas,
    IReadOnlyList<MinimoTemporal> Minimos);

public interface IResolucionTemporalFinancieraService
{
    Task<ResultadoTemporalSalarial> ResolverPeriodoTemporalAsync(PeriodoSalarial periodo, string actor,
        string? referenciaAplicabilidadOrdinaria = null, CancellationToken ct = default);
    // Permite resolver evidencia temporal explícita (incluida la futura proyección) con el mismo algoritmo puro.
    // No convierte una asociación actual en una asignación histórica.
    Task<CatalogosTemporalesSalariales> ConsultarCatalogosTemporalesAsync(PeriodoSalarial periodo,
        IReadOnlyList<string> codigosJornada, IReadOnlyList<long> idsOcupacion, string actor, CancellationToken ct = default);
}

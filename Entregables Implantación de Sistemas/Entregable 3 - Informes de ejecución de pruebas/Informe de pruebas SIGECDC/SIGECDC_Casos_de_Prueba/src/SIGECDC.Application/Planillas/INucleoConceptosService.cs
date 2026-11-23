using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

// La ocurrencia y autorización se aportan explícitamente; E no inventa frecuencia ni prorrateo interno.
public sealed record HechoParametroInterno(string ClaveHecho, string CodigoParametro, long IdColaborador,
    DateTime FechaHecho, ReferenciaSegmentoTemporal? Segmento, string ReferenciaSoporte, string ReferenciaAutorizacion);
public sealed record SolicitudNucleoConceptos(PeriodoSalarial Periodo, long? IdPeriodoPlanilla,
    string? ReferenciaRelacionOrdinaria, IReadOnlyList<HechoParametroInterno> Internos);
public sealed record ResultadoNucleoConceptos(ConciliacionLineas? Conciliacion, IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    // Evidencia derivada de cada línea, conservando la asociación y su orden determinista.
    public IReadOnlyList<ParametroInternoResuelto> Parametros => Array.AsReadOnly(Conciliacion?.Lineas
        .Select(l => l.ParametroAplicado).OfType<ParametroInternoResuelto>().ToArray() ?? []);
    public bool EsValidoNucleo => Bloqueos.Count == 0 && Conciliacion is not null;
    public string Estado => EsValidoNucleo ? "INCOMPLETO" : "BLOQUEADO";
    public bool EsAutorizable => false;
    public IReadOnlyList<string> Pendientes => Array.AsReadOnly(new[] { "F: incidencias", "G: CCSS/LPT", "H: renta", "I: RT/costo", "J: snapshot", "FIN-02" });
}
public sealed record SolicitudOverrideInterno(int IdParametro, AmbitoParametroInterno Ambito, long IdDestino,
    decimal? ValorDecimal, string? ValorTexto, string ReferenciaSoporte);

public interface INucleoConceptosService
{
    Task<ResultadoNucleoConceptos> PrepararAsync(SolicitudNucleoConceptos solicitud, string actor, CancellationToken ct = default);
    Task<ParametroInternoResuelto> ResolverParametroAsync(string codigo, DateTime fecha, long idColaborador,
        long? idPeriodo, string actor, CancellationToken ct = default);
    Task<long> RegistrarOverrideInternoAsync(SolicitudOverrideInterno solicitud, string actor, CancellationToken ct = default);
}

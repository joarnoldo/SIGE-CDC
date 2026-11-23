using SIGECDC.Domain.Forecast;

namespace SIGECDC.Application.Forecast;

public sealed record ImporteComparadoForecast(string Nombre, decimal? Base, decimal? Alternativo)
{
    // Primitiva de diferencias ya certificada; no ejecuta ni materializa M.
    public DiferenciaComparativa Diferencia => ComparacionFinanciera.Comparar(Base, Alternativo);
    public string? Motivo => Diferencia.Motivo switch
    {
        "SinCorrespondencia" => "Sin correspondencia: dimensión o concepto ausente en un escenario.",
        "BaseForecastCero" => "Base cero: porcentaje no definido.",
        _ => null
    };
}

public sealed record FilaSnapshotsComparadosForecast(string Clave, string? EtiquetaBase,
    string? EtiquetaAlternativa, string? DetalleBase, string? DetalleAlternativo,
    IReadOnlyList<ImporteComparadoForecast> Importes,
    IReadOnlyList<ImporteComparadoForecast> Conceptos);

public sealed record ComparacionSnapshotsForecast(IReadOnlyList<ImporteComparadoForecast> Totales,
    IReadOnlyList<FilaSnapshotsComparadosForecast> Filas);

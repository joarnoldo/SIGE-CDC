using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudCompararForecastReal : IValidatableObject
{
    public string Dimension { get; set; } = DimensionesComparacionRealForecast.Periodo;

    public long? IdForecastPeriodo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!DimensionesComparacionRealForecast.EsValida(Dimension))
        {
            yield return new ValidationResult(
                "La dimensión de comparación real no es válida.",
                [nameof(Dimension)]);
        }

        if (IdForecastPeriodo is <= 0)
        {
            yield return new ValidationResult(
                "El período seleccionado no es válido.",
                [nameof(IdForecastPeriodo)]);
        }

        if (DimensionesComparacionRealForecast.RequierePeriodo(Dimension)
            && !IdForecastPeriodo.HasValue)
        {
            yield return new ValidationResult(
                "Debe seleccionar un período disponible para la dimensión solicitada.",
                [nameof(IdForecastPeriodo)]);
        }

        if (Dimension == DimensionesComparacionRealForecast.Periodo
            && IdForecastPeriodo.HasValue)
        {
            yield return new ValidationResult(
                "La comparación por período consulta el escenario completo.",
                [nameof(IdForecastPeriodo)]);
        }
    }
}

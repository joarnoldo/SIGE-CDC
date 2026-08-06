using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class FiltroResultadosForecast : IValidatableObject
{
    public string Dimension { get; set; } = DimensionesResultadoForecast.Periodo;

    public long? IdForecastPeriodo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!DimensionesResultadoForecast.EsValida(Dimension))
        {
            yield return new ValidationResult(
                "La dimensión de resultados no es válida.",
                [nameof(Dimension)]);
        }

        if (IdForecastPeriodo is <= 0)
        {
            yield return new ValidationResult(
                "El período seleccionado no es válido.",
                [nameof(IdForecastPeriodo)]);
        }
    }
}

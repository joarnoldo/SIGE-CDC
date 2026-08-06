using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudCompararEscenariosForecast : IValidatableObject
{
    public long IdEscenarioBase { get; set; }

    public long IdEscenarioAlternativo { get; set; }

    public string Dimension { get; set; } = DimensionesResultadoForecast.Periodo;

    public int? NumeroOrdenPeriodo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdEscenarioBase <= 0)
        {
            yield return new ValidationResult(
                "El escenario base no es válido.",
                [nameof(IdEscenarioBase)]);
        }

        if (IdEscenarioAlternativo <= 0)
        {
            yield return new ValidationResult(
                "El escenario alternativo no es válido.",
                [nameof(IdEscenarioAlternativo)]);
        }

        if (IdEscenarioBase > 0 && IdEscenarioBase == IdEscenarioAlternativo)
        {
            yield return new ValidationResult(
                "Debe seleccionar dos escenarios diferentes.",
                [nameof(IdEscenarioAlternativo)]);
        }

        if (!DimensionesResultadoForecast.EsValida(Dimension))
        {
            yield return new ValidationResult(
                "La dimensión de comparación no es válida.",
                [nameof(Dimension)]);
        }

        if (NumeroOrdenPeriodo is <= 0)
        {
            yield return new ValidationResult(
                "El período seleccionado no es válido.",
                [nameof(NumeroOrdenPeriodo)]);
        }
    }
}

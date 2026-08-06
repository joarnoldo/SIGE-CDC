using System.ComponentModel.DataAnnotations;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudPeriodoForecast : IValidatableObject
{
    [Required(ErrorMessage = "Seleccione el tipo de período.")]
    public string TipoPeriodo { get; set; } = TiposPeriodoPlanilla.Mensual;

    [Required(ErrorMessage = "La fecha inicial del período es obligatoria.")]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha final del período es obligatoria.")]
    public DateTime? FechaFin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(TipoPeriodo)
            && !TiposPeriodoPlanilla.Permitidos.Contains(TipoPeriodo.Trim()))
        {
            yield return new ValidationResult(
                "El tipo de período debe ser Mensual o Quincenal.",
                [nameof(TipoPeriodo)]);
        }

        if (FechaInicio.HasValue && FechaInicio.Value.Date <= DateTime.Today)
        {
            yield return new ValidationResult(
                "La fecha inicial debe ser posterior al día actual.",
                [nameof(FechaInicio)]);
        }

        if (FechaInicio.HasValue
            && FechaFin.HasValue
            && FechaFin.Value.Date < FechaInicio.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha final no puede ser anterior a la fecha inicial.",
                [nameof(FechaFin)]);
        }
    }
}

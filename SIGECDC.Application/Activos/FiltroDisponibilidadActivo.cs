using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class FiltroDisponibilidadActivo : IValidatableObject
{
    [Required(ErrorMessage = "La fecha inicial es obligatoria.")]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha final es obligatoria.")]
    public DateTime? FechaFin { get; set; }

    public int? IdEstadoActivo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue
            && FechaFin.HasValue
            && FechaFin.Value.Date < FechaInicio.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha final no puede ser anterior a la fecha inicial.",
                [nameof(FechaFin)]);
        }

        if (IdEstadoActivo.HasValue && IdEstadoActivo.Value <= 0)
        {
            yield return new ValidationResult(
                "El estado seleccionado no es válido.",
                [nameof(IdEstadoActivo)]);
        }
    }
}

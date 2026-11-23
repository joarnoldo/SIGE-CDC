using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Planillas;

public sealed class SolicitudPeriodoPlanilla : IValidatableObject
{
    [Required(ErrorMessage = "El código del período es obligatorio.")]
    [StringLength(30, ErrorMessage = "El código no debe superar los 30 caracteres.")]
    public string CodigoPeriodo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del período es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no debe superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione el tipo de período.")]
    public string TipoPeriodo { get; set; } = "Quincenal";

    [Required(ErrorMessage = "La fecha inicial es obligatoria.")]
    public DateTime? FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha final es obligatoria.")]
    public DateTime? FechaFin { get; set; } = DateTime.Today;

    [StringLength(500, ErrorMessage = "Las observaciones no deben superar los 500 caracteres.")]
    public string? Observaciones { get; set; }

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
    }
}

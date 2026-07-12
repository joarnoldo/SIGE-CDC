using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudAsignacionActivoProyecto : IValidatableObject
{
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un activo.")]
    public long IdActivo { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un proyecto.")]
    public long IdProyecto { get; set; }

    [Required(ErrorMessage = "La fecha inicial es obligatoria.")]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha final es obligatoria.")]
    public DateTime? FechaFin { get; set; }

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

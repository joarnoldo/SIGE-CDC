using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Operaciones;

public sealed class SolicitudProyecto : IValidatableObject
{
    public long IdProyecto { get; set; }

    [Required(ErrorMessage = "El codigo del proyecto es obligatorio.")]
    [StringLength(30, ErrorMessage = "El codigo no puede superar 30 caracteres.")]
    public string CodigoProyecto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string NombreProyecto { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripcion no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFinEstimada { get; set; }

    public DateTime? FechaFinReal { get; set; }

    [StringLength(150, ErrorMessage = "El responsable no puede superar 150 caracteres.")]
    public string? Responsable { get; set; }

    [StringLength(200, ErrorMessage = "La ubicacion no puede superar 200 caracteres.")]
    public string? Ubicacion { get; set; }

    public int? IdEstadoProyecto { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar 500 caracteres.")]
    public string? Observaciones { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue
            && FechaFinEstimada.HasValue
            && FechaFinEstimada.Value.Date < FechaInicio.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha fin estimada no puede ser anterior a la fecha de inicio.",
                [nameof(FechaFinEstimada)]);
        }

        if (FechaInicio.HasValue
            && FechaFinReal.HasValue
            && FechaFinReal.Value.Date < FechaInicio.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha fin real no puede ser anterior a la fecha de inicio.",
                [nameof(FechaFinReal)]);
        }
    }
}

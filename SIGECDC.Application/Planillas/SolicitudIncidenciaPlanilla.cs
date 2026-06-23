using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Planillas;

public sealed class SolicitudIncidenciaPlanilla : IValidatableObject
{
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un periodo de planilla.")]
    public long IdPeriodoPlanilla { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un colaborador.")]
    public long IdColaborador { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tipo de incidencia.")]
    public int IdTipoIncidenciaPlanilla { get; set; }

    [Required(ErrorMessage = "La fecha de incidencia es obligatoria.")]
    public DateTime? FechaIncidencia { get; set; } = DateTime.Today;

    [Range(0, 999999999999.99, ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal? Cantidad { get; set; }

    [Range(0, 999999999999.99, ErrorMessage = "El monto no puede ser negativo.")]
    public decimal Monto { get; set; }

    [StringLength(300, ErrorMessage = "La descripcion no debe superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaIncidencia is null)
        {
            yield return new ValidationResult(
                "La fecha de incidencia es obligatoria.",
                [nameof(FechaIncidencia)]);
        }
    }
}

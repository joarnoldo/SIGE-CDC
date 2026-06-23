using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Planillas;

public sealed class SolicitudParametroPlanilla : IValidatableObject
{
    [Required(ErrorMessage = "El codigo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El codigo no debe superar los 100 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no debe superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripcion no debe superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de parametro.")]
    public string TipoParametro { get; set; } = "Porcentaje";

    [Range(0, 999999999999.9999, ErrorMessage = "El valor numerico no puede ser negativo.")]
    public decimal? ValorDecimal { get; set; }

    [StringLength(255, ErrorMessage = "El valor de texto no debe superar los 255 caracteres.")]
    public string? ValorTexto { get; set; }

    public DateTime? FechaVigenciaInicio { get; set; }

    public DateTime? FechaVigenciaFin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(TipoParametro, "Texto", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(ValorTexto))
            {
                yield return new ValidationResult(
                    "Ingrese el valor de texto del parametro.",
                    [nameof(ValorTexto)]);
            }
        }
        else if (ValorDecimal is null)
        {
            yield return new ValidationResult(
                "Ingrese el valor numerico del parametro.",
                [nameof(ValorDecimal)]);
        }

        if (FechaVigenciaInicio.HasValue
            && FechaVigenciaFin.HasValue
            && FechaVigenciaFin.Value.Date < FechaVigenciaInicio.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha final de vigencia no puede ser anterior a la fecha inicial.",
                [nameof(FechaVigenciaFin)]);
        }
    }
}

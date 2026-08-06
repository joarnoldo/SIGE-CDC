using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudRegistrarUsoActivo : IValidatableObject
{
    private const decimal ValorMaximoDecimal18_2 =
        9999999999999999.99m;

    [Range(1, long.MaxValue, ErrorMessage = "Debe seleccionar un activo.")]
    public long IdActivo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de medición.")]
    public int? IdTipoMedicionUso { get; set; }

    [Required(ErrorMessage = "La fecha del registro es obligatoria.")]
    public DateTime? FechaRegistro { get; set; }

    public decimal? LecturaInicial { get; set; }

    [Required(ErrorMessage = "La lectura nueva es obligatoria.")]
    public decimal? LecturaNueva { get; set; }

    [StringLength(
        500,
        ErrorMessage = "Las observaciones no pueden superar 500 caracteres.")]
    public string? Observaciones { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (FechaRegistro.HasValue
            && FechaRegistro.Value.Date > DateTime.Today)
        {
            yield return new ValidationResult(
                "La fecha del registro no puede ser futura.",
                [nameof(FechaRegistro)]);
        }

        if (!EsDecimalValido(LecturaInicial))
        {
            yield return new ValidationResult(
                "La lectura inicial debe ser mayor o igual a cero, no superar el máximo permitido y usar hasta dos decimales.",
                [nameof(LecturaInicial)]);
        }

        if (!EsDecimalValido(LecturaNueva))
        {
            yield return new ValidationResult(
                "La lectura nueva debe ser mayor o igual a cero, no superar el máximo permitido y usar hasta dos decimales.",
                [nameof(LecturaNueva)]);
        }
    }

    private static bool EsDecimalValido(decimal? valor)
    {
        return !valor.HasValue
            || (valor.Value >= 0
                && valor.Value <= ValorMaximoDecimal18_2
                && decimal.Round(valor.Value, 2) == valor.Value);
    }
}

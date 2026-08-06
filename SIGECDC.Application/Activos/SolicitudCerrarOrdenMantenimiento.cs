using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudCerrarOrdenMantenimiento
    : IDisposable, IValidatableObject
{
    private const decimal ValorMaximoDecimal18_2 =
        9999999999999999.99m;

    [Range(1, long.MaxValue, ErrorMessage = "La orden de mantenimiento es obligatoria.")]
    public long IdMantenimiento { get; set; }

    [Required(ErrorMessage = "El costo real es obligatorio.")]
    public decimal? CostoReal { get; set; }

    [Required(ErrorMessage = "El tiempo fuera de servicio es obligatorio.")]
    public decimal? TiempoFueraServicioHoras { get; set; }

    [StringLength(500, ErrorMessage = "El resultado no puede superar 500 caracteres.")]
    public string? Resultado { get; set; }

    [Required(ErrorMessage = "Debe adjuntar al menos una evidencia.")]
    [MinLength(1, ErrorMessage = "Debe adjuntar al menos una evidencia.")]
    public IReadOnlyList<SolicitudEvidenciaMantenimiento> Evidencias { get; set; } = [];

    public void Dispose()
    {
        foreach (var evidencia in Evidencias)
        {
            evidencia.Dispose();
        }
    }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!EsDecimalValido(CostoReal))
        {
            yield return new ValidationResult(
                "El costo real debe ser mayor o igual a cero, no superar el máximo permitido y usar hasta dos decimales.",
                [nameof(CostoReal)]);
        }

        if (!EsDecimalValido(TiempoFueraServicioHoras))
        {
            yield return new ValidationResult(
                "El tiempo fuera de servicio debe ser mayor o igual a cero, no superar el máximo permitido y usar hasta dos decimales.",
                [nameof(TiempoFueraServicioHoras)]);
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

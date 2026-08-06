using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudCrearOrdenMantenimiento : IValidatableObject
{
    private const decimal ValorMaximoDecimal18_2 =
        9999999999999999.99m;

    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un activo.")]
    public long IdActivo { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "El proyecto seleccionado no es válido.")]
    public long? IdProyecto { get; set; }

    [Required(ErrorMessage = "La fecha programada es obligatoria.")]
    public DateTime FechaProgramada { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }

    public decimal? CostoEstimado { get; set; }

    [StringLength(150, ErrorMessage = "El responsable no puede superar 150 caracteres.")]
    public string? Responsable { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (CostoEstimado.HasValue
            && (CostoEstimado.Value < 0
                || CostoEstimado.Value > ValorMaximoDecimal18_2
                || decimal.Round(CostoEstimado.Value, 2)
                    != CostoEstimado.Value))
        {
            yield return new ValidationResult(
                "El costo estimado debe ser mayor o igual a cero, no superar el máximo permitido y usar hasta dos decimales.",
                [nameof(CostoEstimado)]);
        }
    }
}

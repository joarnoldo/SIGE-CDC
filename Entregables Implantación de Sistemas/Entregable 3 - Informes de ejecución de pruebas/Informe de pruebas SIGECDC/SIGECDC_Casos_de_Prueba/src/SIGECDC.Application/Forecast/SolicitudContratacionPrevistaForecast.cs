using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudContratacionPrevistaForecast : IValidatableObject
{
    public const decimal SalarioMaximo = 9999999999999999.99m;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento válido.")]
    public int IdDepartamento { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto válido.")]
    public int IdPuesto { get; set; }

    public decimal SalarioBaseMensual { get; set; }

    public DateTime FechaInicioAplicacion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalarioBaseMensual < 0m || SalarioBaseMensual > SalarioMaximo)
        {
            yield return new ValidationResult(
                "El salario debe estar entre 0 y 9999999999999999.99.",
                [nameof(SalarioBaseMensual)]);
        }

        if (decimal.Round(SalarioBaseMensual, 2) != SalarioBaseMensual)
        {
            yield return new ValidationResult(
                "El salario admite como máximo dos decimales.",
                [nameof(SalarioBaseMensual)]);
        }

        if (FechaInicioAplicacion == default)
        {
            yield return new ValidationResult(
                "La fecha inicial de la contratación es obligatoria.",
                [nameof(FechaInicioAplicacion)]);
        }
    }
}

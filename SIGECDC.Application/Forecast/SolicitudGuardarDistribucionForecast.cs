using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudGuardarDistribucionForecast : IValidatableObject
{
    public IReadOnlyList<SolicitudAsignacionProyectoForecast>? Asignaciones { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Asignaciones is null || Asignaciones.Count == 0)
        {
            yield return new ValidationResult(
                "Agregue al menos un proyecto a la distribución.",
                [nameof(Asignaciones)]);
            yield break;
        }

        if (Asignaciones.Any(item => item is null))
        {
            yield return new ValidationResult(
                "La distribución contiene una asignación no válida.",
                [nameof(Asignaciones)]);
            yield break;
        }

        if (Asignaciones.Any(item => item.IdProyecto <= 0))
        {
            yield return new ValidationResult(
                "Todos los proyectos de la distribución deben ser válidos.",
                [nameof(Asignaciones)]);
        }

        if (Asignaciones.GroupBy(item => item.IdProyecto).Any(grupo => grupo.Count() > 1))
        {
            yield return new ValidationResult(
                "Un proyecto no puede repetirse dentro de la misma distribución.",
                [nameof(Asignaciones)]);
        }

        if (Asignaciones.Any(item => item.Porcentaje <= 0m || item.Porcentaje > 100m))
        {
            yield return new ValidationResult(
                "Cada porcentaje debe ser mayor que cero y hasta 100.",
                [nameof(Asignaciones)]);
        }

        if (Asignaciones.Any(item => decimal.Round(item.Porcentaje, 4) != item.Porcentaje))
        {
            yield return new ValidationResult(
                "Los porcentajes admiten como máximo cuatro decimales.",
                [nameof(Asignaciones)]);
        }

        if (Asignaciones.Sum(item => item.Porcentaje) != 100.0000m)
        {
            yield return new ValidationResult(
                "La distribución debe sumar exactamente 100.0000 %.",
                [nameof(Asignaciones)]);
        }
    }
}

public sealed class SolicitudAsignacionProyectoForecast
{
    public long IdProyecto { get; set; }
    public decimal Porcentaje { get; set; }
}

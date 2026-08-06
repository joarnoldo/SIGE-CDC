using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudCrearEscenarioForecast : IValidatableObject
{
    [Required(ErrorMessage = "El nombre del escenario es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no debe superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no debe superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    public List<SolicitudPeriodoForecast> Periodos { get; set; } = [];

    public List<long> IdPlanillasHistoricas { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Periodos is null || Periodos.Count == 0)
        {
            yield return new ValidationResult(
                "Agregue al menos un período futuro.",
                [nameof(Periodos)]);
        }
        else
        {
            DateTime? fechaFinAnterior = null;

            for (var indice = 0; indice < Periodos.Count; indice++)
            {
                var periodo = Periodos[indice];
                if (periodo is null)
                {
                    yield return new ValidationResult(
                        "Los períodos no pueden contener elementos vacíos.",
                        [nameof(Periodos)]);
                    continue;
                }

                var resultados = new List<ValidationResult>();
                Validator.TryValidateObject(
                    periodo,
                    new ValidationContext(periodo),
                    resultados,
                    validateAllProperties: true);

                foreach (var resultado in resultados)
                {
                    yield return new ValidationResult(
                        resultado.ErrorMessage,
                        resultado.MemberNames.Select(nombre => $"Periodos[{indice}].{nombre}"));
                }

                if (fechaFinAnterior.HasValue
                    && periodo.FechaInicio.HasValue
                    && periodo.FechaInicio.Value.Date <= fechaFinAnterior.Value.Date)
                {
                    yield return new ValidationResult(
                        "Los períodos deben estar ordenados cronológicamente y no pueden traslaparse.",
                        [nameof(Periodos)]);
                }

                if (periodo.FechaFin.HasValue)
                {
                    fechaFinAnterior = periodo.FechaFin.Value.Date;
                }
            }
        }

        if (IdPlanillasHistoricas is null || IdPlanillasHistoricas.Count == 0)
        {
            yield return new ValidationResult(
                "Seleccione al menos una planilla histórica.",
                [nameof(IdPlanillasHistoricas)]);
        }
        else
        {
            if (IdPlanillasHistoricas.Any(idPlanilla => idPlanilla <= 0))
            {
                yield return new ValidationResult(
                    "Las planillas históricas seleccionadas no son válidas.",
                    [nameof(IdPlanillasHistoricas)]);
            }

            if (IdPlanillasHistoricas.Distinct().Count() != IdPlanillasHistoricas.Count)
            {
                yield return new ValidationResult(
                    "Una planilla histórica no puede seleccionarse más de una vez.",
                    [nameof(IdPlanillasHistoricas)]);
            }
        }
    }
}

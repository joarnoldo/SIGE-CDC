using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudGuardarParticipantesForecast : IValidatableObject
{
    public IReadOnlyList<SolicitudParticipanteForecast>? Participantes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Participantes is null)
        {
            yield return new ValidationResult(
                "La composición de colaboradores es obligatoria.",
                [nameof(Participantes)]);
            yield break;
        }

        if (Participantes.Any(item => item is null))
        {
            yield return new ValidationResult(
                "La composición contiene una selección no válida.",
                [nameof(Participantes)]);
            yield break;
        }

        foreach (var participante in Participantes)
        {
            var resultados = new List<ValidationResult>();
            if (!Validator.TryValidateObject(
                    participante,
                    new ValidationContext(participante),
                    resultados,
                    validateAllProperties: true))
            {
                foreach (var resultado in resultados)
                {
                    yield return resultado;
                }
            }
        }

        if (Participantes
            .GroupBy(item => item.IdColaborador)
            .Any(grupo => grupo.Count() > 1))
        {
            yield return new ValidationResult(
                "Cada colaborador debe aparecer exactamente una vez en la composición.",
                [nameof(Participantes)]);
        }
    }
}

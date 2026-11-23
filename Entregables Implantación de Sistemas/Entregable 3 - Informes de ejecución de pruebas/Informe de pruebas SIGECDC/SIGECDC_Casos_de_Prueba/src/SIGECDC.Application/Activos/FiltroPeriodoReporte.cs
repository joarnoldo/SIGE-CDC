using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class FiltroPeriodoReporte : IValidatableObject
{
    [DataType(DataType.Date)]
    public DateTime? FechaDesde { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaHasta { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (FechaDesde.HasValue
            && FechaHasta.HasValue
            && FechaDesde.Value.Date > FechaHasta.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha inicial no puede ser posterior a la fecha final.",
                [nameof(FechaDesde), nameof(FechaHasta)]);
        }
    }
}

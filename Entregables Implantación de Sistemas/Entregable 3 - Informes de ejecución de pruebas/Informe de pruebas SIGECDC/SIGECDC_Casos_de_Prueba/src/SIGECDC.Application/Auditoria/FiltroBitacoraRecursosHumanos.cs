using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Auditoria;

public sealed class FiltroBitacoraRecursosHumanos : IValidatableObject
{
    [DataType(DataType.Date)]
    public DateTime? FechaDesde { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaHasta { get; set; }

    public string? Categoria { get; set; }

    public string? Busqueda { get; set; }

    public int Cantidad { get; set; } = 100;

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

        if (!CategoriasBitacoraRecursosHumanos.EsValida(Categoria))
        {
            yield return new ValidationResult(
                "La categoría de bitácora seleccionada no es válida.",
                [nameof(Categoria)]);
        }
    }
}

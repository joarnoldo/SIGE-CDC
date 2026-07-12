using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Planillas;

public sealed class SolicitudAsignacionParametroPlanilla
{
    [Required(ErrorMessage = "Seleccione el ámbito de asignación.")]
    public string Ambito { get; set; } = "Colaborador";

    [Range(1, long.MaxValue, ErrorMessage = "Seleccione el colaborador o período correspondiente.")]
    public long IdDestino { get; set; }

    [Range(0, 999999999999.9999, ErrorMessage = "El valor numérico asignado no puede ser negativo.")]
    public decimal? ValorDecimalOverride { get; set; }

    [StringLength(255, ErrorMessage = "El valor de texto asignado no debe superar los 255 caracteres.")]
    public string? ValorTextoOverride { get; set; }
}

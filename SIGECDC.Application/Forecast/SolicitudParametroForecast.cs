using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudParametroForecast
{
    [Required(ErrorMessage = "El código del parámetro es obligatorio.")]
    [StringLength(100, ErrorMessage = "El código del parámetro no debe superar los 100 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    public bool EstaHabilitado { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo del parámetro.")]
    public string TipoParametro { get; set; } = string.Empty;

    public decimal? ValorDecimal { get; set; }
}

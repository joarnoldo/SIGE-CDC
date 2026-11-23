namespace SIGECDC.Application.Forecast;

public sealed class ColaboradorSalidaPrevistaForecast
{
    public long IdForecastParticipante { get; set; }
    public string CodigoParticipante { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public bool EstaIncluido { get; set; }
    public DateTime? FechaSalidaPrevista { get; set; }
}

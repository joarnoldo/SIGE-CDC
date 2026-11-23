namespace SIGECDC.Application.Forecast;

public sealed class ResultadoGeneracionForecast
{
    public long IdForecastEscenario { get; init; }
    public string EstadoEscenario { get; init; } = string.Empty;
    public DateTime FechaCalculo { get; init; }
    public decimal MontoProyectadoTotal { get; init; }
    public int CantidadPeriodos { get; init; }
    public int CantidadParticipantes { get; init; }
    public int CantidadDetalles { get; init; }
}

namespace SIGECDC.Application.Forecast;

public class EscenarioForecastResumen
{
    public long IdForecastEscenario { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public DateTime FechaInicioProyeccion { get; set; }

    public DateTime FechaFinProyeccion { get; set; }

    public int PeriodosHistoricosConsiderados { get; set; }

    public string EstadoEscenario { get; set; } = string.Empty;

    public decimal MontoProyectadoTotal { get; set; }

    public decimal? MontoRealTotal { get; set; }

    public decimal? DiferenciaTotal { get; set; }

    public DateTime? FechaCalculo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CantidadPeriodos { get; set; }

    public int CantidadFuentesHistoricas { get; set; }
}

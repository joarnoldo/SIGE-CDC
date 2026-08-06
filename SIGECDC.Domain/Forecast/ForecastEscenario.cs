using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastEscenario
{
    public long IdForecastEscenario { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime FechaInicioProyeccion { get; set; }

    public DateTime FechaFinProyeccion { get; set; }

    public int PeriodosHistoricosConsiderados { get; set; } = 3;

    public string EstadoEscenario { get; set; } = EstadosEscenarioForecast.Borrador;

    public decimal MontoProyectadoTotal { get; set; }

    public decimal? MontoRealTotal { get; set; }

    public decimal? DiferenciaTotal { get; set; }

    public DateTime? FechaCalculo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ICollection<ForecastPeriodo> Periodos { get; set; } = [];

    public ICollection<ForecastFuenteHistorica> FuentesHistoricas { get; set; } = [];

    public ICollection<ForecastParametro> Parametros { get; set; } = [];

    public ICollection<ForecastParticipante> Participantes { get; set; } = [];
}

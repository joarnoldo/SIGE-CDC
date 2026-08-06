namespace SIGECDC.Application.Forecast;

public sealed class EscenarioForecastDetalle : EscenarioForecastResumen
{
    public string? Descripcion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;

    public IReadOnlyList<PeriodoForecastResumen> Periodos { get; set; } = [];

    public IReadOnlyList<FuenteHistoricaForecastResumen> FuentesHistoricas { get; set; } = [];
}

namespace SIGECDC.Application.Forecast;

public sealed record PeriodoForecastResumen(
    long IdForecastPeriodo,
    int NumeroOrden,
    string TipoPeriodo,
    DateTime FechaInicio,
    DateTime FechaFin,
    string EstadoRegistro);

namespace SIGECDC.Application.Forecast;

public sealed record FuenteHistoricaForecastResumen(
    long IdForecastFuenteHistorica,
    int NumeroOrden,
    long IdPlanilla,
    long IdPeriodoPlanilla,
    string CodigoPeriodo,
    string NombrePeriodo,
    DateTime FechaInicio,
    DateTime FechaFin,
    string EstadoPlanilla);

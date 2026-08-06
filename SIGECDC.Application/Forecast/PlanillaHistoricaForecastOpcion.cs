namespace SIGECDC.Application.Forecast;

public sealed record PlanillaHistoricaForecastOpcion(
    long IdPlanilla,
    long IdPeriodoPlanilla,
    string CodigoPeriodo,
    string NombrePeriodo,
    DateTime FechaInicio,
    DateTime FechaFin,
    string EstadoPlanilla);

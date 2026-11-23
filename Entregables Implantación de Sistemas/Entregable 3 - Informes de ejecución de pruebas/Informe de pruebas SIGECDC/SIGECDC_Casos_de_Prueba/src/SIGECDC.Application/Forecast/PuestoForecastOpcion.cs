namespace SIGECDC.Application.Forecast;

public sealed record PuestoForecastOpcion(
    int IdPuesto,
    int? IdDepartamento,
    string Nombre);

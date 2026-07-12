namespace SIGECDC.Application.Planillas;

public sealed record PeriodoPlanillaOpcion(
    long Id,
    string Codigo,
    string Nombre,
    DateTime FechaInicio,
    DateTime FechaFin,
    string EstadoPlanilla,
    bool EstaBloqueado);

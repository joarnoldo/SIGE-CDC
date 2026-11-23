namespace SIGECDC.Application.Planillas;

public sealed record DestinoAsignacionParametroPlanillaOpcion(
    long Id,
    string Codigo,
    string Nombre,
    bool EstaBloqueado = false);

namespace SIGECDC.Application.Planillas;

public sealed record TipoIncidenciaPlanillaOpcion(
    int Id,
    string Nombre,
    string Naturaleza,
    string? Descripcion);

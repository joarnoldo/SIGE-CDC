namespace SIGECDC.Application.Activos;

public sealed class ActivoAsignacionOpcion
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public string EstadoActivo { get; set; } = string.Empty;
}

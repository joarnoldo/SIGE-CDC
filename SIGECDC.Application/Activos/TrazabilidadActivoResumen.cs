namespace SIGECDC.Application.Activos;

public sealed class TrazabilidadActivoResumen
{
    public DateTime FechaHora { get; set; }

    public string? IdUsuario { get; set; }

    public string DescripcionCambio { get; set; } = string.Empty;
}

namespace SIGECDC.Application.Planillas;

public sealed class PortalColillasEmpleado
{
    public bool TieneColaboradorVinculado { get; set; }

    public string CodigoColaborador { get; set; } = string.Empty;

    public string NombreColaborador { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public string Puesto { get; set; } = string.Empty;

    public int CantidadPeriodosPendientes { get; set; }

    public IReadOnlyList<ColillaPagoResumen> Colillas { get; set; } = [];
}

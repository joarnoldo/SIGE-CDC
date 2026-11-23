namespace SIGECDC.Application.Activos;

public sealed class EncabezadoReporteUtilizacionCostos
{
    public string TipoReporte { get; set; } = string.Empty;

    public long IdReferencia { get; set; }

    public string CodigoReferencia { get; set; } = string.Empty;

    public string NombreReferencia { get; set; } = string.Empty;
}

namespace SIGECDC.Domain.Planillas;

public sealed class ColillaPago
{
    public long IdColillaPago { get; set; }

    public long IdDetallePlanilla { get; set; }

    public string CodigoColilla { get; set; } = string.Empty;

    public string? RutaArchivo { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public string? GeneradoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public DetallePlanilla? DetallePlanilla { get; set; }
}

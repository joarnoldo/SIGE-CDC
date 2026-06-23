namespace SIGECDC.Domain.RecursosHumanos;

public class Contrato
{
    public long IdContrato { get; set; }

    public long IdColaborador { get; set; }

    public string TipoContrato { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public decimal SalarioBase { get; set; }

    public string? Jornada { get; set; }

    public string PeriodicidadPago { get; set; } = "Quincenal";

    public string EstadoContrato { get; set; } = "Activo";

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public Colaborador? Colaborador { get; set; }
}

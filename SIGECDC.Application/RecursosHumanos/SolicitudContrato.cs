using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.RecursosHumanos;

public sealed class SolicitudContrato
{
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un colaborador.")]
    public long IdColaborador { get; set; }

    [Required(ErrorMessage = "El tipo de contrato es obligatorio.")]
    [StringLength(100, ErrorMessage = "El tipo de contrato no debe superar los 100 caracteres.")]
    public string TipoContrato { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime? FechaInicio { get; set; } = DateTime.Today;

    public DateTime? FechaFin { get; set; }

    [Range(0, 999999999999.99, ErrorMessage = "El salario base no puede ser negativo.")]
    public decimal SalarioBase { get; set; }

    [StringLength(100, ErrorMessage = "La jornada no debe superar los 100 caracteres.")]
    public string? Jornada { get; set; }

    [Required(ErrorMessage = "La periodicidad de pago es obligatoria.")]
    public string PeriodicidadPago { get; set; } = "Quincenal";

    [Required(ErrorMessage = "El estado del contrato es obligatorio.")]
    public string EstadoContrato { get; set; } = "Activo";

    [StringLength(500, ErrorMessage = "Las observaciones no deben superar los 500 caracteres.")]
    public string? Observaciones { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudRegistroMantenimiento
{
	[Range(1, long.MaxValue, ErrorMessage = "Seleccione un activo.")]
	public long IdActivo { get; set; }

	public long? IdProyecto { get; set; }

	[Required(ErrorMessage = "Seleccione el tipo de mantenimiento.")]
	public string TipoMantenimiento { get; set; } = string.Empty;

	[Range(1, int.MaxValue, ErrorMessage = "Seleccione el estado del mantenimiento.")]
	public int IdEstadoMantenimiento { get; set; }

	[Required(ErrorMessage = "La fecha programada es obligatoria.")]
	public DateTime FechaProgramada { get; set; }

	public DateTime? FechaInicio { get; set; }

	public DateTime? FechaFin { get; set; }

	[StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
	public string? Descripcion { get; set; }

	public decimal? CostoEstimado { get; set; }

	public decimal? CostoReal { get; set; }

	public decimal? TiempoFueraServicioHoras { get; set; }

	[StringLength(500)]
	public string? Resultado { get; set; }

	[StringLength(150)]
	public string? Responsable { get; set; }
}

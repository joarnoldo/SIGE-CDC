using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudActualizarMantenimiento
{
	[Range(1, long.MaxValue)]
	public long IdMantenimiento { get; set; }

	[Range(1, int.MaxValue)]
	public int IdEstadoMantenimiento { get; set; }

	public DateTime? FechaInicio { get; set; }

	public DateTime? FechaFin { get; set; }

	public decimal? CostoReal { get; set; }

	public decimal? TiempoFueraServicioHoras { get; set; }

	[StringLength(500)]
	public string? Resultado { get; set; }

	[StringLength(500)]
	public string? Descripcion { get; set; }

	[StringLength(150)]
	public string? Responsable { get; set; }
}
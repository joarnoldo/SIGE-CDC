using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Activos;

public sealed class Mantenimiento
{
	public long IdMantenimiento { get; set; }

	public long IdActivo { get; set; }

	public Activo? Activo { get; set; }

	public long? IdProyecto { get; set; }

	public Proyecto? Proyecto { get; set; }

	public string TipoMantenimiento { get; set; } = string.Empty;

	public int IdEstadoMantenimiento { get; set; }

	public EstadoMantenimiento? EstadoMantenimiento { get; set; }

	public DateTime FechaProgramada { get; set; }

	public DateTime? FechaInicio { get; set; }

	public DateTime? FechaFin { get; set; }

	public string? Descripcion { get; set; }

	public decimal? CostoEstimado { get; set; }

	public decimal? CostoReal { get; set; }

	public decimal? TiempoFueraServicioHoras { get; set; }

	public string? Resultado { get; set; }

	public string? Responsable { get; set; }

	public DateTime FechaCreacion { get; set; }

	public string? CreadoPor { get; set; }

	public DateTime? FechaModificacion { get; set; }

	public string? ModificadoPor { get; set; }

	public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}
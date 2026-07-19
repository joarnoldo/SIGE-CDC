using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Activos;

public sealed class EstadoMantenimiento
{
	public int IdEstadoMantenimiento { get; set; }

	public string Nombre { get; set; } = string.Empty;

	public string? Descripcion { get; set; }

	public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}
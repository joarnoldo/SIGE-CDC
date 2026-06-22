namespace SIGECDC.Domain.SitioPublico;

public sealed class GaleriaMultimedia
{
	public long IdGaleriaMultimedia { get; set; }

	public string Titulo { get; set; } = string.Empty;

	public string? Descripcion { get; set; }

	public string RutaArchivo { get; set; } = string.Empty;

	public DateTime FechaCarga { get; set; }

	public DateTime FechaCreacion { get; set; }

	public string? CreadoPor { get; set; }

	public DateTime? FechaModificacion { get; set; }

	public string? ModificadoPor { get; set; }

	public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}
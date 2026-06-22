namespace SIGECDC.Domain.SitioPublico;

public sealed class Noticia
{
	public long IdNoticia { get; set; }

	public string Titulo { get; set; } = string.Empty;

	public string Resumen { get; set; } = string.Empty;

	public string Contenido { get; set; } = string.Empty;

	public string? ImagenPrincipal { get; set; }

	public bool EstaPublicada { get; set; }

	public DateTime? FechaPublicacion { get; set; }

	public DateTime FechaCreacion { get; set; }

	public string? CreadoPor { get; set; }

	public DateTime? FechaModificacion { get; set; }

	public string? ModificadoPor { get; set; }

	public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}
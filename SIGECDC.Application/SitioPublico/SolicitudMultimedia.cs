using System;

namespace SIGECDC.Application
{
	public class SolicitudMultimedia
	{
		public long IdMultimedia { get; set; }
		public string Titulo { get; set; } = null!;
		public string? Descripcion { get; set; }
		public string UrlMedia { get; set; } = null!;
		public string TipoMedia { get; set; } = null!;
		public DateTime? FechaCreacion { get; set; }
		public string EstadoRegistro { get; set; } = null!;
	}
}

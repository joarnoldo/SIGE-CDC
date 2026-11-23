using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudDefinirMantenimientoPreventivo
{
	[Range(1, long.MaxValue, ErrorMessage = "Seleccione un activo.")]
	public long IdActivo { get; set; }

	[Required(ErrorMessage = "La fecha programada es obligatoria.")]
	public DateTime FechaProgramada { get; set; }

	[StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
	public string? Descripcion { get; set; }
}

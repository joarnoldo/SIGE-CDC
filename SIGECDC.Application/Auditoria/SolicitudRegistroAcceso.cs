using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Auditoria;

public sealed class SolicitudRegistroAcceso
{
    public string? IdUsuario { get; set; }

    [Required]
    [StringLength(100)]
    public string Accion { get; set; } = string.Empty;

    [StringLength(45)]
    public string? DireccionIP { get; set; }

    [StringLength(500)]
    public string? Observacion { get; set; }
}

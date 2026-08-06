using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.RecursosHumanos;

public sealed class SolicitudVinculoCuentaColaborador
{
    [StringLength(255, ErrorMessage = "La cuenta seleccionada no es valida.")]
    public string? IdUsuario { get; set; }
}

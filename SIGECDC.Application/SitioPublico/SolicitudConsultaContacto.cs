using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.SitioPublico;

public class SolicitudConsultaContacto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre completo no debe superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido.")]
    [StringLength(150, ErrorMessage = "El correo electronico no debe superar los 150 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El telefono no debe superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [StringLength(200, ErrorMessage = "El asunto no debe superar los 200 caracteres.")]
    public string? Asunto { get; set; }

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    public string Mensaje { get; set; } = string.Empty;
}

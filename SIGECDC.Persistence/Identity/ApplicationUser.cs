using Microsoft.AspNetCore.Identity;

namespace SIGECDC.Persistence.Identity;

public class ApplicationUser : IdentityUser
{
    public string EstadoRegistro { get; set; } = "Activo";

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public DateTime? FechaModificacion { get; set; }
}
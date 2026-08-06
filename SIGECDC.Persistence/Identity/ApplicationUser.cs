using Microsoft.AspNetCore.Identity;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Identity;

public class ApplicationUser : IdentityUser
{
    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public DateTime? FechaModificacion { get; set; }

    public bool EstaActivo()
    {
        return string.Equals(
            EstadoRegistro,
            EstadosRegistro.Activo,
            StringComparison.Ordinal);
    }
}

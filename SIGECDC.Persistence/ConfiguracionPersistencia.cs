using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGECDC.Application.Operaciones;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Operaciones;
using SIGECDC.Persistence.SitioPublico;

namespace SIGECDC.Persistence;

public static class ConfiguracionPersistencia
{
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException("No se encontro la cadena de conexion 'DefaultConnection'.");
        }

        servicios.AddDbContext<ApplicationDbContext>(opciones =>
            opciones.UseMySQL(cadenaConexion));

        servicios.AddScoped<IConsultaContactoService, ConsultaContactoService>();
        servicios.AddScoped<IPaginaContenidoService, PaginaContenidoService>();
        servicios.AddScoped<IProyectoService, ProyectoService>();
        servicios.AddScoped<IProyectoPublicadoService, ProyectoPublicadoService>();

        return servicios;
    }
}

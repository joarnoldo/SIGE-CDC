using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGECDC.Application.Auditoria;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Persistence.Auditoria;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;
using SIGECDC.Persistence.RecursosHumanos;
using SIGECDC.Persistence.SitioPublico;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Persistence;

public static class ConfiguracionPersistencia
{
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        servicios.AddDbContext<ApplicationDbContext>(opciones =>
            opciones.UseMySQL(cadenaConexion));

        servicios.AddScoped<IConsultaContactoService, ConsultaContactoService>();
        servicios.AddScoped<IColaboradorService, ColaboradorService>();
        servicios.AddScoped<IContratoDocumentoService, ContratoDocumentoService>();
        servicios.AddScoped<IParametroPlanillaService, ParametroPlanillaService>();
        servicios.AddScoped<IIncidenciaPlanillaService, IncidenciaPlanillaService>();
        servicios.AddScoped<IHistorialSistemaService, HistorialSistemaService>();

        return servicios;
    }
}

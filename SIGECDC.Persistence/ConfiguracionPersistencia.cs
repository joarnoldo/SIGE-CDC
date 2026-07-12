using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Auditoria;
using SIGECDC.Application.Operaciones;
using SIGECDC.Application.Planillas;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Auditoria;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Operaciones;
using SIGECDC.Persistence.Planillas;
using SIGECDC.Persistence.RecursosHumanos;
using SIGECDC.Persistence.SitioPublico;

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
        servicios.AddScoped<IFaqService, FaqService>();
        servicios.AddScoped<INoticiaService, NoticiaService>();
        servicios.AddScoped<IGaleriaService, GaleriaService>();
        servicios.AddScoped<IPaginaContenidoService, PaginaContenidoService>();
        servicios.AddScoped<IProyectoService, ProyectoService>();
        servicios.AddScoped<IProyectoPublicadoService, ProyectoPublicadoService>();
        servicios.AddScoped<IColaboradorService, ColaboradorService>();
        servicios.AddScoped<IDepartamentoService, DepartamentoService>();
        servicios.AddScoped<IPuestoService, PuestoService>();
        servicios.AddScoped<IContratoDocumentoService, ContratoDocumentoService>();
        servicios.AddScoped<IParametroPlanillaService, ParametroPlanillaService>();
        servicios.AddScoped<IIncidenciaPlanillaService, IncidenciaPlanillaService>();
        servicios.AddScoped<IPlanillaService, PlanillaService>();
        servicios.AddScoped<IAsignacionActivoProyectoService, AsignacionActivoProyectoService>();
        servicios.AddScoped<IHistorialSistemaService, HistorialSistemaService>();

        return servicios;
    }
}

using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.SitioPublico;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.SitioPublico;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlProyectoPublicadoIntegrationTests
{
    [MySqlQaFact]
    public async Task ConsultaPublica_IncluyeSoloTarjetasActivasYPublicadas()
    {
        await using var contexto = CrearContexto();
        var marca = NuevaMarca();
        var visible = CrearTarjeta(marca, "Visible", "En ejecución", true, EstadosRegistro.Activo);
        var sinEstado = CrearTarjeta(marca, "Sin estado", null, true, EstadosRegistro.Activo);
        var oculta = CrearTarjeta(marca, "Oculta", "En ejecución", false, EstadosRegistro.Activo);
        var inactiva = CrearTarjeta(marca, "Inactiva", "En ejecución", true, EstadosRegistro.Inactivo);

        contexto.ProyectosPublicados.AddRange(visible, sinEstado, oculta, inactiva);
        await contexto.SaveChangesAsync();

        try
        {
            contexto.ChangeTracker.Clear();
            var servicio = new ProyectoPublicadoService(contexto);

            var resultado = await servicio.ObtenerProyectosPublicadosAsync();

            Assert.Contains(resultado, proyecto => proyecto.IdProyectoPublicado == visible.IdProyectoPublicado);
            Assert.Contains(resultado, proyecto => proyecto.IdProyectoPublicado == sinEstado.IdProyectoPublicado);
            Assert.DoesNotContain(resultado, proyecto => proyecto.IdProyectoPublicado == oculta.IdProyectoPublicado);
            Assert.DoesNotContain(resultado, proyecto => proyecto.IdProyectoPublicado == inactiva.IdProyectoPublicado);
        }
        finally
        {
            await EliminarDatosAsync(contexto, marca);
        }
    }

    [MySqlQaFact]
    public async Task FiltroYEstados_UsanSoloEstadosDePublicacionesVisibles()
    {
        await using var contexto = CrearContexto();
        var marca = NuevaMarca();
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var estadoVisible = $"QA Ejecución {sufijo}";
        var otroEstadoVisible = $"QA Finalizado {sufijo}";
        var estadoOculto = $"QA Oculto {sufijo}";
        var estadoInactivo = $"QA Inactivo {sufijo}";
        var coincidencia = CrearTarjeta(marca, "Coincidencia", estadoVisible, true, EstadosRegistro.Activo);
        var otroVisible = CrearTarjeta(marca, "Otro visible", otroEstadoVisible, true, EstadosRegistro.Activo);
        var sinEstado = CrearTarjeta(marca, "Sin estado", "   ", true, EstadosRegistro.Activo);
        var oculta = CrearTarjeta(marca, "Oculta", estadoOculto, false, EstadosRegistro.Activo);
        var inactiva = CrearTarjeta(marca, "Inactiva", estadoInactivo, true, EstadosRegistro.Inactivo);

        contexto.ProyectosPublicados.AddRange(coincidencia, otroVisible, sinEstado, oculta, inactiva);
        await contexto.SaveChangesAsync();

        try
        {
            contexto.ChangeTracker.Clear();
            var servicio = new ProyectoPublicadoService(contexto);

            var estados = await servicio.ObtenerEstadosPublicadosAsync();
            var filtrados = await servicio.ObtenerProyectosPublicadosAsync($"  {estadoVisible}  ");

            Assert.Contains(estadoVisible, estados);
            Assert.Contains(otroEstadoVisible, estados);
            Assert.DoesNotContain(estadoOculto, estados);
            Assert.DoesNotContain(estadoInactivo, estados);
            Assert.DoesNotContain(estados, string.IsNullOrWhiteSpace);
            Assert.Contains(filtrados, proyecto => proyecto.IdProyectoPublicado == coincidencia.IdProyectoPublicado);
            Assert.DoesNotContain(filtrados, proyecto => proyecto.IdProyectoPublicado == otroVisible.IdProyectoPublicado);
            Assert.All(filtrados, proyecto => Assert.Equal(estadoVisible, proyecto.EstadoVisual));
        }
        finally
        {
            await EliminarDatosAsync(contexto, marca);
        }
    }

    [MySqlQaFact]
    public async Task ConsultaPublica_NoRastreaNiExponeInformacionInternaDelProyecto()
    {
        await using var contexto = CrearContexto();
        var marca = NuevaMarca();
        var idEstadoProyecto = await contexto.EstadosProyecto
            .AsNoTracking()
            .Where(estado => estado.Nombre == "Planificado")
            .Select(estado => estado.IdEstadoProyecto)
            .SingleAsync();
        var proyectoInterno = new Proyecto
        {
            CodigoProyecto = $"INT-{Guid.NewGuid():N}"[..20],
            NombreProyecto = $"Nombre interno {marca}",
            Responsable = $"Responsable interno {marca}",
            IdEstadoProyecto = idEstadoProyecto,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.Proyectos.Add(proyectoInterno);
        await contexto.SaveChangesAsync();

        var tarjeta = CrearTarjeta(marca, "Información pública", "Finalizado", true, EstadosRegistro.Activo);
        tarjeta.IdProyecto = proyectoInterno.IdProyecto;
        contexto.ProyectosPublicados.Add(tarjeta);
        await contexto.SaveChangesAsync();

        try
        {
            var cantidadAntes = await contexto.ProyectosPublicados.CountAsync();
            contexto.ChangeTracker.Clear();
            var servicio = new ProyectoPublicadoService(contexto);

            var resultado = await servicio.ObtenerProyectosPublicadosAsync();
            var publicado = Assert.Single(resultado, item => item.IdProyectoPublicado == tarjeta.IdProyectoPublicado);
            var cantidadDespues = await contexto.ProyectosPublicados.CountAsync();
            var propiedadesPublicas = typeof(ProyectoPortafolioResumen)
                .GetProperties()
                .Select(propiedad => propiedad.Name)
                .OrderBy(nombre => nombre)
                .ToArray();

            Assert.Equal(tarjeta.Titulo, publicado.Titulo);
            Assert.Equal(cantidadAntes, cantidadDespues);
            Assert.Empty(contexto.ChangeTracker.Entries());
            Assert.Equal(
                ["Descripcion", "EstadoVisual", "IdProyectoPublicado", "Titulo"],
                propiedadesPublicas);
        }
        finally
        {
            await EliminarDatosAsync(contexto, marca);
            contexto.ChangeTracker.Clear();
            var proyecto = await contexto.Proyectos
                .SingleOrDefaultAsync(item => item.IdProyecto == proyectoInterno.IdProyecto);
            if (proyecto is not null)
            {
                contexto.Proyectos.Remove(proyecto);
                await contexto.SaveChangesAsync();
            }
        }
    }

    [MySqlQaFact]
    public async Task PublicacionAdministrativa_ConservaGestionYVisibilidadPublica()
    {
        await using var contexto = CrearContexto();
        var marca = NuevaMarca();
        var tarjeta = CrearTarjeta(marca, "Publicable", "En ejecución", false, EstadosRegistro.Activo);
        contexto.ProyectosPublicados.Add(tarjeta);
        await contexto.SaveChangesAsync();

        try
        {
            contexto.ChangeTracker.Clear();
            var servicio = new ProyectoPublicadoService(contexto);

            var administracionInicial = await servicio.ObtenerProyectosAsync();
            Assert.Contains(administracionInicial, proyecto =>
                proyecto.IdProyectoPublicado == tarjeta.IdProyectoPublicado
                && !proyecto.EstaPublicado);
            Assert.DoesNotContain(
                await servicio.ObtenerProyectosPublicadosAsync(),
                proyecto => proyecto.IdProyectoPublicado == tarjeta.IdProyectoPublicado);

            await servicio.CambiarPublicacionAsync(tarjeta.IdProyectoPublicado, true);
            Assert.Contains(
                await servicio.ObtenerProyectosPublicadosAsync(),
                proyecto => proyecto.IdProyectoPublicado == tarjeta.IdProyectoPublicado);

            await servicio.CambiarPublicacionAsync(tarjeta.IdProyectoPublicado, false);
            Assert.DoesNotContain(
                await servicio.ObtenerProyectosPublicadosAsync(),
                proyecto => proyecto.IdProyectoPublicado == tarjeta.IdProyectoPublicado);
            Assert.Contains(
                await servicio.ObtenerProyectosAsync(),
                proyecto => proyecto.IdProyectoPublicado == tarjeta.IdProyectoPublicado
                    && !proyecto.EstaPublicado);
        }
        finally
        {
            await EliminarDatosAsync(contexto, marca);
        }
    }

    private static ProyectoPublicado CrearTarjeta(
        string marca,
        string nombre,
        string? estadoVisual,
        bool estaPublicado,
        string estadoRegistro)
    {
        return new ProyectoPublicado
        {
            Titulo = $"{marca} {nombre}",
            Descripcion = $"Descripción pública {marca}",
            EstadoVisual = estadoVisual,
            EstaPublicado = estaPublicado,
            FechaPublicacion = estaPublicado ? DateTime.Now : null,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = estadoRegistro
        };
    }

    private static async Task EliminarDatosAsync(ApplicationDbContext contexto, string marca)
    {
        contexto.ChangeTracker.Clear();
        var registros = await contexto.ProyectosPublicados
            .Where(proyecto => proyecto.Titulo.StartsWith(marca))
            .ToListAsync();
        if (registros.Count == 0)
        {
            return;
        }

        contexto.ProyectosPublicados.RemoveRange(registros);
        await contexto.SaveChangesAsync();
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de integración solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(cadena)
            .Options;
        return new ApplicationDbContext(opciones);
    }

    private static string NuevaMarca()
    {
        return $"QA-PORT-{Guid.NewGuid():N}"[..24];
    }
}

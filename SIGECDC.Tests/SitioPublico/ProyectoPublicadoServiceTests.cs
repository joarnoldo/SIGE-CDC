using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.SitioPublico;

namespace SIGECDC.Tests.SitioPublico;

public sealed class ProyectoPublicadoServiceTests
{
    [Fact]
    public async Task Filtrar_EstadoMayorAlLimite_DevuelveVacioSinConsultarLaBase()
    {
        await using var contexto = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().Options);
        var servicio = new ProyectoPublicadoService(contexto);

        var resultado = await servicio.ObtenerProyectosPublicadosAsync(new string('E', 51));

        Assert.Empty(resultado);
    }

    [Fact]
    public void ContratoPublico_ExponeSoloInformacionAutorizadaDeLaTarjeta()
    {
        var propiedadesPublicas = typeof(ProyectoPortafolioResumen)
            .GetProperties()
            .Select(propiedad => propiedad.Name)
            .OrderBy(nombre => nombre)
            .ToArray();

        Assert.Equal(
            ["Descripcion", "EstadoVisual", "IdProyectoPublicado", "Titulo"],
            propiedadesPublicas);
    }
}

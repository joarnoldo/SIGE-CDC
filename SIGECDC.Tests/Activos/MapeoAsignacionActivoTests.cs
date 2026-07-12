using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

public sealed class MapeoAsignacionActivoTests
{
    [Fact]
    public void ModeloEf_UsaLasTablasYRelacionesOficiales()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("Server=localhost;Database=SIGE_CDC_DB;User=unused;Password=unused;")
            .Options;
        using var contexto = new ApplicationDbContext(opciones);

        var entidadActivo = contexto.Model.FindEntityType(typeof(Activo));
        var entidadEstado = contexto.Model.FindEntityType(typeof(EstadoActivo));
        var entidadAsignacion = contexto.Model.FindEntityType(typeof(AsignacionActivoProyecto));

        Assert.NotNull(entidadActivo);
        Assert.NotNull(entidadEstado);
        Assert.NotNull(entidadAsignacion);
        Assert.Equal("Activo", entidadActivo.GetTableName());
        Assert.Equal("EstadoActivo", entidadEstado.GetTableName());
        Assert.Equal("AsignacionActivoProyecto", entidadAsignacion.GetTableName());
        Assert.Equal(3, entidadAsignacion.GetForeignKeys().Count());
        Assert.NotNull(entidadAsignacion.FindProperty(nameof(AsignacionActivoProyecto.AsignadoPor)));
    }
}

using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

public sealed class MapeoActivoTests
{
    [Fact]
    public void ModeloEf_RespetaCatalogosEIndicesOficialesDeActivos()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("Server=localhost;Database=SIGE_CDC_DB;User=unused;Password=unused;")
            .Options;
        using var contexto = new ApplicationDbContext(opciones);

        var entidadActivo = contexto.Model.FindEntityType(typeof(Activo));
        var entidadTipo = contexto.Model.FindEntityType(typeof(TipoActivo));
        var entidadCategoria = contexto.Model.FindEntityType(typeof(CategoriaActivo));

        Assert.NotNull(entidadActivo);
        Assert.NotNull(entidadTipo);
        Assert.NotNull(entidadCategoria);
        Assert.Equal("Activo", entidadActivo.GetTableName());
        Assert.Equal("TipoActivo", entidadTipo.GetTableName());
        Assert.Equal("CategoriaActivo", entidadCategoria.GetTableName());
        Assert.Equal(30, entidadActivo.FindProperty(nameof(Activo.CodigoActivo))?.GetMaxLength());
        Assert.Contains(entidadActivo.GetIndexes(), indice =>
            indice.IsUnique
            && indice.Properties.Select(propiedad => propiedad.Name)
                .SequenceEqual([nameof(Activo.CodigoActivo)]));
        Assert.Contains(entidadCategoria.GetIndexes(), indice =>
            indice.IsUnique
            && indice.Properties.Select(propiedad => propiedad.Name)
                .SequenceEqual([nameof(CategoriaActivo.IdTipoActivo), nameof(CategoriaActivo.Nombre)]));
    }
}

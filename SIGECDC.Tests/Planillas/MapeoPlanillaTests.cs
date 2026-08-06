using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Planillas;

public sealed class MapeoPlanillaTests
{
    [Fact]
    public void ModeloEf_UsaTablasYColumnaAprobadas()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("Server=localhost;Database=SIGE_CDC_DB;User=unused;Password=unused;")
            .Options;
        using var contexto = new ApplicationDbContext(opciones);

        var entidadPlanilla = contexto.Model.FindEntityType(typeof(Planilla));
        var entidadDetalle = contexto.Model.FindEntityType(typeof(DetallePlanilla));
        var entidadColilla = contexto.Model.FindEntityType(typeof(ColillaPago));

        Assert.NotNull(entidadPlanilla);
        Assert.NotNull(entidadDetalle);
        Assert.NotNull(entidadColilla);
        Assert.Equal("Planilla", entidadPlanilla.GetTableName());
        Assert.Equal("DetallePlanilla", entidadDetalle.GetTableName());
        Assert.Equal("ColillaPago", entidadColilla.GetTableName());
        Assert.NotNull(entidadDetalle.FindProperty(nameof(DetallePlanilla.TotalBeneficiosConfigurables)));
        Assert.True(entidadPlanilla.FindIndex(entidadPlanilla.FindProperty(nameof(Planilla.IdPeriodoPlanilla))!)?.IsUnique);

        var indiceDetalle = entidadColilla.FindIndex(
            entidadColilla.FindProperty(nameof(ColillaPago.IdDetallePlanilla))!);
        var indiceCodigo = entidadColilla.FindIndex(
            entidadColilla.FindProperty(nameof(ColillaPago.CodigoColilla))!);
        Assert.True(indiceDetalle?.IsUnique);
        Assert.True(indiceCodigo?.IsUnique);
        Assert.Equal("UX_ColillaPago_IdDetallePlanilla", indiceDetalle?.GetDatabaseName());
        Assert.Equal("UX_ColillaPago_CodigoColilla", indiceCodigo?.GetDatabaseName());

        var relacionDetalle = Assert.Single(entidadColilla.GetForeignKeys(), relacion =>
            relacion.PrincipalEntityType.ClrType == typeof(DetallePlanilla));
        Assert.True(relacionDetalle.IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, relacionDetalle.DeleteBehavior);

        var rutaArchivo = entidadColilla.FindProperty(nameof(ColillaPago.RutaArchivo));
        Assert.True(rutaArchivo?.IsNullable);
        Assert.Equal(500, rutaArchivo?.GetMaxLength());
        Assert.Equal(50, entidadColilla.FindProperty(nameof(ColillaPago.CodigoColilla))?.GetMaxLength());
    }
}

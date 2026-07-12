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

        Assert.NotNull(entidadPlanilla);
        Assert.NotNull(entidadDetalle);
        Assert.Equal("Planilla", entidadPlanilla.GetTableName());
        Assert.Equal("DetallePlanilla", entidadDetalle.GetTableName());
        Assert.NotNull(entidadDetalle.FindProperty(nameof(DetallePlanilla.TotalBeneficiosConfigurables)));
        Assert.True(entidadPlanilla.FindIndex(entidadPlanilla.FindProperty(nameof(Planilla.IdPeriodoPlanilla))!)?.IsUnique);
    }
}

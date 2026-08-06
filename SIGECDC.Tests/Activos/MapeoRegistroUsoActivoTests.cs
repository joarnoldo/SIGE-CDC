using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

public sealed class MapeoRegistroUsoActivoTests
{
    [Fact]
    public void ModeloEf_RespetaEsquemaOficialDeUsoDeActivos()
    {
        var opciones =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySQL(
                    "Server=localhost;Database=SIGE_CDC_DB;User=unused;Password=unused;")
                .Options;
        using var contexto = new ApplicationDbContext(opciones);

        var entidadTipo = contexto.Model.FindEntityType(
            typeof(TipoMedicionUso));
        var entidadActivo = contexto.Model.FindEntityType(
            typeof(Activo));
        var entidadRegistro = contexto.Model.FindEntityType(
            typeof(RegistroUsoActivo));

        Assert.NotNull(entidadTipo);
        Assert.NotNull(entidadActivo);
        Assert.NotNull(entidadRegistro);
        Assert.Equal(
            "TipoMedicionUso",
            entidadTipo.GetTableName());
        Assert.Equal(
            "RegistroUsoActivo",
            entidadRegistro.GetTableName());

        Assert.Equal(
            50,
            entidadTipo
                .FindProperty(nameof(TipoMedicionUso.Nombre))
                ?.GetMaxLength());
        Assert.Equal(
            255,
            entidadTipo
                .FindProperty(nameof(TipoMedicionUso.Descripcion))
                ?.GetMaxLength());
        Assert.Contains(
            entidadTipo.GetIndexes(),
            indice =>
                indice.IsUnique
                && indice.GetDatabaseName()
                    == "UX_TipoMedicionUso_Nombre");

        Assert.Equal(
            18,
            entidadActivo
                .FindProperty(nameof(Activo.LecturaUsoActual))
                ?.GetPrecision());
        Assert.Equal(
            2,
            entidadActivo
                .FindProperty(nameof(Activo.LecturaUsoActual))
                ?.GetScale());

        Assert.Equal(
            "date",
            entidadRegistro
                .FindProperty(nameof(RegistroUsoActivo.FechaRegistro))
                ?.GetColumnType());
        AssertPrecisionDecimal18_2(
            entidadRegistro,
            nameof(RegistroUsoActivo.LecturaAnterior));
        AssertPrecisionDecimal18_2(
            entidadRegistro,
            nameof(RegistroUsoActivo.LecturaNueva));
        AssertPrecisionDecimal18_2(
            entidadRegistro,
            nameof(RegistroUsoActivo.CantidadUso));
        Assert.Equal(
            500,
            entidadRegistro
                .FindProperty(nameof(RegistroUsoActivo.Observaciones))
                ?.GetMaxLength());
        Assert.Equal(
            255,
            entidadRegistro
                .FindProperty(nameof(RegistroUsoActivo.RegistradoPor))
                ?.GetMaxLength());

        Assert.Contains(
            entidadRegistro.GetIndexes(),
            indice => indice.GetDatabaseName()
                == "IX_RegistroUsoActivo_IdActivo");
        Assert.Contains(
            entidadRegistro.GetIndexes(),
            indice => indice.GetDatabaseName()
                == "IX_RegistroUsoActivo_IdProyecto");
        Assert.Contains(
            entidadRegistro.GetIndexes(),
            indice => indice.GetDatabaseName()
                == "IX_RegistroUsoActivo_FechaRegistro");

        var relacionesRegistro = entidadRegistro.GetForeignKeys()
            .ToDictionary(
                relacion =>
                    relacion.GetConstraintName()
                    ?? string.Empty);
        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionesRegistro[
                "FK_RegistroUsoActivo_Activo"].DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.SetNull,
            relacionesRegistro[
                "FK_RegistroUsoActivo_Proyecto"].DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.SetNull,
            relacionesRegistro[
                "FK_RegistroUsoActivo_RegistradoPor"].DeleteBehavior);

        var relacionTipoActivo = Assert.Single(
            entidadActivo.GetForeignKeys(),
            relacion => relacion.GetConstraintName()
                == "FK_Activo_TipoMedicionUso");
        Assert.Equal(
            DeleteBehavior.SetNull,
            relacionTipoActivo.DeleteBehavior);
    }

    private static void AssertPrecisionDecimal18_2(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entidad,
        string propiedad)
    {
        Assert.Equal(
            18,
            entidad.FindProperty(propiedad)?.GetPrecision());
        Assert.Equal(
            2,
            entidad.FindProperty(propiedad)?.GetScale());
    }
}

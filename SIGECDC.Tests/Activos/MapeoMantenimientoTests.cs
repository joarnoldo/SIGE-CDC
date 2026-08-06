using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

public sealed class MapeoMantenimientoTests
{
    [Fact]
    public void ModeloEf_RespetaTablasPropiedadesYRelacionesOficiales()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("Server=localhost;Database=SIGE_CDC_DB;User=unused;Password=unused;")
            .Options;
        using var contexto = new ApplicationDbContext(opciones);

        var entidadMantenimiento = contexto.Model.FindEntityType(typeof(Mantenimiento));
        var entidadEstado = contexto.Model.FindEntityType(typeof(EstadoMantenimiento));
        var entidadDocumento = contexto.Model.FindEntityType(typeof(DocumentoArchivo));

        Assert.NotNull(entidadMantenimiento);
        Assert.NotNull(entidadEstado);
        Assert.NotNull(entidadDocumento);
        Assert.Equal("Mantenimiento", entidadMantenimiento.GetTableName());
        Assert.Equal("EstadoMantenimiento", entidadEstado.GetTableName());
        Assert.Equal("DocumentoArchivo", entidadDocumento.GetTableName());
        Assert.Equal(
            "enum('Preventivo','Correctivo')",
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.TipoMantenimiento))?.GetColumnType());
        Assert.Equal(
            "date",
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.FechaProgramada))?.GetColumnType());
        Assert.Equal(
            500,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.Descripcion))?.GetMaxLength());
        Assert.Equal(
            18,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.CostoEstimado))?.GetPrecision());
        Assert.Equal(
            2,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.CostoEstimado))?.GetScale());
        Assert.Equal(
            18,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.CostoReal))?.GetPrecision());
        Assert.Equal(
            2,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.CostoReal))?.GetScale());
        Assert.Equal(
            18,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.TiempoFueraServicioHoras))?.GetPrecision());
        Assert.Equal(
            2,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.TiempoFueraServicioHoras))?.GetScale());
        Assert.Equal(
            500,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.Resultado))?.GetMaxLength());
        Assert.Equal(
            150,
            entidadMantenimiento.FindProperty(nameof(Mantenimiento.Responsable))?.GetMaxLength());

        Assert.Equal(
            100,
            entidadDocumento.FindProperty(nameof(DocumentoArchivo.EntidadRelacionada))?.GetMaxLength());
        Assert.Equal(
            255,
            entidadDocumento.FindProperty(nameof(DocumentoArchivo.NombreOriginal))?.GetMaxLength());
        Assert.Equal(
            255,
            entidadDocumento.FindProperty(nameof(DocumentoArchivo.NombreAlmacenado))?.GetMaxLength());
        Assert.Equal(
            500,
            entidadDocumento.FindProperty(nameof(DocumentoArchivo.RutaRelativa))?.GetMaxLength());
        Assert.Equal(
            150,
            entidadDocumento.FindProperty(nameof(DocumentoArchivo.MimeType))?.GetMaxLength());

        var relaciones = entidadMantenimiento.GetForeignKeys()
            .ToDictionary(relacion => relacion.GetConstraintName() ?? string.Empty);

        Assert.Equal(
            DeleteBehavior.Restrict,
            relaciones["FK_Mantenimiento_Activo"].DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.SetNull,
            relaciones["FK_Mantenimiento_Proyecto"].DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            relaciones["FK_Mantenimiento_EstadoMantenimiento"].DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.SetNull,
            relaciones["FK_Mantenimiento_CreadoPor"].DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.SetNull,
            relaciones["FK_Mantenimiento_ModificadoPor"].DeleteBehavior);
        Assert.Contains(entidadEstado.GetIndexes(), indice =>
            indice.IsUnique
            && indice.Properties.Select(propiedad => propiedad.Name)
                .SequenceEqual([nameof(EstadoMantenimiento.Nombre)]));
        Assert.Contains(entidadDocumento.GetIndexes(), indice =>
            indice.GetDatabaseName() == "IX_DocumentoArchivo_Entidad"
            && indice.Properties.Select(propiedad => propiedad.Name)
                .SequenceEqual(
                [
                    nameof(DocumentoArchivo.EntidadRelacionada),
                    nameof(DocumentoArchivo.IdEntidadRelacionada)
                ]));
    }
}

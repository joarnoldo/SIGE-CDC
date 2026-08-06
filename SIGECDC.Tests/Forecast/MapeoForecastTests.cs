using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Forecast;

public sealed class MapeoForecastTests
{
    [Fact]
    public void ModeloEf_CoincideConBaseEstructuralOficial()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("Server=localhost;Database=SIGE_CDC_DB;User=unused;Password=unused;")
            .Options;
        using var contexto = new ApplicationDbContext(opciones);

        var escenario = contexto.Model.FindEntityType(typeof(ForecastEscenario));
        var periodo = contexto.Model.FindEntityType(typeof(ForecastPeriodo));
        var fuente = contexto.Model.FindEntityType(typeof(ForecastFuenteHistorica));
        var parametro = contexto.Model.FindEntityType(typeof(ForecastParametro));
        var participante = contexto.Model.FindEntityType(typeof(ForecastParticipante));
        var asignacion = contexto.Model.FindEntityType(typeof(ForecastAsignacionProyecto));
        var detalle = contexto.Model.FindEntityType(typeof(ForecastDetalle));
        Assert.NotNull(escenario);
        Assert.NotNull(periodo);
        Assert.NotNull(fuente);
        Assert.NotNull(parametro);
        Assert.NotNull(participante);
        Assert.NotNull(asignacion);
        Assert.NotNull(detalle);

        Assert.Equal("ForecastEscenario", escenario.GetTableName());
        Assert.Equal("ForecastPeriodo", periodo.GetTableName());
        Assert.Equal("ForecastFuenteHistorica", fuente.GetTableName());
        Assert.Equal("ForecastParametro", parametro.GetTableName());
        Assert.Equal("ForecastParticipante", participante.GetTableName());
        Assert.Equal("ForecastAsignacionProyecto", asignacion.GetTableName());
        Assert.Equal("ForecastDetalle", detalle.GetTableName());
        Assert.Equal(16, escenario.GetProperties().Count());
        Assert.Equal(7, periodo.GetProperties().Count());
        Assert.Equal(4, fuente.GetProperties().Count());
        Assert.Equal(9, parametro.GetProperties().Count());
        Assert.Equal(13, participante.GetProperties().Count());
        Assert.Equal(13, detalle.GetProperties().Count());
        Assert.Null(fuente.FindProperty("EstadoRegistro"));

        Assert.Equal(
            "date",
            escenario.FindProperty(nameof(ForecastEscenario.FechaInicioProyeccion))?.GetColumnType());
        Assert.Equal(
            "enum('Borrador','Calculado','Guardado','Comparado','Anulado')",
            escenario.FindProperty(nameof(ForecastEscenario.EstadoEscenario))?.GetColumnType());
        Assert.Equal(
            EstadosEscenarioForecast.Borrador,
            escenario.FindProperty(nameof(ForecastEscenario.EstadoEscenario))?.GetDefaultValue());
        Assert.Equal(
            18,
            escenario.FindProperty(nameof(ForecastEscenario.MontoProyectadoTotal))?.GetPrecision());
        Assert.Equal(
            2,
            escenario.FindProperty(nameof(ForecastEscenario.MontoProyectadoTotal))?.GetScale());

        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var escenarioDiseno = modeloDiseno.FindEntityType(typeof(ForecastEscenario));
        Assert.NotNull(escenarioDiseno);
        var checksEscenario = escenarioDiseno.GetCheckConstraints()
            .Select(check => check.Name)
            .ToHashSet();
        Assert.Contains("CK_ForecastEscenario_Fechas", checksEscenario);
        Assert.Contains("CK_ForecastEscenario_Historicos", checksEscenario);

        var indicesPeriodo = periodo.GetIndexes()
            .ToDictionary(indice => indice.GetDatabaseName()!, indice => indice);
        Assert.True(indicesPeriodo["UX_ForecastPeriodo_Escenario_Orden"].IsUnique);
        Assert.True(indicesPeriodo["UX_ForecastPeriodo_Escenario_Fechas"].IsUnique);
        var claveAlternaPeriodo = Assert.Single(
            periodo.GetKeys(),
            clave => !clave.IsPrimaryKey());
        Assert.Equal(
            [nameof(ForecastPeriodo.IdForecastEscenario), nameof(ForecastPeriodo.IdForecastPeriodo)],
            claveAlternaPeriodo.Properties.Select(propiedad => propiedad.Name));
        Assert.Equal(
            "enum('Mensual','Quincenal')",
            periodo.FindProperty(nameof(ForecastPeriodo.TipoPeriodo))?.GetColumnType());

        var relacionPeriodo = Assert.Single(periodo.GetForeignKeys());
        Assert.Equal(typeof(ForecastEscenario), relacionPeriodo.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, relacionPeriodo.DeleteBehavior);
        Assert.Equal(
            "FK_ForecastPeriodo_ForecastEscenario",
            relacionPeriodo.GetConstraintName());

        var indicesFuente = fuente.GetIndexes()
            .ToDictionary(indice => indice.GetDatabaseName()!, indice => indice);
        Assert.True(indicesFuente["UX_ForecastFuente_Escenario_Planilla"].IsUnique);
        Assert.True(indicesFuente["UX_ForecastFuente_Escenario_Orden"].IsUnique);

        var relacionEscenarioFuente = Assert.Single(fuente.GetForeignKeys(), relacion =>
            relacion.PrincipalEntityType.ClrType == typeof(ForecastEscenario));
        var relacionPlanillaFuente = Assert.Single(fuente.GetForeignKeys(), relacion =>
            relacion.PrincipalEntityType.ClrType == typeof(Planilla));
        Assert.Equal(DeleteBehavior.Cascade, relacionEscenarioFuente.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, relacionPlanillaFuente.DeleteBehavior);
        Assert.Equal(
            "FK_ForecastFuente_ForecastEscenario",
            relacionEscenarioFuente.GetConstraintName());
        Assert.Equal("FK_ForecastFuente_Planilla", relacionPlanillaFuente.GetConstraintName());

        Assert.Equal(
            "enum('Porcentaje','Monto','Cantidad','Texto')",
            parametro.FindProperty(nameof(ForecastParametro.TipoParametro))?.GetColumnType());
        Assert.Equal(
            TiposParametroPlanilla.Porcentaje,
            parametro.FindProperty(nameof(ForecastParametro.TipoParametro))?.GetDefaultValue());
        Assert.Equal(
            18,
            parametro.FindProperty(nameof(ForecastParametro.ValorDecimal))?.GetPrecision());
        Assert.Equal(
            4,
            parametro.FindProperty(nameof(ForecastParametro.ValorDecimal))?.GetScale());
        Assert.Equal(
            255,
            parametro.FindProperty(nameof(ForecastParametro.ValorTexto))?.GetMaxLength());
        Assert.Equal(
            300,
            parametro.FindProperty(nameof(ForecastParametro.Descripcion))?.GetMaxLength());

        var indiceParametro = Assert.Single(parametro.GetIndexes());
        Assert.True(indiceParametro.IsUnique);
        Assert.Equal(
            "UX_ForecastParametro_Escenario_Codigo",
            indiceParametro.GetDatabaseName());

        var relacionParametro = Assert.Single(parametro.GetForeignKeys());
        Assert.Equal(typeof(ForecastEscenario), relacionParametro.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, relacionParametro.DeleteBehavior);
        Assert.Equal(
            "FK_ForecastParametro_ForecastEscenario",
            relacionParametro.GetConstraintName());

        Assert.Equal(
            "enum('Colaborador','ContratacionPrevista')",
            participante.FindProperty(nameof(ForecastParticipante.TipoParticipante))?.GetColumnType());
        Assert.Equal(18, participante.FindProperty(nameof(ForecastParticipante.SalarioBaseMensual))?.GetPrecision());
        Assert.Equal(2, participante.FindProperty(nameof(ForecastParticipante.SalarioBaseMensual))?.GetScale());
        Assert.Equal("date", participante.FindProperty(nameof(ForecastParticipante.FechaInicioAplicacion))?.GetColumnType());
        Assert.Equal("date", participante.FindProperty(nameof(ForecastParticipante.FechaSalidaPrevista))?.GetColumnType());
        Assert.Equal("tinyint(1)", participante.FindProperty(nameof(ForecastParticipante.EstaIncluido))?.GetColumnType());
        Assert.Equal(true, participante.FindProperty(nameof(ForecastParticipante.EstaIncluido))?.GetDefaultValue());
        Assert.Equal("enum('Activo','Inactivo')", participante.FindProperty(nameof(ForecastParticipante.EstadoRegistro))?.GetColumnType());
        Assert.Equal(EstadosRegistro.Activo, participante.FindProperty(nameof(ForecastParticipante.EstadoRegistro))?.GetDefaultValue());

        var participanteDiseno = modeloDiseno.FindEntityType(typeof(ForecastParticipante));
        Assert.NotNull(participanteDiseno);
        var checksParticipante = participanteDiseno.GetCheckConstraints()
            .Select(check => check.Name)
            .ToHashSet();
        Assert.Equal(4, checksParticipante.Count);
        Assert.Contains("CK_ForecastParticipante_Tipo", checksParticipante);
        Assert.Contains("CK_ForecastParticipante_Salario", checksParticipante);
        Assert.Contains("CK_ForecastParticipante_Fechas", checksParticipante);
        Assert.Contains("CK_ForecastParticipante_Inclusion", checksParticipante);

        var indicesParticipante = participante.GetIndexes()
            .ToDictionary(indice => indice.GetDatabaseName()!, indice => indice);
        Assert.True(indicesParticipante["UX_ForecastParticipante_Escenario_Codigo"].IsUnique);
        Assert.True(indicesParticipante["UX_ForecastParticipante_Escenario_Colaborador"].IsUnique);
        Assert.False(indicesParticipante["IX_ForecastParticipante_IdDepartamento"].IsUnique);
        Assert.False(indicesParticipante["IX_ForecastParticipante_IdPuesto"].IsUnique);

        var claveAlternaParticipante = Assert.Single(participante.GetKeys(), clave => !clave.IsPrimaryKey());
        Assert.Equal("UX_ForecastParticipante_Escenario_Id", claveAlternaParticipante.GetName());
        Assert.Equal(
            [nameof(ForecastParticipante.IdForecastEscenario), nameof(ForecastParticipante.IdForecastParticipante)],
            claveAlternaParticipante.Properties.Select(propiedad => propiedad.Name));

        var relacionesParticipante = participante.GetForeignKeys()
            .ToDictionary(relacion => relacion.GetConstraintName()!, relacion => relacion);
        Assert.Equal(4, relacionesParticipante.Count);
        Assert.Equal(DeleteBehavior.Cascade, relacionesParticipante["FK_ForecastParticipante_ForecastEscenario"].DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, relacionesParticipante["FK_ForecastParticipante_Colaborador"].DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, relacionesParticipante["FK_ForecastParticipante_Departamento"].DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, relacionesParticipante["FK_ForecastParticipante_Puesto"].DeleteBehavior);

        Assert.Equal(18, detalle.FindProperty(nameof(ForecastDetalle.MontoBase))?.GetPrecision());
        Assert.Equal(2, detalle.FindProperty(nameof(ForecastDetalle.MontoBase))?.GetScale());
        Assert.Equal(500, detalle.FindProperty(nameof(ForecastDetalle.Observaciones))?.GetMaxLength());
        Assert.Equal(
            "enum('Activo','Inactivo')",
            detalle.FindProperty(nameof(ForecastDetalle.EstadoRegistro))?.GetColumnType());

        var indiceDetalle = Assert.Single(detalle.GetIndexes());
        Assert.True(indiceDetalle.IsUnique);
        Assert.Equal(
            "UX_FcstDetalle_Esc_Per_Part_Proy_Concepto",
            indiceDetalle.GetDatabaseName());

        var relacionDetalle = Assert.Single(detalle.GetForeignKeys());
        Assert.Equal(typeof(ForecastAsignacionProyecto), relacionDetalle.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, relacionDetalle.DeleteBehavior);
        Assert.Equal(
            "FK_FcstDetalle_AsignacionProyecto",
            relacionDetalle.GetConstraintName());
        Assert.Equal(
            [
                nameof(ForecastDetalle.IdForecastEscenario),
                nameof(ForecastDetalle.IdForecastPeriodo),
                nameof(ForecastDetalle.IdForecastParticipante),
                nameof(ForecastDetalle.IdProyecto)
            ],
            relacionDetalle.Properties.Select(propiedad => propiedad.Name));
    }
}

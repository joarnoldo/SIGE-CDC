using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Application.Planillas;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class ForecastAcademicoTests(ServidorAcademico servidor)
{
    private const string Actor = DatosFinancierosAcademicos.Actor;

    [Fact(DisplayName = "FOR-001 — Generar proyección desde histórico autorizado"), Trait("HU", "HU-FOR-001")]
    public async Task For001_GenerarProyeccionFuturaConHistoricoAutorizado()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        var id = await d.EscenarioAsync(); var resultado = await d.GenerarAsync(id);
        Assert.Equal("Calculado", resultado.EstadoEscenario); Assert.Equal(2, resultado.CantidadParticipantes); Assert.Equal(672000m, resultado.MontoProyectadoTotal);
        await using var lectura = e.LecturaNueva(); var periodo = await lectura.ForecastPeriodos.SingleAsync(p => p.IdForecastEscenario == id);
        Assert.Equal(new DateTime(2032, 2, 1), periodo.FechaInicio); Assert.Equal(new DateTime(2032, 2, 29), periodo.FechaFin);
        Assert.Equal(d.Planilla, (await lectura.ForecastFuentesHistoricas.SingleAsync(f => f.IdForecastEscenario == id)).IdPlanilla);
        var consulta = await e.Obtener<IForecastEscenarioService>().ObtenerDetalleAsync(id, Actor); Assert.NotNull(consulta);
        Assert.Equal(672000m, consulta.MontoProyectadoTotal);
    }

    [Fact(DisplayName = "FOR-002 — Rechazar histórico no aprobado"), Trait("HU", "HU-FOR-001")]
    public async Task For002_RechazarFuenteHistoricaNoAprobada()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync();
        var noAprobada = d.Planilla; var control = await d.CrearPeriodoAsync("ACA-CONTROL", new(2032, 2, 1));
        var ejecucionControl = await d.Recorrido.CalcularAsync(d.Solicitud(control), Actor); await d.Recorrido.AprobarAsync(control, ejecucionControl, Actor);
        var s = e.Obtener<IForecastEscenarioService>(); var fuentes = await s.ObtenerFuentesHistoricasDisponiblesAsync(Actor);
        Assert.Single(fuentes); Assert.DoesNotContain(fuentes, f => f.IdPlanilla == noAprobada);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => s.CrearBorradorAsync(new() { Nombre = "Fuente inválida académica",
            IdPlanillasHistoricas = [noAprobada], Periodos = [new() { TipoPeriodo = "Mensual", FechaInicio = new(2032, 3, 1), FechaFin = new(2032, 3, 31) }] }, Actor));
        Assert.Contains("aprobadas", error.Message); Assert.Empty(await s.ObtenerEscenariosAsync(Actor));
        await using var lectura = e.LecturaNueva(); Assert.Empty(await lectura.ForecastFuentesHistoricas.ToListAsync());
    }

    [Fact(DisplayName = "FOR-003 — Parámetro proyectado y preservación del histórico"), Trait("HU", "HU-FOR-002")]
    public async Task For003_AplicarParametroSinModificarHistorico()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        var escenario = await d.EscenarioAsync(); await d.BonoProyectadoAsync(escenario, 5000);
        var resultado = await d.GenerarAsync(escenario); Assert.Equal(672000m, resultado.MontoProyectadoTotal);
        // La especificación separa neto y costo laboral: el beneficio no salarial aumenta el primero.
        var consulta = await e.Obtener<IForecastResultadoService>().ObtenerResultadosAsync(escenario, new(), Actor);
        Assert.NotNull(consulta); Assert.Equal(592000m, consulta.SalarioNetoTotalConsulta);
        Assert.Equal(600000m, consulta.BrutoSalarialTotalConsulta); Assert.Equal(72000m, consulta.CargasPatronalesTotalConsulta);
        var configuracion = await e.Obtener<IForecastEvidenciaService>().ConsultarConfiguracionAsync(escenario, Actor);
        var parametro = Assert.Single(configuracion.Overrides); Assert.Equal(5000m, parametro.Valor); Assert.Equal(d.Conceptos["BONO_ACADEMICO"], parametro.IdConceptoVersion);
        await using var lectura = e.LecturaNueva(); var historico = await lectura.EjecucionesPlanilla.SingleAsync(x => x.IdEjecucionPlanilla == d.Ejecucion);
        Assert.Equal(600000m, historico.SalarioBrutoTotal); Assert.Equal(582000m, historico.SalarioNetoTotal); Assert.Equal(672000m, historico.CostoLaboralTotal);
        Assert.Empty(await lectura.IncidenciasPlanilla.ToListAsync());
    }

    [Fact(DisplayName = "FOR-004 — Excluir y reincorporar en escenario editable"), Trait("HU", "HU-FOR-003")]
    public async Task For004_ActualizarTotalesAlExcluirYReincorporarColaborador()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        // Dos borradores equivalentes permiten observar ambos resultados sin intentar
        // editar uno Calculado: la precondición aprobada es un escenario editable.
        var excluido = await d.EscenarioAsync("Composición con exclusión");
        var reincorporado = await d.EscenarioAsync("Composición con reincorporación");
        await d.ComposicionAsync(excluido, false);
        await d.ComposicionAsync(reincorporado, false);
        await using (var lectura = e.LecturaNueva())
        {
            Assert.Single(await lectura.ForecastParticipantes.Where(p => p.IdForecastEscenario == excluido && p.EstaIncluido).ToArrayAsync());
            Assert.Single(await lectura.ForecastParticipantes.Where(p => p.IdForecastEscenario == reincorporado && p.EstaIncluido).ToArrayAsync());
        }
        await d.ComposicionAsync(reincorporado, true);
        var sinSegundo = await d.GenerarAsync(excluido); var conSegundo = await d.GenerarAsync(reincorporado);
        Assert.Equal(1, sinSegundo.CantidadParticipantes); Assert.Equal(2, conSegundo.CantidadParticipantes);
        Assert.Equal(268800m, sinSegundo.MontoProyectadoTotal); Assert.Equal(672000m, conSegundo.MontoProyectadoTotal);
        Assert.Equal(403200m, conSegundo.MontoProyectadoTotal - sinSegundo.MontoProyectadoTotal);
    }

    [Fact(DisplayName = "FOR-005 — Guardar, recuperar y comparar escenarios"), Trait("HU", "HU-FOR-006")]
    public async Task For005_GuardarYCompararEscenariosCompatibles()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        var baseId = await d.EscenarioAsync("Base académica"); var alterno = await d.EscenarioAsync("Alternativa académica");
        await d.BonoProyectadoAsync(alterno, 5000); await d.GenerarAsync(baseId); await d.GenerarAsync(alterno);
        var s = e.Obtener<IForecastEscenarioService>(); await s.GuardarAsync(baseId, Actor); await s.GuardarAsync(alterno, Actor);
        var listado = await s.ObtenerEscenariosAsync(Actor); Assert.Equal(2, listado.Count);
        Assert.Equal(672000m, listado.Single(x => x.IdForecastEscenario == baseId).MontoProyectadoTotal);
        Assert.Equal(672000m, listado.Single(x => x.IdForecastEscenario == alterno).MontoProyectadoTotal);
        Assert.Equal("Base académica", (await s.ObtenerDetalleAsync(baseId, Actor))!.Nombre);
        Assert.Equal("Alternativa académica", (await s.ObtenerDetalleAsync(alterno, Actor))!.Nombre);
        var comparacion = await e.Obtener<IForecastComparacionEscenarioService>().CompararAsync(new() { IdEscenarioBase = baseId, IdEscenarioAlternativo = alterno }, Actor);
        Assert.Equal(baseId, comparacion.EscenarioBase.IdForecastEscenario); Assert.Equal(alterno, comparacion.EscenarioAlternativo.IdForecastEscenario);
        Assert.NotNull(comparacion.Snapshot);
        var neto = comparacion.Snapshot.Totales.Single(t => t.Nombre == "Neto");
        Assert.Equal(582000m, neto.Base); Assert.Equal(592000m, neto.Alternativo);
        Assert.Equal(10000m, neto.Alternativo - neto.Base);
    }

    [Fact(DisplayName = "FOR-006 — Comparar Forecast y planilla real autorizada"), Trait("HU", "HU-FOR-007")]
    public async Task For006_CompararProyeccionConPlanillaRealCompatible()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        var escenario = await d.EscenarioAsync(); await d.GenerarAsync(escenario); await e.Obtener<IForecastEscenarioService>().GuardarAsync(escenario, Actor);
        var periodoReal = await d.CrearPeriodoAsync("ACA-REAL-FEB", new(2032, 2, 1));
        var ejecucionReal = await d.Recorrido.CalcularAsync(d.Solicitud(periodoReal), Actor); await d.Recorrido.AprobarAsync(periodoReal, ejecucionReal, Actor);
        var s = e.Obtener<IForecastComparacionSnapshotService>();
        var detalles = await e.Db.DetallesPlanilla.Where(x => x.IdEjecucionPlanilla == ejecucionReal).Select(x => x.IdDetallePlanilla).ToArrayAsync();
        foreach (var id in detalles) await s.RegistrarDistribucionAsync(new(id, [new(null, true, 100)], DatosFinancierosAcademicos.Soporte), Actor);
        var comparacion = await s.CompararAsync(escenario, Actor); var periodo = Assert.Single(comparacion.Periodos);
        Assert.Equal(ejecucionReal, periodo.IdEjecucionReal); Assert.Equal(new DateTime(2032, 2, 1), periodo.Inicio);
        Assert.NotEqual(d.Ejecucion, periodo.IdEjecucionReal); Assert.NotEmpty(periodo.Filas);
        Assert.All(periodo.Filas.Where(f => f.TieneForecast && f.TieneReal), f => Assert.Equal(0m, f.DiferenciaFirmada));
        Assert.Contains(periodo.Filas, f => f.MontoForecast == 672000m && f.MontoReal == 672000m);
        var guardada = await s.LeerAsync(escenario, Actor); Assert.Equal(periodo.IdComparacion, Assert.Single(guardada.Periodos).IdComparacion);
    }
}

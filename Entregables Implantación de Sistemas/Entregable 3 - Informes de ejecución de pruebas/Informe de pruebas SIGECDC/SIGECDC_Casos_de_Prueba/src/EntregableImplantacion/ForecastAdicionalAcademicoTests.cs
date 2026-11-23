using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Infrastructure.Reportes;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class ForecastAdicionalAcademicoTests(ServidorAcademico servidor)
{
    private const string Actor = DatosFinancierosAcademicos.Actor;

    [Fact(DisplayName = "FOR-007 — Alta y salida futuras con efecto temporal"), Trait("HU", "HU-FOR-004")]
    public async Task For007_ContratacionYSalidaConImportesEsperados()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e);
        await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        var escenario = await d.EscenarioAsync(inicio: new(2032,3,1), condiciones: false);
        var movimientos = e.Obtener<IForecastMovimientoPersonalService>();
        var original = await movimientos.ObtenerConfiguracionAsync(escenario, Actor); Assert.NotNull(original);
        var salida = original.Colaboradores.Single(x => x.CodigoParticipante == "ACA-02");
        await movimientos.GuardarSalidaPrevistaAsync(escenario, salida.IdForecastParticipante, new(2032,3,15), Actor);
        var evidencia = e.Obtener<IForecastEvidenciaService>(); var conf = await evidencia.ConsultarConfiguracionAsync(escenario, Actor);
        var modelo = conf.CondicionesDisponibles.First().Condicion;
        var alta = await movimientos.RegistrarContratacionPrevistaAsync(escenario, new() { IdDepartamento = modelo.IdDepartamento,
            IdPuesto = modelo.IdPuesto, SalarioBaseMensual = 300000, FechaInicioAplicacion = new(2032,3,16) }, Actor);
        var riesgo = await e.Db.ConfiguracionesRiesgoTrabajo.Select(x => x.IdConfiguracionRiesgoTrabajo).SingleAsync();
        foreach (var propuesta in conf.CondicionesDisponibles)
        {
            var c = propuesta.Condicion;
            await evidencia.RegistrarCondicionAsync(c with { Soporte = DatosFinancierosAcademicos.Soporte, Fin = c.IdParticipante == salida.IdForecastParticipante ? new(2032,3,15) : c.Fin,
                IdConfiguracionRT = riesgo, ModalidadRT = SIGECDC.Domain.Planillas.ModalidadBaseRiesgo.RemuneracionSalarialOrdinaria,
                SoporteRT = DatosFinancierosAcademicos.Soporte }, Actor);
        }
        await evidencia.RegistrarCondicionAsync(modelo with { IdParticipante = alta, Inicio = new(2032,3,16), Fin = new(2032,3,31),
            Origen = "ContratacionFutura", Soporte = DatosFinancierosAcademicos.Soporte, IdContrato = null, IdCondicion = null, SalarioMensual = 300000, IdConfiguracionRT = riesgo,
            ModalidadRT = SIGECDC.Domain.Planillas.ModalidadBaseRiesgo.RemuneracionSalarialOrdinaria, SoporteRT = DatosFinancierosAcademicos.Soporte }, Actor);
        await e.Obtener<IForecastAsignacionProyectoService>().GuardarDistribucionAsync(escenario, alta, modelo.IdPeriodo,
            new() { Asignaciones = [new() { Porcentaje = 100 }] }, Actor);
        var guardada = await movimientos.ObtenerConfiguracionAsync(escenario, Actor); Assert.NotNull(guardada);
        Assert.Equal(new DateTime(2032,3,15), guardada.Colaboradores.Single(x => x.IdForecastParticipante == salida.IdForecastParticipante).FechaSalidaPrevista);
        Assert.Equal(new DateTime(2032,3,16), Assert.Single(guardada.Contrataciones).FechaInicioAplicacion);
        var resultado = await d.GenerarAsync(escenario); Assert.Equal(638400m, resultado.MontoProyectadoTotal);
        var consulta = await e.Obtener<IForecastResultadoService>().ObtenerResultadosAsync(escenario, new() { Dimension = DimensionesResultadoForecast.Colaborador }, Actor);
        Assert.NotNull(consulta); Assert.Equal(3, consulta.Filas.Count); Assert.Equal(570000m, consulta.BrutoSalarialTotalConsulta);
        Assert.Equal(552900m, consulta.SalarioNetoTotalConsulta);
        Assert.Equal(201600m, consulta.Filas.Single(x => x.Codigo == "ACA-02").CostoLaboralTotalProyectado);
        var codigoAlta = guardada.Contrataciones.Single(x => x.IdForecastParticipante == alta).CodigoParticipante;
        Assert.Equal(168000m, consulta.Filas.Single(x => x.Codigo == codigoAlta).CostoLaboralTotalProyectado);
    }

    [Fact(DisplayName = "FOR-008 — Resultados por colaborador, departamento, proyecto y período"), Trait("HU", "HU-FOR-005")]
    public async Task For008_DimensionesConciliadasSinOmisiones()
    {
        await using var e = await servidor.NuevoAsync(); var (d, escenario) = await PrepararDimensionesAsync(e);
        var s = e.Obtener<IForecastResultadoService>();
        foreach (var dimension in DimensionesResultadoForecast.Todas)
        {
            var consulta = await s.ObtenerResultadosAsync(escenario, new() { Dimension = dimension }, Actor); Assert.NotNull(consulta);
            Assert.Equal(2, consulta.Filas.Count); Assert.Equal(1344000m, consulta.MontoProyectadoTotalConsulta);
            Assert.Equal(1344000m, consulta.Filas.Sum(x => x.CostoLaboralTotalProyectado));
            Assert.Equal(1164000m, consulta.Filas.Sum(x => x.SalarioNetoProyectado));
            Assert.Equal(1200000m, consulta.Filas.Sum(x => x.SalarioBrutoProyectado));
            Assert.Equal(2, consulta.Filas.Select(x => x.Clave).Distinct().Count());
            var esperados = dimension == DimensionesResultadoForecast.Periodo ? new[] { 672000m,672000m } : new[] { 537600m,806400m };
            Assert.Equal(esperados.Order(), consulta.Filas.Select(x => x.CostoLaboralTotalProyectado).Order());
            foreach (var periodo in consulta.Periodos)
            {
                var filtrada = await s.ObtenerResultadosAsync(escenario, new() { Dimension = dimension, IdForecastPeriodo = periodo.IdForecastPeriodo }, Actor);
                Assert.NotNull(filtrada); Assert.Equal(672000m, filtrada.MontoProyectadoTotalConsulta);
                Assert.Equal(672000m, filtrada.Filas.Sum(x => x.CostoLaboralTotalProyectado));
            }
        }
        Assert.Equal("Calculado", (await e.Obtener<IForecastEscenarioService>().ObtenerDetalleAsync(escenario, Actor))!.EstadoEscenario);
    }

    [Fact(DisplayName = "FOR-009 — Desviaciones absolutas, porcentuales y agrupaciones"), Trait("HU", "HU-FOR-008")]
    public async Task For009_DesviacionesConReglaM_YDepartamentoRequerido()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e);
        await d.PrepararAsync(departamentosDistintos: true); await d.CalcularAsync(aprobar: true);
        var escenario = await d.EscenarioAsync(condiciones: false);
        var evidencia = e.Obtener<IForecastEvidenciaService>(); var conf = await evidencia.ConsultarConfiguracionAsync(escenario, Actor);
        var riesgo = await e.Db.ConfiguracionesRiesgoTrabajo.Select(x => x.IdConfiguracionRiesgoTrabajo).SingleAsync();
        var segundo = await e.Db.ForecastParticipantes.Where(x => x.IdForecastEscenario == escenario && x.IdColaborador == d.Colaboradores[1]).Select(x => x.IdForecastParticipante).SingleAsync();
        foreach (var propuesta in conf.CondicionesDisponibles)
        {
            var c = propuesta.Condicion;
            if (c.IdParticipante == segundo) c = c with { Origen = "CambioFuturo", IdContrato = null, IdCondicion = null, SalarioMensual = 390000 };
            await evidencia.RegistrarCondicionAsync(c with { Soporte = DatosFinancierosAcademicos.Soporte, IdConfiguracionRT = riesgo,
                ModalidadRT = SIGECDC.Domain.Planillas.ModalidadBaseRiesgo.RemuneracionSalarialOrdinaria, SoporteRT = DatosFinancierosAcademicos.Soporte }, Actor);
        }
        await d.GenerarAsync(escenario); await e.Obtener<IForecastEscenarioService>().GuardarAsync(escenario, Actor);
        var periodoReal = await d.CrearPeriodoAsync("ACA-REAL-FOR009", new(2032,2,1));
        var ejecucion = await d.Recorrido.CalcularAsync(d.Solicitud(periodoReal), Actor); await d.Recorrido.AprobarAsync(periodoReal, ejecucion, Actor);
        var m = e.Obtener<IForecastComparacionSnapshotService>();
        foreach (var id in await e.Db.DetallesPlanilla.Where(x => x.IdEjecucionPlanilla == ejecucion).Select(x => x.IdDetallePlanilla).ToArrayAsync())
            await m.RegistrarDistribucionAsync(new(id, [new(null,true,100)], DatosFinancierosAcademicos.Soporte), Actor);
        var comparacion = await m.CompararAsync(escenario, Actor); var periodo = Assert.Single(comparacion.Periodos);
        Assert.Equal(ejecucion, periodo.IdEjecucionReal);
        var total = Assert.Single(periodo.Filas, x => x.Granularidad == "Total");
        Assert.Equal(705600m, total.MontoForecast); Assert.Equal(672000m, total.MontoReal);
        Assert.Equal(-33600m, total.DiferenciaFirmada); Assert.Equal(33600m, total.DiferenciaAbsoluta); Assert.Equal(-4.7619m, total.DiferenciaPorcentual);
        var personas = periodo.Filas.Where(x => x.Granularidad == "Participante").ToArray(); Assert.Equal(2, personas.Length);
        Assert.Equal(-33600m, personas.Sum(x => x.DiferenciaFirmada));
        var proyecto = Assert.Single(periodo.Filas, x => x.Granularidad == "Proyecto"); Assert.Equal(-33600m, proyecto.DiferenciaFirmada);
        var releida = await m.LeerAsync(escenario, Actor); Assert.Equal(periodo.IdComparacion, Assert.Single(releida.Periodos).IdComparacion);
        Assert.True(periodo.Filas.Any(x => x.Granularidad == "Departamento"),
            "QA51-DEF-002: M conserva dimensiones históricas por persona, pero QA02 exige variaciones agrupadas por departamento y la consulta no devuelve esa agrupación.");
    }

    [Fact(DisplayName = "FOR-010 — PDF y Excel del mismo resultado filtrado"), Trait("HU", "HU-FOR-009")]
    public async Task For010_ExportacionesFiltradasDesdeResultadosPersistidos()
    {
        await using var e = await servidor.NuevoAsync(); var (d, escenario) = await PrepararDimensionesAsync(e);
        var s = e.Obtener<IForecastResultadoService>();
        var completa = await s.ObtenerResultadosAsync(escenario, new(), Actor); Assert.NotNull(completa);
        var periodo = completa.Periodos.OrderBy(x => x.NumeroOrden).First();
        var consulta = await s.ObtenerResultadosAsync(escenario, new() { Dimension = DimensionesResultadoForecast.Colaborador, IdForecastPeriodo = periodo.IdForecastPeriodo }, Actor);
        Assert.NotNull(consulta); Assert.Equal(672000m, consulta.MontoProyectadoTotalConsulta); Assert.Equal(2, consulta.Filas.Count);
        var antes = await e.Db.ForecastDetalles.AsNoTracking().Where(x => x.IdForecastEscenario == escenario).Select(x => x.IdForecastDetalle).OrderBy(x => x).ToArrayAsync();
        var excel = new GeneradorForecastExcel().Generar(consulta); using var contenidoExcel = excel.Contenido;
        using var libro = new XLWorkbook(contenidoExcel); var hoja = libro.Worksheet("Resultados");
        Assert.Equal("ACA-01", hoja.Cell(12,2).GetString()); Assert.Equal("ACA-02", hoja.Cell(13,2).GetString());
        Assert.Equal(268800m, hoja.Cell(12,11).GetValue<decimal>()); Assert.Equal(403200m, hoja.Cell(13,11).GetValue<decimal>());
        Assert.Equal(672000m, hoja.Cell(14,11).GetValue<decimal>()); Assert.Equal(582000m, hoja.Cell(14,9).GetValue<decimal>());
        Assert.Equal(2, hoja.RowsUsed().Count(r => r.Cell(2).GetString() is "ACA-01" or "ACA-02"));
        var pdf = new GeneradorForecastPdf().Generar(consulta); using var contenidoPdf = pdf.Contenido;
        using var bytes = new MemoryStream(); await contenidoPdf.CopyToAsync(bytes); var texto = PdfAcademico.Texto(bytes.ToArray());
        Assert.Contains("ACA-01", texto); Assert.Contains("ACA-02", texto); Assert.Contains(consulta.NombreEscenario, texto);
        PdfAcademico.ContieneMonto(texto, 268800m); PdfAcademico.ContieneMonto(texto, 403200m); PdfAcademico.ContieneMonto(texto, 672000m);
        Assert.DoesNotContain("31/03/2032", texto); Assert.DoesNotContain("1,344,000.00", texto);
        Assert.Equal(antes, await e.Db.ForecastDetalles.AsNoTracking().Where(x => x.IdForecastEscenario == escenario).Select(x => x.IdForecastDetalle).OrderBy(x => x).ToArrayAsync());
    }

    private static async Task<(DatosFinancierosAcademicos Datos, long Escenario)> PrepararDimensionesAsync(EntornoAcademico e)
    {
        var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(departamentosDistintos: true); await d.CalcularAsync(aprobar: true);
        var escenario = await d.EscenarioAsync("Forecast multidimensional QA51", meses: 2);
        var proyectos = new[] { await e.ProyectoAsync("PRO-FOR-A"), await e.ProyectoAsync("PRO-FOR-B") };
        var servicioProyectos = e.Obtener<SIGECDC.Application.Operaciones.IProyectoService>();
        foreach (var id in proyectos)
        {
            var p = (await servicioProyectos.ObtenerProyectoPorIdAsync(id))!;
            await servicioProyectos.GuardarProyectoAsync(new() { IdProyecto = id, CodigoProyecto = p.CodigoProyecto, NombreProyecto = p.NombreProyecto,
                FechaInicio = new(2032,1,1), FechaFinEstimada = new(2032,4,30),
                IdEstadoProyecto = (await servicioProyectos.ObtenerEstadosAsync()).Single(x => x.Nombre == "Planificado").IdEstadoProyecto }, EntornoAcademico.Ops);
        }
        var personas = await e.Db.ForecastParticipantes.Where(x => x.IdForecastEscenario == escenario && x.EstaIncluido).OrderBy(x => x.IdColaborador).ToArrayAsync();
        var periodos = await e.Db.ForecastPeriodos.Where(x => x.IdForecastEscenario == escenario).ToArrayAsync();
        for (var i=0;i<personas.Length;i++) foreach(var p in periodos)
            await e.Obtener<IForecastAsignacionProyectoService>().GuardarDistribucionAsync(escenario, personas[i].IdForecastParticipante,p.IdForecastPeriodo,
                new() { Asignaciones = [new() { IdProyecto = proyectos[i], Porcentaje = 100 }] },Actor);
        await d.GenerarAsync(escenario); return (d,escenario);
    }
}

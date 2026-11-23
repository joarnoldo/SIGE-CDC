using System.Net;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.Planillas;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class PlanillaAcademicoTests(ServidorAcademico servidor)
{
    [Fact(DisplayName = "PLA-001 — Registrar incidencias de planilla"), Trait("HU", "HU-RH-004")]
    public async Task Pla001_RegistrarHorasExtraAusenciaYBono()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync();
        await d.CapturarAsync("HORA_EXTRA_ORDINARIA", "Hora", 2, null);
        await d.CapturarAsync("AUSENCIA_SIN_GOCE", "Día", 1, null);
        await d.CapturarAsync("BONO_ACADEMICO", "Monto", null, 2500);
        var otroPeriodo = await d.CrearPeriodoAsync("ACA-CONTROL-INC", new(2032, 2, 1));
        await d.Recorrido.CapturarAsync(new() { IdPeriodo = otroPeriodo, IdColaborador = d.Colaboradores[1],
            IdConceptoVersion = d.Conceptos["BONO_ACADEMICO"], Fecha = new(2032, 2, 10), Unidad = "Monto", Monto = 999,
            ReferenciaHecho = DatosFinancierosAcademicos.Soporte, RelacionOrdinaria = DatosFinancierosAcademicos.Soporte }, DatosFinancierosAcademicos.Actor);
        var filas = await e.Obtener<IConsultaRecorridoPlanilla>().IncidenciasAsync(d.Periodo, DatosFinancierosAcademicos.Actor);
        Assert.Equal(3, filas.Count); Assert.All(filas, f => Assert.Equal(d.Colaboradores[0], f.Captura.IdColaborador));
        Assert.Contains(filas, f => f.Captura.Unidad == "Hora" && f.Captura.Cantidad == 2);
        Assert.Contains(filas, f => f.Captura.Unidad == "Día" && f.Captura.Cantidad == 1);
        Assert.Contains(filas, f => f.Captura.Unidad == "Monto" && f.Captura.MontoManual == 2500);
        await using var lectura = e.LecturaNueva(); Assert.Equal(4, await lectura.IncidenciasPlanilla.CountAsync());
        Assert.DoesNotContain(filas, x => x.Captura.IdPeriodo == otroPeriodo);
    }

    [Fact(DisplayName = "PLA-002 — Calcular planilla válida"), Trait("HU", "HU-RH-005")]
    public async Task Pla002_CalcularPlanillaConTotalesEsperados()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync();
        Assert.Equal("Borrador", (await e.Obtener<IPlanillaService>().ObtenerDetallePeriodoAsync(d.Periodo))!.EstadoPlanilla);
        await d.CalcularAsync();
        var periodo = await e.Obtener<IPlanillaService>().ObtenerDetallePeriodoAsync(d.Periodo); Assert.NotNull(periodo);
        Assert.Equal("Calculada", periodo.EstadoPlanilla); Assert.Equal(2, periodo.Detalles.Count);
        Assert.Equal(600000m, periodo.SalarioBrutoTotal); Assert.Equal(18000m, periodo.DeduccionesTotal); Assert.Equal(582000m, periodo.SalarioNetoTotal);
        Assert.Equal(232800m, periodo.Detalles.Single(x => x.IdColaborador == d.Colaboradores[0]).SalarioNeto);
        Assert.Equal(349200m, periodo.Detalles.Single(x => x.IdColaborador == d.Colaboradores[1]).SalarioNeto);
        await using var lectura = e.LecturaNueva(); Assert.Equal(672000m, (await lectura.EjecucionesPlanilla.SingleAsync()).CostoLaboralTotal);
    }

    [Fact(DisplayName = "PLA-003 — Bloquear cálculo sin condición laboral"), Trait("HU", "HU-RH-005")]
    public async Task Pla003_RechazarCalculoIncompletoSinPersistenciaParcial()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(conCondiciones: false);
        var error = await Assert.ThrowsAsync<ConfiguracionConceptoException>(() => d.CalcularAsync());
        Assert.Contains("HISTORIA_NO_DEMOSTRABLE", error.Message);
        await using var lectura = e.LecturaNueva(); Assert.Empty(await lectura.EjecucionesPlanilla.ToListAsync()); Assert.Empty(await lectura.DetallesPlanilla.ToListAsync());
        Assert.Empty(await lectura.Planillas.ToListAsync());
        Assert.Equal("Borrador", (await e.Obtener<IPlanillaService>().ObtenerDetallePeriodoAsync(d.Periodo))!.EstadoPlanilla);
    }

    [Fact(DisplayName = "PLA-004 — Parámetro aplicado al colaborador correspondiente"), Trait("HU", "HU-RH-006")]
    public async Task Pla004_ConfigurarYAsignarConceptoAUnColaborador()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync();
        var parametro = await d.Recorrido.RegistrarParametroAsync("QA_BENEFICIO", "Beneficio académico", d.Conceptos["BONO_ACADEMICO"], 1250,
            DatosFinancierosAcademicos.Vigencia, DatosFinancierosAcademicos.Soporte, DatosFinancierosAcademicos.Actor);
        await d.Recorrido.AsignarParametroAsync(new(parametro, AmbitoParametroInterno.Colaborador, d.Colaboradores[0], 2500, null,
            DatosFinancierosAcademicos.Soporte), DatosFinancierosAcademicos.Actor);
        await d.CapturarAsync("BONO_ACADEMICO", "Monto", null, 2500, parametro: parametro);
        await d.CalcularAsync();
        var reporte = await e.Obtener<IReportePlanillaService>().ObtenerReporteAsync(d.Periodo, actor: DatosFinancierosAcademicos.Actor); Assert.NotNull(reporte);
        var primero = reporte.Detalles.Single(x => x.CodigoColaborador == "ACA-01"); var segundo = reporte.Detalles.Single(x => x.CodigoColaborador == "ACA-02");
        Assert.Equal(2500m, primero.IngresosNoSalariales); Assert.Equal(235300m, primero.SalarioNeto);
        Assert.Equal(0m, segundo.IngresosNoSalariales); Assert.Equal(349200m, segundo.SalarioNeto); Assert.Equal(584500m, reporte.SalarioNetoTotal);
    }

    [Fact(DisplayName = "PLA-005 — Consultar y descargar colilla propia"), Trait("HU", "HU-RH-007")]
    public async Task Pla005_ConsultarYDescargarColillaPropia()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync();
        await VincularAsync(e, d); await d.CalcularAsync(aprobar: true); var s = e.Obtener<IColillaPagoService>();
        Assert.Equal(2, await s.GenerarPendientesAsync(d.Periodo, DatosFinancierosAcademicos.Actor));
        var portal = await s.ObtenerPortalPropioAsync(EntornoAcademico.Empleado); Assert.True(portal.TieneColaboradorVinculado);
        var propia = Assert.Single(portal.Colillas); Assert.Equal(232800m, propia.SalarioNeto);
        var detalle = await s.ObtenerDetallePropioAsync(propia.IdColillaPago, EntornoAcademico.Empleado); Assert.NotNull(detalle);
        Assert.Equal("ACA-01", detalle.CodigoColaborador); Assert.Equal(240000m, detalle.SalarioBruto); Assert.Equal(7200m, detalle.TotalDeducciones);
        await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var ingreso = await WebAcademica.IngresarAsync(cliente, EntornoAcademico.Empleado); Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);
        using var descarga = await cliente.GetAsync($"/empleado/colillas/{propia.IdColillaPago}/pdf"); Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType?.MediaType);
        var texto = PdfAcademico.Texto(await descarga.Content.ReadAsByteArrayAsync());
        Assert.Contains("ACA-01", texto); Assert.Contains($"Ejecución {d.Ejecucion}", texto); Assert.DoesNotContain("ACA-02", texto);
        Assert.Contains("01/01/2032", texto); Assert.Contains("31/01/2032", texto);
        PdfAcademico.ContieneMonto(texto, 232800m);
    }

    [Fact(DisplayName = "PLA-006 — Proteger colilla de otro empleado"), Trait("HU", "HU-RH-007")]
    public async Task Pla006_DenegarColillaAjenaPorAccesoDirecto()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await VincularAsync(e, d);
        await d.CalcularAsync(aprobar: true); var s = e.Obtener<IColillaPagoService>(); await s.GenerarPendientesAsync(d.Periodo, DatosFinancierosAcademicos.Actor);
        var propia = Assert.Single((await s.ObtenerPortalPropioAsync(EntornoAcademico.Empleado)).Colillas).IdColillaPago;
        var ajena = await e.Db.ColillasPago.Where(c => c.IdColillaPago != propia).Select(c => c.IdColillaPago).SingleAsync();
        Assert.Null(await s.ObtenerDetallePropioAsync(ajena, EntornoAcademico.Empleado));
        await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var ingreso = await WebAcademica.IngresarAsync(cliente, EntornoAcademico.Empleado); Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);
        using var control = await cliente.GetAsync($"/empleado/colillas/{propia}/pdf"); Assert.Equal(HttpStatusCode.OK, control.StatusCode);
        using var denegada = await cliente.GetAsync($"/empleado/colillas/{ajena}/pdf"); Assert.Equal(HttpStatusCode.NotFound, denegada.StatusCode);
        var cuerpo = await denegada.Content.ReadAsStringAsync(); Assert.DoesNotContain("ACA-02", cuerpo); Assert.DoesNotContain("%PDF", cuerpo);
    }

    [Fact(DisplayName = "PLA-007 — Bloqueo de planilla aprobada y cerrada"), Trait("HU", "HU-RH-008")]
    public async Task Pla007_AprobarCerrarYRechazarCambios()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        foreach (var estado in new[] { "Aprobada", "Cerrada" })
        {
            if (estado == "Cerrada") await d.Recorrido.CerrarAsync(d.Periodo, d.Ejecucion, DatosFinancierosAcademicos.Actor);
            Assert.Equal(estado, (await e.Obtener<IPlanillaService>().ObtenerDetallePeriodoAsync(d.Periodo))!.EstadoPlanilla);
            var error = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => d.CapturarAsync("BONO_ACADEMICO", "Monto", null, 5000));
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
            await using var lectura = e.LecturaNueva(); Assert.Empty(await lectura.IncidenciasPlanilla.ToListAsync());
            var guardada = await lectura.EjecucionesPlanilla.SingleAsync(); Assert.Equal(d.Ejecucion, guardada.IdEjecucionPlanilla);
            Assert.Equal(600000m, guardada.SalarioBrutoTotal); Assert.Equal(582000m, guardada.SalarioNetoTotal);
        }
    }

    [Fact(DisplayName = "PLA-008 — Reportes de planilla en PDF y Excel"), Trait("HU", "HU-RH-009")]
    public async Task Pla008_ExportarReporteConPeriodoYTotalesCorrectos()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var ingreso = await WebAcademica.IngresarAsync(cliente, DatosFinancierosAcademicos.Actor); Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);
        using var excel = await cliente.GetAsync($"/rrhh/reportes-planilla/{d.Periodo}/excel"); Assert.Equal(HttpStatusCode.OK, excel.StatusCode);
        using var libro = new XLWorkbook(new MemoryStream(await excel.Content.ReadAsByteArrayAsync())); var hoja = libro.Worksheet("Planilla");
        Assert.Equal($"EJ-{d.Ejecucion}", hoja.Cell("B4").GetString()); Assert.Equal(new DateTime(2032, 1, 1), hoja.Cell("E5").GetDateTime());
        Assert.Equal(new DateTime(2032, 1, 31), hoja.Cell("I5").GetDateTime());
        Assert.Equal(600000m, hoja.Cell("A9").GetValue<decimal>()); Assert.Equal(18000m, hoja.Cell("E9").GetValue<decimal>()); Assert.Equal(582000m, hoja.Cell("I9").GetValue<decimal>());
        var filas = hoja.RowsUsed().Where(r => r.Cell(1).GetString().StartsWith("ACA-0", StringComparison.Ordinal)).ToArray(); Assert.Equal(2, filas.Length);
        Assert.Equal(232800m, filas.Single(r => r.Cell(1).GetString() == "ACA-01").Cell(11).GetValue<decimal>());
        Assert.Equal(349200m, filas.Single(r => r.Cell(1).GetString() == "ACA-02").Cell(11).GetValue<decimal>());
        using var pdf = await cliente.GetAsync($"/rrhh/reportes-planilla/{d.Periodo}/pdf"); Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        var texto = PdfAcademico.Texto(await pdf.Content.ReadAsByteArrayAsync());
        Assert.Contains($"Ejecución {d.Ejecucion}", texto); Assert.Contains("01/01/2032", texto); Assert.Contains("31/01/2032", texto);
        Assert.Contains("ACA-01", texto); Assert.Contains("ACA-02", texto);
        PdfAcademico.ContieneMonto(texto, 600000m); PdfAcademico.ContieneMonto(texto, 18000m); PdfAcademico.ContieneMonto(texto, 582000m);
    }

    private static Task VincularAsync(EntornoAcademico e, DatosFinancierosAcademicos d) => e.Obtener<IColaboradorService>()
        .VincularCuentaEmpleadoAsync(d.Colaboradores[0], new() { IdUsuario = EntornoAcademico.Empleado }, EntornoAcademico.Admin);
}

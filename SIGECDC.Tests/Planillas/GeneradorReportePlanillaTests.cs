using ClosedXML.Excel;
using PdfSharp.Pdf.IO;
using SIGECDC.Application.Planillas;
using SIGECDC.Infrastructure.Reportes;

namespace SIGECDC.Tests.Planillas;

public sealed class GeneradorReportePlanillaTests
{
    [Fact]
    public void GenerarPdf_CreaDocumentoHorizontalMultipaginaConNombreYMimeCorrectos()
    {
        var generador = new GeneradorReportePlanillaPdf();
        var reporte = CrearReporte(75);

        var archivo = generador.Generar(reporte);

        Assert.Equal("application/pdf", archivo.MimeType);
        Assert.Equal("reporte-planilla-PLAN-2026-Q1.pdf", archivo.NombreOriginal);
        Assert.Equal(0, archivo.Contenido.Position);

        using var memoria = new MemoryStream();
        archivo.Contenido.CopyTo(memoria);
        var bytes = memoria.ToArray();
        Assert.True(bytes.Length > 10_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

        memoria.Position = 0;
        using var documento = PdfReader.Open(memoria, PdfDocumentOpenMode.Import);
        Assert.True(documento.PageCount > 1);
        foreach (var pagina in documento.Pages.Cast<PdfSharp.Pdf.PdfPage>())
        {
            Assert.InRange(pagina.Width.Point, 841.5, 842.5);
            Assert.InRange(pagina.Height.Point, 594.5, 595.5);
        }
    }

    [Fact]
    public void GenerarExcel_CreaLibroTipadoConMismasFilasYTotales()
    {
        var generador = new GeneradorReportePlanillaExcel();
        var reporte = CrearReporte(2);
        reporte.Detalles[0].CodigoColaborador = "=2+2";
        reporte.Detalles[0].NombreColaborador = "+José Núñez";
        reporte.Detalles[0].Departamento = "@Recursos Humanos";
        reporte.Detalles[1].CodigoColaborador = "-10+20";

        var archivo = generador.Generar(reporte);

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            archivo.MimeType);
        Assert.Equal("reporte-planilla-PLAN-2026-Q1.xlsx", archivo.NombreOriginal);
        Assert.Equal(0, archivo.Contenido.Position);

        using var libro = new XLWorkbook(archivo.Contenido);
        var hoja = libro.Worksheet("Planilla");

        Assert.Equal("Reporte consolidado de planilla", hoja.Cell("A1").GetString());
        Assert.Equal(reporte.CodigoPeriodo, hoja.Cell("B4").GetString());
        Assert.Equal(reporte.FechaInicio.Date, hoja.Cell("E5").GetDateTime().Date);
        Assert.Equal(reporte.FechaFin.Date, hoja.Cell("I5").GetDateTime().Date);
        Assert.Equal(reporte.SalarioBrutoTotal, hoja.Cell("A9").GetValue<decimal>());
        Assert.Equal(reporte.DeduccionesTotal, hoja.Cell("E9").GetValue<decimal>());
        Assert.Equal(reporte.SalarioNetoTotal, hoja.Cell("I9").GetValue<decimal>());

        Assert.Equal("Código", hoja.Cell("A11").GetString());
        Assert.Equal("Salario neto", hoja.Cell("K11").GetString());
        Assert.Equal(reporte.Detalles.Count, hoja.RangeUsed()!.RowCount() - 12);

        Assert.Equal("=2+2", hoja.Cell("A12").GetString());
        Assert.Equal("+José Núñez", hoja.Cell("B12").GetString());
        Assert.Equal("@Recursos Humanos", hoja.Cell("C12").GetString());
        Assert.False(hoja.Cell("A12").HasFormula);
        Assert.False(hoja.Cell("B12").HasFormula);
        Assert.False(hoja.Cell("C12").HasFormula);
        Assert.Equal("-10+20", hoja.Cell("A13").GetString());
        Assert.False(hoja.Cell("A13").HasFormula);
        Assert.Equal(reporte.Detalles[0].SalarioProporcional, hoja.Cell("D12").GetValue<decimal>());

        var filaTotales = 12 + reporte.Detalles.Count;
        Assert.Equal("Totales oficiales", hoja.Cell(filaTotales, 1).GetString());
        Assert.Equal(reporte.SalarioBrutoTotal, hoja.Cell(filaTotales, 9).GetValue<decimal>());
        Assert.Equal(reporte.DeduccionesTotal, hoja.Cell(filaTotales, 10).GetValue<decimal>());
        Assert.Equal(reporte.SalarioNetoTotal, hoja.Cell(filaTotales, 11).GetValue<decimal>());
    }

    [Fact]
    public void Generadores_RechazanReporteSinFilas()
    {
        var reporte = CrearReporte(0);

        var errorPdf = Assert.Throws<ArgumentException>(() =>
            new GeneradorReportePlanillaPdf().Generar(reporte));
        var errorExcel = Assert.Throws<ArgumentException>(() =>
            new GeneradorReportePlanillaExcel().Generar(reporte));

        Assert.Contains("filas", errorPdf.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("filas", errorExcel.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ReportePlanilla CrearReporte(int cantidadDetalles)
    {
        var detalles = Enumerable.Range(1, cantidadDetalles)
            .Select(indice => new DetalleReportePlanilla
            {
                CodigoColaborador = $"COL-{indice:000}",
                NombreColaborador = $"José Núñez Álvarez {indice:000}",
                Departamento = indice % 2 == 0 ? "Operaciones" : "Recursos Humanos",
                SalarioProporcional = 500_000m + indice,
                TotalHorasExtra = 15_000m,
                TotalBonos = 10_000m,
                TotalBeneficiosConfigurables = 5_000m,
                TotalAusencias = 2_500m,
                SalarioBruto = 527_500m + indice,
                TotalDeducciones = 47_500m,
                SalarioNeto = 480_000m + indice
            })
            .ToList();

        return new ReportePlanilla
        {
            IdPeriodoPlanilla = 17,
            CodigoPeriodo = "PLAN-2026-Q1",
            NombrePeriodo = "Primera quincena de agosto",
            TipoPeriodo = "Quincenal",
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 15),
            EstadoPlanilla = "Cerrada",
            FechaCalculo = new DateTime(2026, 8, 16, 8, 0, 0),
            FechaAprobacion = new DateTime(2026, 8, 16, 9, 0, 0),
            FechaCierre = new DateTime(2026, 8, 16, 10, 0, 0),
            SalarioBrutoTotal = detalles.Sum(detalle => detalle.SalarioBruto),
            DeduccionesTotal = detalles.Sum(detalle => detalle.TotalDeducciones),
            SalarioNetoTotal = detalles.Sum(detalle => detalle.SalarioNeto),
            Detalles = detalles
        };
    }
}

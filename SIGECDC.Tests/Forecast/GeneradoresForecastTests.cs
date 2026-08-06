using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Pdf.IO;
using SIGECDC.Application.Forecast;
using SIGECDC.Infrastructure.Reportes;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

public sealed class GeneradoresForecastTests
{
    [MySqlQaFact]
    public async Task ConsultaMySqlFiltrada_GeneraAmbosArchivosSinModificarElEscenario()
    {
        var token = $"EXP{Guid.NewGuid():N}"[..12];

        try
        {
            var preparado = await MySqlForecastResultadoIntegrationTests
                .PrepararEscenarioCalculadoAsync(token);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var antes = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => new
                {
                    item.EstadoEscenario,
                    item.FechaModificacion,
                    item.ModificadoPor,
                    item.MontoProyectadoTotal
                })
                .SingleAsync();
            var servicio = new ForecastResultadoService(contexto);
            var consulta = Assert.IsType<ConsultaResultadosForecast>(
                await servicio.ObtenerResultadosAsync(
                    preparado.IdEscenario,
                    new FiltroResultadosForecast
                    {
                        Dimension = DimensionesResultadoForecast.Proyecto,
                        IdForecastPeriodo = preparado.IdsPeriodos[0]
                    },
                    preparado.Datos.IdActorRecursosHumanos));

            var pdf = new GeneradorForecastPdf().Generar(consulta);
            var excel = new GeneradorForecastExcel().Generar(consulta);

            Assert.Equal(
                $"forecast-{preparado.IdEscenario}-proyecto-periodo-1.pdf",
                pdf.NombreOriginal);
            Assert.Equal(
                $"forecast-{preparado.IdEscenario}-proyecto-periodo-1.xlsx",
                excel.NombreOriginal);
            Assert.Equal("application/pdf", pdf.MimeType);
            Assert.Equal(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                excel.MimeType);
            Assert.NotEmpty(consulta.Filas);
            Assert.Empty(contexto.ChangeTracker.Entries());

            using (var libro = new XLWorkbook(excel.Contenido))
            {
                Assert.Equal(
                    consulta.Filas.Count,
                    libro.Worksheet("Resultados").RangeUsed()!.RowCount() - 12);
                Assert.Equal(
                    consulta.Filas.Sum(fila => fila.Conceptos.Count),
                    libro.Worksheet("Conceptos").RangeUsed()!.RowCount() - 5);
            }

            var despues = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => new
                {
                    item.EstadoEscenario,
                    item.FechaModificacion,
                    item.ModificadoPor,
                    item.MontoProyectadoTotal
                })
                .SingleAsync();
            Assert.Equal(antes, despues);
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [Fact]
    public void GenerarPdf_CreaDocumentoHorizontalMultipaginaConNombreYMimeCorrectos()
    {
        var consulta = CrearConsulta(35, DimensionesResultadoForecast.Proyecto);

        var archivo = new GeneradorForecastPdf().Generar(consulta);

        Assert.Equal("application/pdf", archivo.MimeType);
        Assert.Equal("forecast-42-proyecto-todos.pdf", archivo.NombreOriginal);
        Assert.Equal(0, archivo.Contenido.Position);

        using var memoria = new MemoryStream();
        archivo.Contenido.CopyTo(memoria);
        var bytes = memoria.ToArray();
        Assert.True(bytes.Length > 15_000);
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
    public void GenerarExcel_CreaResultadosYConceptosTipadosConConsultaExacta()
    {
        var consulta = CrearConsulta(
            2,
            DimensionesResultadoForecast.Colaborador,
            idPeriodoSeleccionado: 102,
            contenidoPeligroso: true);

        var archivo = new GeneradorForecastExcel().Generar(consulta);

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            archivo.MimeType);
        Assert.Equal("forecast-42-colaborador-periodo-2.xlsx", archivo.NombreOriginal);
        Assert.Equal(0, archivo.Contenido.Position);

        using var libro = new XLWorkbook(archivo.Contenido);
        Assert.Equal(
            new[] { "Resultados", "Conceptos" },
            libro.Worksheets.Select(item => item.Name));

        var resultados = libro.Worksheet("Resultados");
        Assert.Equal("Resultados del forecast", resultados.Cell("A1").GetString());
        Assert.Equal(consulta.IdForecastEscenario, resultados.Cell("B4").GetValue<long>());
        Assert.Equal(consulta.NombreEscenario, resultados.Cell("E4").GetString());
        Assert.Equal(consulta.EstadoEscenario, resultados.Cell("H4").GetString());
        Assert.Equal(consulta.FechaCalculo!.Value, resultados.Cell("B5").GetDateTime());
        Assert.Equal("Colaborador", resultados.Cell("E5").GetString());
        Assert.Contains("2. Quincenal", resultados.Cell("H5").GetString());
        Assert.Equal(consulta.MontoProyectadoTotalConsulta, resultados.Cell("A8").GetValue<decimal>());
        Assert.Equal(consulta.DeduccionesTotalConsulta, resultados.Cell("D8").GetValue<decimal>());
        Assert.Equal(consulta.SalarioNetoTotalConsulta, resultados.Cell("G8").GetValue<decimal>());

        Assert.Equal("Clave", resultados.Cell("A11").GetString());
        Assert.Equal("Salario neto", resultados.Cell("I11").GetString());
        Assert.Equal(consulta.Filas[0].Clave, resultados.Cell("A12").GetString());
        Assert.Equal("=2+2", resultados.Cell("B12").GetString());
        Assert.Equal("+José Núñez", resultados.Cell("C12").GetString());
        Assert.Equal("@Recursos Humanos", resultados.Cell("D12").GetString());
        Assert.False(resultados.Cell("B12").HasFormula);
        Assert.False(resultados.Cell("C12").HasFormula);
        Assert.False(resultados.Cell("D12").HasFormula);
        Assert.Equal(consulta.Filas[0].SalarioBrutoProyectado, resultados.Cell("G12").GetValue<decimal>());

        var filaTotales = 12 + consulta.Filas.Count;
        Assert.Equal("Totales de la consulta", resultados.Cell(filaTotales, 1).GetString());
        Assert.Equal(consulta.MontoProyectadoTotalConsulta, resultados.Cell(filaTotales, 7).GetValue<decimal>());
        Assert.Equal(consulta.DeduccionesTotalConsulta, resultados.Cell(filaTotales, 8).GetValue<decimal>());
        Assert.Equal(consulta.SalarioNetoTotalConsulta, resultados.Cell(filaTotales, 9).GetValue<decimal>());

        var conceptos = libro.Worksheet("Conceptos");
        Assert.Equal("Desglose de conceptos del forecast", conceptos.Cell("A1").GetString());
        Assert.Equal("Código de concepto", conceptos.Cell("E5").GetString());
        Assert.Equal("Proyectado", conceptos.Cell("I5").GetString());
        Assert.Equal("-CONCEPTO", conceptos.Cell("E6").GetString());
        Assert.Equal("=Salario proporcional", conceptos.Cell("F6").GetString());
        Assert.False(conceptos.Cell("E6").HasFormula);
        Assert.False(conceptos.Cell("F6").HasFormula);
        Assert.Equal(
            consulta.Filas[0].Conceptos[0].MontoBase,
            conceptos.Cell("G6").GetValue<decimal>());
        Assert.Equal(
            consulta.Filas.Sum(fila => fila.Conceptos.Count),
            conceptos.RangeUsed()!.RowCount() - 5);
    }

    [Theory]
    [InlineData(DimensionesResultadoForecast.Colaborador, "colaborador")]
    [InlineData(DimensionesResultadoForecast.Departamento, "departamento")]
    [InlineData(DimensionesResultadoForecast.Proyecto, "proyecto")]
    [InlineData(DimensionesResultadoForecast.Periodo, "periodo")]
    public void Generadores_ConservanLaDimensionAplicadaEnElNombre(
        string dimension,
        string dimensionArchivo)
    {
        var consulta = CrearConsulta(1, dimension);

        var pdf = new GeneradorForecastPdf().Generar(consulta);
        var excel = new GeneradorForecastExcel().Generar(consulta);

        Assert.Equal($"forecast-42-{dimensionArchivo}-todos.pdf", pdf.NombreOriginal);
        Assert.Equal($"forecast-42-{dimensionArchivo}-todos.xlsx", excel.NombreOriginal);
    }

    [Fact]
    public void Generadores_RechazanConsultaNulaNoCalculadaInvalidaOVacia()
    {
        var pdf = new GeneradorForecastPdf();
        var excel = new GeneradorForecastExcel();

        Assert.Throws<ArgumentNullException>(() => pdf.Generar(null!));
        Assert.Throws<ArgumentNullException>(() => excel.Generar(null!));

        var noCalculada = CrearConsulta(
            1,
            DimensionesResultadoForecast.Periodo,
            noCalculada: true);
        Assert.Throws<ArgumentException>(() => pdf.Generar(noCalculada));

        var dimensionInvalida = CrearConsulta(1, "Desconocida");
        Assert.Throws<ArgumentException>(() => excel.Generar(dimensionInvalida));

        var vacia = CrearConsulta(0, DimensionesResultadoForecast.Periodo);
        Assert.Throws<ArgumentException>(() => pdf.Generar(vacia));
        Assert.Throws<ArgumentException>(() => excel.Generar(vacia));
    }

    [Fact]
    public void Generadores_RechazanPeriodoAjenoYFilaSinConceptos()
    {
        var periodoAjeno = CrearConsulta(
            1,
            DimensionesResultadoForecast.Periodo,
            idPeriodoSeleccionado: 999);
        var sinConceptos = CrearConsulta(
            1,
            DimensionesResultadoForecast.Periodo,
            sinConceptos: true);

        Assert.Throws<ArgumentException>(() => new GeneradorForecastPdf().Generar(periodoAjeno));
        Assert.Throws<ArgumentException>(() => new GeneradorForecastExcel().Generar(periodoAjeno));
        Assert.Throws<ArgumentException>(() => new GeneradorForecastPdf().Generar(sinConceptos));
        Assert.Throws<ArgumentException>(() => new GeneradorForecastExcel().Generar(sinConceptos));
    }

    private static ConsultaResultadosForecast CrearConsulta(
        int cantidadFilas,
        string dimension,
        long? idPeriodoSeleccionado = null,
        bool contenidoPeligroso = false,
        bool noCalculada = false,
        bool sinConceptos = false)
    {
        var filas = Enumerable.Range(1, cantidadFilas)
            .Select(indice => new FilaResultadoForecast
            {
                Clave = $"{dimension}:{indice}",
                Codigo = contenidoPeligroso && indice == 1 ? "=2+2" : $"COD-{indice:000}",
                Etiqueta = contenidoPeligroso && indice == 1
                    ? "+José Núñez"
                    : $"Agrupación española número {indice:000}",
                Detalle = contenidoPeligroso && indice == 1
                    ? "@Recursos Humanos"
                    : $"Departamento y puesto de prueba {indice:000}",
                TipoParticipante = indice % 2 == 0 ? "ContratacionPrevista" : "Colaborador",
                CantidadParticipantes = indice,
                SalarioBrutoProyectado = 550_000m + indice,
                DeduccionesProyectadas = 50_000m + indice,
                SalarioNetoProyectado = 500_000m,
                Conceptos = sinConceptos && indice == 1
                    ? []
                    : CrearConceptos(indice, contenidoPeligroso && indice == 1)
            })
            .ToList();

        return new ConsultaResultadosForecast
        {
            IdForecastEscenario = 42,
            NombreEscenario = "Proyección nómina región Caribe",
            EstadoEscenario = "Calculado",
            FechaCalculo = noCalculada ? null : new DateTime(2026, 8, 2, 14, 30, 0),
            Dimension = dimension,
            IdForecastPeriodo = idPeriodoSeleccionado,
            MontoProyectadoTotalEscenario = filas.Sum(fila => fila.SalarioBrutoProyectado),
            MontoProyectadoTotalConsulta = filas.Sum(fila => fila.SalarioBrutoProyectado),
            DeduccionesTotalConsulta = filas.Sum(fila => fila.DeduccionesProyectadas),
            SalarioNetoTotalConsulta = filas.Sum(fila => fila.SalarioNetoProyectado),
            EsConsistenteConEscenario = true,
            Periodos =
            [
                new PeriodoResultadoForecastOpcion(
                    101,
                    1,
                    "Quincenal",
                    new DateTime(2026, 9, 1),
                    new DateTime(2026, 9, 15)),
                new PeriodoResultadoForecastOpcion(
                    102,
                    2,
                    "Quincenal",
                    new DateTime(2026, 9, 16),
                    new DateTime(2026, 9, 30))
            ],
            Filas = filas
        };
    }

    private static List<ConceptoResultadoForecast> CrearConceptos(
        int indice,
        bool contenidoPeligroso) =>
    [
        new(
            contenidoPeligroso ? "-CONCEPTO" : "SALARIO_PROPORCIONAL",
            contenidoPeligroso ? "=Salario proporcional" : "Salario proporcional",
            500_000m + indice,
            0m,
            500_000m + indice),
        new("HORAS_EXTRA", "Horas extra", 10_000m, 2_000m, 12_000m),
        new("BONOS", "Bonos", 15_000m, 3_000m, 18_000m),
        new("BENEFICIOS_CONFIGURABLES", "Beneficios configurables", 20_000m, 0m, 20_000m),
        new("AUSENCIAS", "Ausencias", 0m, 0m, 0m),
        new("SALARIO_BRUTO", "Salario bruto", 545_000m + indice, 5_000m, 550_000m + indice),
        new("DEDUCCIONES", "Deducciones", 50_000m + indice, 0m, 50_000m + indice),
        new("SALARIO_NETO", "Salario neto", 495_000m, 5_000m, 500_000m)
    ];
}

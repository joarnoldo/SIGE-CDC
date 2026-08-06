using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Infrastructure.Reportes;

public sealed class GeneradorForecastPdf : IGeneradorForecastPdf
{
    private const string MimePdf = "application/pdf";
    private const double AltoLinea = 10;
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    private static readonly ColumnaPdf[] ColumnasResultado =
    [
        new("Agrupación", 140, false),
        new("Código", 68, false),
        new("Detalle", 125, false),
        new("Tipo", 78, false),
        new("Participantes", 59, true),
        new("Salario bruto", 88, true),
        new("Deducciones", 84, true),
        new("Salario neto", 88, true)
    ];

    public ArchivoDescarga Generar(ConsultaResultadosForecast consulta)
    {
        UtilidadesExportacionForecast.Validar(consulta);
        ConfiguracionFuentesPdf.AsegurarInicializacion();

        using var documento = new PdfDocument();
        documento.Info.Title = $"Resultados forecast {consulta.IdForecastEscenario}";
        documento.Info.Subject = "Resultados proyectados del forecast";
        documento.Info.Author = "Canales y Drenajes del Caribe S.R.L.";

        var fuentes = new FuentesPdf();
        var pagina = CrearPagina(documento, consulta, fuentes);

        foreach (var fila in consulta.Filas)
        {
            var altoPrincipal = CalcularAltoFilaPrincipal(pagina.Graficos, fuentes, fila);
            var altoBloque = altoPrincipal + 18 + (fila.Conceptos.Count * 16) + 7;
            if (pagina.Y + altoBloque > pagina.LimiteInferior)
            {
                pagina.Dispose();
                pagina = CrearPagina(documento, consulta, fuentes);
            }

            DibujarFilaPrincipal(pagina, fuentes, fila, altoPrincipal);
            DibujarEncabezadoConceptos(pagina, fuentes);

            foreach (var concepto in fila.Conceptos)
            {
                if (pagina.Y + 17 > pagina.LimiteInferior)
                {
                    pagina.Dispose();
                    pagina = CrearPagina(documento, consulta, fuentes);
                    DibujarContextoContinuacion(pagina, fuentes, fila);
                    DibujarEncabezadoConceptos(pagina, fuentes);
                }

                DibujarConcepto(pagina, fuentes, concepto);
            }

            pagina.Y += 7;
        }

        pagina.Dispose();
        DibujarPaginacion(documento, fuentes);

        var memoria = new MemoryStream();
        documento.Save(memoria, closeStream: false);
        memoria.Position = 0;

        return new ArchivoDescarga(
            memoria,
            UtilidadesExportacionForecast.CrearNombreArchivo(consulta, "pdf"),
            MimePdf);
    }

    private static PaginaExportacion CrearPagina(
        PdfDocument documento,
        ConsultaResultadosForecast consulta,
        FuentesPdf fuentes)
    {
        var paginaPdf = documento.AddPage();
        paginaPdf.Size = PdfSharp.PageSize.A4;
        paginaPdf.Orientation = PdfSharp.PageOrientation.Landscape;

        var graficos = XGraphics.FromPdfPage(paginaPdf);
        var margen = XUnit.FromMillimeter(12).Point;
        var ancho = paginaPdf.Width.Point - (margen * 2);
        var y = margen;

        graficos.DrawString(
            "Canales y Drenajes del Caribe S.R.L.",
            fuentes.Subtitulo,
            XBrushes.DarkSlateGray,
            new XRect(margen, y, ancho, 14),
            XStringFormats.TopLeft);
        y += 17;

        graficos.DrawString(
            "Resultados del forecast",
            fuentes.Titulo,
            XBrushes.Black,
            new XRect(margen, y, ancho, 22),
            XStringFormats.TopLeft);
        y += 25;

        graficos.DrawLine(
            new XPen(XColor.FromArgb(28, 96, 78), 1.4),
            margen,
            y,
            margen + ancho,
            y);
        y += 9;

        var nombre = DividirLineas(graficos, fuentes.Normal, consulta.NombreEscenario, ancho - 95);
        graficos.DrawString(
            "Escenario:",
            fuentes.Negrita,
            XBrushes.Black,
            new XRect(margen, y, 90, 12),
            XStringFormats.TopLeft);
        DibujarLineas(graficos, fuentes.Normal, XBrushes.Black, nombre, margen + 90, y, ancho - 90);
        y += Math.Max(1, nombre.Count) * AltoLinea + 4;

        DibujarMetadato(graficos, fuentes, margen, y, "Estado", consulta.EstadoEscenario, 180);
        DibujarMetadato(
            graficos,
            fuentes,
            margen + 195,
            y,
            "Calculado",
            consulta.FechaCalculo!.Value.ToString("dd/MM/yyyy HH:mm", Cultura),
            220);
        DibujarMetadato(
            graficos,
            fuentes,
            margen + 430,
            y,
            "Dimensión",
            UtilidadesExportacionForecast.NombreDimension(consulta.Dimension),
            190);
        y += 17;

        var periodo = UtilidadesExportacionForecast.ObtenerPeriodoAplicado(consulta);
        var alcance = periodo is null
            ? "Todos los períodos"
            : $"{periodo.NumeroOrden}. {periodo.TipoPeriodo} - {periodo.FechaInicio:dd/MM/yyyy} al {periodo.FechaFin:dd/MM/yyyy}";
        DibujarMetadato(graficos, fuentes, margen, y, "Alcance", alcance, ancho);
        y += 22;

        var anchoResumen = (ancho - 20) / 3;
        DibujarTotal(graficos, fuentes, margen, y, anchoResumen, "Salario bruto", consulta.MontoProyectadoTotalConsulta);
        DibujarTotal(graficos, fuentes, margen + anchoResumen + 10, y, anchoResumen, "Deducciones", consulta.DeduccionesTotalConsulta);
        DibujarTotal(graficos, fuentes, margen + ((anchoResumen + 10) * 2), y, anchoResumen, "Salario neto", consulta.SalarioNetoTotalConsulta);
        y += 38;

        if (consulta.EsConsistenteConEscenario == false)
        {
            var aviso = "Advertencia: la suma visible no coincide con el total persistido del escenario.";
            graficos.DrawRectangle(
                new XSolidBrush(XColor.FromArgb(255, 247, 224)),
                margen,
                y,
                ancho,
                20);
            graficos.DrawString(
                aviso,
                fuentes.Negrita,
                XBrushes.DarkGoldenrod,
                new XRect(margen + 7, y + 5, ancho - 14, 12),
                XStringFormats.TopLeft);
            y += 27;
        }

        DibujarEncabezadoResultados(graficos, fuentes, margen, y);
        y += 24;

        return new PaginaExportacion(
            graficos,
            margen,
            ancho,
            y,
            paginaPdf.Height.Point - margen - 19);
    }

    private static void DibujarMetadato(
        XGraphics graficos,
        FuentesPdf fuentes,
        double x,
        double y,
        string etiqueta,
        string valor,
        double ancho)
    {
        const double anchoEtiqueta = 63;
        graficos.DrawString(
            $"{etiqueta}:",
            fuentes.Negrita,
            XBrushes.Black,
            new XRect(x, y, anchoEtiqueta, 12),
            XStringFormats.TopLeft);
        graficos.DrawString(
            valor,
            fuentes.Normal,
            XBrushes.Black,
            new XRect(x + anchoEtiqueta, y, ancho - anchoEtiqueta, 12),
            XStringFormats.TopLeft);
    }

    private static void DibujarTotal(
        XGraphics graficos,
        FuentesPdf fuentes,
        double x,
        double y,
        double ancho,
        string etiqueta,
        decimal monto)
    {
        var rectangulo = new XRect(x, y, ancho, 29);
        graficos.DrawRoundedRectangle(
            new XPen(XColor.FromArgb(176, 207, 197), 0.7),
            new XSolidBrush(XColor.FromArgb(235, 246, 242)),
            rectangulo,
            new XSize(4, 4));
        graficos.DrawString(
            etiqueta,
            fuentes.Normal,
            XBrushes.DarkSlateGray,
            new XRect(x + 8, y + 5, ancho / 2, 15),
            XStringFormats.TopLeft);
        graficos.DrawString(
            FormatearMoneda(monto),
            fuentes.Total,
            XBrushes.Black,
            new XRect(x + (ancho / 2), y + 7, (ancho / 2) - 8, 15),
            XStringFormats.TopRight);
    }

    private static void DibujarEncabezadoResultados(
        XGraphics graficos,
        FuentesPdf fuentes,
        double x,
        double y)
    {
        var posicion = x;
        var fondo = new XSolidBrush(XColor.FromArgb(28, 96, 78));
        foreach (var columna in ColumnasResultado)
        {
            graficos.DrawRectangle(fondo, posicion, y, columna.Ancho, 23);
            graficos.DrawString(
                columna.Encabezado,
                fuentes.Encabezado,
                XBrushes.White,
                new XRect(posicion + 3, y + 6, columna.Ancho - 6, 12),
                columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            posicion += columna.Ancho;
        }
    }

    private static double CalcularAltoFilaPrincipal(
        XGraphics graficos,
        FuentesPdf fuentes,
        FilaResultadoForecast fila)
    {
        var textos = TextosFilaPrincipal(fila);
        var lineas = ColumnasResultado
            .Take(4)
            .Select((columna, indice) =>
                DividirLineas(graficos, fuentes.Fila, textos[indice], columna.Ancho - 6).Count)
            .DefaultIfEmpty(1)
            .Max();
        return Math.Max(21, (lineas * AltoLinea) + 8);
    }

    private static void DibujarFilaPrincipal(
        PaginaExportacion pagina,
        FuentesPdf fuentes,
        FilaResultadoForecast fila,
        double alto)
    {
        var textos = TextosFilaPrincipal(fila);
        var valores = textos.Concat(
        [
            fila.CantidadParticipantes.ToString(Cultura),
            FormatearMoneda(fila.SalarioBrutoProyectado),
            FormatearMoneda(fila.DeduccionesProyectadas),
            FormatearMoneda(fila.SalarioNetoProyectado)
        ]).ToArray();

        var posicion = pagina.Margen;
        var fondo = pagina.IndiceFila % 2 == 0
            ? XBrushes.White
            : new XSolidBrush(XColor.FromArgb(247, 249, 248));

        for (var indice = 0; indice < ColumnasResultado.Length; indice++)
        {
            var columna = ColumnasResultado[indice];
            pagina.Graficos.DrawRectangle(fondo, posicion, pagina.Y, columna.Ancho, alto);
            pagina.Graficos.DrawLine(
                new XPen(XColor.FromArgb(221, 226, 224), 0.45),
                posicion,
                pagina.Y + alto,
                posicion + columna.Ancho,
                pagina.Y + alto);

            var lineas = DividirLineas(
                pagina.Graficos,
                fuentes.Fila,
                valores[indice],
                columna.Ancho - 6);
            DibujarLineas(
                pagina.Graficos,
                fuentes.Fila,
                XBrushes.Black,
                lineas,
                posicion + 3,
                pagina.Y + 5,
                columna.Ancho - 6,
                columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            posicion += columna.Ancho;
        }

        pagina.Y += alto;
        pagina.IndiceFila++;
    }

    private static string[] TextosFilaPrincipal(FilaResultadoForecast fila) =>
    [
        fila.Etiqueta,
        Texto(fila.Codigo),
        Texto(fila.Detalle),
        TextoTipoParticipante(fila.TipoParticipante)
    ];

    private static void DibujarContextoContinuacion(
        PaginaExportacion pagina,
        FuentesPdf fuentes,
        FilaResultadoForecast fila)
    {
        pagina.Graficos.DrawString(
            $"Continuación del desglose: {fila.Etiqueta}",
            fuentes.Negrita,
            XBrushes.DarkSlateGray,
            new XRect(pagina.Margen, pagina.Y, pagina.Ancho, 13),
            XStringFormats.TopLeft);
        pagina.Y += 17;
    }

    private static void DibujarEncabezadoConceptos(PaginaExportacion pagina, FuentesPdf fuentes)
    {
        var fondo = new XSolidBrush(XColor.FromArgb(224, 238, 233));
        pagina.Graficos.DrawRectangle(fondo, pagina.Margen + 20, pagina.Y, pagina.Ancho - 20, 18);

        var columnas = ColumnasConcepto(pagina.Ancho - 20);
        var x = pagina.Margen + 20;
        foreach (var columna in columnas)
        {
            pagina.Graficos.DrawString(
                columna.Encabezado,
                fuentes.NegritaFila,
                XBrushes.DarkSlateGray,
                new XRect(x + 3, pagina.Y + 4, columna.Ancho - 6, 11),
                columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            x += columna.Ancho;
        }

        pagina.Y += 18;
    }

    private static void DibujarConcepto(
        PaginaExportacion pagina,
        FuentesPdf fuentes,
        ConceptoResultadoForecast concepto)
    {
        var columnas = ColumnasConcepto(pagina.Ancho - 20);
        var valores = new[]
        {
            $"{concepto.Codigo} - {concepto.Nombre}",
            FormatearMoneda(concepto.MontoBase),
            FormatearMoneda(concepto.MontoAjuste),
            FormatearMoneda(concepto.MontoProyectado)
        };
        var x = pagina.Margen + 20;

        for (var indice = 0; indice < columnas.Length; indice++)
        {
            var columna = columnas[indice];
            pagina.Graficos.DrawString(
                valores[indice],
                fuentes.Concepto,
                XBrushes.Black,
                new XRect(x + 3, pagina.Y + 3, columna.Ancho - 6, 11),
                columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            x += columna.Ancho;
        }

        pagina.Graficos.DrawLine(
            new XPen(XColor.FromArgb(231, 234, 233), 0.35),
            pagina.Margen + 20,
            pagina.Y + 16,
            pagina.Margen + pagina.Ancho,
            pagina.Y + 16);
        pagina.Y += 16;
    }

    private static ColumnaPdf[] ColumnasConcepto(double ancho) =>
    [
        new("Concepto", ancho - 330, false),
        new("Base", 110, true),
        new("Ajuste", 110, true),
        new("Proyectado", 110, true)
    ];

    private static IReadOnlyList<string> DividirLineas(
        XGraphics graficos,
        XFont fuente,
        string? texto,
        double ancho)
    {
        var valor = Texto(texto);
        var lineas = new List<string>();
        var actual = string.Empty;

        foreach (var palabraOriginal in valor.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var palabra = palabraOriginal;
            while (graficos.MeasureString(palabra, fuente).Width > ancho && palabra.Length > 1)
            {
                var cantidad = palabra.Length - 1;
                while (cantidad > 1
                    && graficos.MeasureString(palabra[..cantidad], fuente).Width > ancho)
                {
                    cantidad--;
                }

                if (!string.IsNullOrEmpty(actual))
                {
                    lineas.Add(actual);
                    actual = string.Empty;
                }

                lineas.Add(palabra[..cantidad]);
                palabra = palabra[cantidad..];
            }

            var candidato = string.IsNullOrEmpty(actual) ? palabra : $"{actual} {palabra}";
            if (graficos.MeasureString(candidato, fuente).Width <= ancho)
            {
                actual = candidato;
            }
            else
            {
                if (!string.IsNullOrEmpty(actual))
                {
                    lineas.Add(actual);
                }
                actual = palabra;
            }
        }

        if (!string.IsNullOrEmpty(actual))
        {
            lineas.Add(actual);
        }

        return lineas.Count == 0 ? ["—"] : lineas;
    }

    private static void DibujarLineas(
        XGraphics graficos,
        XFont fuente,
        XBrush pincel,
        IReadOnlyList<string> lineas,
        double x,
        double y,
        double ancho,
        XStringFormat? formato = null)
    {
        formato ??= XStringFormats.TopLeft;
        for (var indice = 0; indice < lineas.Count; indice++)
        {
            graficos.DrawString(
                lineas[indice],
                fuente,
                pincel,
                new XRect(x, y + (indice * AltoLinea), ancho, AltoLinea),
                formato);
        }
    }

    private static void DibujarPaginacion(PdfDocument documento, FuentesPdf fuentes)
    {
        for (var indice = 0; indice < documento.PageCount; indice++)
        {
            var pagina = documento.Pages[indice];
            using var graficos = XGraphics.FromPdfPage(pagina, XGraphicsPdfPageOptions.Append);
            graficos.DrawString(
                $"Página {indice + 1} de {documento.PageCount}",
                fuentes.Pie,
                XBrushes.Gray,
                new XRect(0, pagina.Height.Point - 24, pagina.Width.Point - 34, 12),
                XStringFormats.TopRight);
        }
    }

    private static string Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim();

    private static string TextoTipoParticipante(string? tipo) => tipo switch
    {
        "ContratacionPrevista" => "Contratación prevista",
        "Colaborador" => "Colaborador",
        _ => Texto(tipo)
    };

    private static string FormatearMoneda(decimal monto) => monto.ToString("C2", Cultura);

    private sealed record ColumnaPdf(string Encabezado, double Ancho, bool EsNumero);

    private sealed class FuentesPdf
    {
        public XFont Titulo { get; } = new(FuenteNotoSansResolver.NombreFamilia, 15, XFontStyleEx.Bold);
        public XFont Subtitulo { get; } = new(FuenteNotoSansResolver.NombreFamilia, 9, XFontStyleEx.Bold);
        public XFont Normal { get; } = new(FuenteNotoSansResolver.NombreFamilia, 7.5, XFontStyleEx.Regular);
        public XFont Negrita { get; } = new(FuenteNotoSansResolver.NombreFamilia, 7.5, XFontStyleEx.Bold);
        public XFont Total { get; } = new(FuenteNotoSansResolver.NombreFamilia, 9, XFontStyleEx.Bold);
        public XFont Encabezado { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6.4, XFontStyleEx.Bold);
        public XFont Fila { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6.2, XFontStyleEx.Regular);
        public XFont NegritaFila { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6.2, XFontStyleEx.Bold);
        public XFont Concepto { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6, XFontStyleEx.Regular);
        public XFont Pie { get; } = new(FuenteNotoSansResolver.NombreFamilia, 7, XFontStyleEx.Regular);
    }

    private sealed class PaginaExportacion(
        XGraphics graficos,
        double margen,
        double ancho,
        double y,
        double limiteInferior) : IDisposable
    {
        public XGraphics Graficos { get; } = graficos;
        public double Margen { get; } = margen;
        public double Ancho { get; } = ancho;
        public double Y { get; set; } = y;
        public double LimiteInferior { get; } = limiteInferior;
        public int IndiceFila { get; set; }

        public void Dispose() => Graficos.Dispose();
    }
}

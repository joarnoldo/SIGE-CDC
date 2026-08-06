using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Infrastructure.Reportes;

public sealed class GeneradorReportePlanillaPdf : IGeneradorReportePlanillaPdf
{
    private const string MimePdf = "application/pdf";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    private static readonly ColumnaPdf[] Columnas =
    [
        new("Código", 52, false),
        new("Colaborador", 116, false),
        new("Departamento", 76, false),
        new("Salario\nperíodo", 62, true),
        new("Horas\nextra", 55, true),
        new("Bonos", 50, true),
        new("Beneficios", 58, true),
        new("Ausencias", 55, true),
        new("Bruto", 60, true),
        new("Deducciones", 62, true),
        new("Neto", 62, true)
    ];

    public ArchivoDescarga Generar(ReportePlanilla reporte)
    {
        UtilidadesReportePlanilla.Validar(reporte);
        ConfiguracionFuentesPdf.AsegurarInicializacion();

        using var documento = new PdfDocument();
        documento.Info.Title = $"Reporte de planilla {reporte.CodigoPeriodo}";
        documento.Info.Subject = "Reporte consolidado de planilla";
        documento.Info.Author = "Canales y Drenajes del Caribe S.R.L.";

        var fuentes = new FuentesPdf();
        PaginaReporte pagina = CrearPagina(documento, reporte, fuentes);

        foreach (var detalle in reporte.Detalles)
        {
            if (pagina.Y + 21 > pagina.LimiteInferior)
            {
                pagina.Dispose();
                pagina = CrearPagina(documento, reporte, fuentes);
            }

            DibujarFila(pagina, fuentes, detalle);
        }

        if (pagina.Y + 25 > pagina.LimiteInferior)
        {
            pagina.Dispose();
            pagina = CrearPagina(documento, reporte, fuentes);
        }

        DibujarTotales(pagina, fuentes, reporte);
        pagina.Dispose();
        DibujarPaginacion(documento, fuentes);

        var memoria = new MemoryStream();
        documento.Save(memoria, closeStream: false);
        memoria.Position = 0;

        return new ArchivoDescarga(
            memoria,
            $"reporte-planilla-{UtilidadesReportePlanilla.SanearCodigoPeriodo(reporte.CodigoPeriodo)}.pdf",
            MimePdf);
    }

    private static PaginaReporte CrearPagina(
        PdfDocument documento,
        ReportePlanilla reporte,
        FuentesPdf fuentes)
    {
        var pagina = documento.AddPage();
        pagina.Size = PdfSharp.PageSize.A4;
        pagina.Orientation = PdfSharp.PageOrientation.Landscape;

        var graficos = XGraphics.FromPdfPage(pagina);
        var margen = XUnit.FromMillimeter(12).Point;
        var anchoContenido = pagina.Width.Point - (margen * 2);
        var y = margen;

        graficos.DrawString(
            "Canales y Drenajes del Caribe S.R.L.",
            fuentes.Subtitulo,
            XBrushes.DarkSlateGray,
            new XRect(margen, y, anchoContenido, 16),
            XStringFormats.TopLeft);
        y += 19;

        graficos.DrawString(
            "Reporte consolidado de planilla",
            fuentes.Titulo,
            XBrushes.Black,
            new XRect(margen, y, anchoContenido, 23),
            XStringFormats.TopLeft);
        y += 27;

        graficos.DrawLine(
            new XPen(XColor.FromArgb(28, 96, 78), 1.4),
            margen,
            y,
            margen + anchoContenido,
            y);
        y += 11;

        DibujarMetadato(graficos, fuentes, margen, y, "Código", reporte.CodigoPeriodo, 205);
        DibujarMetadato(graficos, fuentes, margen + 220, y, "Período", reporte.NombrePeriodo, 260);
        DibujarMetadato(graficos, fuentes, margen + 500, y, "Estado", reporte.EstadoPlanilla, 190);
        y += 17;

        DibujarMetadato(
            graficos,
            fuentes,
            margen,
            y,
            "Rango",
            $"{FormatearFecha(reporte.FechaInicio)} al {FormatearFecha(reporte.FechaFin)}",
            260);
        DibujarMetadato(graficos, fuentes, margen + 280, y, "Tipo", reporte.TipoPeriodo, 180);
        DibujarMetadato(
            graficos,
            fuentes,
            margen + 480,
            y,
            "Calculada",
            FormatearFechaHora(reporte.FechaCalculo),
            230);
        y += 24;

        var anchoResumen = (anchoContenido - 20) / 3;
        DibujarTotalResumen(graficos, fuentes, margen, y, anchoResumen, "Salario bruto", reporte.SalarioBrutoTotal);
        DibujarTotalResumen(graficos, fuentes, margen + anchoResumen + 10, y, anchoResumen, "Deducciones", reporte.DeduccionesTotal);
        DibujarTotalResumen(graficos, fuentes, margen + (anchoResumen + 10) * 2, y, anchoResumen, "Salario neto", reporte.SalarioNetoTotal);
        y += 39;

        DibujarEncabezadoTabla(graficos, fuentes, margen, y);
        y += 27;

        return new PaginaReporte(
            graficos,
            margen,
            y,
            pagina.Height.Point - margen - 20);
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
        const double anchoEtiqueta = 58;
        graficos.DrawString(
            $"{etiqueta}:",
            fuentes.Negrita,
            XBrushes.Black,
            new XRect(x, y, anchoEtiqueta, 14),
            XStringFormats.TopLeft);
        graficos.DrawString(
            AjustarTexto(graficos, fuentes.Normal, valor, ancho - anchoEtiqueta),
            fuentes.Normal,
            XBrushes.Black,
            new XRect(x + anchoEtiqueta, y, ancho - anchoEtiqueta, 14),
            XStringFormats.TopLeft);
    }

    private static void DibujarTotalResumen(
        XGraphics graficos,
        FuentesPdf fuentes,
        double x,
        double y,
        double ancho,
        string etiqueta,
        decimal monto)
    {
        var rectangulo = new XRect(x, y, ancho, 30);
        graficos.DrawRoundedRectangle(
            new XPen(XColor.FromArgb(176, 207, 197), 0.7),
            new XSolidBrush(XColor.FromArgb(235, 246, 242)),
            rectangulo,
            new XSize(4, 4));
        graficos.DrawString(
            etiqueta,
            fuentes.Normal,
            XBrushes.DarkSlateGray,
            new XRect(x + 8, y + 5, ancho / 2, 16),
            XStringFormats.TopLeft);
        graficos.DrawString(
            FormatearMoneda(monto),
            fuentes.Total,
            XBrushes.Black,
            new XRect(x + ancho / 2, y + 7, ancho / 2 - 8, 16),
            XStringFormats.TopRight);
    }

    private static void DibujarEncabezadoTabla(
        XGraphics graficos,
        FuentesPdf fuentes,
        double x,
        double y)
    {
        var posicion = x;
        var fondo = new XSolidBrush(XColor.FromArgb(28, 96, 78));

        foreach (var columna in Columnas)
        {
            graficos.DrawRectangle(fondo, posicion, y, columna.Ancho, 26);
            var partes = columna.Encabezado.Split('\n');
            if (partes.Length == 1)
            {
                graficos.DrawString(
                    partes[0],
                    fuentes.EncabezadoTabla,
                    XBrushes.White,
                    new XRect(posicion + 3, y + 7, columna.Ancho - 6, 12),
                    columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            }
            else
            {
                graficos.DrawString(
                    partes[0],
                    fuentes.EncabezadoTabla,
                    XBrushes.White,
                    new XRect(posicion + 3, y + 3, columna.Ancho - 6, 10),
                    columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
                graficos.DrawString(
                    partes[1],
                    fuentes.EncabezadoTabla,
                    XBrushes.White,
                    new XRect(posicion + 3, y + 13, columna.Ancho - 6, 10),
                    columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            }

            posicion += columna.Ancho;
        }
    }

    private static void DibujarFila(
        PaginaReporte pagina,
        FuentesPdf fuentes,
        DetalleReportePlanilla detalle)
    {
        const double alto = 20;
        var valores = new[]
        {
            detalle.CodigoColaborador,
            detalle.NombreColaborador,
            detalle.Departamento,
            FormatearMoneda(detalle.SalarioProporcional),
            FormatearMoneda(detalle.TotalHorasExtra),
            FormatearMoneda(detalle.TotalBonos),
            FormatearMoneda(detalle.TotalBeneficiosConfigurables),
            FormatearMoneda(detalle.TotalAusencias),
            FormatearMoneda(detalle.SalarioBruto),
            FormatearMoneda(detalle.TotalDeducciones),
            FormatearMoneda(detalle.SalarioNeto)
        };

        var posicion = pagina.Margen;
        var fondo = pagina.IndiceFila % 2 == 0
            ? XBrushes.White
            : new XSolidBrush(XColor.FromArgb(247, 249, 248));

        for (var indice = 0; indice < Columnas.Length; indice++)
        {
            var columna = Columnas[indice];
            pagina.Graficos.DrawRectangle(fondo, posicion, pagina.Y, columna.Ancho, alto);
            pagina.Graficos.DrawLine(
                new XPen(XColor.FromArgb(221, 226, 224), 0.45),
                posicion,
                pagina.Y + alto,
                posicion + columna.Ancho,
                pagina.Y + alto);
            pagina.Graficos.DrawString(
                AjustarTexto(pagina.Graficos, fuentes.Fila, valores[indice], columna.Ancho - 6),
                fuentes.Fila,
                XBrushes.Black,
                new XRect(posicion + 3, pagina.Y + 5, columna.Ancho - 6, 11),
                columna.EsNumero ? XStringFormats.TopRight : XStringFormats.TopLeft);
            posicion += columna.Ancho;
        }

        pagina.Y += alto;
        pagina.IndiceFila++;
    }

    private static void DibujarTotales(
        PaginaReporte pagina,
        FuentesPdf fuentes,
        ReportePlanilla reporte)
    {
        const double alto = 24;
        var posicion = pagina.Margen;
        var anchoEtiqueta = Columnas.Take(8).Sum(columna => columna.Ancho);

        pagina.Graficos.DrawRectangle(
            new XSolidBrush(XColor.FromArgb(224, 238, 233)),
            posicion,
            pagina.Y,
            anchoEtiqueta,
            alto);
        pagina.Graficos.DrawString(
            "Totales oficiales",
            fuentes.NegritaFila,
            XBrushes.Black,
            new XRect(posicion + 3, pagina.Y + 6, anchoEtiqueta - 6, 12),
            XStringFormats.TopLeft);

        posicion += anchoEtiqueta;

        var valores = new[]
        {
            reporte.SalarioBrutoTotal,
            reporte.DeduccionesTotal,
            reporte.SalarioNetoTotal
        };

        for (var indice = 8; indice < Columnas.Length; indice++)
        {
            var columna = Columnas[indice];
            pagina.Graficos.DrawRectangle(
                new XSolidBrush(XColor.FromArgb(224, 238, 233)),
                posicion,
                pagina.Y,
                columna.Ancho,
                alto);

            var valor = FormatearMoneda(valores[indice - 8]);
            pagina.Graficos.DrawString(
                AjustarTexto(pagina.Graficos, fuentes.NegritaFila, valor, columna.Ancho - 6),
                fuentes.NegritaFila,
                XBrushes.Black,
                new XRect(posicion + 3, pagina.Y + 6, columna.Ancho - 6, 12),
                XStringFormats.TopRight);

            posicion += columna.Ancho;
        }

        pagina.Y += alto;
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

    private static string AjustarTexto(
        XGraphics graficos,
        XFont fuente,
        string? texto,
        double ancho)
    {
        var valor = string.IsNullOrWhiteSpace(texto) ? "—" : texto.Trim();
        if (graficos.MeasureString(valor, fuente).Width <= ancho)
        {
            return valor;
        }

        const string sufijo = "...";
        while (valor.Length > 1
            && graficos.MeasureString(valor + sufijo, fuente).Width > ancho)
        {
            valor = valor[..^1];
        }

        return valor + sufijo;
    }

    private static string FormatearFecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy", Cultura);

    private static string FormatearFechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    private static string FormatearMoneda(decimal monto) => monto.ToString("C2", Cultura);

    private sealed record ColumnaPdf(string Encabezado, double Ancho, bool EsNumero);

    private sealed class FuentesPdf
    {
        public XFont Titulo { get; } = new(FuenteNotoSansResolver.NombreFamilia, 15, XFontStyleEx.Bold);
        public XFont Subtitulo { get; } = new(FuenteNotoSansResolver.NombreFamilia, 9, XFontStyleEx.Bold);
        public XFont Normal { get; } = new(FuenteNotoSansResolver.NombreFamilia, 7.5, XFontStyleEx.Regular);
        public XFont Negrita { get; } = new(FuenteNotoSansResolver.NombreFamilia, 7.5, XFontStyleEx.Bold);
        public XFont Total { get; } = new(FuenteNotoSansResolver.NombreFamilia, 9, XFontStyleEx.Bold);
        public XFont EncabezadoTabla { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6.5, XFontStyleEx.Bold);
        public XFont Fila { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6.3, XFontStyleEx.Regular);
        public XFont NegritaFila { get; } = new(FuenteNotoSansResolver.NombreFamilia, 6.3, XFontStyleEx.Bold);
        public XFont Pie { get; } = new(FuenteNotoSansResolver.NombreFamilia, 7, XFontStyleEx.Regular);
    }

    private sealed class PaginaReporte(
        XGraphics graficos,
        double margen,
        double y,
        double limiteInferior) : IDisposable
    {
        public XGraphics Graficos { get; } = graficos;
        public double Margen { get; } = margen;
        public double Y { get; set; } = y;
        public double LimiteInferior { get; } = limiteInferior;
        public int IndiceFila { get; set; }

        public void Dispose() => Graficos.Dispose();
    }
}

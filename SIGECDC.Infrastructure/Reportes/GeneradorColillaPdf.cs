using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Infrastructure.Reportes;

public sealed class GeneradorColillaPdf : IGeneradorColillaPdf
{
    private const string MimePdf = "application/pdf";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    public ArchivoDescarga Generar(ColillaPagoDetalle colilla)
    {
        ArgumentNullException.ThrowIfNull(colilla);

        if (string.IsNullOrWhiteSpace(colilla.CodigoColilla))
        {
            throw new ArgumentException("La colilla no tiene un código válido.");
        }

        ConfiguracionFuentesPdf.AsegurarInicializacion();

        using var documento = new PdfDocument();
        documento.Info.Title = $"Colilla de pago {colilla.CodigoColilla}";
        documento.Info.Subject = "Comprobante de planilla";
        documento.Info.Author = "Canales y Drenajes del Caribe S.R.L.";

        var pagina = documento.AddPage();
        pagina.Size = PdfSharp.PageSize.A4;

        using var graficos = XGraphics.FromPdfPage(pagina);
        var margen = XUnit.FromMillimeter(18);
        var anchoContenido = pagina.Width - (margen * 2);
        var y = margen.Point;

        var fuenteTitulo = new XFont(
            FuenteNotoSansResolver.NombreFamilia,
            17,
            XFontStyleEx.Bold);
        var fuenteSubtitulo = new XFont(
            FuenteNotoSansResolver.NombreFamilia,
            11,
            XFontStyleEx.Bold);
        var fuenteNormal = new XFont(
            FuenteNotoSansResolver.NombreFamilia,
            9,
            XFontStyleEx.Regular);
        var fuenteNegrita = new XFont(
            FuenteNotoSansResolver.NombreFamilia,
            9,
            XFontStyleEx.Bold);
        var fuenteTotal = new XFont(
            FuenteNotoSansResolver.NombreFamilia,
            11,
            XFontStyleEx.Bold);

        graficos.DrawString(
            "Canales y Drenajes del Caribe S.R.L.",
            fuenteSubtitulo,
            XBrushes.DarkSlateGray,
            new XRect(margen.Point, y, anchoContenido.Point, 18),
            XStringFormats.TopLeft);
        y += 23;

        graficos.DrawString(
            "Colilla de pago",
            fuenteTitulo,
            XBrushes.Black,
            new XRect(margen.Point, y, anchoContenido.Point, 26),
            XStringFormats.TopLeft);
        y += 30;

        graficos.DrawLine(
            new XPen(XColor.FromArgb(28, 96, 78), 1.4),
            margen.Point,
            y,
            (margen + anchoContenido).Point,
            y);
        y += 16;

        DibujarCampo(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Código", colilla.CodigoColilla, anchoContenido.Point);
        y += 18;
        DibujarCampo(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Generada", FormatearFechaHora(colilla.FechaGeneracion), anchoContenido.Point);
        y += 18;
        DibujarCampo(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Período", colilla.CodigoPeriodo, anchoContenido.Point);
        y += 18;
        DibujarCampo(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Nombre", colilla.NombrePeriodo, anchoContenido.Point);
        y += 18;
        DibujarPar(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Fechas", $"{FormatearFecha(colilla.FechaInicio)} al {FormatearFecha(colilla.FechaFin)}",
            "Tipo", colilla.TipoPeriodo,
            anchoContenido.Point);
        y += 18;
        DibujarCampo(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Estado", colilla.EstadoPlanilla, anchoContenido.Point);
        y += 30;

        DibujarTituloSeccion(graficos, fuenteSubtitulo, margen.Point, y, "Colaborador");
        y += 22;
        DibujarPar(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Nombre", colilla.NombreColaborador,
            "Código", colilla.CodigoColaborador,
            anchoContenido.Point);
        y += 18;
        DibujarPar(graficos, fuenteNegrita, fuenteNormal, margen.Point, y,
            "Departamento", colilla.Departamento,
            "Puesto", colilla.Puesto,
            anchoContenido.Point);
        y += 34;

        DibujarTituloSeccion(graficos, fuenteSubtitulo, margen.Point, y, "Detalle del pago");
        y += 25;

        y = DibujarMonto(graficos, fuenteNormal, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Salario base", colilla.SalarioBase);
        y = DibujarMonto(graficos, fuenteNormal, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Salario proporcional", colilla.SalarioProporcional);
        y = DibujarMonto(graficos, fuenteNormal, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Horas extra", colilla.TotalHorasExtra);
        y = DibujarMonto(graficos, fuenteNormal, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Bonos", colilla.TotalBonos);
        y = DibujarMonto(graficos, fuenteNormal, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Beneficios configurables", colilla.TotalBeneficiosConfigurables);
        y = DibujarMonto(graficos, fuenteNormal, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Ausencias", colilla.TotalAusencias, esDeduccion: true);

        y += 5;
        graficos.DrawLine(
            new XPen(XColors.LightGray, 0.8),
            margen.Point,
            y,
            (margen + anchoContenido).Point,
            y);
        y += 12;

        y = DibujarMonto(graficos, fuenteNegrita, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Salario bruto", colilla.SalarioBruto);
        y = DibujarMonto(graficos, fuenteNegrita, fuenteNegrita, margen.Point, y,
            anchoContenido.Point, "Deducciones", colilla.TotalDeducciones, esDeduccion: true);

        var rectanguloNeto = new XRect(margen.Point, y + 5, anchoContenido.Point, 34);
        graficos.DrawRoundedRectangle(
            new XPen(XColor.FromArgb(28, 96, 78), 1),
            new XSolidBrush(XColor.FromArgb(235, 246, 242)),
            rectanguloNeto,
            new XSize(5, 5));
        graficos.DrawString(
            "Salario neto",
            fuenteTotal,
            XBrushes.Black,
            new XRect(margen.Point + 12, y + 14, anchoContenido.Point / 2, 18),
            XStringFormats.TopLeft);
        graficos.DrawString(
            FormatearMoneda(colilla.SalarioNeto),
            fuenteTotal,
            XBrushes.Black,
            new XRect(margen.Point + anchoContenido.Point / 2, y + 14, anchoContenido.Point / 2 - 12, 18),
            XStringFormats.TopRight);

        var memoria = new MemoryStream();
        documento.Save(memoria, closeStream: false);
        memoria.Position = 0;

        return new ArchivoDescarga(
            memoria,
            $"colilla-{SanearNombre(colilla.CodigoColilla)}.pdf",
            MimePdf);
    }

    private static void DibujarTituloSeccion(
        XGraphics graficos,
        XFont fuente,
        double x,
        double y,
        string titulo)
    {
        graficos.DrawString(
            titulo,
            fuente,
            XBrushes.Black,
            new XRect(x, y, 400, 20),
            XStringFormats.TopLeft);
    }

    private static void DibujarPar(
        XGraphics graficos,
        XFont fuenteEtiqueta,
        XFont fuenteValor,
        double x,
        double y,
        string etiquetaIzquierda,
        string valorIzquierdo,
        string etiquetaDerecha,
        string valorDerecho,
        double ancho)
    {
        var mitad = ancho / 2;
        DibujarCampo(graficos, fuenteEtiqueta, fuenteValor, x, y,
            etiquetaIzquierda, valorIzquierdo, mitad - 12);
        DibujarCampo(graficos, fuenteEtiqueta, fuenteValor, x + mitad, y,
            etiquetaDerecha, valorDerecho, mitad);
    }

    private static void DibujarCampo(
        XGraphics graficos,
        XFont fuenteEtiqueta,
        XFont fuenteValor,
        double x,
        double y,
        string etiqueta,
        string valor,
        double ancho)
    {
        const double anchoEtiqueta = 82;
        graficos.DrawString(
            $"{etiqueta}:",
            fuenteEtiqueta,
            XBrushes.Black,
            new XRect(x, y, anchoEtiqueta, 16),
            XStringFormats.TopLeft);
        graficos.DrawString(
            valor,
            fuenteValor,
            XBrushes.Black,
            new XRect(x + anchoEtiqueta, y, Math.Max(0, ancho - anchoEtiqueta), 16),
            XStringFormats.TopLeft);
    }

    private static double DibujarMonto(
        XGraphics graficos,
        XFont fuenteConcepto,
        XFont fuenteMonto,
        double x,
        double y,
        double ancho,
        string concepto,
        decimal monto,
        bool esDeduccion = false)
    {
        graficos.DrawString(
            concepto,
            fuenteConcepto,
            XBrushes.Black,
            new XRect(x, y, ancho * 0.65, 18),
            XStringFormats.TopLeft);
        graficos.DrawString(
            $"{(esDeduccion && monto != 0 ? "−" : string.Empty)}{FormatearMoneda(Math.Abs(monto))}",
            fuenteMonto,
            XBrushes.Black,
            new XRect(x + (ancho * 0.65), y, ancho * 0.35, 18),
            XStringFormats.TopRight);

        return y + 21;
    }

    private static string FormatearMoneda(decimal monto)
    {
        return monto.ToString("C2", Cultura);
    }

    private static string FormatearFecha(DateTime fecha)
    {
        return fecha.ToString("dd/MM/yyyy", Cultura);
    }

    private static string FormatearFechaHora(DateTime fecha)
    {
        return fecha.ToString("dd/MM/yyyy HH:mm", Cultura);
    }

    private static string SanearNombre(string codigo)
    {
        var caracteres = codigo
            .Where(caracter => char.IsLetterOrDigit(caracter) || caracter == '-')
            .ToArray();
        return caracteres.Length == 0 ? "pago" : new string(caracteres);
    }
}

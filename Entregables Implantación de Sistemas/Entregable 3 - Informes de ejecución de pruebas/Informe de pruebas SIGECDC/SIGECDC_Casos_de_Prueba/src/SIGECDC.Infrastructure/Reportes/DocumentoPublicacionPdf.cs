using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Infrastructure.Reportes;

// Solo presentación; recibe la misma proyección que pantalla y Excel. Pagina sin truncar etiquetas.
internal sealed class DocumentoPublicacionPdf : IDisposable
{
    private readonly PdfDocument documento;
    private readonly bool horizontal;
    private readonly string rotulo;
    private readonly string referencia;
    private XGraphics? g;
    private double y, ancho, limite;
    private readonly XFont normal = new(FuenteNotoSansResolver.NombreFamilia, 9);
    private readonly XFont negrita = new(FuenteNotoSansResolver.NombreFamilia, 10, XFontStyleEx.Bold);
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    internal DocumentoPublicacionPdf(PdfDocument documento, bool horizontal, string rotulo, string referencia)
    {
        this.documento = documento; this.horizontal = horizontal; this.rotulo = rotulo; this.referencia = referencia;
        NuevaPagina();
    }
    private void NuevaPagina()
    {
        g?.Dispose();
        var p = documento.AddPage(); p.Size = PdfSharp.PageSize.A4;
        if (horizontal) p.Orientation = PdfSharp.PageOrientation.Landscape;
        g = XGraphics.FromPdfPage(p); ancho = p.Width.Point - 76; limite = p.Height.Point - 40; y = 40;
        Texto("Canales y Drenajes del Caribe S.R.L.", true);
        Texto(rotulo, true); Texto(referencia); y += 8;
    }
    internal void Texto(string texto, bool destacado = false)
    {
        var fuente = destacado ? negrita : normal;
        var linea = "";
        // Cortes por carácter también admiten nombres largos sin espacios.
        foreach (var ch in texto.Replace("\r", "").Replace("\n", " "))
        {
            if (g!.MeasureString(linea + ch, fuente).Width > ancho)
            { Dibujar(linea, fuente); linea = ""; }
            linea += ch;
        }
        Dibujar(linea, fuente);
    }
    private void Dibujar(string texto, XFont fuente)
    {
        if (y + 16 > limite) NuevaPagina();
        g!.DrawString(texto, fuente, XBrushes.Black, new XRect(38, y, ancho, 16), XStringFormats.TopLeft); y += 16;
    }
    internal static string Moneda(decimal valor) => "₡" + valor.ToString("N2", Cultura);

    internal void Detalle(DetalleReportePlanilla d)
    {
        Texto(d.CodigoColaborador + " · " + d.NombreColaborador, true);
        Texto("Puesto: " + d.Puesto + " | Departamento: " + d.Departamento);
        foreach (var s in d.Segmentos)
        {
            Texto($"Condición {s.Inicio:dd/MM/yyyy}–{s.Fin:dd/MM/yyyy}: {s.Puesto} / {s.Departamento} / {s.TipoContrato}");
            Texto($"Jornada {s.Jornada} · {s.HorasDia.ToString("0.############################", Cultura)} horas/día · Salario mensual {Moneda(s.SalarioMensual)} · Días base 30: {s.DiasBase30.ToString("0.############################", Cultura)}");
        }
        Texto("Conceptos aplicados", true);
        foreach (var l in d.Lineas)
        {
            Texto(l.Descripcion + " | " + l.EtiquetaEfecto + " | " + Moneda(l.Monto));
            Texto($"{l.Fecha:dd/MM/yyyy} · Cantidad: {l.Cantidad?.ToString("0.############################", Cultura) ?? "—"} {l.Unidad}");
        }
        Texto("Bruto salarial: " + Moneda(d.SalarioBruto));
        Texto("Ingresos no salariales / reembolsos: " + Moneda(d.IngresosNoSalariales));
        Texto("Bruto pagadero: " + Moneda(d.BrutoPagadero));
        Texto("Cuotas obreras: " + Moneda(d.CuotasObreras) + " | Renta: " + Moneda(d.ImpuestoSalario));
        Texto("Otras deducciones autorizadas: " + Moneda(d.OtrasDeducciones));
        Texto("Total deducciones: " + Moneda(d.TotalDeducciones));
        Texto("Neto: " + Moneda(d.SalarioNeto), true);
    }
    public void Dispose() => g?.Dispose();
}

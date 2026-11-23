using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

namespace SIGECDC.Tests.EntregableImplantacion.Soporte;

// Lector limitado al PDF textual que genera el producto. No sustituye inspección visual.
// Usa PDFsharp ya instalado; decodifica los glifos mediante el ToUnicode del propio PDF.
public static class PdfAcademico
{
    public static string Texto(byte[] bytes)
    {
        Assert.True(bytes.Length > 500); Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        using var documento = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.True(documento.PageCount > 0); var texto = new StringBuilder();
        foreach (var pagina in documento.Pages)
        {
            var fuentes = new Dictionary<string, Dictionary<int, string>>();
            var recursos = pagina.Elements.GetDictionary("/Resources")?.Elements.GetDictionary("/Font");
            if (recursos is not null)
                foreach (var nombre in recursos.Elements.Keys)
                {
                    var mapa = recursos.Elements.GetDictionary(nombre)?.Elements.GetDictionary("/ToUnicode");
                    if (mapa?.Stream is not null) fuentes[nombre] = Mapa(Encoding.ASCII.GetString(mapa.Stream.UnfilteredValue));
                }
            Dictionary<int, string>? actual = null;
            foreach (var operacion in ContentReader.ReadContent(pagina).OfType<COperator>())
            {
                if (operacion.Name == "Tf" && operacion.Operands[0] is CName fuente) actual = fuentes.GetValueOrDefault(fuente.Name);
                if (operacion.Name is "Tj" or "TJ" or "'" or "\"")
                {
                    foreach (var cadena in Cadenas(operacion.Operands)) texto.Append(Decodificar(cadena.Value, actual));
                    texto.Append(' ');
                }
            }
            texto.AppendLine();
        }
        Assert.False(string.IsNullOrWhiteSpace(texto.ToString())); return texto.ToString();
    }

    private static IEnumerable<CString> Cadenas(CSequence secuencia)
    {
        foreach (var objeto in secuencia)
        {
            if (objeto is CString cadena) yield return cadena;
            else if (objeto is CArray arreglo) foreach (var hija in Cadenas(arreglo)) yield return hija;
        }
    }

    private static string Decodificar(string entrada, Dictionary<int, string>? mapa)
    {
        if (mapa is null) return entrada;
        var salida = new StringBuilder();
        if (entrada.Any(c => c > 255))
            foreach (var c in entrada) salida.Append(mapa.GetValueOrDefault(c, "�"));
        else
            for (var i = 0; i + 1 < entrada.Length; i += 2) salida.Append(mapa.GetValueOrDefault((entrada[i] << 8) + entrada[i + 1], "�"));
        return salida.ToString();
    }

    private static Dictionary<int, string> Mapa(string contenido)
    {
        var resultado = new Dictionary<int, string>();
        foreach (Match bloque in Regex.Matches(contenido, @"beginbfchar(.*?)endbfchar", RegexOptions.Singleline))
            foreach (Match par in Regex.Matches(bloque.Groups[1].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
                resultado[Convert.ToInt32(par.Groups[1].Value, 16)] = Encoding.BigEndianUnicode.GetString(Convert.FromHexString(par.Groups[2].Value));
        foreach (Match bloque in Regex.Matches(contenido, @"beginbfrange(.*?)endbfrange", RegexOptions.Singleline))
            foreach (Match rango in Regex.Matches(bloque.Groups[1].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
            {
                var primero = Convert.ToInt32(rango.Groups[1].Value, 16); var ultimo = Convert.ToInt32(rango.Groups[2].Value, 16); var unicode = Convert.ToInt32(rango.Groups[3].Value, 16);
                for (var codigo = primero; codigo <= ultimo; codigo++) resultado[codigo] = char.ConvertFromUtf32(unicode + codigo - primero);
            }
        return resultado;
    }

    public static void ContieneMonto(string texto, decimal monto) => Assert.Contains(
        monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace(".", ""), Regex.Replace(texto, @"[\s.,\u00A0]", ""));
}

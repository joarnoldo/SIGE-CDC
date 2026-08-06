using SIGECDC.Application.Planillas;

namespace SIGECDC.Infrastructure.Reportes;

internal static class UtilidadesReportePlanilla
{
    public static void Validar(ReportePlanilla reporte)
    {
        ArgumentNullException.ThrowIfNull(reporte);

        if (string.IsNullOrWhiteSpace(reporte.CodigoPeriodo))
        {
            throw new ArgumentException("El reporte no tiene un código de período válido.");
        }

        if (reporte.Detalles.Count == 0)
        {
            throw new ArgumentException("El reporte no contiene filas para exportar.");
        }
    }

    public static string SanearCodigoPeriodo(string codigo)
    {
        var caracteresInvalidos = Path.GetInvalidFileNameChars();
        var caracteres = codigo.Trim()
            .Select(caracter => caracteresInvalidos.Contains(caracter) || char.IsWhiteSpace(caracter)
                ? '-'
                : caracter)
            .ToArray();

        var resultado = new string(caracteres).Trim('-', '.');
        return string.IsNullOrWhiteSpace(resultado) ? "periodo" : resultado;
    }
}

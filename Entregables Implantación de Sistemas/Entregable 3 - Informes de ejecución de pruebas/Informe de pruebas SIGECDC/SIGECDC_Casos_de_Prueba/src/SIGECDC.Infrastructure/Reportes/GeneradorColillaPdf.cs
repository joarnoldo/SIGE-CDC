using PdfSharp.Pdf;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Infrastructure.Reportes;

public sealed class GeneradorColillaPdf : IGeneradorColillaPdf
{
    public ArchivoDescarga Generar(ColillaPagoDetalle colilla)
    {
        ArgumentNullException.ThrowIfNull(colilla);
        if (string.IsNullOrWhiteSpace(colilla.CodigoColilla)) throw new ArgumentException("La colilla no tiene código.");
        ConfiguracionFuentesPdf.AsegurarInicializacion();
        using var documento = new PdfDocument();
        documento.Info.Title = "Colilla de pago " + colilla.CodigoColilla;
        documento.Info.Author = "Canales y Drenajes del Caribe S.R.L.";
        using (var pagina = new DocumentoPublicacionPdf(documento, false, colilla.Rotulo,
            $"Colilla · Ejecución {colilla.IdEjecucionPlanilla} · V{colilla.NumeroVersion}"))
        {
            pagina.Texto(colilla.CodigoColilla);
            pagina.Texto($"{colilla.TipoPeriodo} · {colilla.FechaInicio:dd/MM/yyyy}–{colilla.FechaFin:dd/MM/yyyy}");
            pagina.Detalle(colilla);
        }
        var memoria = new MemoryStream(); documento.Save(memoria, false); memoria.Position = 0;
        var codigo = new string(colilla.CodigoColilla.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return new(memoria, $"colilla-{codigo}.pdf", "application/pdf");
    }
}

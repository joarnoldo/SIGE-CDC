using System.Threading;
using PdfSharp.Fonts;

namespace SIGECDC.Infrastructure.Reportes;

internal static class ConfiguracionFuentesPdf
{
    private static readonly Lazy<bool> Inicializacion = new(
        Inicializar,
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static void AsegurarInicializacion()
    {
        _ = Inicializacion.Value;
    }

    private static bool Inicializar()
    {
        GlobalFontSettings.FontResolver = new FuenteNotoSansResolver();
        return true;
    }
}

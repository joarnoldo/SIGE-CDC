using System.Reflection;
using PdfSharp.Fonts;

namespace SIGECDC.Infrastructure.Reportes;

internal sealed class FuenteNotoSansResolver : IFontResolver
{
    public const string NombreFamilia = "Noto Sans SIGECDC";

    private const string CaraRegular = "NotoSans#Regular";
    private const string CaraNegrita = "NotoSans#Bold";
    private const string RecursoRegular =
        "SIGECDC.Infrastructure.Reportes.Fuentes.NotoSans-Regular.ttf";
    private const string RecursoNegrita =
        "SIGECDC.Infrastructure.Reportes.Fuentes.NotoSans-Bold.ttf";

    public byte[]? GetFont(string faceName)
    {
        return faceName switch
        {
            CaraRegular => LeerRecurso(RecursoRegular),
            CaraNegrita => LeerRecurso(RecursoNegrita),
            _ => null
        };
    }

    public FontResolverInfo? ResolveTypeface(
        string familyName,
        bool isBold,
        bool isItalic)
    {
        if (!string.Equals(familyName, NombreFamilia, StringComparison.Ordinal))
        {
            return null;
        }

        return new FontResolverInfo(isBold ? CaraNegrita : CaraRegular);
    }

    private static byte[] LeerRecurso(string nombreRecurso)
    {
        using var recurso = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(nombreRecurso)
            ?? throw new InvalidOperationException(
                $"No se encontró la fuente embebida '{nombreRecurso}'.");

        using var memoria = new MemoryStream();
        recurso.CopyTo(memoria);
        return memoria.ToArray();
    }
}

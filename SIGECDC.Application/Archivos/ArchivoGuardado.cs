namespace SIGECDC.Application.Archivos;

public sealed record ArchivoGuardado(
    string NombreAlmacenado,
    string RutaRelativa,
    string? MimeType,
    long TamanoBytes);

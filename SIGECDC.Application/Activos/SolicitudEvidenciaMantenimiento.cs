using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudEvidenciaMantenimiento : IDisposable
{
    [Required(ErrorMessage = "El nombre del archivo es obligatorio.")]
    [StringLength(255, ErrorMessage = "El nombre del archivo no puede superar 255 caracteres.")]
    public string NombreOriginal { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "El tipo MIME no puede superar 150 caracteres.")]
    public string? MimeType { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "La evidencia no puede estar vacía.")]
    public long TamanoBytes { get; set; }

    [Required(ErrorMessage = "El contenido de la evidencia es obligatorio.")]
    public Stream Contenido { get; set; } = Stream.Null;

    public void Dispose()
    {
        Contenido.Dispose();
    }
}

using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.SitioPublico;

public sealed class SolicitudGaleria
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no debe superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripcion no debe superar los 500 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class SolicitudImagenGaleria : IDisposable
{
    public long IdGaleria { get; set; }

    [StringLength(150, ErrorMessage = "El titulo no debe superar los 150 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(300, ErrorMessage = "La descripcion no debe superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo.")]
    public int Orden { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long TamanoBytes { get; set; }

    public Stream Contenido { get; set; } = Stream.Null;

    public void Dispose()
    {
        Contenido.Dispose();
    }
}

public sealed class SolicitudActualizarImagenGaleria
{
    [StringLength(150, ErrorMessage = "El titulo no debe superar los 150 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(300, ErrorMessage = "La descripcion no debe superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo.")]
    public int Orden { get; set; }
}

public sealed class GaleriaResumen
{
    public long IdGaleria { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EstaPublicado { get; set; }

    public int TotalImagenes { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}

public sealed class ImagenGaleriaResumen
{
    public long IdImagenGaleria { get; set; }

    public long IdGaleria { get; set; }

    public long IdDocumentoArchivo { get; set; }

    public string? Titulo { get; set; }

    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long? TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }
}

public sealed class GaleriaDetalle
{
    public long IdGaleria { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EstaPublicado { get; set; }

    public IReadOnlyList<ImagenGaleriaResumen> Imagenes { get; set; } = [];
}

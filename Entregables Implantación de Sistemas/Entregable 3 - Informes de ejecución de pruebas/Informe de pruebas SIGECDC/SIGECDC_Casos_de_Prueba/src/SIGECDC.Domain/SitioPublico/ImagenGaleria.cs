using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Domain.SitioPublico;

public class ImagenGaleria
{
    public long IdImagenGaleria { get; set; }

    public long IdGaleria { get; set; }

    public long IdDocumentoArchivo { get; set; }

    public string? Titulo { get; set; }

    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public Galeria? Galeria { get; set; }

    public DocumentoArchivo? DocumentoArchivo { get; set; }
}
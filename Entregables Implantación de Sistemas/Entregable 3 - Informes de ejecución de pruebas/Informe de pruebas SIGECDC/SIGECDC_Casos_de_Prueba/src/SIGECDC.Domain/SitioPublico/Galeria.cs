namespace SIGECDC.Domain.SitioPublico;

public class Galeria
{
    public long IdGaleria { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ICollection<ImagenGaleria> Imagenes { get; set; } = [];
}
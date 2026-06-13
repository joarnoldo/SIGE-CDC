using SIGECDC.Domain.Operaciones;

namespace SIGECDC.Domain.SitioPublico;

public sealed class ProyectoPublicado
{
    public long IdProyectoPublicado { get; set; }

    public long? IdProyecto { get; set; }

    public Proyecto? Proyecto { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? EstadoVisual { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

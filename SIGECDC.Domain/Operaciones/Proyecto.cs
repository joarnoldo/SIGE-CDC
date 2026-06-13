using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Operaciones;

public sealed class Proyecto
{
    public long IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = string.Empty;

    public string NombreProyecto { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFinEstimada { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public string? Responsable { get; set; }

    public string? Ubicacion { get; set; }

    public int IdEstadoProyecto { get; set; }

    public EstadoProyecto? EstadoProyecto { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

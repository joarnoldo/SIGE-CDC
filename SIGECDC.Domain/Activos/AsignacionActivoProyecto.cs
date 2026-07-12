using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Activos;

public sealed class AsignacionActivoProyecto
{
    public long IdAsignacionActivoProyecto { get; set; }

    public long IdActivo { get; set; }

    public Activo? Activo { get; set; }

    public long IdProyecto { get; set; }

    public Proyecto? Proyecto { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string? Observaciones { get; set; }

    public string? AsignadoPor { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Activos;

public sealed class RegistroUsoActivo
{
    public long IdRegistroUsoActivo { get; set; }

    public long IdActivo { get; set; }

    public Activo? Activo { get; set; }

    public long? IdProyecto { get; set; }

    public Proyecto? Proyecto { get; set; }

    public DateTime FechaRegistro { get; set; }

    public decimal? LecturaAnterior { get; set; }

    public decimal LecturaNueva { get; set; }

    public decimal? CantidadUso { get; set; }

    public string? Observaciones { get; set; }

    public string? RegistradoPor { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

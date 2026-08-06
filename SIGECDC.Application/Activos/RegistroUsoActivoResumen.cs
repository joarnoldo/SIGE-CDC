namespace SIGECDC.Application.Activos;

public sealed class RegistroUsoActivoResumen
{
    public long IdRegistroUsoActivo { get; set; }

    public long? IdProyecto { get; set; }

    public string? Proyecto { get; set; }

    public DateTime FechaRegistro { get; set; }

    public decimal? LecturaAnterior { get; set; }

    public decimal LecturaNueva { get; set; }

    public decimal? CantidadUso { get; set; }

    public string? Observaciones { get; set; }

    public string? RegistradoPor { get; set; }

    public DateTime FechaCreacion { get; set; }
}

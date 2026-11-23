namespace SIGECDC.Application.Activos;

public sealed class RegistroUsoReporteResumen
{
    public long IdRegistroUsoActivo { get; set; }

    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string Activo { get; set; } = string.Empty;

    public long? IdProyecto { get; set; }

    public string? CodigoProyecto { get; set; }

    public string? Proyecto { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? TipoMedicion { get; set; }

    public decimal? LecturaAnterior { get; set; }

    public decimal LecturaNueva { get; set; }

    public decimal? CantidadUso { get; set; }
}

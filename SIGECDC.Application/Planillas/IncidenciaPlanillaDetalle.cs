namespace SIGECDC.Application.Planillas;

public sealed class IncidenciaPlanillaDetalle
{
    public long IdIncidenciaPlanilla { get; set; }

    public long IdPeriodoPlanilla { get; set; }

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string EstadoPlanilla { get; set; } = string.Empty;

    public bool EstaBloqueada { get; set; }

    public DateTime FechaInicioPeriodo { get; set; }

    public DateTime FechaFinPeriodo { get; set; }

    public long IdColaborador { get; set; }

    public string CodigoColaborador { get; set; } = string.Empty;

    public string NombreColaborador { get; set; } = string.Empty;

    public int IdTipoIncidenciaPlanilla { get; set; }

    public string TipoIncidencia { get; set; } = string.Empty;

    public string Naturaleza { get; set; } = string.Empty;

    public DateTime FechaIncidencia { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}

namespace SIGECDC.Application.Planillas;

public sealed class IncidenciaPlanillaResumen
{
    public long IdIncidenciaPlanilla { get; set; }

    public long IdPeriodoPlanilla { get; set; }

    public string CodigoPeriodo { get; set; } = string.Empty;

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

    public string EstadoRegistro { get; set; } = string.Empty;
}

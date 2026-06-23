using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Domain.Planillas;

public sealed class IncidenciaPlanilla
{
    public long IdIncidenciaPlanilla { get; set; }

    public long IdPeriodoPlanilla { get; set; }

    public long IdColaborador { get; set; }

    public int IdTipoIncidenciaPlanilla { get; set; }

    public DateTime FechaIncidencia { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public string? RegistradoPor { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public PeriodoPlanilla? PeriodoPlanilla { get; set; }

    public Colaborador? Colaborador { get; set; }

    public TipoIncidenciaPlanilla? TipoIncidenciaPlanilla { get; set; }
}

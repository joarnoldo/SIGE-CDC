using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Domain.Planillas;

public sealed class ParametroPlanillaColaborador
{
    public long IdParametroPlanillaColaborador { get; set; }

    public int IdParametroPlanilla { get; set; }

    public long IdColaborador { get; set; }

    public decimal? ValorDecimalOverride { get; set; }

    public string? ValorTextoOverride { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public ParametroPlanilla? ParametroPlanilla { get; set; }

    public Colaborador? Colaborador { get; set; }
}

namespace SIGECDC.Domain.Planillas;

// Evidencia inmutable; correcciones mediante una nueva NumeroVersion. SQL impide UPDATE/DELETE.
public sealed class IncidenciaPlanillaConfirmacion
{
    public long IdIncidenciaPlanillaConfirmacion { get; init; }
    public long IdIncidenciaPlanilla { get; init; }
    public int NumeroVersion { get; init; }
    public DateTime FechaHecho { get; init; }
    public long IdConceptoPlanillaVersion { get; init; }
    public string Unidad { get; init; } = "";
    public decimal? Cantidad { get; init; }
    public decimal? MontoManual { get; init; }
    public string? TipoDiaHE { get; init; }
    public string ReferenciaSoporte { get; init; } = "";
    public string HuellaIncidencia { get; init; } = "";
    public string ConfirmadoPor { get; init; } = "";
    public DateTime FechaConfirmacion { get; init; }
    public bool AutorizacionRequerida { get; init; }
    public string? AutorizadoPor { get; init; }
    public DateTime? FechaAutorizacion { get; init; }
    public string? ReferenciaAutorizacion { get; init; }
    public int? IdParametroPlanilla { get; init; }
    public long? IdCondicionBase { get; init; }
    public DateTime? FechaBaseInicio { get; init; }
    public DateTime? FechaBaseFin { get; init; }
    public IncidenciaPlanilla? IncidenciaPlanilla { get; init; }
    public ConceptoPlanillaVersion? ConceptoPlanillaVersion { get; init; }
    public ParametroPlanilla? ParametroPlanilla { get; init; }
}

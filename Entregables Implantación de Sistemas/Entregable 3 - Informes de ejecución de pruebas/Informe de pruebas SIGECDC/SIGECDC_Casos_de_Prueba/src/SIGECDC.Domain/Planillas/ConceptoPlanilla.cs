namespace SIGECDC.Domain.Planillas;

public sealed class ConceptoPlanilla
{
    public long IdConceptoPlanilla { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }

    public ICollection<ConceptoPlanillaVersion> Versiones { get; set; } = [];
}

public sealed class ConceptoPlanillaVersion
{
    public long IdConceptoPlanillaVersion { get; set; }
    public long IdConceptoPlanilla { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public bool EsSalarial { get; set; }
    public bool EsContributivoCCSS { get; set; }
    public bool EsTributario { get; set; }
    public bool EsPatronal { get; set; }
    public string TipoBase { get; set; } = string.Empty;
    public string UnidadPermitida { get; set; } = string.Empty;
    public bool PermiteMontoManual { get; set; }
    public bool RequiereAutorizacion { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";

    public ConceptoPlanilla? ConceptoPlanilla { get; set; }
}

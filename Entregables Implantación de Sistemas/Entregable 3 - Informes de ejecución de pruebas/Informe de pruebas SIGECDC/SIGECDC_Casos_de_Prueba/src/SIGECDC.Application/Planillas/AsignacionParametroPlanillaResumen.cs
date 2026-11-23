namespace SIGECDC.Application.Planillas;

public sealed class AsignacionParametroPlanillaResumen
{
    public long IdAsignacion { get; set; }

    public string Ambito { get; set; } = string.Empty;

    public long IdDestino { get; set; }

    public string CodigoDestino { get; set; } = string.Empty;

    public string NombreDestino { get; set; } = string.Empty;

    public decimal? ValorDecimalOverride { get; set; }

    public string? ValorTextoOverride { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;

    public bool EstaBloqueada { get; set; }
}

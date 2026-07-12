namespace SIGECDC.Application.Planillas;

public sealed class ParametroPlanillaDetalle
{
    public int IdParametroPlanilla { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string TipoParametro { get; set; } = string.Empty;

    public string? Naturaleza { get; set; }

    public decimal? ValorDecimal { get; set; }

    public string? ValorTexto { get; set; }

    public bool EsEditable { get; set; }

    public DateTime? FechaVigenciaInicio { get; set; }

    public DateTime? FechaVigenciaFin { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}

namespace SIGECDC.Domain.Planillas;

public sealed class ParametroPlanilla
{
    public int IdParametroPlanilla { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string TipoParametro { get; set; } = TiposParametroPlanilla.Porcentaje;

    public string? Naturaleza { get; set; }

    public decimal? ValorDecimal { get; set; }

    public string? ValorTexto { get; set; }

    public bool EsEditable { get; set; } = true;

    public DateTime? FechaVigenciaInicio { get; set; }

    public DateTime? FechaVigenciaFin { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public ICollection<ParametroPlanillaColaborador> AsignacionesColaborador { get; set; } = [];

    public ICollection<ParametroPlanillaPeriodo> AsignacionesPeriodo { get; set; } = [];
}

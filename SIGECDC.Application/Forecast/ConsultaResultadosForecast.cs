namespace SIGECDC.Application.Forecast;

public sealed class ConsultaResultadosForecast
{
    public long IdForecastEscenario { get; init; }
    public string NombreEscenario { get; init; } = string.Empty;
    public string EstadoEscenario { get; init; } = string.Empty;
    public DateTime? FechaCalculo { get; init; }
    public string Dimension { get; init; } = DimensionesResultadoForecast.Periodo;
    public long? IdForecastPeriodo { get; init; }
    public decimal MontoProyectadoTotalEscenario { get; init; }
    public decimal MontoProyectadoTotalConsulta { get; init; }
    public decimal DeduccionesTotalConsulta { get; init; }
    public decimal SalarioNetoTotalConsulta { get; init; }
    public bool? EsConsistenteConEscenario { get; init; }
    public IReadOnlyList<PeriodoResultadoForecastOpcion> Periodos { get; init; } = [];
    public IReadOnlyList<FilaResultadoForecast> Filas { get; init; } = [];
}

public sealed record PeriodoResultadoForecastOpcion(
    long IdForecastPeriodo,
    int NumeroOrden,
    string TipoPeriodo,
    DateTime FechaInicio,
    DateTime FechaFin);

public sealed class FilaResultadoForecast
{
    public string Clave { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;
    public string? Detalle { get; init; }
    public string? TipoParticipante { get; init; }
    public int CantidadParticipantes { get; init; }
    public decimal SalarioBrutoProyectado { get; init; }
    public decimal DeduccionesProyectadas { get; init; }
    public decimal SalarioNetoProyectado { get; init; }
    public IReadOnlyList<ConceptoResultadoForecast> Conceptos { get; init; } = [];
}

public sealed record ConceptoResultadoForecast(
    string Codigo,
    string Nombre,
    decimal MontoBase,
    decimal MontoAjuste,
    decimal MontoProyectado);

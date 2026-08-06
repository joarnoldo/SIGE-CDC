namespace SIGECDC.Application.Forecast;

public sealed class ComparacionEscenariosForecast
{
    public EscenarioComparadoForecast EscenarioBase { get; init; } = new();
    public EscenarioComparadoForecast EscenarioAlternativo { get; init; } = new();
    public string Dimension { get; init; } = DimensionesResultadoForecast.Periodo;
    public int? NumeroOrdenPeriodo { get; init; }
    public IReadOnlyList<PeriodoComparacionForecastOpcion> Periodos { get; init; } = [];
    public IReadOnlyList<ParametroComparadoForecast> Parametros { get; init; } = [];
    public IReadOnlyList<FilaComparacionForecast> Filas { get; init; } = [];
    public ValoresComparacionForecast Totales { get; init; } = new();
}

public class EscenarioComparacionForecastOpcion
{
    public long IdForecastEscenario { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string EstadoEscenario { get; init; } = string.Empty;
    public DateTime FechaInicioProyeccion { get; init; }
    public DateTime FechaFinProyeccion { get; init; }
    public DateTime? FechaCalculo { get; init; }
    public decimal MontoProyectadoTotal { get; init; }
    public int CantidadPeriodos { get; init; }
}

public sealed class EscenarioComparadoForecast : EscenarioComparacionForecastOpcion
{
    public decimal DeduccionesProyectadas { get; init; }
    public decimal SalarioNetoProyectado { get; init; }
}

public sealed record PeriodoComparacionForecastOpcion(
    int NumeroOrden,
    string TipoPeriodo,
    DateTime FechaInicio,
    DateTime FechaFin);

public sealed class ParametroComparadoForecast
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public bool EstaHabilitadoBase { get; init; }
    public string TipoBase { get; init; } = string.Empty;
    public decimal? ValorBase { get; init; }
    public bool EstaHabilitadoAlternativo { get; init; }
    public string TipoAlternativo { get; init; } = string.Empty;
    public decimal? ValorAlternativo { get; init; }
}

public sealed class FilaComparacionForecast
{
    public string Clave { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;
    public string? Detalle { get; init; }
    public int CantidadParticipantesBase { get; init; }
    public int CantidadParticipantesAlternativo { get; init; }
    public ValoresComparacionForecast Valores { get; init; } = new();
    public IReadOnlyList<ConceptoComparadoForecast> Conceptos { get; init; } = [];
}

public sealed class ValoresComparacionForecast
{
    public decimal SalarioBrutoBase { get; init; }
    public decimal SalarioBrutoAlternativo { get; init; }
    public decimal DiferenciaSalarioBruto => SalarioBrutoAlternativo - SalarioBrutoBase;
    public decimal DeduccionesBase { get; init; }
    public decimal DeduccionesAlternativo { get; init; }
    public decimal DiferenciaDeducciones => DeduccionesAlternativo - DeduccionesBase;
    public decimal SalarioNetoBase { get; init; }
    public decimal SalarioNetoAlternativo { get; init; }
    public decimal DiferenciaSalarioNeto => SalarioNetoAlternativo - SalarioNetoBase;
}

public sealed class ConceptoComparadoForecast
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public decimal MontoBaseEscenario { get; init; }
    public decimal MontoAlternativo { get; init; }
    public decimal Diferencia => MontoAlternativo - MontoBaseEscenario;
}

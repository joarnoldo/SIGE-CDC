namespace SIGECDC.Application.Forecast;

public sealed class DisponibilidadComparacionRealForecast
{
    public long IdForecastEscenario { get; init; }
    public string NombreEscenario { get; init; } = string.Empty;
    public string EstadoEscenario { get; init; } = string.Empty;
    public DateTime? FechaCalculo { get; init; }
    public int CantidadPeriodosDisponibles { get; init; }
    public int CantidadPeriodosPendientes { get; init; }
    public IReadOnlyList<PeriodoComparacionRealForecast> Periodos { get; init; } = [];
}

public sealed class ComparacionForecastReal
{
    public DisponibilidadComparacionRealForecast Disponibilidad { get; init; } = new();
    public string Dimension { get; init; } = DimensionesComparacionRealForecast.Periodo;
    public long? IdForecastPeriodo { get; init; }
    public ValoresForecastReal TotalesComparables { get; init; } = new();
    public IReadOnlyList<FilaComparacionForecastReal> Filas { get; init; } = [];
}

public sealed class PeriodoComparacionRealForecast
{
    public long IdForecastPeriodo { get; init; }
    public int NumeroOrden { get; init; }
    public string TipoPeriodo { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public bool EstaDisponible { get; init; }
    public bool EsAmbigua { get; init; }
    public long? IdPlanilla { get; init; }
    public string? CodigoPeriodoReal { get; init; }
    public string? EstadoPlanillaReal { get; init; }
    public string MensajeDisponibilidad { get; init; } = string.Empty;
    public bool? EsConsistenteConDetalles { get; init; }
    public ValoresForecastReal Valores { get; init; } = new();
}

public sealed class FilaComparacionForecastReal
{
    public string Clave { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;
    public string? Detalle { get; init; }
    public bool TieneForecast { get; init; }
    public bool TieneReal { get; init; }
    public bool EsContratacionPrevista { get; init; }
    public ValoresForecastReal Valores { get; init; } = new();
}

public sealed class ValoresForecastReal
{
    public decimal? SalarioBrutoProyectado { get; init; }
    public decimal? SalarioBrutoReal { get; init; }
    public decimal? DeduccionesProyectadas { get; init; }
    public decimal? DeduccionesReales { get; init; }
    public decimal? SalarioNetoProyectado { get; init; }
    public decimal? SalarioNetoReal { get; init; }
}

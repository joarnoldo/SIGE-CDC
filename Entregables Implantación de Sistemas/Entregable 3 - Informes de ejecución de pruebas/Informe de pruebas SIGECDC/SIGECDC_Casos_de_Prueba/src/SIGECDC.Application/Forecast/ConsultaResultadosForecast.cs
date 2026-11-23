namespace SIGECDC.Application.Forecast;

public sealed class ConsultaResultadosForecast
{
    public string? VersionMotor { get; init; }
    public bool HistoricoLimitado { get; init; }
    public int? CantidadFuentesHistoricas { get; init; }
    public string? CodigoAdvertenciaHistorico => CantidadFuentesHistoricas == 0
        ? "SIN_HISTORICO_DISPONIBLE" : HistoricoLimitado ? "HISTORICO_LIMITADO" : null;
    public string? AdvertenciaHistorico => CantidadFuentesHistoricas == 0
        ? "Sin histórico disponible: 0 fuentes. La proyección requiere entradas explícitas completas."
        : HistoricoLimitado ? "Histórico limitado; ausencia no equivale a cero observado." : null;
    public IReadOnlyList<SegmentoResultadoForecast> Segmentos { get; init; } = [];
    public decimal BrutoSalarialTotalConsulta { get; init; }
    public decimal CargasPatronalesTotalConsulta { get; init; }
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
    public decimal CostoLaboralTotalProyectado { get; init; }
    public decimal CargasPatronalesProyectadas { get; init; }
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
    decimal MontoProyectado)
{
    public long? IdConceptoVersion { get; init; }
    public string? Efecto { get; init; }
    public int? ObservacionesMinimas { get; init; }
    public int? ObservacionesMaximas { get; init; }
    public bool HistoricoLimitado { get; init; }
}

public sealed record SegmentoResultadoForecast(long IdParticipante,long IdPeriodo,DateTime Inicio,DateTime Fin,
    string Puesto,string Departamento,string Jornada,decimal SalarioMensual,int DiasBase30,string Origen);

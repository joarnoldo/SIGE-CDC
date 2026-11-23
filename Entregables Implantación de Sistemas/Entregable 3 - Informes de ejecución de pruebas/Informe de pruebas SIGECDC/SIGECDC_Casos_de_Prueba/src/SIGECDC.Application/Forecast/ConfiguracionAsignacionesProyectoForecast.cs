namespace SIGECDC.Application.Forecast;

public sealed class ConfiguracionAsignacionesProyectoForecast
{
    public long IdForecastEscenario { get; set; }
    public string NombreEscenario { get; set; } = string.Empty;
    public string EstadoEscenario { get; set; } = string.Empty;
    public bool PuedeEditar { get; set; }
    public int CeldasRequeridas { get; set; }
    public int CeldasCompletas { get; set; }
    public bool EstaListaParaCalculo { get; set; }
    public IReadOnlyList<PeriodoForecastResumen> Periodos { get; set; } = [];
    public IReadOnlyList<ParticipanteAsignacionForecast> Participantes { get; set; } = [];
    public IReadOnlyList<ProyectoAsignableForecast> Proyectos { get; set; } = [];
    public IReadOnlyList<AsignacionProyectoForecastResumen> Asignaciones { get; set; } = [];
    public IReadOnlyList<CeldaAsignacionForecastResumen> Celdas { get; set; } = [];
}

public sealed class ParticipanteAsignacionForecast
{
    public long IdForecastParticipante { get; set; }
    public string CodigoParticipante { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string TipoParticipante { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
}

public sealed class ProyectoAsignableForecast
{
    public long IdProyecto { get; set; }
    public string CodigoProyecto { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string EstadoProyecto { get; set; } = string.Empty;
    public IReadOnlyList<long> IdsPeriodosElegibles { get; set; } = [];
}

public sealed class AsignacionProyectoForecastResumen
{
    public long IdForecastAsignacionProyecto { get; set; }
    public long IdForecastParticipante { get; set; }
    public long IdForecastPeriodo { get; set; }
    public long IdProyecto { get; set; }
    public string CodigoProyecto { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
    public bool EsProyectoElegible { get; set; }
}

public sealed class CeldaAsignacionForecastResumen
{
    public long IdForecastParticipante { get; set; }
    public long IdForecastPeriodo { get; set; }
    public decimal PorcentajeTotal { get; set; }
    public bool EstaCompleta { get; set; }
}

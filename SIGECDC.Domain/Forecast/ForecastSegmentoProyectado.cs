namespace SIGECDC.Domain.Forecast;

// Evidencia proyectada propia de Forecast; no crea relaciones RRHH ni snapshots de Planilla.
public sealed class ForecastSegmentoProyectado
{
    public long IdForecastSegmentoProyectado { get; set; }
    public long IdForecastEscenario { get; set; }
    public long IdForecastPeriodo { get; set; }
    public long IdForecastParticipante { get; set; }
    public int NumeroOrden { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int DiasBase30 { get; set; }
    public string OrigenCondicion { get; set; } = string.Empty;
    public decimal SalarioMensual { get; set; }
    public long? IdContrato { get; set; }
    public long? IdCondicionLaboralVersion { get; set; }
    public long IdJornadaLaboral { get; set; }
    public string CodigoJornada { get; set; } = string.Empty;
    public string NombreJornada { get; set; } = string.Empty;
    public string TipoJornada { get; set; } = string.Empty;
    public decimal HorasOrdinariasDia { get; set; }
    public decimal HorasOrdinariasSemana { get; set; }
    public DateTime FechaJornadaInicio { get; set; }
    public DateTime? FechaJornadaFin { get; set; }
    public long IdOcupacionSalarioMinimo { get; set; }
    public string CodigoOcupacion { get; set; } = string.Empty;
    public string NombreOcupacion { get; set; } = string.Empty;
    public long? IdSalarioMinimoVersion { get; set; }
    public decimal? MontoMinimo { get; set; }
    public string? PeriodicidadMinimo { get; set; }
    public int IdPuesto { get; set; }
    public string NombrePuesto { get; set; } = string.Empty;
    public int IdDepartamento { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;
    public string TipoContrato { get; set; } = string.Empty;
    public string PeriodicidadPago { get; set; } = string.Empty;
    public string ReferenciaSoporte { get; set; } = string.Empty;
    public string RegistradoPor { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public long? IdConfiguracionRiesgoTrabajo { get; set; }
    public string? ModalidadRT { get; set; }
    public string? ReferenciaAsignacionRT { get; set; }
    public string? AsociadoPorRT { get; set; }
    public DateTime? FechaRTInicio { get; set; }
    public DateTime? FechaRTFin { get; set; }
}


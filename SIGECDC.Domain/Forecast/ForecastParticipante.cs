using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastParticipante
{
    public long IdForecastParticipante { get; set; }

    public long IdForecastEscenario { get; set; }

    public string CodigoParticipante { get; set; } = string.Empty;

    public string Etiqueta { get; set; } = string.Empty;

    public string TipoParticipante { get; set; } = TiposParticipanteForecast.Colaborador;

    public long? IdColaborador { get; set; }

    public int IdDepartamento { get; set; }

    public int IdPuesto { get; set; }

    public decimal SalarioBaseMensual { get; set; }

    public DateTime FechaInicioAplicacion { get; set; }

    public DateTime? FechaSalidaPrevista { get; set; }

    public bool EstaIncluido { get; set; } = true;

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ForecastEscenario? ForecastEscenario { get; set; }

    public Colaborador? Colaborador { get; set; }

    public Departamento? Departamento { get; set; }

    public Puesto? Puesto { get; set; }

    public ICollection<ForecastAsignacionProyecto> AsignacionesProyecto { get; set; } = [];
}

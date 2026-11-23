using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Forecast;

// IdColaborador de cada acreditación identifica aquí al IdForecastParticipante: no escribe RRHH.
// El servicio exige el actor autenticado; una acreditación vacía no significa ausencia de excepción.
public sealed record SolicitudCalculoFinancieroForecast(long IdEscenario,IReadOnlyList<AplicabilidadCuota> Aplicabilidades);

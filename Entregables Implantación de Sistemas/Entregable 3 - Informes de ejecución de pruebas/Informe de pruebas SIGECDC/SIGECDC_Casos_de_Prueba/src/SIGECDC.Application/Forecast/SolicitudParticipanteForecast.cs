using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudParticipanteForecast
{
    [Range(1, long.MaxValue, ErrorMessage = "El colaborador seleccionado no es válido.")]
    public long IdColaborador { get; set; }

    public bool EstaIncluido { get; set; }
}

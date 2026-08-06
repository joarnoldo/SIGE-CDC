using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class SolicitudGuardarParticipantesForecastTests
{
    [Fact]
    public void Solicitud_AceptaListaCompletaConIncluidosYExcluidos()
    {
        var solicitud = new SolicitudGuardarParticipantesForecast
        {
            Participantes =
            [
                new SolicitudParticipanteForecast { IdColaborador = 1, EstaIncluido = true },
                new SolicitudParticipanteForecast { IdColaborador = 2, EstaIncluido = false }
            ]
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Solicitud_RechazaListaNulaElementosNulosYDuplicados()
    {
        var nula = new SolicitudGuardarParticipantesForecast();
        var elementoNulo = new SolicitudGuardarParticipantesForecast
        {
            Participantes = [null!]
        };
        var duplicada = new SolicitudGuardarParticipantesForecast
        {
            Participantes =
            [
                new SolicitudParticipanteForecast { IdColaborador = 7, EstaIncluido = true },
                new SolicitudParticipanteForecast { IdColaborador = 7, EstaIncluido = false }
            ]
        };

        Assert.Contains(Validar(nula), item =>
            item.MemberNames.Contains(nameof(SolicitudGuardarParticipantesForecast.Participantes)));
        Assert.Contains(Validar(elementoNulo), item =>
            item.MemberNames.Contains(nameof(SolicitudGuardarParticipantesForecast.Participantes)));
        Assert.Contains(Validar(duplicada), item =>
            item.ErrorMessage?.Contains("exactamente una vez", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Solicitud_RechazaIdentificadorDeColaboradorInvalido(long idColaborador)
    {
        var solicitud = new SolicitudGuardarParticipantesForecast
        {
            Participantes =
            [
                new SolicitudParticipanteForecast
                {
                    IdColaborador = idColaborador,
                    EstaIncluido = true
                }
            ]
        };

        Assert.Contains(Validar(solicitud), item =>
            item.MemberNames.Contains(nameof(SolicitudParticipanteForecast.IdColaborador)));
    }

    private static IReadOnlyList<ValidationResult> Validar(object instancia)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            instancia,
            new ValidationContext(instancia),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}

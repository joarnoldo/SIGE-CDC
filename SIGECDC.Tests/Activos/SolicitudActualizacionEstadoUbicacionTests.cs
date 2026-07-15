using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class SolicitudActualizacionEstadoUbicacionTests
{
    [Fact]
    public void Validar_DatosCompletos_NoReportaErrores()
    {
        var solicitud = new SolicitudActualizacionEstadoUbicacion
        {
            IdActivo = 1,
            IdEstadoActivo = 1,
            UbicacionActual = "Plantel central"
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Validar_SinActivoNiEstado_ReportaAmbosErrores()
    {
        var solicitud = new SolicitudActualizacionEstadoUbicacion();

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.IdActivo)));
        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.IdEstadoActivo)));
    }

    private static IReadOnlyCollection<ValidationResult> Validar(SolicitudActualizacionEstadoUbicacion solicitud)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}

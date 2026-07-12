using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class SolicitudAsignacionActivoProyectoTests
{
    [Fact]
    public void Validar_DatosCompletos_NoReportaErrores()
    {
        var solicitud = new SolicitudAsignacionActivoProyecto
        {
            IdActivo = 1,
            IdProyecto = 1,
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 15)
        };

        var resultados = Validar(solicitud);

        Assert.Empty(resultados);
    }

    [Fact]
    public void Validar_FechaFinalAnterior_ReportaError()
    {
        var solicitud = new SolicitudAsignacionActivoProyecto
        {
            IdActivo = 1,
            IdProyecto = 1,
            FechaInicio = new DateTime(2026, 8, 15),
            FechaFin = new DateTime(2026, 8, 1)
        };

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(solicitud.FechaFin)));
    }

    [Fact]
    public void Validar_SinActivoNiProyecto_ReportaAmbosErrores()
    {
        var solicitud = new SolicitudAsignacionActivoProyecto
        {
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 15)
        };

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(solicitud.IdActivo)));
        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(solicitud.IdProyecto)));
    }

    private static IReadOnlyCollection<ValidationResult> Validar(SolicitudAsignacionActivoProyecto solicitud)
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

using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Tests.Planillas;

public sealed class SolicitudPeriodoPlanillaTests
{
    [Fact]
    public void Validar_FechaFinalAnterior_ReportaError()
    {
        var solicitud = new SolicitudPeriodoPlanilla
        {
            CodigoPeriodo = "Q1-2026",
            Nombre = "Primera quincena",
            TipoPeriodo = "Quincenal",
            FechaInicio = new DateTime(2026, 1, 16),
            FechaFin = new DateTime(2026, 1, 15)
        };
        var resultados = new List<ValidationResult>();

        var esValida = Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);

        Assert.False(esValida);
        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.FechaFin)));
    }

    [Fact]
    public void Validar_DatosCompletos_NoReportaErrores()
    {
        var solicitud = new SolicitudPeriodoPlanilla
        {
            CodigoPeriodo = "ENE-2026",
            Nombre = "Enero 2026",
            TipoPeriodo = "Mensual",
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 1, 31)
        };
        var resultados = new List<ValidationResult>();

        var esValida = Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);

        Assert.True(esValida);
        Assert.Empty(resultados);
    }
}

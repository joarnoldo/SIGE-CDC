using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class FiltroDisponibilidadActivoTests
{
    [Fact]
    public void Validar_RangoCorrectoSinEstado_NoReportaErrores()
    {
        var filtro = new FiltroDisponibilidadActivo
        {
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 31)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_FechaFinalAnterior_ReportaErrorDeRango()
    {
        var filtro = new FiltroDisponibilidadActivo
        {
            FechaInicio = new DateTime(2026, 8, 2),
            FechaFin = new DateTime(2026, 8, 1)
        };

        var resultados = Validar(filtro);

        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(filtro.FechaFin)));
    }

    [Fact]
    public void Validar_EstadoNoPositivo_ReportaErrorDeEstado()
    {
        var filtro = new FiltroDisponibilidadActivo
        {
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 31),
            IdEstadoActivo = 0
        };

        var resultados = Validar(filtro);

        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(filtro.IdEstadoActivo)));
    }

    private static IReadOnlyCollection<ValidationResult> Validar(FiltroDisponibilidadActivo filtro)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            filtro,
            new ValidationContext(filtro),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}

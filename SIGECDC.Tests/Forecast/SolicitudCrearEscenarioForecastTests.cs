using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Tests.Forecast;

public sealed class SolicitudCrearEscenarioForecastTests
{
    [Fact]
    public void Solicitud_AceptaUnoYVariosPeriodosConEspacios()
    {
        var inicio = DateTime.Today.AddMonths(2);
        var unPeriodo = CrearSolicitud(
            [new SolicitudPeriodoForecast
            {
                TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                FechaInicio = inicio,
                FechaFin = inicio.AddDays(29)
            }]);
        var variosPeriodos = CrearSolicitud(
            [
                new SolicitudPeriodoForecast
                {
                    TipoPeriodo = TiposPeriodoPlanilla.Quincenal,
                    FechaInicio = inicio,
                    FechaFin = inicio.AddDays(14)
                },
                new SolicitudPeriodoForecast
                {
                    TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                    FechaInicio = inicio.AddMonths(1),
                    FechaFin = inicio.AddMonths(1).AddDays(29)
                }
            ]);

        Assert.Empty(Validar(unPeriodo));
        Assert.Empty(Validar(variosPeriodos));
    }

    [Fact]
    public void Solicitud_RechazaDatosPeriodosYFuentesInvalidas()
    {
        var inicio = DateTime.Today.AddMonths(2);

        var nombreInvalido = CrearSolicitud(
            [new SolicitudPeriodoForecast
            {
                TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                FechaInicio = inicio,
                FechaFin = inicio.AddDays(29)
            }]);
        nombreInvalido.Nombre = " ";
        var vacia = new SolicitudCrearEscenarioForecast
        {
            Nombre = "Escenario sin composición",
            Periodos = [],
            IdPlanillasHistoricas = []
        };
        var traslapada = CrearSolicitud(
            [
                new SolicitudPeriodoForecast
                {
                    TipoPeriodo = "Semanal",
                    FechaInicio = inicio,
                    FechaFin = inicio.AddDays(14)
                },
                new SolicitudPeriodoForecast
                {
                    TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                    FechaInicio = inicio.AddDays(14),
                    FechaFin = inicio.AddDays(10)
                }
            ]);
        traslapada.IdPlanillasHistoricas = [0, 9, 9];

        var resultadosNombre = Validar(nombreInvalido);
        var resultadosVacios = Validar(vacia);
        var resultadosInvalidos = Validar(traslapada);

        Assert.Contains(resultadosNombre, resultado =>
            resultado.MemberNames.Contains(nameof(SolicitudCrearEscenarioForecast.Nombre)));
        Assert.Contains(resultadosVacios, resultado =>
            resultado.MemberNames.Contains(nameof(SolicitudCrearEscenarioForecast.Periodos)));
        Assert.Contains(resultadosVacios, resultado =>
            resultado.MemberNames.Contains(nameof(SolicitudCrearEscenarioForecast.IdPlanillasHistoricas)));
        Assert.Contains(resultadosInvalidos, resultado =>
            resultado.ErrorMessage?.Contains("Mensual o Quincenal") == true);
        Assert.Contains(resultadosInvalidos, resultado =>
            resultado.ErrorMessage?.Contains("anterior") == true);
        Assert.Contains(resultadosInvalidos, resultado =>
            resultado.ErrorMessage?.Contains("traslaparse") == true);
        Assert.Contains(resultadosInvalidos, resultado =>
            resultado.ErrorMessage?.Contains("no son válidas") == true);
        Assert.Contains(resultadosInvalidos, resultado =>
            resultado.ErrorMessage?.Contains("más de una vez") == true);
    }

    private static SolicitudCrearEscenarioForecast CrearSolicitud(
        List<SolicitudPeriodoForecast> periodos)
    {
        return new SolicitudCrearEscenarioForecast
        {
            Nombre = "Escenario válido",
            Descripcion = "Prueba de validación",
            Periodos = periodos,
            IdPlanillasHistoricas = [10]
        };
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

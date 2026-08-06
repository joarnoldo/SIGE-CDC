using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Tests.Forecast;

public sealed class SolicitudGuardarParametrosForecastTests
{
    [Fact]
    public void Catalogo_ConservaCodigosOrdenTiposYNombresOficiales()
    {
        Assert.Equal(
            [
                CodigosParametroForecast.AjusteSalarial,
                CodigosParametroForecast.HorasExtraEstimadas,
                CodigosParametroForecast.BonosEstimados,
                CodigosParametroForecast.DeduccionesRecurrentes
            ],
            CodigosParametroForecast.Definiciones.Select(definicion => definicion.Codigo));
        Assert.Equal(
            ["Ajuste salarial", "Horas extra estimadas", "Bonos estimados", "Deducciones recurrentes"],
            CodigosParametroForecast.Definiciones.Select(definicion => definicion.Nombre));
        Assert.Equal(
            [TiposParametroPlanilla.Porcentaje],
            CodigosParametroForecast.Definiciones[0].TiposPermitidos);
        Assert.All(
            CodigosParametroForecast.Definiciones.Skip(1),
            definicion => Assert.Equal(
                [TiposParametroPlanilla.Porcentaje, TiposParametroPlanilla.Monto],
                definicion.TiposPermitidos));
    }

    [Fact]
    public void Solicitud_AceptaConfiguracionMixtaCeroYParametrosDeshabilitados()
    {
        var solicitud = CrearSolicitudValida();
        solicitud.Parametros[0].ValorDecimal = 0m;
        solicitud.Parametros[3].EstaHabilitado = false;
        solicitud.Parametros[3].ValorDecimal = null;

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Solicitud_RechazaCodigosFaltantesDuplicadosYDesconocidos()
    {
        var faltante = CrearSolicitudValida();
        faltante.Parametros.RemoveAt(3);
        var duplicada = CrearSolicitudValida();
        duplicada.Parametros[3].Codigo = CodigosParametroForecast.BonosEstimados;
        var desconocida = CrearSolicitudValida();
        desconocida.Parametros[3].Codigo = "PARAMETRO_NO_OFICIAL";

        Assert.Contains(Validar(faltante), resultado =>
            resultado.ErrorMessage?.Contains("exactamente") == true);
        Assert.Contains(Validar(duplicada), resultado =>
            resultado.ErrorMessage?.Contains("una sola vez") == true);
        Assert.Contains(Validar(desconocida), resultado =>
            resultado.ErrorMessage?.Contains("no permitido") == true);
    }

    [Fact]
    public void Solicitud_RechazaTiposRangosYEscalaInvalidos()
    {
        var ajusteMonto = CrearSolicitudValida();
        ajusteMonto.Parametros[0].TipoParametro = TiposParametroPlanilla.Monto;
        var porcentajeMayor = CrearSolicitudValida();
        porcentajeMayor.Parametros[2].TipoParametro = TiposParametroPlanilla.Porcentaje;
        porcentajeMayor.Parametros[2].ValorDecimal = 100.0001m;
        var montoNegativo = CrearSolicitudValida();
        montoNegativo.Parametros[1].ValorDecimal = -0.0001m;
        var montoExcesivo = CrearSolicitudValida();
        montoExcesivo.Parametros[1].ValorDecimal = SolicitudGuardarParametrosForecast.MontoMaximo + 0.0001m;
        var escalaExcesiva = CrearSolicitudValida();
        escalaExcesiva.Parametros[3].ValorDecimal = 1.00001m;
        var cantidad = CrearSolicitudValida();
        cantidad.Parametros[1].TipoParametro = TiposParametroPlanilla.Cantidad;

        Assert.Contains(Validar(ajusteMonto), resultado =>
            resultado.ErrorMessage?.Contains("no es válido") == true);
        Assert.Contains(Validar(porcentajeMayor), resultado =>
            resultado.ErrorMessage?.Contains("entre 0 y 100") == true);
        Assert.Contains(Validar(montoNegativo), resultado =>
            resultado.ErrorMessage?.Contains("fuera del rango") == true);
        Assert.Contains(Validar(montoExcesivo), resultado =>
            resultado.ErrorMessage?.Contains("fuera del rango") == true);
        Assert.Contains(Validar(escalaExcesiva), resultado =>
            resultado.ErrorMessage?.Contains("cuatro decimales") == true);
        Assert.Contains(Validar(cantidad), resultado =>
            resultado.ErrorMessage?.Contains("no es válido") == true);
    }

    internal static SolicitudGuardarParametrosForecast CrearSolicitudValida() => new()
    {
        Parametros =
        [
            new SolicitudParametroForecast
            {
                Codigo = CodigosParametroForecast.AjusteSalarial,
                EstaHabilitado = true,
                TipoParametro = TiposParametroPlanilla.Porcentaje,
                ValorDecimal = 4.5000m
            },
            new SolicitudParametroForecast
            {
                Codigo = CodigosParametroForecast.HorasExtraEstimadas,
                EstaHabilitado = true,
                TipoParametro = TiposParametroPlanilla.Monto,
                ValorDecimal = 25_000.1250m
            },
            new SolicitudParametroForecast
            {
                Codigo = CodigosParametroForecast.BonosEstimados,
                EstaHabilitado = true,
                TipoParametro = TiposParametroPlanilla.Porcentaje,
                ValorDecimal = 2.2500m
            },
            new SolicitudParametroForecast
            {
                Codigo = CodigosParametroForecast.DeduccionesRecurrentes,
                EstaHabilitado = true,
                TipoParametro = TiposParametroPlanilla.Monto,
                ValorDecimal = 10_000m
            }
        ]
    };

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

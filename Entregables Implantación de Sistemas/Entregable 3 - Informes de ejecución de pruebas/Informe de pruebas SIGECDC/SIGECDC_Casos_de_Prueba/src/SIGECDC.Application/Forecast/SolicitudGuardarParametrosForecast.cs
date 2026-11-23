using System.ComponentModel.DataAnnotations;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Forecast;

public sealed class SolicitudGuardarParametrosForecast : IValidatableObject
{
    public const decimal MontoMaximo = 99_999_999_999_999.9999m;

    public List<SolicitudParametroForecast> Parametros { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Parametros is null)
        {
            yield return new ValidationResult(
                "La configuración de parámetros es obligatoria.",
                [nameof(Parametros)]);
            yield break;
        }

        if (Parametros.Count != CodigosParametroForecast.Definiciones.Count)
        {
            yield return new ValidationResult(
                "La configuración debe incluir exactamente los cuatro parámetros oficiales.",
                [nameof(Parametros)]);
        }

        var codigos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parametro in Parametros)
        {
            if (parametro is null)
            {
                yield return new ValidationResult(
                    "La configuración no puede contener parámetros vacíos.",
                    [nameof(Parametros)]);
                continue;
            }

            var resultadosAnotaciones = new List<ValidationResult>();
            Validator.TryValidateObject(
                parametro,
                new ValidationContext(parametro),
                resultadosAnotaciones,
                validateAllProperties: true);

            foreach (var resultado in resultadosAnotaciones)
            {
                yield return resultado;
            }

            var codigo = parametro.Codigo?.Trim() ?? string.Empty;
            if (!codigos.Add(codigo))
            {
                yield return new ValidationResult(
                    "Cada parámetro oficial debe aparecer una sola vez.",
                    [nameof(Parametros)]);
            }

            var definicion = CodigosParametroForecast.Obtener(codigo);
            if (definicion is null)
            {
                yield return new ValidationResult(
                    "La configuración contiene un código de parámetro no permitido.",
                    [nameof(Parametros)]);
                continue;
            }

            var tipo = parametro.TipoParametro?.Trim() ?? string.Empty;
            if (!definicion.TiposPermitidos.Any(tipoPermitido => string.Equals(
                    tipoPermitido,
                    tipo,
                    StringComparison.OrdinalIgnoreCase)))
            {
                yield return new ValidationResult(
                    $"El tipo seleccionado no es válido para {definicion.Nombre}.",
                    [nameof(Parametros)]);
                continue;
            }

            if (parametro.ValorDecimal is null)
            {
                if (parametro.EstaHabilitado)
                {
                    yield return new ValidationResult(
                        $"Ingrese el valor de {definicion.Nombre}.",
                        [nameof(Parametros)]);
                }

                continue;
            }

            var valor = parametro.ValorDecimal.Value;
            if (valor < 0m || valor > MontoMaximo)
            {
                yield return new ValidationResult(
                    $"El valor de {definicion.Nombre} está fuera del rango permitido.",
                    [nameof(Parametros)]);
            }

            if (string.Equals(tipo, TiposParametroPlanilla.Porcentaje, StringComparison.OrdinalIgnoreCase)
                && valor > 100m)
            {
                yield return new ValidationResult(
                    $"El porcentaje de {definicion.Nombre} debe estar entre 0 y 100.",
                    [nameof(Parametros)]);
            }

            if (ObtenerEscala(valor) > 4)
            {
                yield return new ValidationResult(
                    $"El valor de {definicion.Nombre} no debe superar cuatro decimales.",
                    [nameof(Parametros)]);
            }
        }

        foreach (var definicion in CodigosParametroForecast.Definiciones)
        {
            if (!codigos.Contains(definicion.Codigo))
            {
                yield return new ValidationResult(
                    $"Falta el parámetro {definicion.Nombre}.",
                    [nameof(Parametros)]);
            }
        }
    }

    private static byte ObtenerEscala(decimal valor) =>
        (byte)((decimal.GetBits(valor)[3] >> 16) & 0x7F);
}

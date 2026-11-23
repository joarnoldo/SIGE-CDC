using SIGECDC.Application.Forecast;

namespace SIGECDC.Infrastructure.Reportes;

internal static class UtilidadesExportacionForecast
{
    public static void Validar(ConsultaResultadosForecast consulta)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        if (consulta.IdForecastEscenario <= 0)
        {
            throw new ArgumentException("La consulta no identifica un escenario forecast válido.");
        }

        if (string.IsNullOrWhiteSpace(consulta.NombreEscenario)
            || string.IsNullOrWhiteSpace(consulta.EstadoEscenario))
        {
            throw new ArgumentException("La consulta no contiene la información del escenario forecast.");
        }

        if (!consulta.FechaCalculo.HasValue)
        {
            throw new ArgumentException("El escenario forecast todavía no ha sido calculado.");
        }

        if (!DimensionesResultadoForecast.EsValida(consulta.Dimension))
        {
            throw new ArgumentException("La dimensión de la consulta forecast no es válida.");
        }

        if (consulta.IdForecastPeriodo.HasValue
            && consulta.Periodos.All(periodo =>
                periodo.IdForecastPeriodo != consulta.IdForecastPeriodo.Value))
        {
            throw new ArgumentException("El período aplicado no pertenece a la consulta forecast.");
        }

        if (consulta.Filas.Count == 0)
        {
            throw new ArgumentException("La consulta forecast no contiene filas para exportar.");
        }

        if (consulta.Filas.Any(fila => string.IsNullOrWhiteSpace(fila.Etiqueta)
            || fila.Conceptos.Count == 0))
        {
            throw new ArgumentException("La consulta forecast no contiene el desglose requerido para exportar.");
        }
    }

    public static string CrearNombreArchivo(ConsultaResultadosForecast consulta, string extension)
    {
        var dimension = consulta.Dimension.ToLowerInvariant();
        var alcance = "todos";

        if (consulta.IdForecastPeriodo.HasValue)
        {
            var periodo = consulta.Periodos.Single(item =>
                item.IdForecastPeriodo == consulta.IdForecastPeriodo.Value);
            alcance = $"periodo-{periodo.NumeroOrden}";
        }

        return $"forecast-{consulta.IdForecastEscenario}-{dimension}-{alcance}.{extension}";
    }

    public static PeriodoResultadoForecastOpcion? ObtenerPeriodoAplicado(
        ConsultaResultadosForecast consulta) => consulta.IdForecastPeriodo.HasValue
        ? consulta.Periodos.Single(periodo =>
            periodo.IdForecastPeriodo == consulta.IdForecastPeriodo.Value)
        : null;

    public static string NombreDimension(string dimension) => dimension switch
    {
        DimensionesResultadoForecast.Colaborador => "Colaborador",
        DimensionesResultadoForecast.Departamento => "Departamento",
        DimensionesResultadoForecast.Proyecto => "Proyecto",
        _ => "Período"
    };
}

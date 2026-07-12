namespace SIGECDC.Domain.Planillas;

public static class FlujoEstadosPlanilla
{
    public static bool EstaBloqueada(string? estado)
    {
        return EsEstado(estado, EstadosPlanilla.Aprobada)
            || EsEstado(estado, EstadosPlanilla.Cerrada);
    }

    public static void ValidarAprobacion(string? estadoPeriodo, string? estadoPlanilla)
    {
        ValidarCoherencia(estadoPeriodo, estadoPlanilla);

        if (!EsEstado(estadoPlanilla, EstadosPlanilla.Calculada))
        {
            throw new InvalidOperationException("Solo se puede aprobar una planilla en estado Calculada.");
        }
    }

    public static void ValidarCierre(string? estadoPeriodo, string? estadoPlanilla)
    {
        ValidarCoherencia(estadoPeriodo, estadoPlanilla);

        if (!EsEstado(estadoPlanilla, EstadosPlanilla.Aprobada))
        {
            throw new InvalidOperationException("Solo se puede cerrar una planilla en estado Aprobada.");
        }
    }

    private static void ValidarCoherencia(string? estadoPeriodo, string? estadoPlanilla)
    {
        if (string.IsNullOrWhiteSpace(estadoPeriodo)
            || string.IsNullOrWhiteSpace(estadoPlanilla)
            || !string.Equals(estadoPeriodo, estadoPlanilla, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El período y la planilla no tienen un estado coherente.");
        }
    }

    private static bool EsEstado(string? actual, string esperado)
    {
        return string.Equals(actual, esperado, StringComparison.OrdinalIgnoreCase);
    }
}

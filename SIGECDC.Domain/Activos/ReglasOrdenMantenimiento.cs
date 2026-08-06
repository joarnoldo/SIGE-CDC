namespace SIGECDC.Domain.Activos;

public static class ReglasOrdenMantenimiento
{
    public static void ValidarInicio(string? estadoMantenimiento)
    {
        if (!string.Equals(
                estadoMantenimiento,
                EstadosMantenimiento.Programado,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Solo un mantenimiento programado puede iniciar su ejecución.");
        }
    }

    public static void ValidarCierre(string? estadoMantenimiento)
    {
        if (!string.Equals(
                estadoMantenimiento,
                EstadosMantenimiento.EnProceso,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Solo un mantenimiento en proceso puede finalizarse.");
        }
    }

    public static string DeterminarEstadoActivoAlIniciar(string? estadoActivo)
    {
        ValidarEstadoActivoOficial(estadoActivo);

        if (string.Equals(estadoActivo, EstadosActivo.DadoDeBaja, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "No se puede iniciar un mantenimiento para un activo dado de baja.");
        }

        if (string.Equals(estadoActivo, EstadosActivo.FueraDeServicio, StringComparison.OrdinalIgnoreCase))
        {
            return EstadosActivo.FueraDeServicio;
        }

        return EstadosActivo.EnMantenimiento;
    }

    public static string DeterminarEstadoActivoAlFinalizar(
        string? estadoActivo,
        bool existeOtroMantenimientoEnProceso,
        bool tieneAsignacionNoFinalizada)
    {
        ValidarEstadoActivoOficial(estadoActivo);

        if (string.Equals(estadoActivo, EstadosActivo.DadoDeBaja, StringComparison.OrdinalIgnoreCase))
        {
            return EstadosActivo.DadoDeBaja;
        }

        if (string.Equals(estadoActivo, EstadosActivo.FueraDeServicio, StringComparison.OrdinalIgnoreCase))
        {
            return EstadosActivo.FueraDeServicio;
        }

        if (existeOtroMantenimientoEnProceso)
        {
            return EstadosActivo.EnMantenimiento;
        }

        return tieneAsignacionNoFinalizada
            ? EstadosActivo.Asignado
            : EstadosActivo.Disponible;
    }

    private static void ValidarEstadoActivoOficial(string? estadoActivo)
    {
        if (!ReglasEstadoActivo.EsEstadoOficial(estadoActivo))
        {
            throw new InvalidOperationException(
                "El estado actual del activo no pertenece al catálogo oficial.");
        }
    }
}

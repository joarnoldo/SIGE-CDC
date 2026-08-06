namespace SIGECDC.Domain.Activos;

public static class ReglasAsignacionActivo
{
    public static void ValidarRango(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaFin.Date < fechaInicio.Date)
        {
            throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial.");
        }
    }

    public static bool EsEstadoAsignable(string? estado)
    {
        return string.Equals(estado, EstadosActivo.Disponible, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EsEstadoCompatibleConAsignacionVigente(string? estado)
    {
        return string.Equals(estado, EstadosActivo.Asignado, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EsEstadoReservable(string? estado)
    {
        return EsEstadoAsignable(estado)
            || EsEstadoCompatibleConAsignacionVigente(estado);
    }

    public static void ValidarEstadoAsignable(string? estado)
    {
        if (!EsEstadoAsignable(estado))
        {
            throw new InvalidOperationException(
                $"El activo está en estado {estado ?? "Sin estado"} y no se encuentra disponible para asignación.");
        }
    }

    public static void ValidarEstadoReservable(string? estado)
    {
        if (!EsEstadoReservable(estado))
        {
            throw new InvalidOperationException(
                $"El activo estÃ¡ en estado {estado ?? "Sin estado"} y no se encuentra disponible para asignaciÃ³n.");
        }
    }

    public static bool EstaDisponible(
        string? estado,
        bool tieneConflictoDeAsignacion,
        bool tieneMantenimientoEnProceso = false)
    {
        return EsEstadoReservable(estado)
            && !tieneConflictoDeAsignacion
            && !tieneMantenimientoEnProceso;
    }

    public static string DescribirDisponibilidad(
        string? estado,
        bool tieneConflictoDeAsignacion,
        bool tieneMantenimientoEnProceso = false)
    {
        var razones = new List<string>();

        if (!EsEstadoReservable(estado))
        {
            razones.Add($"estado {estado ?? "Sin estado"}");
        }

        if (tieneConflictoDeAsignacion)
        {
            razones.Add("una asignación vigente en el rango consultado");
        }

        if (tieneMantenimientoEnProceso)
        {
            razones.Add("un mantenimiento en proceso");
        }

        return razones.Count == 0
            ? "Disponible para el rango consultado."
            : $"No disponible por {string.Join(" y por ", razones)}.";
    }

    public static bool HayTraslape(
        DateTime fechaInicioExistente,
        DateTime fechaFinExistente,
        DateTime fechaInicioSolicitada,
        DateTime fechaFinSolicitada)
    {
        ValidarRango(fechaInicioExistente, fechaFinExistente);
        ValidarRango(fechaInicioSolicitada, fechaFinSolicitada);

        return fechaInicioExistente.Date <= fechaFinSolicitada.Date
            && fechaFinExistente.Date >= fechaInicioSolicitada.Date;
    }
}

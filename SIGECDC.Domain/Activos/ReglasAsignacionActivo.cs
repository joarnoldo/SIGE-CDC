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

    public static void ValidarEstadoAsignable(string? estado)
    {
        if (!EsEstadoAsignable(estado))
        {
            throw new InvalidOperationException(
                $"El activo está en estado {estado ?? "Sin estado"} y no se encuentra disponible para asignación.");
        }
    }

    public static bool EstaDisponible(string? estado, bool tieneConflictoDeAsignacion)
    {
        return EsEstadoAsignable(estado) && !tieneConflictoDeAsignacion;
    }

    public static string DescribirDisponibilidad(string? estado, bool tieneConflictoDeAsignacion)
    {
        if (!EsEstadoAsignable(estado))
        {
            return $"No disponible por estado {estado ?? "Sin estado"}.";
        }

        return tieneConflictoDeAsignacion
            ? "No disponible por una asignación vigente en el rango consultado."
            : "Disponible para el rango consultado.";
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

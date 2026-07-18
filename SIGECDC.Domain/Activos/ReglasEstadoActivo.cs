namespace SIGECDC.Domain.Activos;

public static class ReglasEstadoActivo
{
    public static bool EsEstadoOficial(string? estado)
    {
        return string.Equals(estado, EstadosActivo.Disponible, StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, EstadosActivo.Asignado, StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, EstadosActivo.EnMantenimiento, StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, EstadosActivo.FueraDeServicio, StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, EstadosActivo.DadoDeBaja, StringComparison.OrdinalIgnoreCase);
    }

    public static void ValidarTransicion(
        string? estadoActual,
        string? estadoNuevo,
        bool tieneAsignacionNoFinalizada)
    {
        if (!EsEstadoOficial(estadoActual) || !EsEstadoOficial(estadoNuevo))
        {
            throw new InvalidOperationException("El estado actual o el estado solicitado no pertenece al catálogo oficial de activos.");
        }

        if (string.Equals(estadoActual, EstadosActivo.DadoDeBaja, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(estadoNuevo, EstadosActivo.DadoDeBaja, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Un activo dado de baja no puede regresar a un estado operativo.");
        }

        if (tieneAsignacionNoFinalizada
            && !ReglasAsignacionActivo.EsEstadoCompatibleConAsignacionVigente(estadoNuevo))
        {
            throw new InvalidOperationException(
                "El activo posee una asignación activa o futura y no puede pasar a un estado incompatible.");
        }
    }
}

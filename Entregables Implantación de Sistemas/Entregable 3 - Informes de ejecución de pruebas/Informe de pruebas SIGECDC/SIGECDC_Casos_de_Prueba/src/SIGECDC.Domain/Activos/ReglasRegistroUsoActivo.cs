namespace SIGECDC.Domain.Activos;

public static class ReglasRegistroUsoActivo
{
    public const decimal ValorMaximoDecimal18_2 = 9999999999999999.99m;

    public static bool EsEstadoUtilizable(string? estado)
    {
        return string.Equals(
                estado,
                EstadosActivo.Disponible,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                estado,
                EstadosActivo.Asignado,
                StringComparison.OrdinalIgnoreCase);
    }

    public static void ValidarEstadoUtilizable(
        string? estado,
        bool tieneMantenimientoEnLaFecha)
    {
        if (!EsEstadoUtilizable(estado))
        {
            throw new InvalidOperationException(
                "Solo se puede registrar uso para un activo Disponible o Asignado.");
        }

        if (tieneMantenimientoEnLaFecha)
        {
            throw new InvalidOperationException(
                "No se puede registrar uso en una fecha que coincide con un período fuera de servicio por mantenimiento.");
        }
    }

    public static bool EsFechaAfectadaPorMantenimiento(
        DateTime fechaRegistro,
        string? estadoMantenimiento,
        DateTime? fechaInicio,
        DateTime? fechaFin)
    {
        if (!fechaInicio.HasValue
            || (!string.Equals(
                    estadoMantenimiento,
                    EstadosMantenimiento.EnProceso,
                    StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    estadoMantenimiento,
                    EstadosMantenimiento.Finalizado,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var fecha = fechaRegistro.Date;
        return fechaInicio.Value.Date <= fecha
            && (!fechaFin.HasValue
                || fechaFin.Value.Date >= fecha);
    }

    public static bool EsTipoMedicionRegistrable(string? tipoMedicion)
    {
        return string.Equals(
                tipoMedicion,
                TiposMedicionUso.Horas,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                tipoMedicion,
                TiposMedicionUso.Kilometros,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                tipoMedicion,
                TiposMedicionUso.Unidades,
                StringComparison.OrdinalIgnoreCase);
    }

    public static void ValidarTipoMedicionRegistrable(string? tipoMedicion)
    {
        if (!EsTipoMedicionRegistrable(tipoMedicion))
        {
            throw new InvalidOperationException(
                $"El tipo de medición {tipoMedicion ?? "Sin tipo"} no permite registrar uso.");
        }
    }

    public static void ValidarLectura(decimal valor, string nombreCampo)
    {
        if (valor < 0)
        {
            throw new ArgumentException(
                $"{nombreCampo} no puede ser negativa.");
        }

        if (valor > ValorMaximoDecimal18_2)
        {
            throw new ArgumentException(
                $"{nombreCampo} supera el máximo permitido.");
        }

        if (decimal.Round(valor, 2) != valor)
        {
            throw new ArgumentException(
                $"{nombreCampo} no puede tener más de dos decimales.");
        }
    }

    public static decimal CalcularCantidadUso(
        decimal lecturaAnterior,
        decimal lecturaNueva)
    {
        ValidarLectura(lecturaAnterior, "La lectura anterior");
        ValidarLectura(lecturaNueva, "La lectura nueva");

        if (lecturaNueva <= lecturaAnterior)
        {
            throw new ArgumentException(
                "La lectura nueva debe ser mayor que la lectura anterior.");
        }

        var cantidadUso = lecturaNueva - lecturaAnterior;
        ValidarLectura(cantidadUso, "La cantidad de uso");
        return cantidadUso;
    }
}

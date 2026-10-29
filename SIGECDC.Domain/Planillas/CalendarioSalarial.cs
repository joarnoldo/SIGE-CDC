namespace SIGECDC.Domain.Planillas;

public sealed record PeriodoSalarial(DateTime Inicio, DateTime Fin, string Tipo)
{
    public void Validar()
    {
        new VigenciaFinanciera(Inicio, Fin).Validar();
        var mismoMes = Inicio.Year == Fin.Year && Inicio.Month == Fin.Month;
        var ultimo = DateTime.DaysInMonth(Fin.Year, Fin.Month);
        var valido = Tipo switch
        {
            TiposPeriodoPlanilla.Mensual => Inicio.Day == 1 && Fin.Day == ultimo,
            TiposPeriodoPlanilla.Quincenal => (Inicio.Day == 1 && Fin.Day == 15)
                || (Inicio.Day == 16 && Fin.Day == ultimo),
            _ => false
        };
        if (!mismoMes || !valido) throw new ArgumentException("El período debe ser un mensual o una quincena calendario estándar.");
    }
}

// Especificación financiera §3.2: ordinal acumulado, no proporción de días calendario.
// La diferencia de acumulados hace aditiva cualquier partición: febrero cierra en 30;
// el 31 conserva ese acumulado y no aporta otra unidad ya representada por el 30.
public static class CalendarioSalarial
{
    public const int UnidadesPorMes = 30;

    public static int DiasBase30(DateTime inicio, DateTime fin)
    {
        new VigenciaFinanciera(inicio, fin).Validar();
        var anteriorAlInicio = Mes(inicio) * UnidadesPorMes + Math.Min(inicio.Day - 1, UnidadesPorMes);
        var ordinalFin = fin.Day == DateTime.DaysInMonth(fin.Year, fin.Month) ? UnidadesPorMes : Math.Min(fin.Day, UnidadesPorMes);
        return Mes(fin) * UnidadesPorMes + ordinalFin - anteriorAlInicio;
    }

    // Fronteras efectivas inclusivas; reutilizable para futuras vigencias normativas.
    public static IReadOnlyList<VigenciaFinanciera> Dividir(VigenciaFinanciera periodo, IEnumerable<DateTime> fronteras)
    {
        periodo.Validar();
        if (periodo.Fin is null) throw new ArgumentException("La segmentación requiere un período finito.");
        var cortes = new SortedSet<DateTime> { periodo.Inicio };
        foreach (var fecha in fronteras)
        {
            if (fecha.TimeOfDay != TimeSpan.Zero) throw new ArgumentException("Las fronteras deben ser fechas civiles.");
            if (periodo.Contiene(fecha)) cortes.Add(fecha);
        }
        var fechas = cortes.ToArray();
        return fechas.Select((inicio, i) => new VigenciaFinanciera(inicio,
            i + 1 < fechas.Length ? fechas[i + 1].AddDays(-1) : periodo.Fin)).ToArray();
    }

    public static IReadOnlyList<int> DiasPorSegmento(VigenciaFinanciera periodo, IReadOnlyList<VigenciaFinanciera> segmentos)
    {
        periodo.Validar();
        if (periodo.Fin is null) throw new ArgumentException("El período debe ser finito.");
        DateTime? anterior = null;
        var dias = new List<int>();
        foreach (var segmento in segmentos)
        {
            segmento.Validar();
            if (segmento.Fin is null || !periodo.Contiene(segmento) || (anterior.HasValue && segmento.Inicio <= anterior))
                throw new ArgumentException("Segmentos fuera del período, desordenados o superpuestos.");
            dias.Add(DiasBase30(segmento.Inicio, segmento.Fin.Value));
            anterior = segmento.Fin;
        }
        return dias;
    }

    private static int Mes(DateTime fecha) => (fecha.Year - 1) * 12 + fecha.Month - 1;
}

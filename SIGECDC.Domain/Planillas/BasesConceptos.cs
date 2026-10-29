namespace SIGECDC.Domain.Planillas;

public static class BasesConceptos
{
    // Códigos de catálogo C; MONTO_AUTORIZADO expresa exclusivamente M de PLA-GD-05.
    // No se asigna este código ni clasificación a ningún concepto empresarial existente.
    public static FamiliaBaseConcepto Familia(string codigo) => codigo switch
    {
        "SALARIO_DEVENGADO" => FamiliaBaseConcepto.SalarioOrdinario,
        "MONTO_AUTORIZADO" => FamiliaBaseConcepto.MontoAutorizado,
        "HORA_ORDINARIA" or "REDUCCION_DEVENGO" => FamiliaBaseConcepto.Incidencias,
        "BASE_SEM" or "BASE_IVM" or "REMUNERACION_CONTRIBUTIVA" => FamiliaBaseConcepto.Cuotas,
        "RENTA_SALARIAL" => FamiliaBaseConcepto.Renta,
        "BASE_RT_POLIZA" => FamiliaBaseConcepto.RiesgosTrabajo,
        _ => throw new ConfiguracionConceptoException("TIPO_BASE_DESCONOCIDO")
    };

    public static decimal Resolver(string codigo, SegmentoSalarialTemporal? segmento, decimal? montoAutorizado = null)
        => Familia(codigo) switch
        {
            FamiliaBaseConcepto.SalarioOrdinario => Ordinario(segmento),
            FamiliaBaseConcepto.MontoAutorizado => montoAutorizado is >= 0 ? montoAutorizado.Value : throw new ConfiguracionConceptoException("BASE_AUTORIZADA_FALTANTE"),
            _ => throw new ConfiguracionConceptoException("MOTOR_BASE_NO_DISPONIBLE")
        };

    public static decimal Ordinario(SegmentoSalarialTemporal? s)
    {
        if (s is null) throw new ConfiguracionConceptoException("SEGMENTO_FALTANTE");
        if (s.SalarioMensual <= 0 || s.DiasBase30 < 0 || s.Inicio > s.Fin
            || s.DiasBase30 != CalendarioSalarial.DiasBase30(s.Inicio, s.Fin))
            throw new ConfiguracionConceptoException("SEGMENTO_INVALIDO");
        // Divisor salarial aprobado; ninguna materialización intermedia.
        return s.SalarioMensual / CalendarioSalarial.UnidadesPorMes * s.DiasBase30;
    }

    public static decimal HoraOrdinaria(SegmentoSalarialTemporal s)
    {
        _ = Ordinario(s);
        if (s.Jornada is null || !s.Jornada.Utilizable || s.Jornada.HorasDia <= 0)
            throw new ConfiguracionConceptoException("JORNADA_NO_DEMOSTRADA");
        return s.SalarioMensual / CalendarioSalarial.UnidadesPorMes / s.Jornada.HorasDia;
    }

    public static decimal BaseAusencia(SegmentoSalarialTemporal s, string unidad)
    {
        _ = Ordinario(s);
        return unidad switch
        {
            "Día" => s.SalarioMensual / CalendarioSalarial.UnidadesPorMes,
            "Hora" => HoraOrdinaria(s),
            _ => throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE")
        };
    }
}

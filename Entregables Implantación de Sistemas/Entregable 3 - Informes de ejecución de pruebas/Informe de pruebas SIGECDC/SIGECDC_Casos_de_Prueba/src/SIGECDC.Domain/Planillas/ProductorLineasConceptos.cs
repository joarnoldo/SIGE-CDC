using System.Globalization;

namespace SIGECDC.Domain.Planillas;

public static class ProductorLineasConceptos
{
    public static LineaMonetaria Ordinario(SegmentoSalarialTemporal s, ConceptoMonetarioVersion concepto)
    {
        var referencia = ReferenciaSegmentoTemporal.De(s);
        var fuente = new FuenteLineaMonetaria(s.IdColaborador,
            string.Create(CultureInfo.InvariantCulture, $"ORDINARIO:{s.IdColaborador}:{s.IdContrato}:{s.Inicio:yyyy-MM-dd}:{s.Fin:yyyy-MM-dd}"),
            "Contrato", s.IdContrato, s.Inicio, referencia, s.ReferenciaRelacionOrdinaria);
        return Ordinario(s, concepto, fuente);
    }

    // El adaptador Forecast aporta su referencia proyectada, sin crear contratos/condiciones RRHH.
    public static LineaMonetaria Ordinario(SegmentoSalarialTemporal s, ConceptoMonetarioVersion concepto, FuenteLineaMonetaria fuente)
    {
        if (concepto.TipoBase != "SALARIO_DEVENGADO" || concepto.UnidadPermitida != "Monto"
            || ResolucionConceptos.Validar(concepto) != EfectoMonetario.IngresoSalarial)
            throw new ConfiguracionConceptoException("CONCEPTO_ORDINARIO_INCOMPATIBLE");
        if (!concepto.Vigencia.Contiene(new VigenciaFinanciera(s.Inicio, s.Fin))) throw new ConfiguracionConceptoException("CONCEPTO_CAMBIA_EN_SEGMENTO");
        if (fuente.IdColaborador != s.IdColaborador || fuente.Segmento is not { } referencia
            || referencia.Inicio != s.Inicio || referencia.Fin != s.Fin || !fuente.TieneRangoDevengo
            || referencia.IdContrato != s.IdContrato || referencia.IdCondicion != (s.IdCondicionLaboralVersion ?? 0)
            || fuente.TipoFuente == "Contrato" && s.IdCondicionLaboralVersion is not > 0)
            throw new ConfiguracionConceptoException("SEGMENTO_FUENTE_INCOMPATIBLE");
        return LineaMonetaria.Materializar(concepto, fuente,
            new(s.DiasBase30, "Monto", s.SalarioMensual / CalendarioSalarial.UnidadesPorMes, null, null,
                BasesConceptos.Resolver(concepto.TipoBase, s), s.SalarioMensual, "Día base 30"));
    }

    // Una ocurrencia explícita, nunca una repetición automática por segmento/período de un monto interno.
    public static LineaMonetaria Interno(ParametroInternoResuelto parametro, ConceptoMonetarioVersion concepto,
        FuenteLineaMonetaria fuente, SegmentoSalarialTemporal? segmento, string? autorizacion, bool exigirAutorizacion = true)
    {
        var efecto = ResolucionConceptos.Validar(concepto);
        if (!parametro.Version.Gobernado || !parametro.Version.Activo || parametro.Version.Vigencia is null
            || !parametro.Version.Vigencia.Contiene(fuente.FechaHecho)) throw new ConfiguracionConceptoException("PARAMETRO_NO_GOBERNADO");
        if (parametro.Version.EsNormativo || parametro.Version.IdConcepto != concepto.IdConcepto
            || efecto is EfectoMonetario.DeduccionLegal or EfectoMonetario.CargaPatronal or EfectoMonetario.ReduccionDevengo)
            throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
        if (parametro.Version.Naturaleza == "Deduccion" ? efecto != EfectoMonetario.DeduccionInterna
            : parametro.Version.Naturaleza != "Beneficio" || efecto is not (EfectoMonetario.IngresoSalarial or EfectoMonetario.IngresoNoSalarial))
            throw new ConfiguracionConceptoException("NATURALEZA_INTERNA_INCOMPATIBLE");
        if (exigirAutorizacion && string.IsNullOrWhiteSpace(autorizacion)) throw new ConfiguracionConceptoException("AUTORIZACION_FALTANTE");
        if (parametro.Version.Tipo != concepto.UnidadPermitida) throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        ResolucionParametrosInternos.ValidarValor(parametro.Version.Tipo, parametro.ValorDecimal, parametro.ValorTexto);
        var tipo = parametro.Version.Tipo;
        if (tipo is not ("Monto" or "Porcentaje")) throw new ConfiguracionConceptoException("MOTOR_PARAMETRO_NO_DISPONIBLE");
        if (tipo == "Monto" && (!concepto.PermiteMontoManual || concepto.TipoBase != "MONTO_AUTORIZADO"))
            throw new ConfiguracionConceptoException("MONTO_NO_PERMITIDO");
        if (tipo == "Porcentaje" && concepto.TipoBase != "SALARIO_DEVENGADO")
            throw new ConfiguracionConceptoException("BASE_PORCENTUAL_NO_DISPONIBLE");
        var valor = parametro.ValorDecimal!.Value;
        var baseCalculo = BasesConceptos.Resolver(concepto.TipoBase, segmento, tipo == "Monto" ? valor : null);
        // Porcentaje interno conserva la unidad histórica 0..100; Tasa es fracción exacta, sin R2.
        decimal? tasa = tipo == "Porcentaje" ? valor / 100m : null;
        return LineaMonetaria.Materializar(concepto, fuente,
            new(null, tipo, baseCalculo, tasa, null, tasa is { } p ? baseCalculo * p : valor), autorizacion, parametro);
    }
}

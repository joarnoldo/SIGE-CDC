namespace SIGECDC.Domain.Planillas;

// Identidades del catálogo aprobado en C. No contiene tasas, mínimos ni cifras anuales.
public sealed record ComponenteCuota(string Codigo, string Sujeto, string CodigoBase, string? CodigoMinimo = null);
public static class CatalogoCuotasSociales
{
    public static IReadOnlyList<ComponenteCuota> Componentes { get; } = Array.AsReadOnly(new ComponenteCuota[]
    {
        new("SEM_TRABAJADOR", "Trabajador", "BASE_SEM", "BMC_SEM"),
        new("SEM_PATRONO", "Patrono", "BASE_SEM", "BMC_SEM"),
        new("IVM_TRABAJADOR", "Trabajador", "BASE_IVM", "BMC_IVM"),
        new("IVM_PATRONO", "Patrono", "BASE_IVM", "BMC_IVM"),
        new("IVM_ESTADO", "Estado", "BASE_IVM", "BMC_IVM"),
        new("BPOP_PATRONO", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("ASIGNACIONES_FAMILIARES", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("IMAS", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("INA", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("LPT_BPOP_PATRONO", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("LPT_BPOP_TRABAJADOR", "Trabajador", "REMUNERACION_CONTRIBUTIVA"),
        new("LPT_FCL", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("LPT_OPC", "Patrono", "REMUNERACION_CONTRIBUTIVA"),
        new("LPT_INS", "Patrono", "REMUNERACION_CONTRIBUTIVA")
    });
    public static IReadOnlyList<string> Claves { get; } = Array.AsReadOnly(Componentes.Select(c => c.Codigo)
        .Concat(Componentes.Select(c => c.CodigoMinimo).OfType<string>()).Distinct(StringComparer.Ordinal).ToArray());
}

public sealed record ValorCuotaVersionado(long IdParametroNormativo, long IdValorParametroNormativo, long IdVersionNormativa,
    string Codigo, string Institucion, string Sujeto, string Unidad, string CodigoBase, string ClaveAplicacion,
    VigenciaFinanciera Vigencia, VigenciaFinanciera VigenciaNormativa, decimal Valor,
    bool Activo, bool Revisado, string FuenteOficial, string HuellaEvidencia);

public enum ReglaBaseCuota { Desconocida, RemuneracionContributiva, MinimoMensualOrdinario, ExcepcionSinMinimo }

// Evidencia empresarial explícita del adaptador autorizado; nunca se deriva de la ausencia de datos.
// La referencia de regla respalda la aplicabilidad, no sustituye tasas ni habilita overrides normativos.
public sealed record AplicabilidadCuota(long IdColaborador, string Componente, string ClaveAplicacion,
    VigenciaFinanciera Vigencia, ReglaBaseCuota Regla, string ReferenciaSoporte, string ReferenciaRegla, string ConfirmadoPor);
public sealed record AporteBaseCuota(FuenteLineaMonetaria Fuente, long IdConceptoVersion, decimal MontoConSigno);
public sealed record AplicacionCuota(LineaMonetaria Linea, ValorCuotaVersionado Tasa, ValorCuotaVersionado? Minimo,
    VigenciaFinanciera Rango, decimal BaseContributiva, decimal BaseAplicada, AplicabilidadCuota Aplicabilidad,
    IReadOnlyList<AporteBaseCuota> Aportes);

// Evidencia explícita del primer corte; nunca se reconstruye desde totales opacos ni campos vivos.
public sealed record AcumuladoMensualCuotas(DateTime InicioMes, DateTime Hasta, IReadOnlyList<LineaMonetaria> Lineas,
    IReadOnlyList<AplicacionCuota> CuotasPrevias, string ReferenciaSoporte, IReadOnlyList<long>? Colaboradores = null);
// La línea del corte es una diferencia de importes materializados, no una tasa/base ficticia.
// Las aplicaciones conservan cada versión y base de la obligación mensual y del corte anterior.
public sealed record RegularizacionCuota(LineaMonetaria Linea, DateTime InicioMes, VigenciaFinanciera Corte,
    IReadOnlyList<AplicacionCuota> ObligacionMensual, IReadOnlyList<AplicacionCuota> CuotasPrevias)
{
    public decimal TotalMensual => ObligacionMensual.Sum(a => a.Linea.Monto);
    public decimal TotalPrevio => CuotasPrevias.Sum(a => a.Linea.Monto);
}
public sealed record ResultadoCuotasSociales(ConciliacionLineas? Conciliacion, IReadOnlyList<AplicacionCuota> Aplicaciones,
    IReadOnlyList<ValorCuotaVersionado> NormativaEstado, IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    public bool EsResoluble => Bloqueos.Count == 0 && Conciliacion is not null;
    public bool EsAutorizable => false;
    public string Estado => EsResoluble ? "INCOMPLETO" : "BLOQUEADO";
    public DateTime? InicioMes { get; init; }
    public VigenciaFinanciera? Corte { get; init; }
    public bool EsProvisional { get; init; }
    public IReadOnlyList<RegularizacionCuota> Regularizaciones { get; init; } = [];
    public IEnumerable<LineaMonetaria> LineasCuotasCorte => Regularizaciones.Count > 0
        ? Regularizaciones.Select(r => r.Linea) : Aplicaciones.Select(a => a.Linea);
    public decimal CuotasObrerasCCSSLPT => LineasCuotasCorte.Where(l => l.Efecto == EfectoMonetario.DeduccionLegal).Sum(l => l.Monto);
    public decimal CargasPatronalesCCSSLPT => LineasCuotasCorte.Where(l => l.Efecto == EfectoMonetario.CargaPatronal).Sum(l => l.Monto);
}

public static class ResolucionValoresCuotas
{
    public static ValorCuotaVersionado Resolver(string codigo, string clave, DateTime fecha,
        IReadOnlyList<ValorCuotaVersionado> valores)
    {
        var candidatos = valores.Where(v => v.Codigo == codigo && v.ClaveAplicacion == clave).ToArray();
        foreach (var v in candidatos) { v.Vigencia.Validar(); v.VigenciaNormativa.Validar(); }
        var filas = candidatos.Where(v => v.Vigencia.Contiene(fecha)).ToArray();
        if (filas.Length != 1) throw new ConfiguracionConceptoException(filas.Length == 0 ? "NORMATIVA_FALTANTE" : "NORMATIVA_AMBIGUA");
        var n = filas[0];
        if (!n.Activo) throw new ConfiguracionConceptoException("NORMATIVA_INACTIVA");
        if (!n.Revisado || n.IdParametroNormativo <= 0 || n.IdValorParametroNormativo <= 0 || n.IdVersionNormativa <= 0
            || string.IsNullOrWhiteSpace(n.FuenteOficial) || n.HuellaEvidencia.Length != 64 || !n.HuellaEvidencia.All(Uri.IsHexDigit))
            throw new ConfiguracionConceptoException("NORMATIVA_EVIDENCIA_INVALIDA");
        if (!n.VigenciaNormativa.Contiene(n.Vigencia)) throw new ConfiguracionConceptoException("NORMATIVA_FUERA_VIGENCIA");
        if (n.Institucion != "CCSS") throw new ConfiguracionConceptoException("INSTITUCION_CUOTA_INCOMPATIBLE");
        var componente = CatalogoCuotasSociales.Componentes.SingleOrDefault(c => c.Codigo == codigo);
        var minimo = codigo is "BMC_SEM" or "BMC_IVM";
        var baseEsperada = minimo ? codigo == "BMC_SEM" ? "BASE_SEM" : "BASE_IVM" : componente?.CodigoBase;
        if (baseEsperada is null || n.CodigoBase != baseEsperada) throw new ConfiguracionConceptoException("BASE_CUOTA_DESCONOCIDA");
        if (n.Sujeto != (minimo ? "Empresa" : componente!.Sujeto)) throw new ConfiguracionConceptoException("SUJETO_CUOTA_INCOMPATIBLE");
        if (n.Unidad != (minimo ? "CRC_Mensual" : "Proporcion")) throw new ConfiguracionConceptoException("UNIDAD_NORMATIVA_INCOMPATIBLE");
        if (n.Valor < 0 || (minimo ? n.Valor == 0 : n.Valor > 1)) throw new ConfiguracionConceptoException("VALOR_NORMATIVO_INVALIDO");
        return n;
    }

    public static IEnumerable<DateTime> Fronteras(IEnumerable<VigenciaFinanciera> vigencias, DateTime fin)
        => vigencias.SelectMany(v => v.Fin is { } f && f < fin ? new[] { v.Inicio, f.AddDays(1) } : new[] { v.Inicio });
}

public static class ProductorCuotasSociales
{
    public static ResultadoCuotasSociales Producir(PeriodoSalarial periodo, IReadOnlyList<LineaMonetaria> entradas,
        IReadOnlyList<ValorCuotaVersionado> valores, IReadOnlyList<ConceptoMonetarioVersion> conceptos,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, IReadOnlyList<BloqueoConcepto>? bloqueosPrevios = null,
        AcumuladoMensualCuotas? acumulado = null)
    {
        try
        {
            periodo.Validar();
            if (bloqueosPrevios?.Count > 0) return Fallido(bloqueosPrevios);
            ValidarEntradas(periodo, entradas);
            if (periodo.Tipo != TiposPeriodoPlanilla.Quincenal || periodo.Inicio.Day == 1)
            {
                if (acumulado is not null) throw new ConfiguracionConceptoException("ACUMULADO_CCSS_FUERA_CORTE_FINAL");
                return CalcularObligacion(periodo, entradas, valores, conceptos, aplicabilidades,
                    aplicarMinimo: periodo.Tipo != TiposPeriodoPlanilla.Quincenal);
            }
            return Regularizar(periodo, entradas, valores, conceptos, aplicabilidades, acumulado);
        }
        catch (ConfiguracionConceptoException e) { return Fallido([new(e.Codigo, "CCSS/LPT", periodo.Inicio)]); }
        catch (ArgumentException) { return Fallido([new("VIGENCIA_CUOTA_INVALIDA", "CCSS/LPT", periodo.Inicio)]); }
        catch (OverflowException) { return Fallido([new("DESBORDAMIENTO_MONETARIO", "CCSS/LPT", periodo.Inicio)]); }
    }

    private static ResultadoCuotasSociales CalcularObligacion(PeriodoSalarial periodo, IReadOnlyList<LineaMonetaria> entradas,
        IReadOnlyList<ValorCuotaVersionado> valores, IReadOnlyList<ConceptoMonetarioVersion> conceptos,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, bool aplicarMinimo)
    {
        var bloqueos = new List<BloqueoConcepto>();
        var aplicaciones = new List<AplicacionCuota>(); var estado = new List<ValorCuotaVersionado>();
        void Falta(string codigo, string clave, DateTime fecha, long? persona = null) => bloqueos.Add(new(codigo, clave, fecha, persona));
        try
        {
            periodo.Validar();
            var lineas = AgregadorLineasMonetarias.Conciliar(entradas).Lineas;
            if (lineas.Count == 0) Falta("BASE_CONTRIBUTIVA_NO_DEMOSTRABLE", "CCSS/LPT", periodo.Inicio);
            foreach (var l in lineas)
            {
                ResolucionConceptos.Validar(l.Concepto);
                if (l.Fuente.TipoFuente == "Normativa") Falta("CUOTAS_YA_APORTADAS", l.Fuente.ClaveHecho, l.Fuente.FechaHecho);
                if (l.Fuente.FechaHecho < periodo.Inicio || l.Fuente.FechaHecho > periodo.Fin)
                    Falta("LINEA_FUERA_PERIODO", l.Fuente.ClaveHecho, l.Fuente.FechaHecho, l.Fuente.IdColaborador);
            }
            if (bloqueos.Count > 0) return Fallido(bloqueos);
            foreach (var persona in lineas.GroupBy(l => l.Fuente.IdColaborador))
            foreach (var componente in CatalogoCuotasSociales.Componentes)
            {
                var pruebas = aplicabilidades.Where(a => a.IdColaborador == persona.Key && a.Componente == componente.Codigo).ToArray();
                var claves = componente.Sujeto == "Estado" ? new[] { "ORDINARIO" } : pruebas.Select(p => p.ClaveAplicacion).Distinct().ToArray();
                var relevantes = valores.Where(v => (v.Codigo == componente.Codigo || v.Codigo == componente.CodigoMinimo)
                    && claves.Contains(v.ClaveAplicacion, StringComparer.Ordinal)).ToArray();
                var cs = conceptos.Where(c => c.Codigo == componente.Codigo).ToArray();
                var fronteras = ResolucionValoresCuotas.Fronteras(relevantes.SelectMany(v => new[] { v.Vigencia, v.VigenciaNormativa })
                    .Concat(pruebas.Select(a => a.Vigencia)).Concat(cs.Select(c => c.Vigencia)), periodo.Fin);
                var rangos = CalendarioSalarial.Dividir(new(periodo.Inicio, periodo.Fin), fronteras);
                foreach (var rango in rangos)
                {
                    try
                    {
                        // Estado se conserva como referencia; no exige ni genera una obligación de la empresa.
                        if (componente.Sujeto == "Estado")
                        {
                            estado.Add(ResolucionValoresCuotas.Resolver(componente.Codigo, "ORDINARIO", rango.Inicio, valores));
                            continue;
                        }
                        foreach (var a in pruebas) a.Vigencia.Validar();
                        var aplicables = pruebas.Where(a => a.Vigencia.Contiene(rango)).ToArray();
                        if (aplicables.Length != 1) throw new ConfiguracionConceptoException(aplicables.Length == 0 ? "APLICABILIDAD_CUOTA_NO_DEMOSTRABLE" : "APLICABILIDAD_CUOTA_AMBIGUA");
                        var prueba = aplicables[0];
                        if (prueba.Regla == ReglaBaseCuota.Desconocida || !Enum.IsDefined(prueba.Regla)
                            || string.IsNullOrWhiteSpace(prueba.ReferenciaSoporte) || string.IsNullOrWhiteSpace(prueba.ReferenciaRegla)
                            || string.IsNullOrWhiteSpace(prueba.ConfirmadoPor)) throw new ConfiguracionConceptoException("APLICABILIDAD_CUOTA_NO_DEMOSTRABLE");
                        // Cada requisito se diagnostica independientemente: una huella alterada de BMC
                        // no debe ocultar un solape de tasas, ni una tasa alterada ocultar la BMC faltante.
                        ValorCuotaVersionado? Requisito(string codigo)
                        {
                            try { return ResolucionValoresCuotas.Resolver(codigo, prueba.ClaveAplicacion, rango.Inicio, valores); }
                            catch (ConfiguracionConceptoException e) { Falta(e.Codigo, codigo, rango.Inicio, persona.Key); return null; }
                        }
                        var minimo = componente.CodigoMinimo is { } codigoMinimo
                            ? Requisito(codigoMinimo) : null;
                        var tasa = Requisito(componente.Codigo);
                        if (tasa is null || componente.CodigoMinimo is not null && minimo is null) continue;
                        var concepto = ResolucionConceptos.Resolver(componente.Codigo, rango.Inicio, conceptos);
                        if (concepto.TipoBase != tasa.CodigoBase || concepto.Clasificacion != (tasa.Sujeto == "Trabajador" ? "DeduccionLegal" : "CargaPatronal")
                            || concepto.PermiteMontoManual || concepto.RequiereAutorizacion)
                            throw new ConfiguracionConceptoException("CONCEPTO_CUOTA_INCOMPATIBLE");
                        var aportes = BaseContributiva(persona, rango);
                        var contributiva = aportes.Sum(a => a.MontoConSigno);
                        if (contributiva < 0) throw new ConfiguracionConceptoException("BASE_CONTRIBUTIVA_NEGATIVA");
                        var aplicada = contributiva;
                        if (minimo is not null)
                        {
                            if (prueba.Regla == ReglaBaseCuota.MinimoMensualOrdinario)
                            {
                                var mensual = BaseContributiva(persona, new(periodo.Inicio, periodo.Fin)).Sum(a => a.MontoConSigno);
                                // G-DEC-01: Q1 no eleva la base. G-DEC-02: sin ajuste mensual,
                                // cada vigencia usa su base real aunque ese tramo sea inferior a BMC.
                                if (aplicarMinimo && mensual < minimo.Valor)
                                {
                                    if (rangos.Count != 1)
                                        throw new ConfiguracionConceptoException("DISTRIBUCION_BMC_MULTIVIGENCIA_NO_SOPORTADA");
                                    aplicada = minimo.Valor;
                                }
                            }
                            else if (prueba.Regla != ReglaBaseCuota.ExcepcionSinMinimo)
                                throw new ConfiguracionConceptoException("APLICABILIDAD_BMC_NO_DEMOSTRABLE");
                        }
                        else if (prueba.Regla != ReglaBaseCuota.RemuneracionContributiva)
                            throw new ConfiguracionConceptoException("REGLA_BASE_CUOTA_INCOMPATIBLE");
                        var clave = FormattableString.Invariant($"CUOTA:{componente.Codigo}:{prueba.ClaveAplicacion}:{rango.Inicio:yyyyMMdd}:{rango.Fin:yyyyMMdd}");
                        var linea = LineaMonetaria.Materializar(concepto,
                            new(persona.Key, clave, "Normativa", tasa.IdValorParametroNormativo, rango.Inicio, null, prueba.ReferenciaSoporte),
                            new(null, "Porcentaje", aplicada, tasa.Valor, null, aplicada * tasa.Valor));
                        aplicaciones.Add(new(linea, tasa, minimo, rango, contributiva, aplicada, prueba, aportes));
                    }
                    catch (ConfiguracionConceptoException e) { Falta(e.Codigo, componente.Codigo, rango.Inicio, persona.Key); }
                }
            }
            if (bloqueos.Count > 0) return Fallido(bloqueos);
            var ordenadas = aplicaciones.OrderBy(a => a.Linea.Fuente.IdColaborador).ThenBy(a => a.Rango.Inicio)
                .ThenBy(a => a.Tasa.Codigo, StringComparer.Ordinal).ToArray();
            return new(AgregadorLineasMonetarias.Conciliar(lineas.Concat(ordenadas.Select(a => a.Linea))), Array.AsReadOnly(ordenadas),
                Array.AsReadOnly(estado.Distinct().OrderBy(n => n.Vigencia.Inicio).ThenBy(n => n.Codigo, StringComparer.Ordinal).ToArray()), [])
            { InicioMes = new(periodo.Inicio.Year, periodo.Inicio.Month, 1), Corte = new(periodo.Inicio, periodo.Fin), EsProvisional = !aplicarMinimo };
        }
        catch (ConfiguracionConceptoException e) { Falta(e.Codigo, "CCSS/LPT", periodo.Inicio); }
        catch (ArgumentException) { Falta("VIGENCIA_CUOTA_INVALIDA", "CCSS/LPT", periodo.Inicio); }
        catch (OverflowException) { Falta("DESBORDAMIENTO_MONETARIO", "CCSS/LPT", periodo.Inicio); }
        return Fallido(bloqueos);
    }

    private static void ValidarEntradas(PeriodoSalarial periodo, IReadOnlyList<LineaMonetaria> entradas)
    {
        var lineas = AgregadorLineasMonetarias.Conciliar(entradas).Lineas;
        foreach (var l in lineas)
        {
            ResolucionConceptos.Validar(l.Concepto);
            if (l.Fuente.TipoFuente is "Normativa" or "Derivado") throw new ConfiguracionConceptoException("CUOTAS_YA_APORTADAS");
            if (l.Fuente.FechaHecho < periodo.Inicio || l.Fuente.FechaHecho > periodo.Fin
                || l.Fuente.TieneRangoDevengo && (l.Fuente.Segmento is not { } s
                    || !new VigenciaFinanciera(periodo.Inicio, periodo.Fin).Contiene(new VigenciaFinanciera(s.Inicio, s.Fin))))
                throw new ConfiguracionConceptoException("LINEA_FUERA_PERIODO");
        }
    }

    private static ResultadoCuotasSociales Regularizar(PeriodoSalarial corte, IReadOnlyList<LineaMonetaria> entradas,
        IReadOnlyList<ValorCuotaVersionado> valores, IReadOnlyList<ConceptoMonetarioVersion> conceptos,
        IReadOnlyList<AplicabilidadCuota> pruebas, AcumuladoMensualCuotas? acumulado)
    {
        var inicio = new DateTime(corte.Inicio.Year, corte.Inicio.Month, 1);
        const string falta = "ACUMULADO_MENSUAL_CCSS_NO_DEMOSTRABLE";
        if (acumulado is null || acumulado.InicioMes != inicio || acumulado.Hasta != corte.Inicio.AddDays(-1)
            || string.IsNullOrWhiteSpace(acumulado.ReferenciaSoporte) || acumulado.Colaboradores is not { Count: > 0 }
            || acumulado.Colaboradores.Any(id => id <= 0) || acumulado.Colaboradores.Distinct().Count() != acumulado.Colaboradores.Count
            || !acumulado.Lineas.Select(l => l.Fuente.IdColaborador).Distinct().Order().SequenceEqual(acumulado.Colaboradores.Order())
            || entradas.Any(l => !acumulado.Colaboradores.Contains(l.Fuente.IdColaborador)))
            throw new ConfiguracionConceptoException(falta);
        var q1 = new PeriodoSalarial(inicio, acumulado.Hasta, TiposPeriodoPlanilla.Quincenal);
        ValidarEntradas(q1, acumulado.Lineas);
        // Se verifica el detalle previo con las mismas versiones, nunca se sustituye el importe ya pagado.
        var previoVerificado = CalcularObligacion(q1, acumulado.Lineas, valores, conceptos, pruebas, aplicarMinimo: false);
        if (!previoVerificado.EsResoluble) return previoVerificado;
        var anteriores = Ordenar(acumulado.CuotasPrevias);
        var verificadas = Ordenar(previoVerificado.Aplicaciones);
        if (anteriores.Length != verificadas.Length || anteriores.Where((a, i) => !MismaAplicacion(a, verificadas[i])).Any())
            throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_CCSS_INCONSISTENTE");
        anteriores = verificadas; // Mismo contenido validado, orden canónico independiente del transporte.
        var mensual = CalcularObligacion(new(inicio, corte.Fin, TiposPeriodoPlanilla.Mensual),
            acumulado.Lineas.Concat(entradas).ToArray(), valores, conceptos, pruebas, aplicarMinimo: true);
        if (!mensual.EsResoluble) return mensual;
        var regularizaciones = new List<RegularizacionCuota>();
        foreach (var grupo in mensual.Aplicaciones.GroupBy(a => (a.Linea.Fuente.IdColaborador, a.Tasa.Codigo))
            .OrderBy(g => g.Key.IdColaborador).ThenBy(g => g.Key.Codigo, StringComparer.Ordinal))
        {
            var previas = anteriores.Where(a => a.Linea.Fuente.IdColaborador == grupo.Key.IdColaborador && a.Tasa.Codigo == grupo.Key.Codigo).ToArray();
            var diferencia = grupo.Sum(a => a.Linea.Monto) - previas.Sum(a => a.Linea.Monto);
            if (diferencia < 0) return Fallido([new("REGULARIZACION_CCSS_NEGATIVA_REQUIERE_CORRECCION",
                grupo.Key.Codigo, corte.Inicio, grupo.Key.IdColaborador)]);
            var concepto = ResolucionConceptos.Resolver(grupo.Key.Codigo, corte.Fin, conceptos);
            var clave = FormattableString.Invariant($"REGULARIZACION_CCSS:{grupo.Key.Codigo}:{inicio:yyyyMM}:{corte.Inicio:yyyyMMdd}");
            var linea = LineaMonetaria.Materializar(concepto,
                new(grupo.Key.IdColaborador, clave, "Derivado", null, corte.Fin, null, acumulado.ReferenciaSoporte),
                new(null, "Porcentaje", null, null, null, diferencia));
            regularizaciones.Add(new(linea, inicio, new(corte.Inicio, corte.Fin), Array.AsReadOnly(grupo.ToArray()), Array.AsReadOnly(previas)));
        }
        return mensual with
        {
            Corte = new(corte.Inicio, corte.Fin),
            Regularizaciones = Array.AsReadOnly(regularizaciones.ToArray()),
            Conciliacion = AgregadorLineasMonetarias.Conciliar(entradas.Concat(regularizaciones.Select(r => r.Linea)))
        };
    }

    private static AplicacionCuota[] Ordenar(IEnumerable<AplicacionCuota> aplicaciones) => aplicaciones
        .OrderBy(a => a.Linea.Fuente.IdColaborador).ThenBy(a => a.Tasa.Codigo, StringComparer.Ordinal).ThenBy(a => a.Rango.Inicio).ToArray();
    private static bool MismaAplicacion(AplicacionCuota a, AplicacionCuota b)
        => a.Tasa == b.Tasa && a.Minimo == b.Minimo && a.Rango == b.Rango && a.BaseContributiva == b.BaseContributiva
            && a.BaseAplicada == b.BaseAplicada && a.Aplicabilidad == b.Aplicabilidad
            && a.Aportes.Count == b.Aportes.Count && a.Aportes.ToHashSet().SetEquals(b.Aportes)
            && a.Linea.Concepto == b.Linea.Concepto && a.Linea.Fuente == b.Linea.Fuente && a.Linea.Factores == b.Linea.Factores
            && a.Linea.Monto == b.Linea.Monto && a.Linea.Efecto == b.Linea.Efecto
            && a.Linea.ReferenciaAutorizacion == b.Linea.ReferenciaAutorizacion && a.Linea.ParametroAplicado == b.Linea.ParametroAplicado;

    private static IReadOnlyList<AporteBaseCuota> BaseContributiva(IEnumerable<LineaMonetaria> lineas, VigenciaFinanciera rango)
    {
        var aportes = new List<AporteBaseCuota>();
        foreach (var l in lineas.Where(l => l.Concepto.EsContributivoCCSS == true))
        {
            // Ordinario: el productor E ya debe recibir fronteras normativas antes de materializar.
            // Incidencia: fecha del hecho confirmado, no el rango usado como referencia de su base.
            var vigencia = l.Fuente.TieneRangoDevengo
                ? l.Fuente.Segmento is { } s ? new VigenciaFinanciera(s.Inicio, s.Fin)
                    : throw new ConfiguracionConceptoException("BASE_TEMPORAL_NO_DEMOSTRABLE")
                : l.Fuente.TipoFuente == "Incidencia" ? new(l.Fuente.FechaHecho, l.Fuente.FechaHecho)
                : throw new ConfiguracionConceptoException("BASE_TEMPORAL_NO_DEMOSTRABLE");
            vigencia.Validar();
            if (!rango.Solapa(vigencia)) continue;
            if (!rango.Contiene(vigencia)) throw new ConfiguracionConceptoException("BASE_ENTRE_VERSIONES_NO_DEMOSTRABLE");
            var signo = l.Efecto switch
            {
                EfectoMonetario.IngresoSalarial or EfectoMonetario.IngresoNoSalarial => 1,
                EfectoMonetario.ReduccionDevengo => -1,
                _ => throw new ConfiguracionConceptoException("EFECTO_CONTRIBUTIVO_INCOMPATIBLE")
            };
            aportes.Add(new(l.Fuente, l.Concepto.IdVersion, signo * l.Monto));
        }
        return Array.AsReadOnly(aportes.ToArray());
    }

    public static ResultadoCuotasSociales Fallido(IEnumerable<BloqueoConcepto> bloqueos)
        => new(null, [], [], Array.AsReadOnly(bloqueos.Distinct().OrderBy(b => b.IdColaborador).ThenBy(b => b.Fecha)
            .ThenBy(b => b.Clave, StringComparer.Ordinal).ThenBy(b => b.Codigo, StringComparer.Ordinal).ToArray()));
}

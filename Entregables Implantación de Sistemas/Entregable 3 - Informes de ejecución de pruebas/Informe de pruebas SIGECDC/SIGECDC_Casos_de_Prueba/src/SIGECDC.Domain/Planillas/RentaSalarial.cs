namespace SIGECDC.Domain.Planillas;

public sealed record TramoRentaVersionado(long IdTramo, long IdVersionNormativa, int Orden,
    decimal Inferior, decimal? Superior, decimal Tasa);
public sealed record CreditoRentaVersionado(long IdCreditoNormativo, long IdVersionNormativa, string CodigoTipo,
    decimal MontoUnitario, VigenciaFinanciera Vigencia, string Requisitos, bool Activo);
public sealed record VersionFiscalRenta(long IdVersionNormativa, string Codigo, string Institucion,
    VigenciaFinanciera Vigencia, string FuenteOficial, string HuellaEvidencia, bool Activa, bool Revisada,
    IReadOnlyList<TramoRentaVersionado> Tramos, IReadOnlyList<CreditoRentaVersionado> Creditos);
public sealed record AcreditacionRenta(long IdCreditoColaborador, long IdColaborador, long IdCreditoNormativo,
    decimal Cantidad, VigenciaFinanciera Vigencia, string ReferenciaSoporte, string AprobadoPor,
    DateTime? FechaAprobacion, bool Activa, bool AprobacionDemostrada);
public sealed record AporteBaseRenta(FuenteLineaMonetaria Fuente, ConceptoMonetarioVersion Concepto, decimal MontoConSigno);
public sealed record AplicacionTramoRenta(TramoRentaVersionado Tramo, decimal BaseGravada, decimal ImpuestoSinRedondear);
public sealed record AplicacionCreditoRenta(CreditoRentaVersionado Normativo, AcreditacionRenta Personal, decimal Monto);
public sealed record EvaluacionRenta(long IdColaborador, VigenciaFinanciera RangoAcumulado, VersionFiscalRenta Version,
    decimal BaseTributaria, IReadOnlyList<AporteBaseRenta> Aportes, IReadOnlyList<AplicacionTramoRenta> Tramos,
    IReadOnlyList<AplicacionCreditoRenta> Creditos, LineaMonetaria ImpuestoAcumulado)
{
    public decimal ImpuestoAntesCreditos => Tramos.Sum(t => t.ImpuestoSinRedondear);
    public decimal CreditosValidos => Creditos.Sum(c => c.Monto);
}
public sealed record AplicacionRenta(LineaMonetaria Linea, EvaluacionRenta Evaluacion, EvaluacionRenta? EvaluacionPrevia)
{
    public decimal RetenidoPreviamente => EvaluacionPrevia?.ImpuestoAcumulado.Monto ?? 0;
    public decimal DiferenciaAntesLimiteCero => Evaluacion.ImpuestoAcumulado.Monto - RetenidoPreviamente;
}
// Detalle explícito del corte previo. El adaptador autorizado aportará evidencia real, no agregados legados.
public sealed record AcumuladoMensualRenta(DateTime InicioMes, DateTime Hasta, IReadOnlyList<long> Colaboradores,
    IReadOnlyList<LineaMonetaria> Lineas, IReadOnlyList<AplicacionRenta> Retenciones, string ReferenciaSoporte,
    AcumuladoMensualCuotas? CuotasSociales = null);
public sealed record ResultadoRentaSalarial(ConciliacionLineas? Conciliacion, IReadOnlyList<AplicacionRenta> Aplicaciones,
    IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    public bool EsResoluble => Bloqueos.Count == 0 && Conciliacion is not null;
    public bool EsAutorizable => false;
    public string Estado => EsResoluble ? "INCOMPLETO" : "BLOQUEADO";
    public decimal ImpuestoSalario => Aplicaciones.Sum(a => a.Linea.Monto);
    // Saldo de las líneas aportadas; el servicio persistente incluye G. Nunca autoriza pago/aprobación.
    public decimal? NetoPreliminar => Conciliacion?.Totales.SaldoAlgebraico;
    public ResultadoCuotasSociales? CuotasSociales { get; init; }
}

public static class ProductorRentaSalarial
{
    public static ResultadoRentaSalarial Producir(PeriodoSalarial periodo, IReadOnlyList<LineaMonetaria> entradas,
        IReadOnlyList<VersionFiscalRenta> versiones, IReadOnlyList<ConceptoMonetarioVersion> conceptos,
        IReadOnlyList<AcreditacionRenta> acreditaciones, AcumuladoMensualRenta? acumulado = null,
        IReadOnlyList<BloqueoConcepto>? bloqueosPrevios = null)
    {
        try
        {
            periodo.Validar();
            if (bloqueosPrevios?.Count > 0) return Fallido(bloqueosPrevios);
            var mes = new VigenciaFinanciera(new(periodo.Inicio.Year, periodo.Inicio.Month, 1),
                new DateTime(periodo.Inicio.Year, periodo.Inicio.Month, 1).AddMonths(1).AddDays(-1));
            var fiscal = ResolverVersion(mes, versiones);
            var actuales = ValidarLineas(entradas, new(periodo.Inicio, periodo.Fin));
            var finales = periodo.Tipo == TiposPeriodoPlanilla.Quincenal && periodo.Inicio.Day == 16;
            if (!finales && acumulado is not null) throw new ConfiguracionConceptoException("ACUMULADO_RENTA_FUERA_CORTE_FINAL");
            IReadOnlyList<LineaMonetaria> anteriores = [];
            var previas = new Dictionary<long, EvaluacionRenta>();
            if (finales)
            {
                ValidarIdentidadAcumulado(acumulado, mes.Inicio, periodo.Inicio.AddDays(-1), actuales);
                anteriores = ValidarLineas(acumulado!.Lineas, new(mes.Inicio, acumulado.Hasta));
                foreach (var persona in anteriores.GroupBy(l => l.Fuente.IdColaborador))
                {
                    var evaluacion = Evaluar(persona.Key, new(mes.Inicio, acumulado.Hasta), persona, fiscal, conceptos, acreditaciones);
                    var prev = acumulado.Retenciones.Where(a => a.Linea.Fuente.IdColaborador == persona.Key).ToArray();
                    if (prev.Length != 1 || prev[0].EvaluacionPrevia is not null || !MismaEvaluacion(evaluacion, prev[0].Evaluacion)
                        || !MismaLinea(prev[0].Linea, evaluacion.ImpuestoAcumulado))
                        throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_RENTA_INCONSISTENTE");
                    previas.Add(persona.Key, evaluacion);
                }
                if (acumulado.Retenciones.Count != previas.Count) throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_RENTA_INCONSISTENTE");
            }
            var todas = AgregadorLineasMonetarias.Conciliar(anteriores.Concat(actuales)).Lineas;
            if (todas.Count == 0) throw new ConfiguracionConceptoException("BASE_RENTA_NO_DEMOSTRABLE");
            var aplicaciones = new List<AplicacionRenta>();
            foreach (var persona in todas.GroupBy(l => l.Fuente.IdColaborador).OrderBy(g => g.Key))
            {
                var evaluacion = Evaluar(persona.Key, new(mes.Inicio, periodo.Fin), persona, fiscal, conceptos, acreditaciones);
                previas.TryGetValue(persona.Key, out var previa);
                var linea = evaluacion.ImpuestoAcumulado;
                if (finales)
                {
                    // La evaluación previa ya coincidió con sus líneas/versiones/créditos. El límite cero
                    // se aplica solo a entradas coherentes, sin ocultar alteraciones ni reescribir Q1.
                    var diferencia = Math.Max(0m, linea.Monto - previa!.ImpuestoAcumulado.Monto);
                    linea = LineaMonetaria.Materializar(linea.Concepto,
                        new(persona.Key, FormattableString.Invariant($"RETENCION_RENTA:{periodo.Inicio:yyyyMMdd}:{periodo.Fin:yyyyMMdd}"),
                            "Derivado", fiscal.IdVersionNormativa, periodo.Fin, null, acumulado!.ReferenciaSoporte),
                        new(null, "Monto", null, null, null, diferencia));
                }
                aplicaciones.Add(new(linea, evaluacion, previa));
            }
            var conciliacion = AgregadorLineasMonetarias.Conciliar(actuales.Concat(aplicaciones.Select(a => a.Linea)));
            var negativos = conciliacion.Lineas.GroupBy(l => l.Fuente.IdColaborador)
                .Where(g => AgregadorLineasMonetarias.Conciliar(g).Totales.SaldoAlgebraico < 0)
                .Select(g => new BloqueoConcepto("NETO_PRELIMINAR_NEGATIVO", "NETO", periodo.Fin, g.Key)).ToArray();
            if (negativos.Length > 0) return Fallido(negativos);
            return new(conciliacion, Array.AsReadOnly(aplicaciones.ToArray()), []);
        }
        catch (ConfiguracionConceptoException e) { return Fallido([new(e.Codigo, "RENTA", periodo.Inicio)]); }
        catch (ArgumentException) { return Fallido([new("VIGENCIA_RENTA_INVALIDA", "RENTA", periodo.Inicio)]); }
        catch (OverflowException) { return Fallido([new("DESBORDAMIENTO_MONETARIO", "RENTA", periodo.Inicio)]); }
    }

    public static VersionFiscalRenta ResolverVersion(VigenciaFinanciera mes, IReadOnlyList<VersionFiscalRenta> versiones)
    {
        foreach (var v in versiones) v.Vigencia.Validar();
        var aplicables = versiones.Where(v => v.Vigencia.Solapa(mes)).ToArray();
        if (aplicables.Length == 0) throw new ConfiguracionConceptoException("VERSION_FISCAL_AUSENTE");
        if (aplicables.Length != 1 || !aplicables[0].Vigencia.Contiene(mes))
            throw new ConfiguracionConceptoException("VERSION_FISCAL_MULTIPLE_O_MES_NO_CUBIERTO");
        var fiscal = aplicables[0];
        if (!fiscal.Activa) throw new ConfiguracionConceptoException("VERSION_FISCAL_INACTIVA");
        if (fiscal.IdVersionNormativa <= 0 || fiscal.Institucion != "HACIENDA" || !fiscal.Revisada
            || string.IsNullOrWhiteSpace(fiscal.FuenteOficial) || fiscal.HuellaEvidencia.Length != 64 || !fiscal.HuellaEvidencia.All(Uri.IsHexDigit))
            throw new ConfiguracionConceptoException("EVIDENCIA_FISCAL_INVALIDA");
        var tramos = fiscal.Tramos.OrderBy(t => t.Orden).ToArray();
        if (tramos.Length == 0) throw new ConfiguracionConceptoException("TRAMOS_RENTA_AUSENTES");
        if (tramos.Select(t => t.IdTramo).Distinct().Count() != tramos.Length) throw new ConfiguracionConceptoException("TRAMOS_RENTA_INVALIDOS");
        for (var i = 0; i < tramos.Length; i++)
        {
            var t = tramos[i];
            if (t.IdTramo <= 0 || t.IdVersionNormativa != fiscal.IdVersionNormativa || t.Orden != i + 1
                || t.Inferior != (i == 0 ? 0 : tramos[i - 1].Superior) || t.Tasa is < 0 or > 1
                || (i == tramos.Length - 1 ? t.Superior is not null : t.Superior is null || t.Superior <= t.Inferior))
                throw new ConfiguracionConceptoException("TRAMOS_RENTA_INVALIDOS");
        }
        if (fiscal.Creditos.GroupBy(c => c.CodigoTipo).Any(g => g.Count() != 1)
            || fiscal.Creditos.Select(c => c.IdCreditoNormativo).Distinct().Count() != fiscal.Creditos.Count)
            throw new ConfiguracionConceptoException("CREDITO_NORMATIVO_AMBIGUO");
        foreach (var c in fiscal.Creditos)
        {
            c.Vigencia.Validar();
            if (c.IdCreditoNormativo <= 0 || c.IdVersionNormativa != fiscal.IdVersionNormativa || !c.Activo || c.MontoUnitario < 0
                || string.IsNullOrWhiteSpace(c.CodigoTipo) || string.IsNullOrWhiteSpace(c.Requisitos) || !fiscal.Vigencia.Contiene(c.Vigencia))
                throw new ConfiguracionConceptoException("CREDITO_NORMATIVO_INVALIDO");
        }
        return fiscal with { Tramos = Array.AsReadOnly(tramos), Creditos = Array.AsReadOnly(fiscal.Creditos.OrderBy(c => c.CodigoTipo, StringComparer.Ordinal).ToArray()) };
    }

    private static EvaluacionRenta Evaluar(long persona, VigenciaFinanciera rango, IEnumerable<LineaMonetaria> entradas,
        VersionFiscalRenta fiscal, IReadOnlyList<ConceptoMonetarioVersion> conceptos, IReadOnlyList<AcreditacionRenta> acreditaciones)
    {
        var aportes = entradas.Where(l => l.Concepto.EsTributario == true).Select(l => new AporteBaseRenta(l.Fuente, l.Concepto,
            l.Efecto switch
            {
                EfectoMonetario.IngresoSalarial or EfectoMonetario.IngresoNoSalarial => l.Monto,
                EfectoMonetario.ReduccionDevengo => -l.Monto,
                _ => throw new ConfiguracionConceptoException("EFECTO_TRIBUTARIO_INCOMPATIBLE")
            })).ToArray();
        var baseRenta = aportes.Sum(a => a.MontoConSigno);
        if (baseRenta < 0) throw new ConfiguracionConceptoException("BASE_RENTA_NEGATIVA");
        var personales = acreditaciones.Where(a => a.IdColaborador == persona).ToArray();
        foreach (var a in personales) a.Vigencia.Validar();
        personales = personales.Where(a => a.Vigencia.Solapa(rango)).ToArray();
        var creditos = new List<AplicacionCreditoRenta>();
        foreach (var a in personales)
        {
            if (!a.Activa || !a.AprobacionDemostrada || a.IdCreditoColaborador <= 0 || a.Cantidad <= 0
                || string.IsNullOrWhiteSpace(a.ReferenciaSoporte) || string.IsNullOrWhiteSpace(a.AprobadoPor)
                || a.FechaAprobacion is null || a.FechaAprobacion == default(DateTime))
                throw new ConfiguracionConceptoException("CREDITO_PERSONAL_NO_ACREDITADO");
            var c = fiscal.Creditos.SingleOrDefault(c => c.IdCreditoNormativo == a.IdCreditoNormativo)
                ?? throw new ConfiguracionConceptoException("CREDITO_PERSONAL_VERSION_INCOMPATIBLE");
            if (!c.Vigencia.Contiene(a.Vigencia) || !a.Vigencia.Contiene(rango))
                throw new ConfiguracionConceptoException("VIGENCIA_CREDITO_MENSUAL_NO_DEMOSTRABLE");
            creditos.Add(new(c, a, a.Cantidad * c.MontoUnitario));
        }
        if (creditos.GroupBy(c => c.Normativo.CodigoTipo).Any(g => g.Count() != 1))
            throw new ConfiguracionConceptoException("CREDITO_PERSONAL_AMBIGUO");
        var tramos = fiscal.Tramos.Select(t =>
        {
            var gravado = Math.Max(0m, Math.Min(baseRenta, t.Superior ?? baseRenta) - t.Inferior);
            return new AplicacionTramoRenta(t, gravado, gravado * t.Tasa);
        }).ToArray();
        var impuesto = Math.Max(0m, tramos.Sum(t => t.ImpuestoSinRedondear) - creditos.Sum(c => c.Monto));
        var concepto = ResolucionConceptos.Resolver("IMPUESTO_SALARIO", rango.Fin!.Value, conceptos);
        if (concepto.TipoBase != "RENTA_SALARIAL" || concepto.Clasificacion != "DeduccionLegal" || concepto.UnidadPermitida != "Monto"
            || concepto.PermiteMontoManual || concepto.RequiereAutorizacion || !concepto.Vigencia.Contiene(rango))
            throw new ConfiguracionConceptoException("CONCEPTO_RENTA_INCOMPATIBLE");
        var linea = LineaMonetaria.Materializar(concepto,
            new(persona, FormattableString.Invariant($"RENTA_ACUMULADA:{rango.Inicio:yyyyMMdd}:{rango.Fin:yyyyMMdd}"),
                "Normativa", fiscal.IdVersionNormativa, rango.Fin.Value, null, fiscal.FuenteOficial),
            new(null, "Monto", baseRenta, null, null, impuesto));
        return new(persona, rango, fiscal, baseRenta, Array.AsReadOnly(aportes), Array.AsReadOnly(tramos),
            Array.AsReadOnly(creditos.OrderBy(c => c.Normativo.CodigoTipo, StringComparer.Ordinal).ToArray()), linea);
    }

    private static IReadOnlyList<LineaMonetaria> ValidarLineas(IEnumerable<LineaMonetaria> entradas, VigenciaFinanciera rango)
    {
        var lineas = AgregadorLineasMonetarias.Conciliar(entradas).Lineas;
        foreach (var l in lineas)
        {
            ResolucionConceptos.Validar(l.Concepto);
            if (l.Concepto.TipoBase == "RENTA_SALARIAL") throw new ConfiguracionConceptoException("RENTA_YA_APORTADA");
            if (!rango.Contiene(l.Fuente.FechaHecho) || l.Fuente.TieneRangoDevengo
                && (l.Fuente.Segmento is not { } s || !rango.Contiene(new VigenciaFinanciera(s.Inicio, s.Fin))))
                throw new ConfiguracionConceptoException("LINEA_RENTA_FUERA_PERIODO");
        }
        return lineas;
    }

    private static void ValidarIdentidadAcumulado(AcumuladoMensualRenta? a, DateTime mes, DateTime hasta, IReadOnlyList<LineaMonetaria> actuales)
    {
        if (a is null || a.InicioMes != mes || a.Hasta != hasta || string.IsNullOrWhiteSpace(a.ReferenciaSoporte)
            || a.Colaboradores.Count == 0 || a.Colaboradores.Any(id => id <= 0) || a.Colaboradores.Distinct().Count() != a.Colaboradores.Count
            || !a.Lineas.Select(l => l.Fuente.IdColaborador).Distinct().Order().SequenceEqual(a.Colaboradores.Order())
            || actuales.Any(l => !a.Colaboradores.Contains(l.Fuente.IdColaborador)))
            throw new ConfiguracionConceptoException("ACUMULADO_MENSUAL_RENTA_NO_DEMOSTRABLE");
    }

    public static bool MismasLineas(IEnumerable<LineaMonetaria> a, IEnumerable<LineaMonetaria> b)
    {
        var x = AgregadorLineasMonetarias.Conciliar(a).Lineas; var y = AgregadorLineasMonetarias.Conciliar(b).Lineas;
        return x.Count == y.Count && !x.Where((l, i) => !MismaLinea(l, y[i])).Any();
    }
    private static bool MismaLinea(LineaMonetaria a, LineaMonetaria b) => a.Concepto == b.Concepto && a.Fuente == b.Fuente
        && a.Factores == b.Factores && a.Monto == b.Monto && a.Efecto == b.Efecto
        && a.ReferenciaAutorizacion == b.ReferenciaAutorizacion && a.ParametroAplicado == b.ParametroAplicado;
    private static bool MismaEvaluacion(EvaluacionRenta a, EvaluacionRenta b)
        => a.IdColaborador == b.IdColaborador && a.RangoAcumulado == b.RangoAcumulado && a.BaseTributaria == b.BaseTributaria
            && a.Version.IdVersionNormativa == b.Version.IdVersionNormativa && a.Version.Codigo == b.Version.Codigo
            && a.Version.Institucion == b.Version.Institucion && a.Version.Tramos.SequenceEqual(b.Version.Tramos)
            && a.Version.Creditos.SequenceEqual(b.Version.Creditos)
            && a.Version.Vigencia == b.Version.Vigencia && a.Version.HuellaEvidencia == b.Version.HuellaEvidencia
            && a.Version.FuenteOficial == b.Version.FuenteOficial && a.Version.Revisada == b.Version.Revisada && a.Version.Activa == b.Version.Activa
            && a.Tramos.SequenceEqual(b.Tramos) && a.Creditos.SequenceEqual(b.Creditos) && a.Aportes.SequenceEqual(b.Aportes)
            && MismaLinea(a.ImpuestoAcumulado, b.ImpuestoAcumulado);
    public static ResultadoRentaSalarial Fallido(IEnumerable<BloqueoConcepto> bloqueos)
        => new(null, [], Array.AsReadOnly(bloqueos.Distinct().OrderBy(b => b.IdColaborador).ThenBy(b => b.Fecha)
            .ThenBy(b => b.Codigo, StringComparer.Ordinal).ThenBy(b => b.Clave, StringComparer.Ordinal).ToArray()));
}

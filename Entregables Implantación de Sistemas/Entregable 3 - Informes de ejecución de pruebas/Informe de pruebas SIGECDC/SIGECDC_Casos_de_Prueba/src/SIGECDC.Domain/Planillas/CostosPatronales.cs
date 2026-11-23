namespace SIGECDC.Domain.Planillas;

public enum ModalidadBaseRiesgo { Desconocida, RemuneracionSalarialOrdinaria, Especial }
// Acreditación empresarial explícita del adaptador autorizado, como AplicabilidadCuota en G.
// No es una asignación: debe coincidir con el vínculo contractual gobernado y no aporta tarifas.
public sealed record AplicabilidadRiesgoTrabajo(long IdContrato, string NumeroPoliza, string CodigoActividad,
    VigenciaFinanciera Vigencia, ModalidadBaseRiesgo Modalidad, string ReferenciaSoporte, string ConfirmadoPor);
public sealed record AsignacionRiesgoContrato(long IdContrato, long IdColaborador, VigenciaFinanciera VigenciaContrato,
    long? IdConfiguracionAncla, string NumeroPoliza, string CodigoActividad, string ReferenciaSoporte,
    string AsociadoPor, bool Demostrada);
public sealed record RiesgoTrabajoVersionado(long IdConfiguracion, string NumeroPoliza, string CodigoActividad,
    string? DescripcionActividad, decimal? Tarifa, decimal Descuento, decimal Recargo, VigenciaFinanciera Vigencia,
    string ReferenciaDocumento, long? IdVersionNormativa, string CreadoPor, DateTime FechaCreacion,
    bool Activo, bool EvidenciaDemostrada)
{
    public decimal Factor => 1m - Descuento + Recargo;
}
public sealed record AporteBaseRiesgo(FuenteLineaMonetaria Fuente, long IdConceptoVersion, decimal MontoConSigno);
public sealed record AplicacionRiesgoTrabajo(LineaMonetaria Linea, RiesgoTrabajoVersionado Configuracion,
    AsignacionRiesgoContrato Asignacion, AplicabilidadRiesgoTrabajo Aplicabilidad, VigenciaFinanciera Rango,
    decimal BaseAplicada, IReadOnlyList<AporteBaseRiesgo> Aportes);
// Ocurrencia empresarial explícita, no carga automática por disponer de un concepto en el catálogo.
// Importe/porcentaje se aporta con soporte; no sustituye parámetros normativos ni reutiliza incidencias F.
public sealed record AutorizacionCostoHistorico(long IdConceptoDetallePlanilla, string Actor, string Referencia);
public sealed record ImporteHistoricoCostoForecast(decimal Promedio, decimal FactorTemporal,
    IReadOnlyList<long> Observaciones);
public sealed record HechoCostoPatronal(string ClaveHecho, long IdColaborador, long IdConceptoVersion,
    string CodigoConcepto, DateTime FechaHecho, string Unidad, decimal? Valor, string ReferenciaSoporte,
    string RegistradoPor, string? AutorizadoPor = null, string? ReferenciaAutorizacion = null,
    ReferenciaSegmentoTemporal? BaseTemporal = null,
    IReadOnlyList<AutorizacionCostoHistorico>? AutorizacionesHistoricasForecast = null,
    ImporteHistoricoCostoForecast? ImporteHistoricoForecast = null);
public sealed record ResultadoCostoLaboral(ConciliacionLineas? Conciliacion, ResultadoRentaSalarial? Renta,
    IReadOnlyList<AplicacionRiesgoTrabajo> AplicacionesRT, IReadOnlyList<LineaMonetaria> OtrosCostos,
    IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    public bool EsResoluble => Bloqueos.Count == 0 && Conciliacion is not null && Renta?.EsResoluble == true;
    public bool MotorMonetarioCompleto => EsResoluble;
    public bool EsAutorizable => false;
    public string Estado => EsResoluble ? "NO_PRODUCTIVO" : "BLOQUEADO";
    public decimal? BrutoSalarial => Conciliacion?.Totales.BrutoSalarial;
    public decimal? BrutoPagadero => Conciliacion?.Totales.BrutoPagadero;
    public decimal? CuotasObreras => Renta?.CuotasSociales?.CuotasObrerasCCSSLPT;
    public decimal? ImpuestoSalario => Renta?.ImpuestoSalario;
    public decimal? OtrasDeducciones => Renta?.Conciliacion?.Totales.DeduccionesInternas;
    public decimal? NetoPreliminar => Renta?.NetoPreliminar;
    public decimal? CargasPatronalesCCSSLPT => Renta?.CuotasSociales?.CargasPatronalesCCSSLPT;
    public decimal RiesgosTrabajo => AplicacionesRT.Sum(a => a.Linea.Monto);
    public decimal OtrosCostosPatronales => OtrosCostos.Sum(l => l.Monto);
    public decimal? CargaPatronal => Conciliacion?.Totales.CargasPatronales;
    public decimal? CostoLaboralTotal => Conciliacion?.Totales.CostoAlgebraico;
}

public static class ResolucionRiesgoTrabajo
{
    public static (AsignacionRiesgoContrato Asignacion, RiesgoTrabajoVersionado Configuracion, AplicabilidadRiesgoTrabajo Aplicabilidad)
        Resolver(long persona, long? contrato, VigenciaFinanciera rango, IReadOnlyList<AsignacionRiesgoContrato> asignaciones,
            IReadOnlyList<RiesgoTrabajoVersionado> configuraciones, IReadOnlyList<AplicabilidadRiesgoTrabajo> aplicabilidades)
    {
        rango.Validar();
        var asociaciones = asignaciones.Where(a => a.IdColaborador == persona && (contrato is null || a.IdContrato == contrato)
            && a.VigenciaContrato.Solapa(rango)).ToArray();
        if (asociaciones.Length != 1 || !asociaciones[0].VigenciaContrato.Contiene(rango))
            throw new ConfiguracionConceptoException("CONFIGURACION_RT_NO_DEMOSTRABLE");
        var a = asociaciones[0]; a.VigenciaContrato.Validar();
        if (!a.Demostrada || a.IdConfiguracionAncla is not > 0 || string.IsNullOrWhiteSpace(a.NumeroPoliza)
            || string.IsNullOrWhiteSpace(a.CodigoActividad) || string.IsNullOrWhiteSpace(a.ReferenciaSoporte)
            || string.IsNullOrWhiteSpace(a.AsociadoPor)) throw new ConfiguracionConceptoException("CONFIGURACION_RT_NO_DEMOSTRABLE");
        var candidatas = configuraciones.Where(c => c.NumeroPoliza == a.NumeroPoliza && c.CodigoActividad == a.CodigoActividad).ToArray();
        foreach (var c in candidatas) c.Vigencia.Validar();
        if (!candidatas.Any(c => c.IdConfiguracion == a.IdConfiguracionAncla))
            throw new ConfiguracionConceptoException("CONFIGURACION_RT_NO_DEMOSTRABLE");
        var vigentes = candidatas.Where(c => c.Vigencia.Solapa(rango)).ToArray();
        if (vigentes.Length == 0) throw new ConfiguracionConceptoException("CONFIGURACION_RT_FALTANTE");
        if (vigentes.Length != 1)
            throw new ConfiguracionConceptoException(vigentes.Any(c => vigentes.Any(v => v.IdConfiguracion != c.IdConfiguracion && v.Vigencia.Solapa(c.Vigencia)))
                ? "CONFIGURACION_RT_AMBIGUA" : "BASE_RT_CRUZA_VIGENCIAS");
        var v = vigentes[0];
        if (!v.Vigencia.Contiene(rango)) throw new ConfiguracionConceptoException("BASE_RT_CRUZA_VIGENCIAS");
        if (!v.Activo) throw new ConfiguracionConceptoException("CONFIGURACION_RT_INACTIVA");
        if (v.IdConfiguracion <= 0 || !v.EvidenciaDemostrada || string.IsNullOrWhiteSpace(v.ReferenciaDocumento)
            || string.IsNullOrWhiteSpace(v.CreadoPor) || v.FechaCreacion == default)
            throw new ConfiguracionConceptoException("EVIDENCIA_RT_INVALIDA");
        if (v.Tarifa is null or < 0 || v.Descuento is < 0 or > 1 || v.Recargo is < 0 or > 1 || v.Factor < 0)
            throw new ConfiguracionConceptoException("TARIFA_RT_INVALIDA");
        var ps = aplicabilidades.Where(p => p.IdContrato == a.IdContrato && p.Vigencia.Solapa(rango)).ToArray();
        foreach (var p in ps) p.Vigencia.Validar();
        if (ps.Length != 1 || !ps[0].Vigencia.Contiene(rango)) throw new ConfiguracionConceptoException("APLICABILIDAD_RT_NO_DEMOSTRABLE");
        var prueba = ps[0];
        if (prueba.Modalidad != ModalidadBaseRiesgo.RemuneracionSalarialOrdinaria)
            throw new ConfiguracionConceptoException("MODALIDAD_RT_NO_SOPORTADA");
        if (prueba.NumeroPoliza != a.NumeroPoliza || prueba.CodigoActividad != a.CodigoActividad
            || string.IsNullOrWhiteSpace(prueba.ReferenciaSoporte) || string.IsNullOrWhiteSpace(prueba.ConfirmadoPor))
            throw new ConfiguracionConceptoException("APLICABILIDAD_RT_NO_DEMOSTRABLE");
        return (a, v, prueba);
    }
}

public static class ProductorCostosPatronales
{
    public static ResultadoCostoLaboral Producir(PeriodoSalarial periodo, ResultadoRentaSalarial renta,
        IReadOnlyList<AsignacionRiesgoContrato> asignaciones, IReadOnlyList<RiesgoTrabajoVersionado> configuraciones,
        IReadOnlyList<AplicabilidadRiesgoTrabajo> aplicabilidades, IReadOnlyList<ConceptoMonetarioVersion> conceptos,
        IReadOnlyList<HechoCostoPatronal> otros, IReadOnlyList<SegmentoSalarialTemporal> segmentos)
    {
        try
        {
            periodo.Validar();
            if (!renta.EsResoluble) return Fallido(renta.Bloqueos);
            if (renta.CuotasSociales?.EsResoluble != true) throw new ConfiguracionConceptoException("CUOTAS_PREVIAS_NO_DEMOSTRABLES");
            var entradas = AgregadorLineasMonetarias.Conciliar(renta.Conciliacion!.Lineas).Lineas;
            if (entradas.Any(l => l.Concepto.TipoBase == "BASE_RT_POLIZA")) throw new ConfiguracionConceptoException("RT_YA_APORTADO");
            var cargasG = renta.CuotasSociales.LineasCuotasCorte.Where(l => l.Efecto == EfectoMonetario.CargaPatronal).ToArray();
            if (!ProductorRentaSalarial.MismasLineas(entradas.Where(l => l.Efecto == EfectoMonetario.CargaPatronal), cargasG))
                throw new ConfiguracionConceptoException("CARGAS_PREVIAS_INCONSISTENTES");
            var aportes = new List<(LineaMonetaria Linea, AsignacionRiesgoContrato Asignacion, RiesgoTrabajoVersionado Config,
                AplicabilidadRiesgoTrabajo Aplicabilidad, ConceptoMonetarioVersion Concepto, VigenciaFinanciera Rango, decimal Monto)>();
            foreach (var l in entradas)
            {
                ResolucionConceptos.Validar(l.Concepto);
                if (l.Concepto.EsSalarial != true) continue;
                var signo = l.Efecto switch { EfectoMonetario.IngresoSalarial => 1m, EfectoMonetario.ReduccionDevengo => -1m,
                    _ => throw new ConfiguracionConceptoException("EFECTO_BASE_RT_INCOMPATIBLE") };
                var rango = l.Fuente.TieneRangoDevengo && l.Fuente.Segmento is { } st
                    ? new VigenciaFinanciera(st.Inicio, st.Fin) : new(l.Fuente.FechaHecho, l.Fuente.FechaHecho);
                if (!new VigenciaFinanciera(periodo.Inicio, periodo.Fin).Contiene(rango)
                    || l.Fuente.TieneRangoDevengo && l.Fuente.Segmento is null)
                    throw new ConfiguracionConceptoException("BASE_RT_NO_DEMOSTRABLE");
                var resuelta = ResolucionRiesgoTrabajo.Resolver(l.Fuente.IdColaborador, l.Fuente.Segmento?.IdContrato,
                    rango, asignaciones, configuraciones, aplicabilidades);
                var concepto = ResolucionConceptos.Resolver("RIESGO_TRABAJO", rango.Inicio, conceptos);
                if (concepto.TipoBase != "BASE_RT_POLIZA" || concepto.Clasificacion != "CargaPatronal"
                    || concepto.UnidadPermitida != "Porcentaje" || concepto.PermiteMontoManual || concepto.RequiereAutorizacion)
                    throw new ConfiguracionConceptoException("CONCEPTO_RT_INCOMPATIBLE");
                if (!concepto.Vigencia.Contiene(rango)) throw new ConfiguracionConceptoException("BASE_RT_CRUZA_VIGENCIAS");
                aportes.Add((l, resuelta.Asignacion, resuelta.Configuracion, resuelta.Aplicabilidad, concepto, rango, signo * l.Monto));
            }
            if (aportes.Count == 0) throw new ConfiguracionConceptoException("BASE_RT_NO_DEMOSTRABLE");
            var aplicaciones = new List<AplicacionRiesgoTrabajo>();
            foreach (var g in aportes.GroupBy(a => (a.Asignacion.IdColaborador, a.Asignacion.IdContrato, a.Config.IdConfiguracion,
                a.Concepto.IdVersion, a.Aplicabilidad.Vigencia)))
            {
                var a = g.First(); var baseRt = g.Sum(x => x.Monto);
                if (baseRt < 0) throw new ConfiguracionConceptoException("BASE_RT_NEGATIVA");
                var rango = new VigenciaFinanciera(g.Min(x => x.Rango.Inicio), g.Max(x => x.Rango.Fin));
                var rt = LineaMonetaria.Materializar(a.Concepto,
                    new(a.Asignacion.IdColaborador, FormattableString.Invariant($"RT:{a.Asignacion.IdContrato}:{a.Config.IdConfiguracion}:{a.Concepto.IdVersion}:{rango.Inicio:yyyyMMdd}:{rango.Fin:yyyyMMdd}"),
                        "Normativa", a.Config.IdConfiguracion, rango.Inicio, null, a.Config.ReferenciaDocumento),
                    new(null, "Porcentaje", baseRt, a.Config.Tarifa, a.Config.Factor, baseRt * a.Config.Tarifa!.Value * a.Config.Factor));
                aplicaciones.Add(new(rt, a.Config, a.Asignacion, a.Aplicabilidad, rango, baseRt,
                    Array.AsReadOnly(g.Select(x => new AporteBaseRiesgo(x.Linea.Fuente, x.Linea.Concepto.IdVersion, x.Monto)).ToArray())));
            }
            var costos = otros.Select(h => ProducirOtro(periodo, h, conceptos, segmentos)).ToArray();
            if (costos.Any(c => !entradas.Any(l => l.Fuente.IdColaborador == c.Fuente.IdColaborador)))
                throw new ConfiguracionConceptoException("COSTO_PATRONAL_FUERA_RELACION");
            var conciliacion = AgregadorLineasMonetarias.Conciliar(entradas.Concat(aplicaciones.Select(a => a.Linea)).Concat(costos));
            if (conciliacion.Totales.CargasPatronales != cargasG.Sum(l => l.Monto) + aplicaciones.Sum(a => a.Linea.Monto) + costos.Sum(l => l.Monto)
                || conciliacion.Totales.SaldoAlgebraico != renta.NetoPreliminar)
                throw new ConfiguracionConceptoException("COSTO_PATRONAL_NO_CONCILIA");
            _ = conciliacion.Totales.CostoAlgebraico; // Desbordamiento bloquea antes de exponer resultados.
            return new(conciliacion, renta, Array.AsReadOnly(aplicaciones.OrderBy(a => a.Linea.Fuente.IdColaborador)
                .ThenBy(a => a.Rango.Inicio).ThenBy(a => a.Asignacion.IdContrato).ThenBy(a => a.Configuracion.IdConfiguracion).ToArray()),
                AgregadorLineasMonetarias.Conciliar(costos).Lineas, []);
        }
        catch (ConfiguracionConceptoException e) { return Fallido([new(e.Codigo, "RT/COSTO", periodo.Inicio)]); }
        catch (ArgumentException) { return Fallido([new("VIGENCIA_RT_INVALIDA", "RT/COSTO", periodo.Inicio)]); }
        catch (OverflowException) { return Fallido([new("DESBORDAMIENTO_MONETARIO", "RT/COSTO", periodo.Inicio)]); }
    }

    private static LineaMonetaria ProducirOtro(PeriodoSalarial periodo, HechoCostoPatronal h,
        IReadOnlyList<ConceptoMonetarioVersion> conceptos, IReadOnlyList<SegmentoSalarialTemporal> segmentos)
    {
        if (!new VigenciaFinanciera(periodo.Inicio, periodo.Fin).Contiene(h.FechaHecho)
            || string.IsNullOrWhiteSpace(h.RegistradoPor) || string.IsNullOrWhiteSpace(h.ClaveHecho))
            throw new ConfiguracionConceptoException("COSTO_PATRONAL_NO_DEMOSTRABLE");
        var c = ResolucionConceptos.Resolver(h.CodigoConcepto, h.FechaHecho, conceptos);
        if (c.IdVersion != h.IdConceptoVersion || c.EsPatronal != true || c.Clasificacion != "CargaPatronal"
            || c.TipoBase is not ("MONTO_AUTORIZADO" or "SALARIO_DEVENGADO"))
            throw new ConfiguracionConceptoException("CONCEPTO_OTRO_PATRONAL_INCOMPATIBLE");
        if (h.Unidad != c.UnidadPermitida || h.Unidad is not ("Monto" or "Porcentaje"))
            throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        var historicas=h.AutorizacionesHistoricasForecast;
        var historiaAcreditada=h.BaseTemporal?.IdForecastSegmentoProyectado is >0 && historicas is {Count:>0}
            && historicas.All(a=>a.IdConceptoDetallePlanilla>0 && !string.IsNullOrWhiteSpace(a.Actor) && !string.IsNullOrWhiteSpace(a.Referencia))
            && historicas.Select(a=>a.IdConceptoDetallePlanilla).Distinct().Count()==historicas.Count;
        if(historicas is {Count:>0} && !historiaAcreditada)throw new ConfiguracionConceptoException("AUTORIZACION_HISTORICA_FORECAST_NO_DEMOSTRABLE");
        if (c.RequiereAutorizacion && !historiaAcreditada && (string.IsNullOrWhiteSpace(h.AutorizadoPor) || string.IsNullOrWhiteSpace(h.ReferenciaAutorizacion)))
            throw new ConfiguracionConceptoException("AUTORIZACION_FALTANTE");
        if(h.ImporteHistoricoForecast is {} observado)
        {
            // L proyecta importes ya observados; no vuelve a interpretar una media CRC como
            // porcentaje ni como monto manual. Las observaciones exactas quedan ligadas en L-P03.
            if(h.BaseTemporal?.IdForecastSegmentoProyectado is not >0 || h.Valor is not null
                || observado.Promedio<0 || observado.FactorTemporal is <0 or >1
                || observado.Observaciones.Any(id=>id<=0)
                || observado.Observaciones.Distinct().Count()!=observado.Observaciones.Count
                || observado.Observaciones.Count==0 && observado.Promedio!=0)
                throw new ConfiguracionConceptoException("COSTO_HISTORICO_FORECAST_NO_DEMOSTRABLE");
            return LineaMonetaria.Materializar(c,new(h.IdColaborador,"OTRO_PATRONAL:"+h.ClaveHecho,"Forecast",
                h.BaseTemporal.IdForecastSegmentoProyectado,h.FechaHecho,h.BaseTemporal,h.ReferenciaSoporte),
                new(null,c.UnidadPermitida,observado.Promedio,null,observado.FactorTemporal,observado.Promedio*observado.FactorTemporal),
                historiaAcreditada ? "Autorizaciones de los hechos J vinculados a las observaciones Forecast." : h.ReferenciaAutorizacion);
        }
        ResolucionParametrosInternos.ValidarValor(h.Unidad, h.Valor, null);
        if (h.Unidad == "Monto" && (!c.PermiteMontoManual || c.TipoBase != "MONTO_AUTORIZADO"))
            throw new ConfiguracionConceptoException("MONTO_NO_PERMITIDO");
        SegmentoSalarialTemporal? segmento = null;
        if (h.Unidad == "Porcentaje")
        {
            if (c.TipoBase != "SALARIO_DEVENGADO") throw new ConfiguracionConceptoException("BASE_PORCENTUAL_NO_DISPONIBLE");
            var ss = segmentos.Where(s => s.IdColaborador == h.IdColaborador && (h.BaseTemporal?.IdForecastSegmentoProyectado is > 0
                    ? h.BaseTemporal.IdContrato == s.IdContrato && h.BaseTemporal.IdCondicion == (s.IdCondicionLaboralVersion ?? 0)
                        && h.BaseTemporal.Inicio == s.Inicio && h.BaseTemporal.Fin == s.Fin
                    : ReferenciaSegmentoTemporal.De(s) == h.BaseTemporal)
                && s.Inicio <= h.FechaHecho && h.FechaHecho <= s.Fin).ToArray();
            if (ss.Length != 1) throw new ConfiguracionConceptoException("BASE_PATRONAL_NO_DEMOSTRABLE");
            segmento = ss[0];
        }
        var b = BasesConceptos.Resolver(c.TipoBase, segmento, h.Unidad == "Monto" ? h.Valor : null);
        decimal? tasa = h.Unidad == "Porcentaje" ? h.Valor!.Value / 100m : null;
        return LineaMonetaria.Materializar(c, new(h.IdColaborador, "OTRO_PATRONAL:" + h.ClaveHecho, "Derivado",
            null, h.FechaHecho, h.BaseTemporal, h.ReferenciaSoporte), new(null, h.Unidad, b, tasa, null, tasa is { } t ? b * t : h.Valor!.Value),
            historiaAcreditada ? "Autorizaciones de los hechos J vinculados a las observaciones Forecast." : h.ReferenciaAutorizacion);
    }

    public static ResultadoCostoLaboral Fallido(IEnumerable<BloqueoConcepto> bloqueos)
        => new(null, null, [], [], Array.AsReadOnly(bloqueos.Distinct().OrderBy(b => b.IdColaborador).ThenBy(b => b.Fecha)
            .ThenBy(b => b.Codigo, StringComparer.Ordinal).ThenBy(b => b.Clave, StringComparer.Ordinal).ToArray()));
}

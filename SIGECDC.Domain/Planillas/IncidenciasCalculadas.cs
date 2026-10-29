using System.Globalization;

namespace SIGECDC.Domain.Planillas;

public enum ClaseHechoIncidencia
{
    NoDemostrada, HoraExtraOrdinaria, HoraExtraFeriado, HoraExtraDescanso, HoraExtraEspecial,
    AusenciaSinGoce, Incapacidad, PermisoPagado, Bono, DeduccionInterna
}

// Entrada pura: el proveedor debe demostrar estas condiciones, nunca inferirlas del nombre o Descripcion.
// No es un estado persistente ni una API de aprobación. El proveedor valida IncidenciaPlanillaConfirmacion.
public sealed record HechoIncidenciaCalculable(long IdIncidencia, long IdColaborador, long IdConcepto,
    long? IdConceptoVersion, DateTime Fecha, DateTime Fin, ClaseHechoIncidencia Clase,
    decimal? Cantidad, string? Unidad, decimal MontoManual, string ReferenciaSoporte,
    bool Confirmado, bool AutorizacionDemostrada, string? ReferenciaAutorizacion,
    ReferenciaSegmentoTemporal? BaseParametroDemostrada = null,
    long? IdConfirmacion = null, int? NumeroVersionConfirmacion = null);

public sealed record ResultadoLineasIncidencias(IReadOnlyList<LineaMonetaria> Lineas, IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    public bool EsResoluble => Bloqueos.Count == 0;
    public bool EsAutorizable => false;
}

public static class ProductorIncidencias
{
    public static ResultadoLineasIncidencias Producir(PeriodoSalarial periodo, IEnumerable<HechoIncidenciaCalculable> entradas,
        ResultadoTemporalSalarial temporal, IReadOnlyList<ConceptoMonetarioVersion> conceptos,
        IReadOnlyDictionary<long, ParametroInternoResuelto>? parametros = null)
    {
        periodo.Validar();
        var hechos = entradas.ToArray(); var lineas = new List<LineaMonetaria>(); var bloqueos = new List<BloqueoConcepto>();
        foreach (var duplicado in hechos.GroupBy(h => h.IdIncidencia).Where(g => g.Count() > 1))
            bloqueos.Add(new("INCIDENCIA_DUPLICADA", duplicado.Key.ToString(CultureInfo.InvariantCulture), periodo.Inicio));
        if (bloqueos.Count == 0)
            foreach (var hecho in hechos.OrderBy(h => h.Fecha).ThenBy(h => h.IdIncidencia))
            {
                try { lineas.Add(ProducirUna(periodo, hecho, temporal, conceptos, parametros?.GetValueOrDefault(hecho.IdIncidencia))); }
                catch (ConfiguracionConceptoException e) { Bloquear(e.Codigo, hecho); }
                catch (ArgumentException) { Bloquear("VIGENCIA_INCIDENCIA_INVALIDA", hecho); }
                catch (OverflowException) { Bloquear("DESBORDAMIENTO_MONETARIO", hecho); }
            }
        return new(Array.AsReadOnly(bloqueos.Count == 0 ? lineas.ToArray() : []), Array.AsReadOnly(bloqueos.ToArray()));
        void Bloquear(string codigo, HechoIncidenciaCalculable h)
            => bloqueos.Add(new(codigo, h.IdIncidencia.ToString(CultureInfo.InvariantCulture), h.Fecha, h.IdColaborador));
    }

    private static LineaMonetaria ProducirUna(PeriodoSalarial periodo, HechoIncidenciaCalculable h,
        ResultadoTemporalSalarial temporal, IReadOnlyList<ConceptoMonetarioVersion> conceptos, ParametroInternoResuelto? parametro)
    {
        if (h.IdIncidencia <= 0 || h.IdColaborador <= 0 || !h.Confirmado || string.IsNullOrWhiteSpace(h.ReferenciaSoporte))
            throw new ConfiguracionConceptoException("HECHO_NO_DEMOSTRADO");
        new VigenciaFinanciera(h.Fecha, h.Fin).Validar();
        if (h.Fecha < periodo.Inicio || h.Fin > periodo.Fin) throw new ConfiguracionConceptoException("INCIDENCIA_FUERA_PERIODO");
        // Solo hechos individualizados por fecha. Una cantidad agregada entre fechas no demuestra su distribución.
        if (h.Fecha != h.Fin) throw new ConfiguracionConceptoException("DISTRIBUCION_TEMPORAL_NO_DEMOSTRABLE");
        if (h.IdConceptoVersion is null) throw new ConfiguracionConceptoException("INCIDENCIA_SIN_VERSION_FIJADA");
        var fijadas = conceptos.Where(c => c.IdVersion == h.IdConceptoVersion).ToArray();
        if (fijadas.Length != 1 || fijadas[0].IdConcepto != h.IdConcepto) throw new ConfiguracionConceptoException("VINCULO_CONCEPTO_INCOMPATIBLE");
        var fijada = fijadas[0];
        var concepto = ResolucionConceptos.Resolver(fijada.Codigo, h.Fecha, conceptos);
        if (concepto.IdVersion != fijada.IdVersion) throw new ConfiguracionConceptoException("VERSION_FIJADA_FUERA_VIGENCIA");
        var personas = temporal.Poblacion.Where(p => p.IdColaborador == h.IdColaborador).ToArray();
        if (personas.Length != 1 || !personas[0].EsResoluble) throw new ConfiguracionConceptoException("TEMPORAL_BLOQUEADO");
        var segmentos = personas[0].Segmentos.Where(s => s.Inicio <= h.Fecha && h.Fecha <= s.Fin).ToArray();
        if (segmentos.Length != 1) throw new ConfiguracionConceptoException("SEGMENTO_INCIDENCIA_NO_UNICO");
        var s = segmentos[0];
        if (s.Jornada is null || !s.Jornada.Utilizable || s.Jornada.HorasDia <= 0 || !s.Jornada.Vigencia.Contiene(h.Fecha)
            || s.Jornada.Tipo is not ("Diurna" or "Mixta" or "Nocturna"))
            throw new ConfiguracionConceptoException("JORNADA_NO_DEMOSTRADA");
        _ = BasesConceptos.Ordinario(s); // Invariantes de D; no materializa ni redondea.
        if (concepto.RequiereAutorizacion
            && (!h.AutorizacionDemostrada || string.IsNullOrWhiteSpace(h.ReferenciaAutorizacion)))
            throw new ConfiguracionConceptoException("AUTORIZACION_INCIDENCIA_NO_DEMOSTRABLE");
        var fuente = new FuenteLineaMonetaria(h.IdColaborador, h.IdIncidencia.ToString(CultureInfo.InvariantCulture),
            "Incidencia", h.IdIncidencia, h.Fecha, ReferenciaSegmentoTemporal.De(s), h.ReferenciaSoporte,
            h.IdConfirmacion, h.NumeroVersionConfirmacion);
        var efecto = ResolucionConceptos.Validar(concepto);
        if (h.Clase is ClaseHechoIncidencia.HoraExtraFeriado or ClaseHechoIncidencia.HoraExtraDescanso or ClaseHechoIncidencia.HoraExtraEspecial)
            throw new ConfiguracionConceptoException("HE_ESPECIAL_SIN_REGLA");
        if (h.Clase == ClaseHechoIncidencia.HoraExtraOrdinaria)
        {
            if (concepto.Codigo != "HORA_EXTRA_ORDINARIA" || concepto.TipoBase != "HORA_ORDINARIA"
                || efecto != EfectoMonetario.IngresoSalarial || h.Unidad != "Hora")
                throw new ConfiguracionConceptoException("CONCEPTO_HE_INCOMPATIBLE");
            CantidadPositiva(h);
            return LineaMonetaria.Materializar(concepto, fuente,
                FactoresHoraExtraOrdinaria(s, h.Cantidad!.Value), h.ReferenciaAutorizacion);
        }
        if (h.Clase == ClaseHechoIncidencia.AusenciaSinGoce)
        {
            if (concepto.Codigo != "AUSENCIA_SIN_GOCE" || concepto.TipoBase != "REDUCCION_DEVENGO"
                || efecto != EfectoMonetario.ReduccionDevengo || h.Unidad is not ("Día" or "Hora"))
                throw new ConfiguracionConceptoException("CONCEPTO_AUSENCIA_INCOMPATIBLE");
            CantidadPositiva(h);
            // Más de un día exige hechos por fecha; no se extrapola desde FechaIncidencia.
            if (h.Unidad == "Día" && h.Cantidad > 1) throw new ConfiguracionConceptoException("DISTRIBUCION_TEMPORAL_NO_DEMOSTRABLE");
            return LineaMonetaria.Materializar(concepto, fuente,
                FactoresAusenciaSinGoce(s, h.Cantidad!.Value, h.Unidad), h.ReferenciaAutorizacion);
        }
        if (h.Clase is not (ClaseHechoIncidencia.Bono or ClaseHechoIncidencia.DeduccionInterna))
            throw new ConfiguracionConceptoException("CLASE_HECHO_NO_SOPORTADA");
        if (h.Clase == ClaseHechoIncidencia.Bono ? efecto is not (EfectoMonetario.IngresoSalarial or EfectoMonetario.IngresoNoSalarial)
            : efecto != EfectoMonetario.DeduccionInterna) throw new ConfiguracionConceptoException("CLASIFICACION_INCIDENCIA_INCOMPATIBLE");
        if (h.Unidad != concepto.UnidadPermitida) throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        if (parametro is not null)
        {
            if (parametro.Version.Tipo == "Porcentaje" && h.BaseParametroDemostrada != ReferenciaSegmentoTemporal.De(s))
                throw new ConfiguracionConceptoException("BASE_PORCENTUAL_NO_DEMOSTRADA");
            return ProductorLineasConceptos.Interno(parametro, concepto, fuente, s, h.ReferenciaAutorizacion,
                exigirAutorizacion: concepto.RequiereAutorizacion);
        }
        if (h.Unidad != "Monto" || !concepto.PermiteMontoManual || concepto.TipoBase != "MONTO_AUTORIZADO")
            throw new ConfiguracionConceptoException("PARAMETRO_INTERNO_O_MODALIDAD_FALTANTE");
        var monto = BasesConceptos.Resolver(concepto.TipoBase, s, h.MontoManual);
        return LineaMonetaria.Materializar(concepto, fuente, new(null, "Monto", monto, null, null, monto), h.ReferenciaAutorizacion);
    }

    // Factores puros compartidos con proyecciones explícitas; confirmar una incidencia real
    // continúa exigiendo sus invariantes anteriores. Cero proyectado no fabrica un hecho RRHH.
    public static FactoresLineaMonetaria FactoresHoraExtraOrdinaria(SegmentoSalarialTemporal s, decimal cantidad)
    {
        if (cantidad < 0) throw new ConfiguracionConceptoException("CANTIDAD_INVALIDA");
        var vh = BasesConceptos.HoraOrdinaria(s);
        return new(cantidad, "Hora", vh, null, 1.5m, cantidad * vh * 1.5m, s.SalarioMensual, "Hora", s.Jornada);
    }

    public static FactoresLineaMonetaria FactoresAusenciaSinGoce(SegmentoSalarialTemporal s, decimal cantidad, string unidad)
    {
        if (cantidad < 0) throw new ConfiguracionConceptoException("CANTIDAD_INVALIDA");
        var baseAusencia = BasesConceptos.BaseAusencia(s, unidad);
        return new(cantidad, "Cantidad", baseAusencia, null, null, cantidad * baseAusencia, s.SalarioMensual, unidad, s.Jornada);
    }

    private static void CantidadPositiva(HechoIncidenciaCalculable h)
    {
        if (h.Cantidad is null or <= 0) throw new ConfiguracionConceptoException("CANTIDAD_INCIDENCIA_INVALIDA");
    }
}

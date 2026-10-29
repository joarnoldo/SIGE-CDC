namespace SIGECDC.Domain.Planillas;

public enum EfectoMonetario { IngresoSalarial, IngresoNoSalarial, ReduccionDevengo, DeduccionLegal, DeduccionInterna, CargaPatronal }
public enum FamiliaBaseConcepto { SalarioOrdinario, MontoAutorizado, Incidencias, Cuotas, Renta, RiesgosTrabajo }
public sealed record BloqueoConcepto(string Codigo, string Clave, DateTime Fecha, long? IdColaborador = null);
public sealed class ConfiguracionConceptoException(string codigo) : InvalidOperationException(codigo)
{
    public string Codigo { get; } = codigo;
}

public sealed record ConceptoMonetarioVersion(long IdConcepto, long IdVersion, string Codigo, string Nombre,
    VigenciaFinanciera Vigencia, string Clasificacion, bool? EsSalarial, bool? EsContributivoCCSS,
    bool? EsTributario, bool? EsPatronal, string TipoBase, string UnidadPermitida,
    bool PermiteMontoManual, bool RequiereAutorizacion, bool Activo, bool EvidenciaConfirmada);

public static class ResolucionConceptos
{
    public static ConceptoMonetarioVersion Resolver(string codigo, DateTime fecha, IEnumerable<ConceptoMonetarioVersion> versiones)
    {
        var filas = versiones.Where(v => v.Codigo == codigo).ToArray();
        foreach (var v in filas) v.Vigencia.Validar();
        filas = filas.Where(v => v.Vigencia.Contiene(fecha)).ToArray();
        if (filas.Length != 1) throw new ConfiguracionConceptoException(filas.Length == 0 ? "CONCEPTO_VERSION_FALTANTE" : "CONCEPTO_VERSION_AMBIGUA");
        Validar(filas[0]); return filas[0];
    }

    public static EfectoMonetario Validar(ConceptoMonetarioVersion c)
    {
        if (!c.Activo) throw new ConfiguracionConceptoException("CONCEPTO_INACTIVO");
        if (!c.EvidenciaConfirmada) throw new ConfiguracionConceptoException("CONCEPTO_SIN_EVIDENCIA");
        if (c.IdConcepto <= 0 || c.IdVersion <= 0 || string.IsNullOrWhiteSpace(c.Codigo) || string.IsNullOrWhiteSpace(c.Nombre))
            throw new ConfiguracionConceptoException("CONCEPTO_INVALIDO");
        if (c.EsSalarial is null || c.EsContributivoCCSS is null || c.EsTributario is null || c.EsPatronal is null)
            throw new ConfiguracionConceptoException("BANDERAS_FALTANTES");
        if (c.EsSalarial != (c.Clasificacion == "IngresoSalarial") || c.EsPatronal != (c.Clasificacion == "CargaPatronal")
            || (c.Clasificacion is "DeduccionLegal" or "DeduccionInterna" or "CargaPatronal" && (c.EsContributivoCCSS == true || c.EsTributario == true)))
            throw new ConfiguracionConceptoException("CLASIFICACION_CONTRADICTORIA");
        var familia = BasesConceptos.Familia(c.TipoBase);
        if (c.UnidadPermitida is not ("Monto" or "Porcentaje" or "Hora" or "Día" or "Cantidad"))
            throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        if (familia is FamiliaBaseConcepto.SalarioOrdinario or FamiliaBaseConcepto.MontoAutorizado
            && c.Clasificacion is not ("IngresoSalarial" or "IngresoNoSalarial" or "DeduccionInterna" or "CargaPatronal"))
            throw new ConfiguracionConceptoException("CLASIFICACION_CONTRADICTORIA");
        if (familia == FamiliaBaseConcepto.MontoAutorizado && c.UnidadPermitida != "Monto")
            throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        if (familia == FamiliaBaseConcepto.Incidencias && (c.Clasificacion != "IngresoSalarial"
            || (c.TipoBase == "HORA_ORDINARIA" ? c.UnidadPermitida != "Hora" : c.UnidadPermitida != "Cantidad")))
            throw new ConfiguracionConceptoException("CLASIFICACION_CONTRADICTORIA");
        if (familia == FamiliaBaseConcepto.Cuotas && (c.Clasificacion is not ("DeduccionLegal" or "CargaPatronal") || c.UnidadPermitida != "Porcentaje")
            || familia == FamiliaBaseConcepto.Renta && (c.Clasificacion != "DeduccionLegal" || c.UnidadPermitida != "Monto")
            || familia == FamiliaBaseConcepto.RiesgosTrabajo && (c.Clasificacion != "CargaPatronal" || c.UnidadPermitida != "Porcentaje"))
            throw new ConfiguracionConceptoException("CLASIFICACION_CONTRADICTORIA");
        return c.Clasificacion switch
        {
            "IngresoSalarial" when c.TipoBase == "REDUCCION_DEVENGO" => EfectoMonetario.ReduccionDevengo,
            "IngresoSalarial" => EfectoMonetario.IngresoSalarial,
            "IngresoNoSalarial" => EfectoMonetario.IngresoNoSalarial,
            "DeduccionLegal" => EfectoMonetario.DeduccionLegal,
            "DeduccionInterna" => EfectoMonetario.DeduccionInterna,
            "CargaPatronal" => EfectoMonetario.CargaPatronal,
            _ => throw new ConfiguracionConceptoException("CLASIFICACION_DESCONOCIDA")
        };
    }
}

public sealed record ReferenciaSegmentoTemporal(long IdContrato, long IdCondicion, DateTime Inicio, DateTime Fin,
    long? IdForecastSegmentoProyectado = null)
{
    public static ReferenciaSegmentoTemporal De(SegmentoSalarialTemporal s) => new(s.IdContrato,
        s.IdCondicionLaboralVersion ?? throw new ConfiguracionConceptoException("CONDICION_TEMPORAL_NO_DEMOSTRADA"), s.Inicio, s.Fin);
}
public sealed record FuenteLineaMonetaria(long IdColaborador, string ClaveHecho, string TipoFuente, long? IdFuente,
    DateTime FechaHecho, ReferenciaSegmentoTemporal? Segmento, string ReferenciaSoporte,
    long? IdConfirmacionIncidencia = null, int? NumeroVersionConfirmacion = null)
{
    // En Forecast la identidad de población es IdForecastParticipante y la referencia tipada
    // pertenece a Forecast. Cero en Contrato/Condicion significa ausencia, nunca un maestro ficticio.
    public bool TieneRangoDevengo => TipoFuente == "Contrato" || TipoFuente == "Forecast" && Segmento?.IdForecastSegmentoProyectado is > 0;
}
public sealed record FactoresLineaMonetaria(decimal? Cantidad, string Unidad, decimal? BaseSinRedondear,
    decimal? Tasa, decimal? Multiplicador, decimal MontoSinRedondear, decimal? SalarioMensualReferencia = null,
    string? UnidadCantidad = null, JornadaTemporal? JornadaReferencia = null);

// No tiene setters ni constructor público: el monto materializado se obtiene una sola vez.
// Magnitud positiva; el efecto tipado aporta el sentido. R2 admite negativos independientemente.
public sealed class LineaMonetaria
{
    public ConceptoMonetarioVersion Concepto { get; }
    public FuenteLineaMonetaria Fuente { get; }
    public FactoresLineaMonetaria Factores { get; }
    public EfectoMonetario Efecto { get; }
    public decimal Monto { get; }
    public string? ReferenciaAutorizacion { get; }
    public ParametroInternoResuelto? ParametroAplicado { get; }
    private LineaMonetaria(ConceptoMonetarioVersion concepto, FuenteLineaMonetaria fuente, FactoresLineaMonetaria factores, EfectoMonetario efecto,
        string? autorizacion, ParametroInternoResuelto? parametro, decimal? materializado = null)
    {
        Concepto = concepto; Fuente = fuente; Factores = factores; Efecto = efecto; ReferenciaAutorizacion = autorizacion; ParametroAplicado = parametro;
        Monto = materializado ?? PrecisionMonetaria.R2(factores.MontoSinRedondear);
    }

    // Rehidrata evidencia ya materializada. La comprobación no altera ni vuelve a materializar el importe.
    public static LineaMonetaria DesdeSnapshot(ConceptoMonetarioVersion concepto, FuenteLineaMonetaria fuente,
        FactoresLineaMonetaria factores, decimal monto, string? autorizacion, ParametroInternoResuelto? parametro)
    {
        if (monto < 0 || monto != PrecisionMonetaria.R2(factores.MontoSinRedondear))
            throw new ConfiguracionConceptoException("SNAPSHOT_NO_CONCILIA");
        return new(concepto, fuente, factores, ResolucionConceptos.Validar(concepto), autorizacion, parametro, monto);
    }

    // Punto común para productores de E y, posteriormente, F/G/H/I. No implementa sus fórmulas.
    public static LineaMonetaria Materializar(ConceptoMonetarioVersion concepto, FuenteLineaMonetaria fuente,
        FactoresLineaMonetaria factores, string? autorizacion = null, ParametroInternoResuelto? parametro = null)
    {
        var efecto = ResolucionConceptos.Validar(concepto);
        concepto.Vigencia.Validar();
        if (!concepto.Vigencia.Contiene(fuente.FechaHecho)) throw new ConfiguracionConceptoException("CONCEPTO_FUERA_VIGENCIA");
        if (fuente.IdColaborador <= 0 || string.IsNullOrWhiteSpace(fuente.ClaveHecho) || string.IsNullOrWhiteSpace(fuente.ReferenciaSoporte)
            || fuente.TipoFuente is not ("Contrato" or "Incidencia" or "Parámetro" or "Normativa" or "Derivado" or "Forecast") || fuente.FechaHecho.TimeOfDay != TimeSpan.Zero
            || fuente.TipoFuente == "Forecast" && fuente.Segmento?.IdForecastSegmentoProyectado is not > 0)
            throw new ConfiguracionConceptoException("FUENTE_INVALIDA");
        if (concepto.RequiereAutorizacion && string.IsNullOrWhiteSpace(autorizacion)) throw new ConfiguracionConceptoException("AUTORIZACION_FALTANTE");
        if (factores.Unidad != concepto.UnidadPermitida) throw new ConfiguracionConceptoException("UNIDAD_INCOMPATIBLE");
        if (factores.MontoSinRedondear < 0 || factores.Cantidad < 0 || factores.BaseSinRedondear < 0 || factores.Tasa < 0 || factores.Multiplicador < 0)
            throw new ConfiguracionConceptoException("VALOR_INVALIDO");
        return new(concepto, fuente, factores, efecto, autorizacion, parametro);
    }
}

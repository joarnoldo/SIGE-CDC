using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public sealed record ObservacionConceptoForecast(long IdObservacion, long IdConcepto, decimal? Monto, bool Comparable, long? IdFuenteHistorica = null);
public sealed record MediaConceptoForecast(decimal Valor, IReadOnlyList<long> Observaciones, int? CantidadFuentes = null)
{
    public int Cantidad => CantidadFuentes ?? Observaciones.Count;
    public bool HistoricoLimitado => Cantidad < 3;
}
public sealed record OverrideConceptoForecast(long IdParametro, long IdConceptoVersion, string Tipo, string Unidad,
    decimal Valor, string Soporte, string Actor, bool AutorizacionRequerida,
    string? AutorizadoPor = null, DateTime? FechaAutorizacion = null, string? ReferenciaAutorizacion = null);
public sealed record DestinoCostoForecast(long IdAsignacion, long? IdProyecto, bool SinAsignar, decimal Porcentaje);
public sealed record CostoDistribuidoForecast(DestinoCostoForecast Destino, decimal SinRedondear, decimal Monto, decimal Residuo);
public sealed record ComponenteProyectadoForecast(LineaMonetaria Linea, MediaConceptoForecast Historico,
    long? IdParametro, string FuenteEfectiva);
public sealed record ConceptoDistribuidoForecast(long IdConceptoVersion,DestinoCostoForecast Destino,decimal BaseMaterializada,
    decimal SinRedondear,decimal Monto,decimal Residuo);

// Matemática específica de FCT: observaciones, sustitución y distribución. El dinero y las
// obligaciones se producen por E/F/G/H/I. No accede a tablas ni inventa normativa o asociaciones.
public static class ProyeccionFinanciera
{
    public const string VersionMotor = "FIN-FOR-V1";

    public static MediaConceptoForecast Media(long concepto, IEnumerable<ObservacionConceptoForecast> observaciones)
    {
        var filas = observaciones.Where(o => o.IdConcepto == concepto && o.Comparable).OrderBy(o => o.IdObservacion).ToArray();
        if (filas.Any(o => o.IdObservacion <= 0 || o.Monto is null or < 0)
            || filas.Select(o => o.IdObservacion).Distinct().Count() != filas.Length)
            throw new ConfiguracionConceptoException("OBSERVACION_FORECAST_INVALIDA");
        // Dos versiones del mismo concepto dentro de una fuente no son dos períodos históricos.
        var cantidad=filas.Select(o=>o.IdFuenteHistorica??o.IdObservacion).Distinct().Count();
        return new(cantidad == 0 ? 0 : filas.Sum(o => o.Monto!.Value) / cantidad,
            Array.AsReadOnly(filas.Select(o => o.IdObservacion).ToArray()),cantidad);
    }

    public static IReadOnlyList<CostoDistribuidoForecast> Distribuir(decimal costoLaboral, IReadOnlyList<DestinoCostoForecast> destinos)
    {
        if (costoLaboral < 0 || PrecisionMonetaria.R2(costoLaboral) != costoLaboral || destinos.Count == 0
            || destinos.Sum(d => d.Porcentaje) != 100m
            || destinos.Any(d => d.IdAsignacion <= 0 || d.Porcentaje is <= 0 or > 100 || decimal.Round(d.Porcentaje,4) != d.Porcentaje
                || d.SinAsignar != (d.IdProyecto is null) || d.IdProyecto is <= 0)
            || destinos.GroupBy(d => d.IdProyecto).Any(g => g.Count() > 1)
            || destinos.GroupBy(d => d.IdAsignacion).Any(g => g.Count() > 1))
            throw new ConfiguracionConceptoException("DISTRIBUCION_FORECAST_INVALIDA");
        var orden = destinos.OrderByDescending(d => d.Porcentaje).ThenBy(d => d.IdProyecto ?? 0).ToArray();
        var filas = orden.Select(d => new CostoDistribuidoForecast(d, costoLaboral * d.Porcentaje / 100m,
            PrecisionMonetaria.R2(costoLaboral * d.Porcentaje / 100m), 0)).ToArray();
        var residuo = costoLaboral - filas.Sum(d => d.Monto);
        filas[0] = filas[0] with { Monto=filas[0].Monto+residuo,Residuo=residuo };
        if (filas.Any(d => d.Monto < 0) || filas.Sum(d => d.Monto) != costoLaboral)
            throw new ConfiguracionConceptoException("DISTRIBUCION_FORECAST_NO_CONCILIA");
        return Array.AsReadOnly(filas.OrderBy(d => d.Destino.IdProyecto ?? 0).ToArray());
    }

    public static void ValidarOverride(OverrideConceptoForecast o, ConceptoMonetarioVersion c)
    {
        ResolucionConceptos.Validar(c);
        if (o.IdParametro <= 0 || o.IdConceptoVersion != c.IdVersion || o.Valor < 0
            || string.IsNullOrWhiteSpace(o.Soporte) || string.IsNullOrWhiteSpace(o.Actor))
            throw new ConfiguracionConceptoException("OVERRIDE_FORECAST_NO_DEMOSTRADO");
        if (BasesConceptos.Familia(c.TipoBase) is FamiliaBaseConcepto.Cuotas or FamiliaBaseConcepto.Renta or FamiliaBaseConcepto.RiesgosTrabajo)
            throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
        if (o.AutorizacionRequerida != c.RequiereAutorizacion
            || c.RequiereAutorizacion && (string.IsNullOrWhiteSpace(o.AutorizadoPor) || o.FechaAutorizacion is null
                || string.IsNullOrWhiteSpace(o.ReferenciaAutorizacion)))
            throw new ConfiguracionConceptoException("AUTORIZACION_FORECAST_NO_DEMOSTRADA");
        var unidad = o.Unidad == c.UnidadPermitida || c.UnidadPermitida == "Cantidad" && o.Unidad is "Hora" or "Día";
        if (!unidad || o.Tipo is not ("Monto" or "Porcentaje" or "Cantidad")
            || o.Tipo == "Monto" && (!c.PermiteMontoManual || o.Unidad != "Monto")
            || o.Tipo == "Porcentaje" && (o.Unidad != "Porcentaje" || o.Valor > 100 || c.TipoBase != "SALARIO_DEVENGADO")
            || o.Tipo == "Cantidad" && o.Unidad is not ("Hora" or "Día" or "Cantidad"))
            throw new ConfiguracionConceptoException("MODALIDAD_FORECAST_INCOMPATIBLE");
    }

    // Conserva simultáneamente los importes de cada concepto y los destinos CTL aprobados.
    // Los movimientos de centavos son solo atribución entre proyectos; nunca alteran dinero
    // producido por E–I ni crean una línea financiera de ajuste.
    public static IReadOnlyList<ConceptoDistribuidoForecast> DistribuirLineas(ConciliacionLineas conciliacion,IReadOnlyList<DestinoCostoForecast> destinos)
    {
        var ctl=Distribuir(conciliacion.Totales.CostoAlgebraico,destinos);
        var grupos=conciliacion.Lineas.GroupBy(l=>l.Concepto.IdVersion).OrderBy(g=>g.First().Concepto.Codigo,StringComparer.Ordinal).ThenBy(g=>g.Key).ToArray();
        var signos=grupos.ToDictionary(g=>g.Key,g=>g.First().Efecto switch{EfectoMonetario.IngresoSalarial or EfectoMonetario.CargaPatronal=>1,EfectoMonetario.ReduccionDevengo=>-1,_=>0});
        var celdas=new List<ConceptoDistribuidoForecast>();
        foreach(var g in grupos)
        {
            var total=g.Sum(l=>l.Monto);
            celdas.AddRange(Distribuir(total,destinos).Select(d=>new ConceptoDistribuidoForecast(g.Key,d.Destino,total,d.SinRedondear,d.Monto,d.Residuo)));
        }
        decimal Diferencia(CostoDistribuidoForecast d)=>d.Monto-celdas.Where(c=>c.Destino.IdAsignacion==d.Destino.IdAsignacion).Sum(c=>c.Monto*signos[c.IdConceptoVersion]);
        foreach(var destino in ctl)
        {
            while(Diferencia(destino)>0)
            {
                var faltante=Diferencia(destino);var donante=ctl.FirstOrDefault(d=>Diferencia(d)<0)
                    ??throw new ConfiguracionConceptoException("DISTRIBUCION_FORECAST_NO_CONCILIA");
                var origen=celdas.FindIndex(c=>c.Destino.IdAsignacion==donante.Destino.IdAsignacion && signos[c.IdConceptoVersion]==1 && c.Monto>0);
                if(origen<0)throw new ConfiguracionConceptoException("DISTRIBUCION_FORECAST_NO_CONCILIA");
                var disponible=celdas[origen];var mover=Math.Min(Math.Min(faltante,-Diferencia(donante)),disponible.Monto);
                var receptor=celdas.FindIndex(c=>c.Destino.IdAsignacion==destino.Destino.IdAsignacion && c.IdConceptoVersion==disponible.IdConceptoVersion);
                celdas[origen]=disponible with {Monto=disponible.Monto-mover,Residuo=disponible.Residuo-mover};
                var recibe=celdas[receptor];celdas[receptor]=recibe with{Monto=recibe.Monto+mover,Residuo=recibe.Residuo+mover};
            }
        }
        if(ctl.Any(d=>Diferencia(d)!=0) || grupos.Any(g=>celdas.Where(c=>c.IdConceptoVersion==g.Key).Sum(c=>c.Monto)!=g.Sum(l=>l.Monto)))
            throw new ConfiguracionConceptoException("DISTRIBUCION_FORECAST_NO_CONCILIA");
        return celdas.AsReadOnly();
    }

    public static ComponenteProyectadoForecast Variable(PeriodoSalarial periodo, SegmentoSalarialTemporal segmento,
        long idSegmentoForecast, ConceptoMonetarioVersion concepto, MediaConceptoForecast historico,
        OverrideConceptoForecast? sustitucion, string soporte, string? referenciaAutorizacionHistorica = null)
    {
        periodo.Validar(); ResolucionConceptos.Validar(concepto);
        if (idSegmentoForecast <= 0 || !new VigenciaFinanciera(periodo.Inicio,periodo.Fin).Contiene(new VigenciaFinanciera(segmento.Inicio,segmento.Fin))
            || !concepto.Vigencia.Contiene(new VigenciaFinanciera(segmento.Inicio,segmento.Fin)))
            throw new ConfiguracionConceptoException("SEGMENTO_FORECAST_INVALIDO");
        var familia = BasesConceptos.Familia(concepto.TipoBase);
        if (familia is FamiliaBaseConcepto.Cuotas or FamiliaBaseConcepto.Renta or FamiliaBaseConcepto.RiesgosTrabajo)
            throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
        if (sustitucion is not null) ValidarOverride(sustitucion,concepto);
        var factor = (decimal)segmento.DiasBase30 / CalendarioSalarial.DiasBase30(periodo.Inicio,periodo.Fin);
        FactoresLineaMonetaria factores;
        if (sustitucion is { Tipo:"Cantidad" } cantidad)
        {
            // Una cantidad explícita no se reparte entre tarifas sin evidencia. El caller debe
            // aportar un único segmento aplicable; la composición valida esa unicidad.
            factores = concepto.Codigo switch
            {
                "HORA_EXTRA_ORDINARIA" when concepto.TipoBase=="HORA_ORDINARIA" => ProductorIncidencias.FactoresHoraExtraOrdinaria(segmento,cantidad.Valor),
                "AUSENCIA_SIN_GOCE" when concepto.TipoBase=="REDUCCION_DEVENGO" => ProductorIncidencias.FactoresAusenciaSinGoce(segmento,cantidad.Valor,cantidad.Unidad),
                _ => throw new ConfiguracionConceptoException("CANTIDAD_FORECAST_NO_SOPORTADA")
            };
        }
        else if (sustitucion is { Tipo:"Porcentaje" } porcentaje)
        {
            var baseProyectada = BasesConceptos.Ordinario(segmento);
            factores = new(null,concepto.UnidadPermitida,baseProyectada,porcentaje.Valor/100m,null,baseProyectada*porcentaje.Valor/100m);
        }
        else
        {
            var valor = sustitucion?.Valor ?? historico.Valor;
            factores = new(null,concepto.UnidadPermitida,valor,null,factor,valor*factor);
        }
        var referencia = new ReferenciaSegmentoTemporal(segmento.IdContrato,segmento.IdCondicionLaboralVersion??0,
            segmento.Inicio,segmento.Fin,idSegmentoForecast);
        var fuente = new FuenteLineaMonetaria(segmento.IdColaborador,
            FormattableString.Invariant($"FORECAST:{idSegmentoForecast}:{concepto.IdVersion}"),"Forecast",idSegmentoForecast,
            segmento.Inicio,referencia,soporte);
        var linea = LineaMonetaria.Materializar(concepto,fuente,factores,sustitucion is null?referenciaAutorizacionHistorica:sustitucion.ReferenciaAutorizacion);
        return new(linea,historico,sustitucion?.IdParametro,sustitucion is null?"Historico":"Override");
    }
}

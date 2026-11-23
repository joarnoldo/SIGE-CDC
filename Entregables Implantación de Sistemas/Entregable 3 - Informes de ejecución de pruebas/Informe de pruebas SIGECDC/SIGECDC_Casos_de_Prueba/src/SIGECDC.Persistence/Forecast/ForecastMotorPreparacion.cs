using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;

namespace SIGECDC.Persistence.Forecast;

internal sealed record ResultadoParticipanteForecast(ResultadoCostoLaboral Costo,
    IReadOnlyList<ForecastSegmentoProyectado> Segmentos,IReadOnlyList<ComponenteProyectadoForecast> Variables,
    IReadOnlyDictionary<long,OverrideConceptoForecast> Parametros);

internal static class ForecastMotorPreparacion
{
    internal static async Task<ResultadoParticipanteForecast> PrepararAsync(ApplicationDbContext db,ForecastPeriodo p,
        ForecastParticipante persona,IReadOnlyList<ForecastObservacionHistorica> observaciones,
        IReadOnlyList<AplicabilidadCuota> acreditaciones,string actor,AcumuladoMensualRenta? previo,CancellationToken ct)
    {
        var periodo=new PeriodoSalarial(p.FechaInicio,p.FechaFin,p.TipoPeriodo);periodo.Validar();
        var gobierno=new GobiernoFinancieroService(db);
        var filas=await db.ForecastSegmentosProyectados.Where(s=>s.IdForecastPeriodo==p.IdForecastPeriodo && s.IdForecastParticipante==persona.IdForecastParticipante)
            .OrderBy(s=>s.FechaInicio).ToListAsync(ct);
        if(filas.Count==0)throw new ConfiguracionConceptoException("CONDICION_FORECAST_NO_DEMOSTRABLE");
        var cat=await gobierno.LeerConfiguracionForecastAsync(periodo,persona.IdColaborador is {} real?[real]:[],
            filas.Select(s=>s.IdConfiguracionRiesgoTrabajo).OfType<long>().Distinct().ToArray(),actor,ct);
        var parametros=await db.ForecastParametros.AsNoTracking().Where(v=>v.IdForecastEscenario==p.IdForecastEscenario && v.EstadoRegistro=="Activo").ToArrayAsync(ct);
        var overrides=new Dictionary<long,OverrideConceptoForecast>();
        foreach(var v in parametros)
        {
            if(v.IdConceptoPlanillaVersion is null || v.ValorDecimal is null || v.Unidad is null || v.AutorizacionRequerida is null
                || v.FechaRegistro is null || v.ReferenciaSoporte is null || v.RegistradoPor is null)
                throw new ConfiguracionConceptoException("OVERRIDE_FORECAST_LEGADO_NO_DEMOSTRABLE");
            var concepto=cat.Conceptos.SingleOrDefault(c=>c.IdVersion==v.IdConceptoPlanillaVersion)??throw new ConfiguracionConceptoException("CONCEPTO_VERSION_FALTANTE");
            var valor=new OverrideConceptoForecast(v.IdForecastParametro,concepto.IdVersion,v.TipoParametro,v.Unidad,v.ValorDecimal.Value,
                v.ReferenciaSoporte,v.RegistradoPor,v.AutorizacionRequerida.Value,v.AutorizadoPor,v.FechaAutorizacion,v.ReferenciaAutorizacion);
            ProyeccionFinanciera.ValidarOverride(valor,concepto);
            if(!concepto.Vigencia.Contiene(new VigenciaFinanciera(periodo.Inicio,periodo.Fin)) || !overrides.TryAdd(concepto.IdConcepto,valor))
                throw new ConfiguracionConceptoException("OVERRIDE_FORECAST_AMBIGUO_O_FUERA_VIGENCIA");
        }
        var propias=observaciones.Where(o=>o.IdForecastParticipante==persona.IdForecastParticipante).ToArray();
        var conceptosHistoricos=propias.Select(o=>cat.Conceptos.SingleOrDefault(c=>c.IdVersion==o.IdConceptoPlanillaVersion)
            ??throw new ConfiguracionConceptoException("CONCEPTO_HISTORICO_NO_DEMOSTRABLE")).ToArray();
        var variables=conceptosHistoricos.Concat(cat.Conceptos.Where(c=>overrides.ContainsKey(c.IdConcepto)))
            .Where(c=>c.Codigo!="SALARIO_ORDINARIO" && BasesConceptos.Familia(c.TipoBase) is not (FamiliaBaseConcepto.Cuotas or FamiliaBaseConcepto.Renta or FamiliaBaseConcepto.RiesgosTrabajo))
            .Select(c=>c.Codigo).Distinct().ToArray();
        var activos=cat.Conceptos.Where(c=>c.Codigo=="SALARIO_ORDINARIO" || c.Codigo=="RIESGO_TRABAJO"
            || variables.Contains(c.Codigo) || CatalogoCuotasSociales.Componentes.Any(x=>x.Codigo==c.Codigo)).ToArray();
        var pruebas=acreditaciones.Where(a=>a.IdColaborador==persona.IdForecastParticipante).ToArray();
        if(pruebas.Any(a=>a.ConfirmadoPor!=actor))throw new ConfiguracionConceptoException("ACTOR_APLICABILIDAD_INCOMPATIBLE");
        var fronteras=ResolucionValoresCuotas.Fronteras(cat.Cuotas.SelectMany(v=>new[]{v.Vigencia,v.VigenciaNormativa})
            .Concat(activos.Select(c=>c.Vigencia)).Concat(pruebas.Select(a=>a.Vigencia))
            .Concat(cat.Riesgos.Select(r=>r.Vigencia)),periodo.Fin).Distinct().Order().ToArray();
        // Solo el borrador puede particionar su condición explícita antes de materializar.
        var nuevas=new List<ForecastSegmentoProyectado>();var orden=filas.Max(f=>f.NumeroOrden);
        foreach(var s in filas)
        {
            var cortes=CalendarioSalarial.Dividir(new(s.FechaInicio,s.FechaFin),fronteras);
            for(var n=1;n<cortes.Count;n++)
            {
                var copia=(ForecastSegmentoProyectado)db.Entry(s).CurrentValues.ToObject();copia.IdForecastSegmentoProyectado=0;copia.NumeroOrden=++orden;
                copia.FechaInicio=cortes[n].Inicio;copia.FechaFin=cortes[n].Fin!.Value;copia.DiasBase30=CalendarioSalarial.DiasBase30(copia.FechaInicio,copia.FechaFin);
                nuevas.Add(copia);
            }
            s.FechaFin=cortes[0].Fin!.Value;s.DiasBase30=CalendarioSalarial.DiasBase30(s.FechaInicio,s.FechaFin);
        }
        db.ForecastSegmentosProyectados.AddRange(nuevas);await db.SaveChangesAsync(ct);filas.AddRange(nuevas);filas=filas.OrderBy(s=>s.FechaInicio).ToList();
        foreach(var f in filas.Where(f=>f.IdConfiguracionRiesgoTrabajo is not null))
        {
            var ancla=cat.Riesgos.Single(r=>r.IdConfiguracion==f.IdConfiguracionRiesgoTrabajo);
            var vigentes=cat.Riesgos.Where(r=>r.NumeroPoliza==ancla.NumeroPoliza && r.CodigoActividad==ancla.CodigoActividad
                && r.Vigencia.Solapa(new(f.FechaInicio,f.FechaFin))).ToArray();
            if(vigentes.Length!=1 || !vigentes[0].Vigencia.Contiene(new VigenciaFinanciera(f.FechaInicio,f.FechaFin)))
                throw new ConfiguracionConceptoException("CONFIGURACION_RT_NO_DEMOSTRABLE");
            f.IdConfiguracionRiesgoTrabajo=vigentes[0].IdConfiguracion;
        }
        _=CalendarioSalarial.DiasPorSegmento(new(periodo.Inicio,periodo.Fin),filas.Select(s=>new VigenciaFinanciera(s.FechaInicio,s.FechaFin)).ToArray());
        var inicio=persona.FechaInicioAplicacion>periodo.Inicio?persona.FechaInicioAplicacion:periodo.Inicio;
        var fin=persona.FechaSalidaPrevista is {} salida && salida<periodo.Fin?salida:periodo.Fin;
        if(filas[0].FechaInicio!=inicio || filas[^1].FechaFin!=fin || filas.Zip(filas.Skip(1)).Any(x=>x.First.FechaFin.AddDays(1)!=x.Second.FechaInicio))
            throw new ConfiguracionConceptoException("GAP_CONDICION_FORECAST");
        var catalogos=await gobierno.LeerCatalogosForecastAsync(periodo,filas.Select(f=>f.CodigoJornada).Distinct().ToArray(),
            filas.Select(f=>f.IdOcupacionSalarioMinimo).Distinct().ToArray(),actor,ct);
        var contratos=filas.Select(f=>new ContratoTemporal(f.IdContrato??0,new(f.FechaInicio,f.FechaFin),f.SalarioMensual,f.CodigoJornada,
            f.ReferenciaSoporte,true,f.IdCondicionLaboralVersion,f.IdPuesto,f.IdDepartamento,f.IdJornadaLaboral)).ToArray();
        var ocupaciones=filas.Select(f=>new OcupacionTemporal(f.IdOcupacionSalarioMinimo,f.CodigoOcupacion,new(f.FechaInicio,f.FechaFin),f.ReferenciaSoporte,f.IdContrato??0)).ToArray();
        var temporal=ResolvedorTemporalSalarial.Resolver(periodo,[new(persona.IdForecastParticipante,new(inicio,fin),contratos,ocupaciones,[])],catalogos.Jornadas,catalogos.Minimos);
        if(!temporal.EsResoluble)throw new ConfiguracionConceptoException(temporal.Poblacion.SelectMany(p=>p.Bloqueos).FirstOrDefault()?.Codigo??"CONDICION_FORECAST_NO_RESOLUBLE");
        if(filas.Any(s=>s.OrigenCondicion=="CondicionExistente"))
        {
            var reales=await gobierno.LeerCondicionesForecastAsync(periodo,"Condición existente consumida por Forecast",actor,ct);
            var r=reales.Poblacion.SingleOrDefault(p=>p.IdColaborador==persona.IdColaborador);
            if(r?.EsResoluble!=true || filas.Where(f=>f.OrigenCondicion=="CondicionExistente").Any(f=>!r.Segmentos.Any(s=>s.IdContrato==f.IdContrato
                && s.IdCondicionLaboralVersion==f.IdCondicionLaboralVersion && s.Inicio<=f.FechaInicio && s.Fin>=f.FechaFin && s.SalarioMensual==f.SalarioMensual
                && s.Jornada.IdJornada==f.IdJornadaLaboral && s.Ocupacion.IdOcupacion==f.IdOcupacionSalarioMinimo && s.IdPuesto==f.IdPuesto && s.IdDepartamento==f.IdDepartamento)))
                throw new ConfiguracionConceptoException("CONDICION_EXISTENTE_NO_DEMOSTRABLE");
        }
        var segmentos=temporal.Poblacion.Single().Segmentos;
        if(segmentos.Count!=filas.Count)throw new ConfiguracionConceptoException("CONDICION_FORECAST_VIGENCIA_CAMBIO");
        var lineas=new List<LineaMonetaria>();var variablesResueltas=new List<ComponenteProyectadoForecast>();var otros=new List<HechoCostoPatronal>();
        var historicosPatronales=new Dictionary<string,(MediaConceptoForecast Media,long? Parametro)>();
        foreach(var s in segmentos)
        {
            var f=filas.Single(f=>f.FechaInicio==s.Inicio && f.FechaFin==s.Fin);
            if(s.Minimo.IdMinimo!=f.IdSalarioMinimoVersion || s.Minimo.Monto!=f.MontoMinimo || s.Jornada.HorasDia!=f.HorasOrdinariasDia || s.Jornada.HorasSemana!=f.HorasOrdinariasSemana)
                throw new ConfiguracionConceptoException("CONDICION_PROYECTADA_MODIFICADA");
            var referencia=new ReferenciaSegmentoTemporal(s.IdContrato,s.IdCondicionLaboralVersion??0,s.Inicio,s.Fin,f.IdForecastSegmentoProyectado);
            var ordinario=ResolucionConceptos.Resolver("SALARIO_ORDINARIO",s.Inicio,cat.Conceptos);
            lineas.Add(ProductorLineasConceptos.Ordinario(s,ordinario,new(persona.IdForecastParticipante,
                FormattableString.Invariant($"ORDINARIO-FOR:{f.IdForecastSegmentoProyectado}"),"Forecast",f.IdForecastSegmentoProyectado,s.Inicio,referencia,f.ReferenciaSoporte)));
            foreach(var codigo in variables)
            {
                var c=ResolucionConceptos.Resolver(codigo,s.Inicio,cat.Conceptos);
                var historicos=propias.Where(o=>cat.Conceptos.Single(v=>v.IdVersion==o.IdConceptoPlanillaVersion).IdConcepto==c.IdConcepto).ToArray();
                var media=ProyeccionFinanciera.Media(c.IdConcepto,historicos.Select(o=>new ObservacionConceptoForecast(o.IdForecastObservacionHistorica,c.IdConcepto,o.MontoObservado,o.EsComparable,o.IdForecastFuenteHistorica)));
                overrides.TryGetValue(c.IdConcepto,out var parametro);
                if(parametro?.Tipo=="Cantidad" && segmentos.Count!=1)throw new ConfiguracionConceptoException("DISTRIBUCION_CANTIDAD_FORECAST_NO_DEMOSTRABLE");
                var soporte=parametro?.Soporte??"Observaciones J autorizadas del escenario";
                IReadOnlyList<AutorizacionCostoHistorico> autorizaciones=parametro is null
                    ?await ForecastFuentesSnapshot.AutorizacionHistoricaAsync(db,historicos,c.RequiereAutorizacion,ct)
                    :[];
                var referenciaAutorizacion=parametro?.ReferenciaAutorizacion
                    ??(autorizaciones.Count>0?"Autorizaciones de los hechos J vinculados a las observaciones Forecast.":null);
                if(c.EsPatronal==true)
                {
                    var tipo=parametro?.Tipo??c.UnidadPermitida;decimal? valor=parametro?.Valor;
                    if(tipo=="Cantidad")throw new ConfiguracionConceptoException("CANTIDAD_PATRONAL_NO_SOPORTADA");
                    if(tipo=="Monto")valor*= (decimal)s.DiasBase30/CalendarioSalarial.DiasBase30(periodo.Inicio,periodo.Fin);
                    var clave=FormattableString.Invariant($"FOR:{f.IdForecastSegmentoProyectado}:{c.IdVersion}");
                    otros.Add(new(clave,persona.IdForecastParticipante,c.IdVersion,c.Codigo,
                        s.Inicio,tipo,valor,soporte,parametro?.Actor??actor,parametro?.AutorizadoPor,referenciaAutorizacion,referencia,autorizaciones,
                        parametro is null?new(media.Valor,(decimal)s.DiasBase30/CalendarioSalarial.DiasBase30(periodo.Inicio,periodo.Fin),media.Observaciones):null));
                    historicosPatronales.Add("OTRO_PATRONAL:"+clave,(media,parametro?.IdParametro));
                }
                else
                {
                    var v=ProyeccionFinanciera.Variable(periodo,s,f.IdForecastSegmentoProyectado,c,media,parametro,soporte,referenciaAutorizacion);
                    variablesResueltas.Add(v);lineas.Add(v.Linea);
                }
            }
        }
        var g=ProductorCuotasSociales.Producir(periodo,lineas,cat.Cuotas,cat.Conceptos,pruebas,acumulado:previo?.CuotasSociales);
        if(!g.EsResoluble)throw new ConfiguracionConceptoException(g.Bloqueos[0].Codigo);
        var personales=cat.Creditos.Select(c=>c with{IdColaborador=persona.IdForecastParticipante}).ToArray();
        var h=ProductorRentaSalarial.Producir(periodo,g.Conciliacion!.Lineas,cat.Fiscales,cat.Conceptos,personales,previo) with{CuotasSociales=g};
        if(!h.EsResoluble)throw new ConfiguracionConceptoException(h.Bloqueos[0].Codigo);
        var asociaciones=new List<AsignacionRiesgoContrato>();var riesgos=new List<AplicabilidadRiesgoTrabajo>();
        foreach(var f in filas)
        {
            var cfg=cat.Riesgos.SingleOrDefault(c=>c.IdConfiguracion==f.IdConfiguracionRiesgoTrabajo)
                ??throw new ConfiguracionConceptoException("CONFIGURACION_RT_NO_DEMOSTRABLE");
            if(f.FechaRTInicio is null || f.FechaRTInicio>f.FechaInicio || f.FechaRTFin is {} frt && frt<f.FechaFin
                || !Enum.TryParse<ModalidadBaseRiesgo>(f.ModalidadRT,out var modalidad))throw new ConfiguracionConceptoException("APLICABILIDAD_RT_NO_DEMOSTRABLE");
            var rango=new VigenciaFinanciera(f.FechaInicio,f.FechaFin);
            asociaciones.Add(new(f.IdContrato??0,persona.IdForecastParticipante,rango,cfg.IdConfiguracion,cfg.NumeroPoliza,cfg.CodigoActividad,
                f.ReferenciaAsignacionRT??"",f.AsociadoPorRT??"",!string.IsNullOrWhiteSpace(f.ReferenciaAsignacionRT)&&!string.IsNullOrWhiteSpace(f.AsociadoPorRT)));
            riesgos.Add(new(f.IdContrato??0,cfg.NumeroPoliza,cfg.CodigoActividad,rango,modalidad,f.ReferenciaAsignacionRT??"",f.AsociadoPorRT??""));
        }
        var i=ProductorCostosPatronales.Producir(periodo,h,asociaciones,cat.Riesgos,riesgos,cat.Conceptos,otros,segmentos);
        if(!i.EsResoluble)throw new ConfiguracionConceptoException(i.Bloqueos[0].Codigo);
        foreach(var l in i.OtrosCostos)
        {
            var origen=historicosPatronales[l.Fuente.ClaveHecho];
            variablesResueltas.Add(new(l,origen.Media,origen.Parametro,origen.Parametro is null?"Historico":"Override"));
        }
        return new(i,filas,variablesResueltas,overrides);
    }
}

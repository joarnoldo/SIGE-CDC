using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

// Persistencia del resultado ya producido por D–I. No calcula obligaciones ni usa maestros
// para reconstruir un resultado. DISTRIBUCION identifica atribución de importes materializados;
// LINEA conserva el importe original una sola vez, en el primer destino del concepto.
internal static class ForecastSnapshotEscritura
{
    internal static async Task<IReadOnlyList<ForecastDetalle>> PersistirAsync(ApplicationDbContext db,ForecastPeriodo periodo,
        ForecastParticipante persona,ResultadoParticipanteForecast resultado,IReadOnlyList<DestinoCostoForecast> destinos,
        IReadOnlyList<ForecastObservacionHistorica> observaciones,IReadOnlyList<ForecastDetalle> previos,CancellationToken ct)
    {
        var costo=resultado.Costo;var lineas=costo.Conciliacion!.Lineas;
        var distribucion=ProyeccionFinanciera.DistribuirLineas(costo.Conciliacion,destinos);
        var detalles=new List<ForecastDetalle>();var porConcepto=new Dictionary<long,ForecastDetalle>();
        var aplicaciones=new List<ForecastAplicacionFinanciera>();
        var idsVersiones=observaciones.Select(o=>o.IdConceptoPlanillaVersion).Distinct().ToArray();
        var conceptosObservados=await db.ConceptosPlanillaVersion.AsNoTracking().Where(v=>idsVersiones.Contains(v.IdConceptoPlanillaVersion))
            .ToDictionaryAsync(v=>v.IdConceptoPlanillaVersion,v=>v.IdConceptoPlanilla,ct);
        var medias=lineas.Select(l=>l.Concepto.IdConcepto).Distinct().ToDictionary(id=>id,id=>ProyeccionFinanciera.Media(id,
            observaciones.Where(o=>o.IdForecastParticipante==persona.IdForecastParticipante && conceptosObservados[o.IdConceptoPlanillaVersion]==id)
                .Select(o=>new ObservacionConceptoForecast(o.IdForecastObservacionHistorica,id,o.MontoObservado,o.EsComparable,o.IdForecastFuenteHistorica))));
        foreach(var celda in distribucion)
        {
            var lineasConcepto=lineas.Where(l=>l.Concepto.IdVersion==celda.IdConceptoVersion).ToArray();var l=lineasConcepto[0];
            var variable=resultado.Variables.FirstOrDefault(v=>v.Linea.Concepto.IdVersion==l.Concepto.IdVersion);
            var media=variable?.Historico??medias[l.Concepto.IdConcepto];
            var baseHistorica=media.Valor*celda.Destino.Porcentaje/100m;
            var d=new ForecastDetalle{IdForecastEscenario=periodo.IdForecastEscenario,IdForecastPeriodo=periodo.IdForecastPeriodo,
                IdForecastParticipante=persona.IdForecastParticipante,IdProyecto=celda.Destino.IdProyecto,IdForecastAsignacionProyecto=celda.Destino.IdAsignacion,
                IdConceptoPlanillaVersion=l.Concepto.IdVersion,Concepto=l.Concepto.Codigo,MontoBaseSinRedondear=baseHistorica,
                MontoBase=PrecisionMonetaria.R2(baseHistorica),MontoProyectadoSinRedondear=celda.SinRedondear,MontoProyectado=celda.Monto,
                MontoAjuste=celda.Monto-PrecisionMonetaria.R2(baseHistorica),CantidadObservaciones=media.Cantidad,HistoricoLimitado=media.HistoricoLimitado,
                VersionMotor=ProyeccionFinanciera.VersionMotor,Observaciones=media.Cantidad==0?"Sin observaciones históricas comparables; no se inventó una observación cero.":null};
            detalles.Add(d);porConcepto.TryAdd(l.Concepto.IdVersion,d);
        }
        db.ForecastDetalles.AddRange(detalles);await db.SaveChangesAsync(ct);
        foreach(var celda in distribucion)
        {
            var d=detalles.Single(d=>d.IdForecastAsignacionProyecto==celda.Destino.IdAsignacion && d.IdConceptoPlanillaVersion==celda.IdConceptoVersion);
            var ls=lineas.Where(l=>l.Concepto.IdVersion==celda.IdConceptoVersion).ToArray();var l=ls[0];
            var rangos=ls.Select(l=>l.Fuente.Segmento is {} s?new VigenciaFinanciera(s.Inicio,s.Fin):new(l.Fuente.FechaHecho,l.Fuente.FechaHecho)).ToArray();
            var a=Crear(d,l,"DISTRIBUCION","Derivado",new(rangos.Min(r=>r.Inicio),rangos.Max(r=>r.Fin)));
            a.BaseAplicada=celda.BaseMaterializada;a.ValorAplicado=celda.Destino.Porcentaje/100m;
            a.MontoSinRedondear=celda.SinRedondear;a.MontoMaterializado=celda.Monto;aplicaciones.Add(a);
        }
        foreach(var l in lineas)
        {
            var d=porConcepto[l.Concepto.IdVersion];var s=l.Fuente.Segmento;
            var rango=s is null?new VigenciaFinanciera(l.Fuente.FechaHecho,l.Fuente.FechaHecho):new(s.Inicio,s.Fin);
            var variable=resultado.Variables.SingleOrDefault(v=>ReferenceEquals(v.Linea,l));
            var fuente=variable?.IdParametro is not null?"Override":variable?.Historico.Cantidad>0?"Historico":s?.IdForecastSegmentoProyectado is not null?"CondicionProyectada":"Derivado";
            var a=Crear(d,l,"LINEA",fuente,rango);
            a.IdForecastSegmentoProyectado=s?.IdForecastSegmentoProyectado;a.IdForecastParametro=variable?.IdParametro;
            a.IdForecastObservacionHistorica=variable?.Historico.Observaciones.FirstOrDefault() is >0?variable.Historico.Observaciones[0]:null;
            if(resultado.Parametros.TryGetValue(l.Concepto.IdConcepto,out var parametro))
            {a.IdForecastParametro=parametro.IdParametro;a.FuenteEfectiva="Override";}
            a.BaseAplicada=l.Factores.BaseSinRedondear;a.ValorAplicado=l.Factores.Tasa;a.Cantidad=l.Factores.Cantidad;a.Multiplicador=l.Factores.Multiplicador;
            a.MontoSinRedondear=l.Factores.MontoSinRedondear;a.MontoMaterializado=l.Monto;a.ReferenciaSoporte=l.Fuente.ReferenciaSoporte;
            aplicaciones.Add(a);
            // Una referencia por observación realmente consumida, sin dividir el promedio otra vez.
            if(variable is not null)
            foreach(var id in variable.Historico.Observaciones)
            {
                var o=observaciones.Single(o=>o.IdForecastObservacionHistorica==id);
                var h=Crear(d,l,"OBSERVACION","Historico",rango);h.IdForecastObservacionHistorica=id;
                h.IdForecastSegmentoProyectado=s?.IdForecastSegmentoProyectado;h.BaseAplicada=variable.Historico.Valor;h.Cantidad=variable.Historico.Cantidad;
                h.MontoSinRedondear=o.MontoObservado!.Value;aplicaciones.Add(h);
            }
        }
        // Ordinario y obligaciones se recalculan con D–I. Su base histórica informativa
        // conserva, igualmente, observaciones reales; no se convierte en un cero ficticio.
        foreach(var grupo in lineas.GroupBy(l=>l.Concepto.IdVersion).Where(g=>!resultado.Variables.Any(v=>v.Linea.Concepto.IdVersion==g.Key)))
        {
            var l=grupo.First();var media=medias[l.Concepto.IdConcepto];var d=porConcepto[l.Concepto.IdVersion];
            foreach(var id in media.Observaciones)
            {
                var o=observaciones.Single(o=>o.IdForecastObservacionHistorica==id);
                var h=Crear(d,l,"OBSERVACION","Historico",new(periodo.FechaInicio,periodo.FechaFin));
                h.IdForecastObservacionHistorica=id;h.BaseAplicada=media.Valor;h.Cantidad=media.Cantidad;
                h.MontoSinRedondear=o.MontoObservado!.Value;aplicaciones.Add(h);
            }
        }
        var g=costo.Renta!.CuotasSociales!;
        foreach(var a in g.Aplicaciones)AgregarCuota(a,"CorteActual",null);
        foreach(var r in g.Regularizaciones)
        {
            foreach(var a in r.ObligacionMensual)AgregarCuota(a,"MensualDefinitiva",null,r.Linea);
            foreach(var a in r.CuotasPrevias)AgregarCuota(a,"AcumuladoPrevio",Anterior(a.Linea.Concepto.IdVersion),r.Linea);
        }
        foreach(var a in costo.Renta.Aplicaciones)
        {
            AgregarRenta(a.Linea,a.Evaluacion,a.EvaluacionPrevia is null?"CorteActual":"MensualDefinitiva",null);
            if(a.EvaluacionPrevia is {} anterior)AgregarRenta(a.Linea,anterior,"AcumuladoPrevio",Anterior(anterior.ImpuestoAcumulado.Concepto.IdVersion));
        }
        foreach(var r in costo.AplicacionesRT)
        {
            var d=porConcepto[r.Linea.Concepto.IdVersion];var a=Crear(d,r.Linea,"RT","Normativa",r.Rango);
            var segmentos=resultado.Segmentos.Where(s=>s.FechaInicio<=r.Rango.Inicio && s.FechaFin>=r.Rango.Fin!.Value
                && s.IdConfiguracionRiesgoTrabajo==r.Configuracion.IdConfiguracion).ToArray();
            if(segmentos.Length!=1)throw new ConfiguracionConceptoException("RT_SEGMENTO_FORECAST_NO_DEMOSTRABLE");
            a.IdForecastSegmentoProyectado=segmentos[0].IdForecastSegmentoProyectado;a.IdConfiguracionRiesgoTrabajo=r.Configuracion.IdConfiguracion;
            a.ModalidadRT=r.Aplicabilidad.Modalidad.ToString();a.NumeroPoliza=r.Configuracion.NumeroPoliza;a.CodigoActividad=r.Configuracion.CodigoActividad;
            a.DescuentoRT=r.Configuracion.Descuento;a.RecargoRT=r.Configuracion.Recargo;a.BaseAplicada=r.BaseAplicada;a.ValorAplicado=r.Configuracion.Tarifa;
            a.Multiplicador=r.Configuracion.Factor;a.Sujeto="Patrono";a.MontoSinRedondear=r.Linea.Factores.MontoSinRedondear;a.MontoMaterializado=r.Linea.Monto;
            a.ReferenciaSoporte=r.Aplicabilidad.ReferenciaSoporte;a.AcreditadoPor=r.Aplicabilidad.ConfirmadoPor;
            a.FechaAplicabilidadInicio=r.Aplicabilidad.Vigencia.Inicio;a.FechaAplicabilidadFin=r.Aplicabilidad.Vigencia.Fin;aplicaciones.Add(a);
        }
        // El corte final fija todas las entradas del corte previo, no solo su total opaco.
        foreach(var dAnterior in previos.OrderBy(d=>d.IdForecastDetalle))
        {
            var actual=porConcepto.GetValueOrDefault(dAnterior.IdConceptoPlanillaVersion!.Value)??detalles[0];
            var l=lineas.First(l=>l.Concepto.IdVersion==actual.IdConceptoPlanillaVersion);
            var a=Crear(actual,l,"ENTRADA_PREVIA","Derivado",new(periodo.FechaInicio,periodo.FechaInicio));
            a.AlcanceAplicacion="AcumuladoPrevio";a.IdForecastDetallePrevio=dAnterior.IdForecastDetalle;
            a.MontoSinRedondear=dAnterior.MontoProyectado;a.MontoMaterializado=null;aplicaciones.Add(a);
        }
        foreach(var grupo in aplicaciones.GroupBy(a=>a.IdForecastDetalle))
        {var orden=0;foreach(var a in grupo)a.NumeroOrden=++orden;}
        db.ForecastAplicacionesFinancieras.AddRange(aplicaciones);await db.SaveChangesAsync(ct);
        return detalles;

        long Anterior(long version)=>previos.Where(d=>d.IdConceptoPlanillaVersion==version).OrderBy(d=>d.IdForecastDetalle).FirstOrDefault()?.IdForecastDetalle
            ??throw new ConfiguracionConceptoException("ACUMULADO_FORECAST_NO_DEMOSTRABLE");

        void AgregarCuota(AplicacionCuota q,string alcance,long? anterior,LineaMonetaria? lineaCorte=null)
        {
            var l=lineaCorte??q.Linea;var d=porConcepto[l.Concepto.IdVersion];var a=Crear(d,l,q.Tasa.Codigo,"Normativa",q.Rango);
            a.AlcanceAplicacion=alcance;a.IdForecastDetallePrevio=anterior;a.IdValorParametroNormativo=q.Tasa.IdValorParametroNormativo;
            a.BaseAplicada=q.BaseAplicada;a.ValorAplicado=q.Tasa.Valor;a.Unidad=q.Tasa.Unidad;a.TipoBase=q.Tasa.CodigoBase;a.Sujeto=q.Tasa.Sujeto;
            a.MontoSinRedondear=q.Linea.Factores.MontoSinRedondear;a.MontoMaterializado=q.Linea.Monto;
            a.ReglaAplicabilidad=q.Aplicabilidad.Regla.ToString();a.FechaAplicabilidadInicio=q.Aplicabilidad.Vigencia.Inicio;a.FechaAplicabilidadFin=q.Aplicabilidad.Vigencia.Fin;
            a.ReferenciaSoporte=q.Aplicabilidad.ReferenciaSoporte;a.ReferenciaRegla=q.Aplicabilidad.ReferenciaRegla;a.AcreditadoPor=q.Aplicabilidad.ConfirmadoPor;
            aplicaciones.Add(a);
            if(q.Minimo is {} m)
            {
                var b=Crear(d,l,m.Codigo,"Normativa",q.Rango);b.AlcanceAplicacion=alcance;b.IdForecastDetallePrevio=anterior;
                b.IdValorParametroNormativo=m.IdValorParametroNormativo;b.BaseAplicada=q.BaseContributiva;b.ValorAplicado=m.Valor;b.Unidad=m.Unidad;b.TipoBase=m.CodigoBase;b.Sujeto=m.Sujeto;
                b.MontoSinRedondear=q.BaseAplicada;b.MontoMaterializado=null;aplicaciones.Add(b);
            }
        }
        void AgregarRenta(LineaMonetaria l,EvaluacionRenta e,string alcance,long? anterior)
        {
            var d=porConcepto[l.Concepto.IdVersion];
            foreach(var t in e.Tramos)
            {
                var a=Crear(d,l,"TRAMO_RENTA","Normativa",e.RangoAcumulado);a.AlcanceAplicacion=alcance;a.IdForecastDetallePrevio=anterior;
                a.IdTramoImpuestoSalario=t.Tramo.IdTramo;a.BaseAplicada=t.BaseGravada;a.ValorAplicado=t.Tramo.Tasa;a.Unidad="Proporcion";a.TipoBase="BASE_RENTA";a.Sujeto="Trabajador";
                a.MontoSinRedondear=t.ImpuestoSinRedondear;a.MontoMaterializado=null;aplicaciones.Add(a);
            }
            foreach(var c in e.Creditos)
            {
                var a=Crear(d,l,"CREDITO_RENTA","Normativa",e.RangoAcumulado);a.AlcanceAplicacion=alcance;a.IdForecastDetallePrevio=anterior;
                a.IdCreditoFiscalNormativo=c.Normativo.IdCreditoNormativo;a.IdCreditoFiscalColaborador=c.Personal.IdCreditoColaborador;
                a.BaseAplicada=c.Personal.Cantidad;a.Cantidad=c.Personal.Cantidad;a.ValorAplicado=c.Normativo.MontoUnitario;a.Unidad="Monto";a.TipoBase="CREDITO_RENTA";a.Sujeto="Trabajador";
                a.MontoSinRedondear=c.Monto;a.MontoMaterializado=null;aplicaciones.Add(a);
            }
        }
    }

    private static ForecastAplicacionFinanciera Crear(ForecastDetalle d,LineaMonetaria l,string componente,string fuente,VigenciaFinanciera rango)
        =>new(){IdForecastDetalle=d.IdForecastDetalle,Componente=componente,FuenteEfectiva=fuente,AlcanceAplicacion="CorteActual",FechaInicio=rango.Inicio,FechaFin=rango.Fin??rango.Inicio,
            Unidad=l.Factores.Unidad,TipoBase=l.Concepto.TipoBase,EsSalarial=l.Concepto.EsSalarial!.Value,EsContributivoCCSS=l.Concepto.EsContributivoCCSS!.Value,
            EsTributario=l.Concepto.EsTributario!.Value,EsPatronal=l.Concepto.EsPatronal!.Value,Efecto=l.Efecto.ToString(),DescripcionConcepto=l.Concepto.Nombre};

    internal static async Task ConciliarAsync(ApplicationDbContext db,long escenario,decimal total,CancellationToken ct)
    {
        var detalles=await db.ForecastDetalles.AsNoTracking().Where(d=>d.IdForecastEscenario==escenario).ToArrayAsync(ct);
        var ids=detalles.Select(d=>d.IdForecastDetalle).ToArray();
        var aplicaciones=await db.ForecastAplicacionesFinancieras.AsNoTracking().Where(a=>ids.Contains(a.IdForecastDetalle)).ToArrayAsync(ct);
        decimal costo=0;
        foreach(var d in detalles)
        {
            var distribucion=aplicaciones.Where(a=>a.IdForecastDetalle==d.IdForecastDetalle && a.Componente=="DISTRIBUCION").ToArray();
            if(d.VersionMotor!=ProyeccionFinanciera.VersionMotor || distribucion.Length!=1 || distribucion[0].MontoMaterializado!=d.MontoProyectado
                || distribucion[0].MontoSinRedondear!=d.MontoProyectadoSinRedondear)throw new ConfiguracionConceptoException("FORECAST_SNAPSHOT_NO_CONCILIA");
            costo+=d.MontoProyectado*Signo(distribucion[0].Efecto);
        }
        foreach(var grupo in detalles.GroupBy(d=>(d.IdForecastPeriodo,d.IdForecastParticipante,d.IdConceptoPlanillaVersion)))
        {
            var propios=grupo.Select(d=>d.IdForecastDetalle).ToArray();
            var originales=aplicaciones.Where(a=>propios.Contains(a.IdForecastDetalle) && a.Componente=="LINEA").ToArray();
            if(originales.Length==0 || originales.Any(a=>a.MontoMaterializado is null) || originales.Sum(a=>a.MontoMaterializado)!=grupo.Sum(d=>d.MontoProyectado))
                throw new ConfiguracionConceptoException("FORECAST_SNAPSHOT_NO_CONCILIA");
        }
        if(detalles.Length==0 || costo!=total)throw new ConfiguracionConceptoException("FORECAST_SNAPSHOT_NO_CONCILIA");
    }
    internal static int Signo(string efecto)=>efecto switch
    {nameof(EfectoMonetario.IngresoSalarial) or nameof(EfectoMonetario.CargaPatronal)=>1,nameof(EfectoMonetario.ReduccionDevengo)=>-1,
        nameof(EfectoMonetario.IngresoNoSalarial) or nameof(EfectoMonetario.DeduccionLegal) or nameof(EfectoMonetario.DeduccionInterna)=>0,
        _=>throw new ConfiguracionConceptoException("EFECTO_FORECAST_INVALIDO")};
}

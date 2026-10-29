using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    private async Task ConstruirSnapshotAsync(EjecucionPlanilla e, ResultadoCostoLaboral r, SolicitudSnapshotPlanilla solicitud, CancellationToken ct)
    {
        var periodo = new PeriodoSalarial(e.FechaInicioSnapshot, e.FechaFinSnapshot, e.TipoPeriodoSnapshot);
        var temporal = await ResolverTemporalEnTransaccionAsync(periodo, solicitud.ReferenciaRelacionOrdinaria, ct);
        if (!temporal.EsResoluble) throw new ConfiguracionConceptoException("TEMPORAL_NO_DEMOSTRABLE");
        var personas = await FiltrarClaves(contexto.Colaboradores.AsNoTracking(), p => p.IdColaborador,
            r.Conciliacion!.Lineas.Select(l => l.Fuente.IdColaborador)).ToDictionaryAsync(p => p.IdColaborador, ct);
        var condiciones = await FiltrarClaves(contexto.CondicionesLaboralesVersion.AsNoTracking()
            .Include(c => c.Puesto).Include(c => c.Departamento).Include(c => c.Contrato).Include(c => c.OcupacionSalarioMinimo),
            c => c.IdCondicionLaboralVersion, temporal.Poblacion.SelectMany(p => p.Segmentos).Select(s => s.IdCondicionLaboralVersion!.Value))
            .ToDictionaryAsync(c => c.IdCondicionLaboralVersion, ct);
        var previos = solicitud.IdEjecucionPrevia is { } idPrevio ? (await CargarSnapshotAsync(idPrevio, ct)).Detalles.ToDictionary(d => d.IdColaborador) : [];
        foreach (var grupo in r.Conciliacion.Lineas.GroupBy(l => l.Fuente.IdColaborador).OrderBy(g => g.Key))
        {
            var persona = personas[grupo.Key]; var lineas = grupo.ToArray();
            var totales = AgregadorLineasMonetarias.Conciliar(lineas).Totales;
            var g = r.Renta!.CuotasSociales!;
            decimal Cuotas(EfectoMonetario efecto) => g.LineasCuotasCorte.Where(l => l.Fuente.IdColaborador == grupo.Key && l.Efecto == efecto).Sum(l => l.Monto);
            var d = new DetallePlanilla { IdPlanilla = e.IdPlanilla, IdColaborador = grupo.Key, FechaCreacion = e.FechaCalculo,
                CodigoColaborador = persona.CodigoColaborador, NombreCompleto = string.Join(" ", new[] { persona.Nombre, persona.PrimerApellido, persona.SegundoApellido }.Where(s => !string.IsNullOrWhiteSpace(s))),
                SnapshotCompleto = true, FuenteDimensionesSnapshot = "CondicionLaboralVersion/Segmentos",
                SalarioBruto = totales.BrutoSalarial, IngresosNoSalariales = totales.IngresosNoSalariales,
                CuotasObreras = Cuotas(EfectoMonetario.DeduccionLegal), ImpuestoSalario = r.Renta.Aplicaciones.Where(a => a.Linea.Fuente.IdColaborador == grupo.Key).Sum(a => a.Linea.Monto),
                OtrasDeducciones = totales.DeduccionesInternas, TotalDeducciones = totales.Deducciones,
                SalarioNeto = totales.SaldoAlgebraico, CargasPatronales = totales.CargasPatronales,
                CostoPatronalEstimado = totales.CargasPatronales, CostoLaboralTotal = totales.CostoAlgebraico,
                RiesgosTrabajo = r.AplicacionesRT.Where(a => a.Linea.Fuente.IdColaborador == grupo.Key).Sum(a => a.Linea.Monto),
                OtrosCostosPatronales = r.OtrosCostos.Where(l => l.Fuente.IdColaborador == grupo.Key).Sum(l => l.Monto), TotalAusencias = totales.ReduccionesDevengo };
            var segmentos = new Dictionary<ReferenciaSegmentoTemporal, SegmentoCalculoPlanilla>();
            foreach (var l in lineas.Where(l => l.Fuente.TipoFuente == "Contrato" && l.Concepto.TipoBase == "SALARIO_DEVENGADO"))
            {
                var clave = l.Fuente.Segmento ?? throw new ConfiguracionConceptoException("SEGMENTO_FALTANTE");
                var original = temporal.Poblacion.Single(p => p.IdColaborador == grupo.Key).Segmentos.Single(s =>
                    s.IdCondicionLaboralVersion == clave.IdCondicion && s.Inicio <= clave.Inicio && s.Fin >= clave.Fin);
                var c = condiciones[clave.IdCondicion];
                // E puede dividir D por fronteras G/I: conservar el subsegmento y sus unidades efectivamente consumidas.
                var s = new SegmentoCalculoPlanilla { NumeroOrden = segmentos.Count + 1, FechaInicio = clave.Inicio, FechaFin = clave.Fin,
                    IdContratoOrigen = clave.IdContrato, IdCondicionLaboralVersion = clave.IdCondicion,
                    IdJornadaLaboral = original.Jornada.IdJornada, IdSalarioMinimoVersion = original.Minimo.IdMinimo,
                    NombrePuesto = c.Puesto!.Nombre, NombreDepartamento = c.Departamento!.Nombre,
                    TipoContrato = c.Contrato!.TipoContrato, PeriodicidadPago = c.Contrato.PeriodicidadPago,
                    CodigoOcupacion = original.Ocupacion.Codigo, NombreOcupacion = c.OcupacionSalarioMinimo!.Nombre,
                    TipoJornada = original.Jornada.Tipo, CodigoJornada = original.Jornada.Codigo,
                    HorasOrdinariasDia = original.Jornada.HorasDia, HorasOrdinariasSemana = original.Jornada.HorasSemana,
                    FechaJornadaInicio = original.Jornada.Vigencia.Inicio, FechaJornadaFin = original.Jornada.Vigencia.Fin,
                    MontoMinimo = original.Minimo.Monto, PeriodicidadMinimo = original.Minimo.Periodicidad, TipoJornadaMinimo = original.Minimo.TipoJornada,
                    FechaMinimoInicio = original.Minimo.Vigencia.Inicio, FechaMinimoFin = original.Minimo.Vigencia.Fin,
                    ReferenciaRelacionOrdinaria = original.ReferenciaRelacionOrdinaria, SalarioMensual = original.SalarioMensual,
                    DiasBase30 = l.Factores.Cantidad!.Value, MontoOrdinarioSinRedondear = l.Factores.MontoSinRedondear, MontoOrdinario = l.Monto };
                segmentos.Add(clave, s); d.Segmentos.Add(s);
            }
            if (segmentos.Count == 0) throw new ConfiguracionConceptoException("SEGMENTOS_FALTANTES");
            static T? Unico<T>(IEnumerable<T> valores) { var a = valores.Distinct().ToArray(); return a.Length == 1 ? a[0] : default; }
            d.NombrePuesto = Unico(d.Segmentos.Select(s => s.NombrePuesto)); d.NombreDepartamento = Unico(d.Segmentos.Select(s => s.NombreDepartamento));
            d.TipoContrato = Unico(d.Segmentos.Select(s => s.TipoContrato)); d.PeriodicidadPago = Unico(d.Segmentos.Select(s => s.PeriodicidadPago));
            d.CodigoJornada = Unico(d.Segmentos.Select(s => s.CodigoJornada)); d.HorasOrdinariasDia = Unico(d.Segmentos.Select(s => (decimal?)s.HorasOrdinariasDia));
            d.SalarioContractual = Unico(d.Segmentos.Select(s => (decimal?)s.SalarioMensual)); d.SalarioBase = d.SalarioContractual ?? 0;
            d.SalarioProporcional = d.Segmentos.Sum(s => s.MontoOrdinario);
            d.TotalHorasExtra = lineas.Where(l => l.Concepto.TipoBase == "HORA_ORDINARIA").Sum(l => l.Monto);
            d.TotalBonos = lineas.Where(l => l.Efecto == EfectoMonetario.IngresoSalarial && l.Fuente.TipoFuente != "Contrato" && l.Concepto.TipoBase != "HORA_ORDINARIA").Sum(l => l.Monto);
            foreach (var l in lineas)
            {
                var f = l.Factores; var p = l.ParametroAplicado;
                var otro = solicitud.OtrosCostos.SingleOrDefault(h => h.IdColaborador == grupo.Key && "OTRO_PATRONAL:" + h.ClaveHecho == l.Fuente.ClaveHecho);
                var segmentoLinea = l.Fuente.Segmento is { } sr ? segmentos.GetValueOrDefault(sr) : null;
                var baseHecho = segmentoLinea is null && otro is not null ? otro.BaseTemporal : null;
                if (l.Fuente.Segmento is not null && segmentoLinea is null && baseHecho is null)
                    throw new ConfiguracionConceptoException("SEGMENTO_LINEA_NO_DEMOSTRABLE");
                var fila = new ConceptoDetallePlanilla { NumeroOrden = d.Conceptos.Count + 1, IdConceptoPlanillaVersion = l.Concepto.IdVersion,
                    SegmentoCalculoPlanilla = segmentoLinea, IdCondicionLaboralBase = baseHecho?.IdCondicion,
                    FechaBaseInicio = baseHecho?.Inicio, FechaBaseFin = baseHecho?.Fin,
                    TipoFuente = l.Fuente.TipoFuente, IdFuente = l.Fuente.IdFuente, FechaHecho = l.Fuente.FechaHecho,
                    Cantidad = f.Cantidad, Unidad = f.Unidad, UnidadCantidad = f.UnidadCantidad, BaseCalculo = f.BaseSinRedondear,
                    Tasa = f.Tasa, Multiplicador = f.Multiplicador, MontoSinRedondear = f.MontoSinRedondear, Monto = l.Monto,
                    EsSalarialSnapshot = l.Concepto.EsSalarial!.Value, EsContributivoCCSSSnapshot = l.Concepto.EsContributivoCCSS!.Value,
                    EsTributarioSnapshot = l.Concepto.EsTributario!.Value, EsPatronalSnapshot = l.Concepto.EsPatronal!.Value,
                    DescripcionSnapshot = l.Concepto.Nombre, ClaveHecho = l.Fuente.ClaveHecho, ReferenciaSoporte = l.Fuente.ReferenciaSoporte,
                    ReferenciaAutorizacion = l.ReferenciaAutorizacion, RegistradoPorHecho = otro?.RegistradoPor, AutorizadoPorHecho = otro?.AutorizadoPor,
                    IdIncidenciaPlanillaConfirmacion = l.Fuente.IdConfirmacionIncidencia,
                    IdParametroPlanilla = p?.Version.IdVersion, AmbitoParametro = p?.Ambito.ToString(),
                    IdParametroPlanillaPeriodo = p?.Ambito == AmbitoParametroInterno.Periodo ? p.IdOverride : null,
                    IdParametroPlanillaColaborador = p?.Ambito == AmbitoParametroInterno.Colaborador ? p.IdOverride : null,
                    ValorParametroAplicado = p?.ValorDecimal, UnidadParametroAplicada = p?.Version.Tipo };
                if (fila.TipoFuente == "Incidencia")
                {
                    var confirmacion = await contexto.ConfirmacionesIncidencias.AsNoTracking().SingleOrDefaultAsync(c => c.IdIncidenciaPlanillaConfirmacion == fila.IdIncidenciaPlanillaConfirmacion, ct);
                    if (confirmacion is null || confirmacion.IdIncidenciaPlanilla != fila.IdFuente || confirmacion.NumeroVersion != l.Fuente.NumeroVersionConfirmacion
                        || confirmacion.IdConceptoPlanillaVersion != fila.IdConceptoPlanillaVersion)
                        throw new ConfiguracionConceptoException("CONFIRMACION_SNAPSHOT_INCOMPATIBLE");
                }
                AgregarAplicacionesSnapshot(fila, l, r, previos.GetValueOrDefault(grupo.Key)?.IdDetallePlanilla);
                d.Conceptos.Add(fila);
            }
            e.Detalles.Add(d);
        }
    }

    private static void TotalizarSnapshot(EjecucionPlanilla e)
    {
        e.SalarioBrutoTotal = e.Detalles.Sum(d => d.SalarioBruto); e.IngresosNoSalarialesTotal = e.Detalles.Sum(d => d.IngresosNoSalariales);
        e.DeduccionesTotal = e.Detalles.Sum(d => d.TotalDeducciones); e.SalarioNetoTotal = e.Detalles.Sum(d => d.SalarioNeto);
        e.CargasPatronalesTotal = e.Detalles.Sum(d => d.CargasPatronales); e.RiesgosTrabajoTotal = e.Detalles.Sum(d => d.RiesgosTrabajo);
        e.OtrosCostosPatronalesTotal = e.Detalles.Sum(d => d.OtrosCostosPatronales); e.CostoLaboralTotal = e.Detalles.Sum(d => d.CostoLaboralTotal);
    }
}

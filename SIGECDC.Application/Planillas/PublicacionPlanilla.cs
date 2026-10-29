using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

// Datos de salida exclusivamente históricos; no expone soportes privados ni actores de acreditación.
public sealed record SegmentoPublicacionPlanilla(DateTime Inicio, DateTime Fin, string Puesto,
    string Departamento, string TipoContrato, string Jornada, decimal HorasDia, decimal SalarioMensual, decimal DiasBase30);
public sealed record LineaPublicacionPlanilla(int Orden, string Descripcion, string Clasificacion,
    EfectoMonetario Efecto, DateTime? Fecha, decimal? Cantidad, string? Unidad, decimal Monto)
{
    public string EtiquetaEfecto => Efecto switch
    {
        EfectoMonetario.IngresoSalarial => "Ingreso salarial",
        EfectoMonetario.IngresoNoSalarial => "Ingreso no salarial",
        EfectoMonetario.ReduccionDevengo => "Reducción de devengo",
        EfectoMonetario.DeduccionLegal => "Deducción legal",
        EfectoMonetario.DeduccionInterna => "Deducción interna",
        EfectoMonetario.CargaPatronal => "Carga patronal",
        _ => throw new InvalidOperationException("EFECTO_NO_PUBLICABLE")
    };
}

public static class PublicacionPlanilla
{
    public static string Rotulo(string estado) => estado switch
    {
        "Calculada" => "NO AUTORIZADA PARA PAGO",
        "Aprobada" => "APROBADA",
        "Cerrada" => "CERRADA",
        _ => throw new InvalidOperationException("EJECUCION_NO_PUBLICABLE")
    };

    public static ColillaPagoDetalle Proyectar(EjecucionPlanilla e, DetallePlanilla d,
        IReadOnlyDictionary<long, ConceptoPlanillaVersion> versiones, string estado, ColillaPago metadata,
        bool incluirPatronal = false)
    {
        Exigir(e.SnapshotCompleto && e.EstadoReproducibilidad == "REPRODUCIBLE"
            && e.EstadoEjecucion == (estado == "Calculada" ? "Calculada" : "Autorizada")
            && d.SnapshotCompleto && d.IdEjecucionPlanilla == e.IdEjecucionPlanilla && d.IdPlanilla == e.IdPlanilla
            && metadata.IdDetallePlanilla == d.IdDetallePlanilla && d.Segmentos.Count > 0 && d.Conceptos.Count > 0
            && !string.IsNullOrWhiteSpace(d.CodigoColaborador) && !string.IsNullOrWhiteSpace(d.NombreCompleto));
        _ = Rotulo(estado);
        var lineas = d.Conceptos.OrderBy(l => l.NumeroOrden).Select(l =>
        {
            Exigir(versiones.ContainsKey(l.IdConceptoPlanillaVersion));
            // FK a la versión exacta protegida por J; nunca catálogo vivo ni resolver otra vigencia.
            var v = versiones[l.IdConceptoPlanillaVersion];
            Exigir(v.EsSalarial == l.EsSalarialSnapshot && v.EsContributivoCCSS == l.EsContributivoCCSSSnapshot
                && v.EsTributario == l.EsTributarioSnapshot && v.EsPatronal == l.EsPatronalSnapshot
                && !string.IsNullOrWhiteSpace(l.DescripcionSnapshot) && l.Monto >= 0);
            var efecto = ResolucionConceptos.Validar(new(v.IdConceptoPlanilla, v.IdConceptoPlanillaVersion,
                v.IdConceptoPlanillaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), l.DescripcionSnapshot,
                new(v.FechaVigenciaInicio, v.FechaVigenciaFin), v.Clasificacion,
                l.EsSalarialSnapshot, l.EsContributivoCCSSSnapshot, l.EsTributarioSnapshot, l.EsPatronalSnapshot,
                v.TipoBase, v.UnidadPermitida, v.PermiteMontoManual, v.RequiereAutorizacion, true, true));
            return new LineaPublicacionPlanilla(l.NumeroOrden, l.DescripcionSnapshot, v.Clasificacion, efecto,
                l.FechaHecho, l.Cantidad, l.UnidadCantidad ?? l.Unidad, l.Monto);
        }).ToArray();
        Exigir(lineas.Select(l => l.Orden).Distinct().Count() == lineas.Length);
        decimal Suma(EfectoMonetario efecto) => lineas.Where(l => l.Efecto == efecto).Sum(l => l.Monto);
        // Conciliación de importes ya persistidos. No tasas, redondeo ni motor monetario.
        Exigir(Suma(EfectoMonetario.IngresoSalarial) - Suma(EfectoMonetario.ReduccionDevengo) == d.SalarioBruto
            && Suma(EfectoMonetario.IngresoNoSalarial) == d.IngresosNoSalariales
            && Suma(EfectoMonetario.ReduccionDevengo) == d.TotalAusencias
            && Suma(EfectoMonetario.DeduccionLegal) == d.CuotasObreras + d.ImpuestoSalario
            && Suma(EfectoMonetario.DeduccionInterna) == d.OtrasDeducciones
            && d.CuotasObreras + d.ImpuestoSalario + d.OtrasDeducciones == d.TotalDeducciones
            && Suma(EfectoMonetario.CargaPatronal) == d.CargasPatronales
            && d.SalarioBruto + d.IngresosNoSalariales - d.TotalDeducciones == d.SalarioNeto
            && d.SalarioBruto + d.CargasPatronales == d.CostoLaboralTotal && d.SalarioNeto >= 0);
        var segmentos = d.Segmentos.OrderBy(s => s.NumeroOrden).Select(s =>
        {
            Exigir(s.IdCondicionLaboralVersion != null && s.IdJornadaLaboral != null
                && s.FechaInicio <= s.FechaFin && s.FechaInicio >= e.FechaInicioSnapshot && s.FechaFin <= e.FechaFinSnapshot
                && s.NombrePuesto != null && s.NombreDepartamento != null && s.TipoContrato != null);
            return new SegmentoPublicacionPlanilla(s.FechaInicio, s.FechaFin, s.NombrePuesto!, s.NombreDepartamento!,
                s.TipoContrato!, s.CodigoJornada, s.HorasOrdinariasDia, s.SalarioMensual, s.DiasBase30);
        }).ToArray();
        string Resumen(IEnumerable<string> valores) => valores.Distinct(StringComparer.Ordinal).Count() == 1
            ? valores.First() : "Varios: ver segmentos";
        return new ColillaPagoDetalle
        {
            IdDetallePlanilla = d.IdDetallePlanilla, IdColillaPago = metadata.IdColillaPago, CodigoColilla = metadata.CodigoColilla,
            IdEjecucionPlanilla = e.IdEjecucionPlanilla, NumeroVersion = e.NumeroVersion,
            // El rango/tipo J identifica el período sin recuperar etiquetas editables del maestro.
            CodigoPeriodo = "EJ-" + e.IdEjecucionPlanilla.ToString(System.Globalization.CultureInfo.InvariantCulture),
            NombrePeriodo = e.TipoPeriodoSnapshot, TipoPeriodo = e.TipoPeriodoSnapshot,
            FechaInicio = e.FechaInicioSnapshot, FechaFin = e.FechaFinSnapshot, EstadoPlanilla = estado,
            FechaGeneracion = metadata.FechaGeneracion, CodigoColaborador = d.CodigoColaborador!, NombreColaborador = d.NombreCompleto!,
            Departamento = Resumen(segmentos.Select(s => s.Departamento)), Puesto = Resumen(segmentos.Select(s => s.Puesto)),
            SalarioBase = d.SalarioBase, SalarioProporcional = d.SalarioProporcional,
            TotalHorasExtra = d.TotalHorasExtra, TotalBonos = d.TotalBonos, TotalBeneficiosConfigurables = d.TotalBeneficiosConfigurables,
            TotalAusencias = d.TotalAusencias, SalarioBruto = d.SalarioBruto, IngresosNoSalariales = d.IngresosNoSalariales,
            BrutoPagadero = d.SalarioBruto + d.IngresosNoSalariales, TotalDeducciones = d.TotalDeducciones, SalarioNeto = d.SalarioNeto,
            CuotasObreras = d.CuotasObreras, ImpuestoSalario = d.ImpuestoSalario, OtrasDeducciones = d.OtrasDeducciones,
            CargasPatronales = incluirPatronal ? d.CargasPatronales : 0, RiesgosTrabajo = incluirPatronal ? d.RiesgosTrabajo : 0,
            OtrosCostosPatronales = incluirPatronal ? d.OtrosCostosPatronales : 0, CostoLaboralTotal = incluirPatronal ? d.CostoLaboralTotal : 0,
            Segmentos = segmentos, Lineas = lineas.Where(l => incluirPatronal || l.Efecto != EfectoMonetario.CargaPatronal).ToArray()
        };
    }

    public static void Conciliar(EjecucionPlanilla e)
    {
        Exigir(e.Detalles.Count > 0 && e.Detalles.All(d => d.SnapshotCompleto)
            && e.Detalles.Sum(d => d.SalarioBruto) == e.SalarioBrutoTotal
            && e.Detalles.Sum(d => d.IngresosNoSalariales) == e.IngresosNoSalarialesTotal
            && e.Detalles.Sum(d => d.TotalDeducciones) == e.DeduccionesTotal
            && e.Detalles.Sum(d => d.SalarioNeto) == e.SalarioNetoTotal
            && e.Detalles.Sum(d => d.CargasPatronales) == e.CargasPatronalesTotal
            && e.Detalles.Sum(d => d.RiesgosTrabajo) == e.RiesgosTrabajoTotal
            && e.Detalles.Sum(d => d.OtrosCostosPatronales) == e.OtrosCostosPatronalesTotal
            && e.Detalles.Sum(d => d.CostoLaboralTotal) == e.CostoLaboralTotal);
    }
    private static void Exigir(bool valido)
    {
        if (!valido) throw new InvalidOperationException("REPORTE_SNAPSHOT_NO_CONCILIA");
    }
}

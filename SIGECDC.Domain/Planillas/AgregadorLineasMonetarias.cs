namespace SIGECDC.Domain.Planillas;

public sealed record TotalesLineasMonetarias(decimal IngresosSalariales, decimal ReduccionesDevengo, decimal IngresosNoSalariales,
    decimal DeduccionesLegales, decimal DeduccionesInternas, decimal CargasPatronales)
{
    public decimal BrutoSalarial => IngresosSalariales - ReduccionesDevengo;
    public decimal BrutoPagadero => BrutoSalarial + IngresosNoSalariales;
    public decimal Deducciones => DeduccionesLegales + DeduccionesInternas;
    // Operaciones algebraicas sobre lo aportado, NO certifican neto/costo completos.
    public decimal SaldoAlgebraico => BrutoPagadero - Deducciones;
    public decimal CostoAlgebraico => BrutoSalarial + CargasPatronales;
}
public sealed record ConciliacionLineas(IReadOnlyList<LineaMonetaria> Lineas, TotalesLineasMonetarias Totales);

public static class AgregadorLineasMonetarias
{
    public static ConciliacionLineas Conciliar(IEnumerable<LineaMonetaria> entradas)
    {
        var lineas = entradas.OrderBy(l => l.Fuente.IdColaborador).ThenBy(l => l.Fuente.FechaHecho)
            .ThenBy(l => l.Fuente.Segmento?.Inicio).ThenBy(l => l.Fuente.Segmento?.Fin)
            .ThenBy(l => l.Concepto.Codigo, StringComparer.Ordinal).ThenBy(l => l.Fuente.TipoFuente, StringComparer.Ordinal)
            .ThenBy(l => l.Fuente.ClaveHecho, StringComparer.Ordinal).ToArray();
        if (lineas.GroupBy(l => (l.Fuente.IdColaborador, l.Fuente.TipoFuente, l.Fuente.ClaveHecho)).Any(g => g.Count() > 1))
            throw new ConfiguracionConceptoException("LINEA_DUPLICADA");
        decimal Sumar(EfectoMonetario efecto) => lineas.Where(l => l.Efecto == efecto).Sum(l => l.Monto);
        return new(Array.AsReadOnly(lineas), new(Sumar(EfectoMonetario.IngresoSalarial), Sumar(EfectoMonetario.ReduccionDevengo),
            Sumar(EfectoMonetario.IngresoNoSalarial), Sumar(EfectoMonetario.DeduccionLegal),
            Sumar(EfectoMonetario.DeduccionInterna), Sumar(EfectoMonetario.CargaPatronal)));
    }
}

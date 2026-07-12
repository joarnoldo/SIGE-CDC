namespace SIGECDC.Domain.Planillas;

public static class CalculadoraPlanilla
{
    public static ResultadoCalculoPlanilla Calcular(DatosCalculoPlanilla datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        ValidarMontoNoNegativo(datos.SalarioBaseMensual, "El salario base mensual");
        ValidarMontoNoNegativo(datos.TotalHorasExtra, "El total de horas extra");
        ValidarMontoNoNegativo(datos.TotalBonos, "El total de bonos");
        ValidarMontoNoNegativo(datos.TotalAusencias, "El total de ausencias");
        ValidarMontoNoNegativo(datos.DeduccionesManuales, "El total de deducciones manuales");

        var salarioBase = Redondear(datos.SalarioBaseMensual);
        var salarioProporcional = datos.TipoPeriodo switch
        {
            TiposPeriodoPlanilla.Mensual => salarioBase,
            TiposPeriodoPlanilla.Quincenal => Redondear(salarioBase / 2m),
            _ => throw new ArgumentException("El tipo de período no es válido.", nameof(datos))
        };

        var totalHorasExtra = Redondear(datos.TotalHorasExtra);
        var totalBonos = Redondear(datos.TotalBonos);
        var totalAusencias = Redondear(datos.TotalAusencias);
        var deduccionesManuales = Redondear(datos.DeduccionesManuales);
        var brutoPrevio = salarioProporcional + totalHorasExtra + totalBonos - totalAusencias;

        if (brutoPrevio < 0)
        {
            throw new InvalidOperationException("Las ausencias no pueden superar el salario y los ingresos del período.");
        }

        decimal beneficiosConfigurables = 0;
        decimal deduccionesConfigurables = 0;

        foreach (var parametro in datos.Parametros)
        {
            if (parametro.TipoParametro != TiposParametroPlanilla.Porcentaje
                && parametro.TipoParametro != TiposParametroPlanilla.Monto)
            {
                continue;
            }

            if (parametro.Naturaleza != NaturalezasParametroPlanilla.Beneficio
                && parametro.Naturaleza != NaturalezasParametroPlanilla.Deduccion)
            {
                continue;
            }

            var valor = parametro.ValorColaborador
                ?? parametro.ValorPeriodo
                ?? parametro.ValorGlobal;

            if (!valor.HasValue)
            {
                continue;
            }

            ValidarMontoNoNegativo(valor.Value, $"El valor del parámetro {parametro.Codigo}");

            var monto = parametro.TipoParametro == TiposParametroPlanilla.Porcentaje
                ? brutoPrevio * valor.Value / 100m
                : valor.Value;

            if (parametro.Naturaleza == NaturalezasParametroPlanilla.Beneficio)
            {
                beneficiosConfigurables += monto;
            }
            else
            {
                deduccionesConfigurables += monto;
            }
        }

        beneficiosConfigurables = Redondear(beneficiosConfigurables);
        deduccionesConfigurables = Redondear(deduccionesConfigurables);
        var salarioBruto = Redondear(brutoPrevio + beneficiosConfigurables);
        var totalDeducciones = Redondear(deduccionesManuales + deduccionesConfigurables);
        var salarioNeto = Redondear(salarioBruto - totalDeducciones);

        if (salarioNeto < 0)
        {
            throw new InvalidOperationException("El salario neto no puede ser negativo.");
        }

        return new ResultadoCalculoPlanilla(
            salarioBase,
            salarioProporcional,
            totalHorasExtra,
            totalBonos,
            beneficiosConfigurables,
            totalAusencias,
            salarioBruto,
            totalDeducciones,
            salarioNeto);
    }

    public static decimal Redondear(decimal monto)
    {
        return Math.Round(monto, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidarMontoNoNegativo(decimal monto, string nombre)
    {
        if (monto < 0)
        {
            throw new ArgumentException($"{nombre} no puede ser negativo.");
        }
    }
}

public sealed record DatosCalculoPlanilla(
    decimal SalarioBaseMensual,
    string TipoPeriodo,
    decimal TotalHorasExtra,
    decimal TotalBonos,
    decimal TotalAusencias,
    decimal DeduccionesManuales,
    IReadOnlyCollection<ParametroAplicablePlanilla> Parametros);

public sealed record ParametroAplicablePlanilla(
    string Codigo,
    string TipoParametro,
    string? Naturaleza,
    decimal? ValorGlobal,
    decimal? ValorPeriodo,
    decimal? ValorColaborador);

public sealed record ResultadoCalculoPlanilla(
    decimal SalarioBase,
    decimal SalarioProporcional,
    decimal TotalHorasExtra,
    decimal TotalBonos,
    decimal TotalBeneficiosConfigurables,
    decimal TotalAusencias,
    decimal SalarioBruto,
    decimal TotalDeducciones,
    decimal SalarioNeto);

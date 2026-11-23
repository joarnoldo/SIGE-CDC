using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public static class CalculadoraForecast
{
    public static ResultadoCalculoForecast Calcular(DatosCalculoForecast datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ValidarPeriodo(datos.TipoPeriodo);
        ValidarMontoNoNegativo(datos.SalarioBaseMensual, "El salario base mensual");
        ValidarComponentesHistoricos(datos.Historicos);

        var proporcionActiva = ObtenerProporcionActiva(
            datos.FechaInicioPeriodo,
            datos.FechaFinPeriodo,
            datos.FechaInicioAplicacion,
            datos.FechaSalidaPrevista);
        var salarioProporcional = Redondear(
            ObtenerSalarioPeriodo(datos.SalarioBaseMensual, datos.TipoPeriodo)
            * proporcionActiva);
        var historico = datos.Historicos;

        var ajusteSalarial = ObtenerMontoParametro(
            CodigosParametroForecast.AjusteSalarial,
            0m,
            salarioProporcional,
            proporcionActiva,
            datos.Parametros,
            permitirMonto: false);
        var salarioAjustado = Redondear(salarioProporcional + ajusteSalarial);

        var horasExtra = ObtenerComponenteVariable(
            CodigosParametroForecast.HorasExtraEstimadas,
            historico.TotalHorasExtra,
            salarioProporcional,
            proporcionActiva,
            datos.Parametros);
        var bonos = ObtenerComponenteVariable(
            CodigosParametroForecast.BonosEstimados,
            historico.TotalBonos,
            salarioProporcional,
            proporcionActiva,
            datos.Parametros);
        var beneficios = Redondear(historico.TotalBeneficiosConfigurables * proporcionActiva);
        var ausencias = Redondear(historico.TotalAusencias * proporcionActiva);
        var deducciones = ObtenerComponenteVariable(
            CodigosParametroForecast.DeduccionesRecurrentes,
            historico.TotalDeducciones,
            salarioProporcional,
            proporcionActiva,
            datos.Parametros);

        var brutoBase = Redondear(
            salarioProporcional
            + Redondear(historico.TotalHorasExtra * proporcionActiva)
            + Redondear(historico.TotalBonos * proporcionActiva)
            + beneficios
            - ausencias);
        var salarioBruto = Redondear(salarioAjustado + horasExtra + bonos + beneficios - ausencias);
        var deduccionesBase = Redondear(historico.TotalDeducciones * proporcionActiva);
        var salarioNetoBase = Redondear(brutoBase - deduccionesBase);
        var salarioNeto = Redondear(salarioBruto - deducciones);

        var conceptos = new List<ConceptoForecastCalculado>
        {
            CrearConcepto(ConceptosForecast.SalarioProporcional, salarioProporcional, salarioAjustado),
            CrearConcepto(
                ConceptosForecast.HorasExtra,
                Redondear(historico.TotalHorasExtra * proporcionActiva),
                horasExtra),
            CrearConcepto(
                ConceptosForecast.Bonos,
                Redondear(historico.TotalBonos * proporcionActiva),
                bonos),
            CrearConcepto(ConceptosForecast.BeneficiosConfigurables, beneficios, beneficios),
            CrearConcepto(ConceptosForecast.Ausencias, ausencias, ausencias),
            CrearConcepto(ConceptosForecast.SalarioBruto, brutoBase, salarioBruto),
            CrearConcepto(ConceptosForecast.Deducciones, deduccionesBase, deducciones),
            CrearConcepto(ConceptosForecast.SalarioNeto, salarioNetoBase, salarioNeto)
        };

        return new ResultadoCalculoForecast(salarioBruto, conceptos);
    }

    public static IReadOnlyList<DetalleForecastProyectoCalculado> DistribuirPorProyecto(
        IReadOnlyCollection<ConceptoForecastCalculado> conceptos,
        IReadOnlyCollection<AsignacionProyectoForecastCalculo> asignaciones)
    {
        ArgumentNullException.ThrowIfNull(conceptos);
        ArgumentNullException.ThrowIfNull(asignaciones);

        if (asignaciones.Count == 0
            || asignaciones.Any(asignacion => asignacion.IdProyecto <= 0
                || asignacion.Porcentaje <= 0m
                || asignacion.Porcentaje > 100m)
            || asignaciones.Select(asignacion => asignacion.IdProyecto).Distinct().Count() != asignaciones.Count
            || asignaciones.Sum(asignacion => asignacion.Porcentaje) != 100.0000m)
        {
            throw new ArgumentException(
                "Las asignaciones por proyecto deben ser unicas y sumar exactamente 100.0000 %.",
                nameof(asignaciones));
        }

        var resultado = new List<DetalleForecastProyectoCalculado>();
        foreach (var concepto in conceptos)
        {
            var basePorProyecto = DistribuirMonto(concepto.MontoBase, asignaciones);
            var proyectadoPorProyecto = DistribuirMonto(concepto.MontoProyectado, asignaciones);
            foreach (var asignacion in asignaciones.OrderBy(item => item.IdProyecto))
            {
                var montoBase = basePorProyecto[asignacion.IdProyecto];
                var montoProyectado = proyectadoPorProyecto[asignacion.IdProyecto];
                resultado.Add(new DetalleForecastProyectoCalculado(
                    asignacion.IdProyecto,
                    concepto.Concepto,
                    montoBase,
                    Redondear(montoProyectado - montoBase),
                    montoProyectado));
            }
        }

        return resultado;
    }

    public static decimal Redondear(decimal monto) =>
        Math.Round(monto, 2, MidpointRounding.AwayFromZero);

    private static ConceptoForecastCalculado CrearConcepto(
        string concepto,
        decimal montoBase,
        decimal montoProyectado)
    {
        var baseRedondeada = Redondear(montoBase);
        var proyectadoRedondeado = Redondear(montoProyectado);
        return new ConceptoForecastCalculado(
            concepto,
            baseRedondeada,
            Redondear(proyectadoRedondeado - baseRedondeada),
            proyectadoRedondeado);
    }

    private static decimal ObtenerMontoParametro(
        string codigo,
        decimal montoHistorico,
        decimal salarioProporcional,
        decimal proporcionActiva,
        IReadOnlyCollection<ParametroForecastCalculo> parametros,
        bool permitirMonto)
    {
        var parametro = parametros.FirstOrDefault(item => item.Codigo == codigo);
        if (parametro is null)
        {
            return Redondear(montoHistorico * proporcionActiva);
        }

        ValidarParametro(parametro, permitirMonto);
        return parametro.TipoParametro == TiposParametroPlanilla.Porcentaje
            ? Redondear(salarioProporcional * parametro.ValorDecimal / 100m)
            : Redondear(parametro.ValorDecimal * proporcionActiva);
    }

    private static decimal ObtenerComponenteVariable(
        string codigo,
        decimal montoHistorico,
        decimal salarioProporcional,
        decimal proporcionActiva,
        IReadOnlyCollection<ParametroForecastCalculo> parametros) =>
        ObtenerMontoParametro(
            codigo,
            montoHistorico,
            salarioProporcional,
            proporcionActiva,
            parametros,
            permitirMonto: true);

    private static decimal ObtenerSalarioPeriodo(decimal salarioBaseMensual, string tipoPeriodo) =>
        tipoPeriodo switch
        {
            TiposPeriodoPlanilla.Mensual => Redondear(salarioBaseMensual),
            TiposPeriodoPlanilla.Quincenal => Redondear(salarioBaseMensual / 2m),
            _ => throw new ArgumentException("El tipo de periodo no es valido.", nameof(tipoPeriodo))
        };

    private static decimal ObtenerProporcionActiva(
        DateTime fechaInicioPeriodo,
        DateTime fechaFinPeriodo,
        DateTime fechaInicioAplicacion,
        DateTime? fechaSalidaPrevista)
    {
        var inicioPeriodo = fechaInicioPeriodo.Date;
        var finPeriodo = fechaFinPeriodo.Date;
        if (finPeriodo < inicioPeriodo)
        {
            throw new ArgumentException("El periodo tiene un rango de fechas invalido.");
        }

        var inicioActivo = fechaInicioAplicacion.Date > inicioPeriodo
            ? fechaInicioAplicacion.Date
            : inicioPeriodo;
        var finActivo = fechaSalidaPrevista.HasValue
            ? fechaSalidaPrevista.Value.Date.AddDays(-1)
            : finPeriodo;
        if (finActivo > finPeriodo)
        {
            finActivo = finPeriodo;
        }

        if (finActivo < inicioActivo)
        {
            return 0m;
        }

        var diasPeriodo = (finPeriodo - inicioPeriodo).Days + 1;
        var diasActivos = (finActivo - inicioActivo).Days + 1;
        return diasActivos / (decimal)diasPeriodo;
    }

    private static IReadOnlyDictionary<long, decimal> DistribuirMonto(
        decimal monto,
        IReadOnlyCollection<AsignacionProyectoForecastCalculo> asignaciones)
    {
        var ordenadas = asignaciones.OrderBy(item => item.IdProyecto).ToList();
        var distribucion = new Dictionary<long, decimal>();
        var acumulado = 0m;
        for (var indice = 0; indice < ordenadas.Count; indice++)
        {
            var asignacion = ordenadas[indice];
            var asignado = indice == ordenadas.Count - 1
                ? Redondear(monto - acumulado)
                : Redondear(monto * asignacion.Porcentaje / 100m);
            distribucion.Add(asignacion.IdProyecto, asignado);
            acumulado += asignado;
        }

        return distribucion;
    }

    private static void ValidarPeriodo(string tipoPeriodo)
    {
        if (!TiposPeriodoPlanilla.Permitidos.Contains(tipoPeriodo))
        {
            throw new ArgumentException("El tipo de periodo no es valido.", nameof(tipoPeriodo));
        }
    }

    private static void ValidarComponentesHistoricos(ComponentesHistoricosForecast historicos)
    {
        ArgumentNullException.ThrowIfNull(historicos);
        ValidarMontoNoNegativo(historicos.SalarioProporcional, "El salario proporcional historico");
        ValidarMontoNoNegativo(historicos.TotalHorasExtra, "Las horas extra historicas");
        ValidarMontoNoNegativo(historicos.TotalBonos, "Los bonos historicos");
        ValidarMontoNoNegativo(historicos.TotalBeneficiosConfigurables, "Los beneficios historicos");
        ValidarMontoNoNegativo(historicos.TotalAusencias, "Las ausencias historicas");
        ValidarMontoNoNegativo(historicos.TotalDeducciones, "Las deducciones historicas");
    }

    private static void ValidarParametro(ParametroForecastCalculo parametro, bool permitirMonto)
    {
        if (parametro.ValorDecimal < 0m
            || (parametro.TipoParametro != TiposParametroPlanilla.Porcentaje
                && (!permitirMonto || parametro.TipoParametro != TiposParametroPlanilla.Monto))
            || (parametro.TipoParametro == TiposParametroPlanilla.Porcentaje
                && parametro.ValorDecimal > 100m))
        {
            throw new ArgumentException(
                $"El parametro {parametro.Codigo} no tiene un tipo o valor valido.",
                nameof(parametro));
        }
    }

    private static void ValidarMontoNoNegativo(decimal monto, string nombre)
    {
        if (monto < 0m)
        {
            throw new ArgumentException($"{nombre} no puede ser negativo.");
        }
    }
}

public static class ConceptosForecast
{
    public const string SalarioProporcional = "SALARIO_PROPORCIONAL";
    public const string HorasExtra = "HORAS_EXTRA";
    public const string Bonos = "BONOS";
    public const string BeneficiosConfigurables = "BENEFICIOS_CONFIGURABLES";
    public const string Ausencias = "AUSENCIAS";
    public const string SalarioBruto = "SALARIO_BRUTO";
    public const string Deducciones = "DEDUCCIONES";
    public const string SalarioNeto = "SALARIO_NETO";
}

public sealed record DatosCalculoForecast(
    string TipoPeriodo,
    DateTime FechaInicioPeriodo,
    DateTime FechaFinPeriodo,
    decimal SalarioBaseMensual,
    DateTime FechaInicioAplicacion,
    DateTime? FechaSalidaPrevista,
    ComponentesHistoricosForecast Historicos,
    IReadOnlyCollection<ParametroForecastCalculo> Parametros);

public sealed record ComponentesHistoricosForecast(
    decimal SalarioProporcional,
    decimal TotalHorasExtra,
    decimal TotalBonos,
    decimal TotalBeneficiosConfigurables,
    decimal TotalAusencias,
    decimal TotalDeducciones);

public sealed record ParametroForecastCalculo(
    string Codigo,
    string TipoParametro,
    decimal ValorDecimal);

public sealed record AsignacionProyectoForecastCalculo(
    long IdProyecto,
    decimal Porcentaje);

public sealed record ConceptoForecastCalculado(
    string Concepto,
    decimal MontoBase,
    decimal MontoAjuste,
    decimal MontoProyectado);

public sealed record DetalleForecastProyectoCalculado(
    long IdProyecto,
    string Concepto,
    decimal MontoBase,
    decimal MontoAjuste,
    decimal MontoProyectado);

public sealed record ResultadoCalculoForecast(
    decimal MontoProyectadoTotal,
    IReadOnlyList<ConceptoForecastCalculado> Conceptos);

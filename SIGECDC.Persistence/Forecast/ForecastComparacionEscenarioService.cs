using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastComparacionEscenarioService(ApplicationDbContext contexto)
    : IForecastComparacionEscenarioService
{
    public async Task<IReadOnlyList<EscenarioComparacionForecastOpcion>>
        ObtenerEscenariosElegiblesAsync(
            string idUsuarioActual,
            CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);

        return await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(item => item.EstadoRegistro == EstadosRegistro.Activo
                && (item.EstadoEscenario == EstadosEscenarioForecast.Guardado
                    || item.EstadoEscenario == EstadosEscenarioForecast.Comparado)
                && item.FechaCalculo.HasValue
                && contexto.ForecastDetalles.Any(detalle =>
                    detalle.IdForecastEscenario == item.IdForecastEscenario
                    && detalle.EstadoRegistro == EstadosRegistro.Activo))
            .OrderByDescending(item => item.FechaModificacion ?? item.FechaCreacion)
            .ThenByDescending(item => item.IdForecastEscenario)
            .Select(item => new EscenarioComparacionForecastOpcion
            {
                IdForecastEscenario = item.IdForecastEscenario,
                Nombre = item.Nombre,
                EstadoEscenario = item.EstadoEscenario,
                FechaInicioProyeccion = item.FechaInicioProyeccion,
                FechaFinProyeccion = item.FechaFinProyeccion,
                FechaCalculo = item.FechaCalculo,
                MontoProyectadoTotal = item.MontoProyectadoTotal,
                CantidadPeriodos = item.Periodos.Count(periodo =>
                    periodo.EstadoRegistro == EstadosRegistro.Activo)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ComparacionEscenariosForecast> CompararAsync(
        SolicitudCompararEscenariosForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ArgumentNullException.ThrowIfNull(solicitud);
        Validator.ValidateObject(solicitud, new ValidationContext(solicitud), true);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var ids = new[] { solicitud.IdEscenarioBase, solicitud.IdEscenarioAlternativo };
            var escenarios = await contexto.ForecastEscenarios
                .Where(item => ids.Contains(item.IdForecastEscenario)
                    && item.EstadoRegistro == EstadosRegistro.Activo)
                .ToListAsync(cancellationToken);

            if (escenarios.Count != 2)
            {
                throw new KeyNotFoundException(
                    "Uno o más escenarios seleccionados no existen o no están activos.");
            }

            var escenarioBase = escenarios.Single(item =>
                item.IdForecastEscenario == solicitud.IdEscenarioBase);
            var escenarioAlternativo = escenarios.Single(item =>
                item.IdForecastEscenario == solicitud.IdEscenarioAlternativo);
            ValidarElegible(escenarioBase);
            ValidarElegible(escenarioAlternativo);

            var periodos = await contexto.ForecastPeriodos
                .AsNoTracking()
                .Where(item => ids.Contains(item.IdForecastEscenario)
                    && item.EstadoRegistro == EstadosRegistro.Activo)
                .OrderBy(item => item.IdForecastEscenario)
                .ThenBy(item => item.NumeroOrden)
                .Select(item => new PeriodoConsulta
                {
                    IdForecastEscenario = item.IdForecastEscenario,
                    NumeroOrden = item.NumeroOrden,
                    TipoPeriodo = item.TipoPeriodo,
                    FechaInicio = item.FechaInicio,
                    FechaFin = item.FechaFin
                })
                .ToListAsync(cancellationToken);

            var periodosBase = periodos
                .Where(item => item.IdForecastEscenario == escenarioBase.IdForecastEscenario)
                .OrderBy(item => item.NumeroOrden)
                .ToList();
            var periodosAlternativos = periodos
                .Where(item => item.IdForecastEscenario == escenarioAlternativo.IdForecastEscenario)
                .OrderBy(item => item.NumeroOrden)
                .ToList();
            ValidarPeriodosComparables(periodosBase, periodosAlternativos);

            if (solicitud.NumeroOrdenPeriodo.HasValue
                && periodosBase.All(item =>
                    item.NumeroOrden != solicitud.NumeroOrdenPeriodo.Value))
            {
                throw new ValidationException(
                    "El período seleccionado no pertenece a los escenarios comparados.");
            }

            var detalles = await ForecastResultadoConsultaInterna.Crear(contexto, ids)
                .ToListAsync(cancellationToken);
            if (detalles.All(item => item.IdForecastEscenario != escenarioBase.IdForecastEscenario)
                || detalles.All(item => item.IdForecastEscenario != escenarioAlternativo.IdForecastEscenario))
            {
                throw new InvalidOperationException(
                    "Los escenarios seleccionados no conservan resultados comparables.");
            }

            var parametros = await contexto.ForecastParametros
                .AsNoTracking()
                .Where(item => ids.Contains(item.IdForecastEscenario))
                .ToListAsync(cancellationToken);
            var detallesFiltrados = solicitud.NumeroOrdenPeriodo.HasValue
                ? detalles.Where(item =>
                    item.NumeroOrdenPeriodo == solicitud.NumeroOrdenPeriodo.Value).ToList()
                : detalles;
            var filas = CrearFilas(
                detallesFiltrados,
                escenarioBase.IdForecastEscenario,
                escenarioAlternativo.IdForecastEscenario,
                solicitud.Dimension);
            var totales = CrearTotales(filas);

            var ahora = DateTime.Now;
            foreach (var escenario in escenarios.Where(item =>
                         item.EstadoEscenario == EstadosEscenarioForecast.Guardado))
            {
                escenario.EstadoEscenario = EstadosEscenarioForecast.Comparado;
                escenario.FechaModificacion = ahora;
                escenario.ModificadoPor = idActor;
            }

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            return new ComparacionEscenariosForecast
            {
                EscenarioBase = CrearResumenEscenario(escenarioBase, detalles),
                EscenarioAlternativo = CrearResumenEscenario(escenarioAlternativo, detalles),
                Dimension = solicitud.Dimension,
                NumeroOrdenPeriodo = solicitud.NumeroOrdenPeriodo,
                Periodos = periodosBase.Select(item => new PeriodoComparacionForecastOpcion(
                    item.NumeroOrden,
                    item.TipoPeriodo,
                    item.FechaInicio,
                    item.FechaFin)).ToList(),
                Parametros = CrearParametrosComparados(
                    parametros,
                    escenarioBase.IdForecastEscenario,
                    escenarioAlternativo.IdForecastEscenario),
                Filas = filas,
                Totales = totales
            };
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private static void ValidarElegible(ForecastEscenario escenario)
    {
        if (escenario.EstadoEscenario is not (EstadosEscenarioForecast.Guardado
            or EstadosEscenarioForecast.Comparado)
            || !escenario.FechaCalculo.HasValue)
        {
            throw new InvalidOperationException(
                "Solo se pueden comparar escenarios guardados con resultados calculados.");
        }
    }

    private static void ValidarPeriodosComparables(
        IReadOnlyList<PeriodoConsulta> periodosBase,
        IReadOnlyList<PeriodoConsulta> periodosAlternativos)
    {
        if (periodosBase.Count == 0 || periodosBase.Count != periodosAlternativos.Count)
        {
            throw new ValidationException(
                "Los escenarios no tienen la misma cantidad de períodos activos.");
        }

        for (var indice = 0; indice < periodosBase.Count; indice++)
        {
            var periodoBase = periodosBase[indice];
            var periodoAlternativo = periodosAlternativos[indice];
            if (periodoBase.NumeroOrden != periodoAlternativo.NumeroOrden
                || periodoBase.TipoPeriodo != periodoAlternativo.TipoPeriodo
                || periodoBase.FechaInicio.Date != periodoAlternativo.FechaInicio.Date
                || periodoBase.FechaFin.Date != periodoAlternativo.FechaFin.Date)
            {
                throw new ValidationException(
                    "Los escenarios no contienen períodos equivalentes.");
            }
        }
    }

    private static IReadOnlyList<ParametroComparadoForecast> CrearParametrosComparados(
        IReadOnlyCollection<ForecastParametro> parametros,
        long idBase,
        long idAlternativo) => CodigosParametroForecast.Definiciones
        .Select(definicion =>
        {
            var parametroBase = parametros.FirstOrDefault(item =>
                item.IdForecastEscenario == idBase
                && string.Equals(item.Codigo, definicion.Codigo, StringComparison.OrdinalIgnoreCase));
            var parametroAlternativo = parametros.FirstOrDefault(item =>
                item.IdForecastEscenario == idAlternativo
                && string.Equals(item.Codigo, definicion.Codigo, StringComparison.OrdinalIgnoreCase));

            return new ParametroComparadoForecast
            {
                Codigo = definicion.Codigo,
                Nombre = definicion.Nombre,
                EstaHabilitadoBase = parametroBase?.EstadoRegistro == EstadosRegistro.Activo,
                TipoBase = parametroBase?.TipoParametro ?? definicion.TipoInicial,
                ValorBase = parametroBase?.ValorDecimal,
                EstaHabilitadoAlternativo = parametroAlternativo?.EstadoRegistro == EstadosRegistro.Activo,
                TipoAlternativo = parametroAlternativo?.TipoParametro ?? definicion.TipoInicial,
                ValorAlternativo = parametroAlternativo?.ValorDecimal
            };
        })
        .ToList();

    private static IReadOnlyList<FilaComparacionForecast> CrearFilas(
        IReadOnlyCollection<DetalleResultadoConsulta> detalles,
        long idBase,
        long idAlternativo,
        string dimension)
    {
        var agrupados = detalles
            .GroupBy(item => new
            {
                item.IdForecastEscenario,
                Clave = ObtenerClave(item, dimension),
                item.Concepto
            })
            .Select(grupo => new ValorAgrupado
            {
                IdForecastEscenario = grupo.Key.IdForecastEscenario,
                Clave = grupo.Key.Clave,
                Concepto = grupo.Key.Concepto,
                Monto = grupo.Sum(item => item.MontoProyectado),
                CantidadParticipantes = grupo.Select(item => item.IdForecastParticipante).Distinct().Count(),
                Encabezado = CrearEncabezado(grupo.First(), dimension)
            })
            .ToList();

        return agrupados
            .GroupBy(item => item.Clave, StringComparer.Ordinal)
            .Select(grupo =>
            {
                var encabezado = grupo.First().Encabezado;
                var conceptos = CatalogoConceptosResultadoForecast.Todos
                    .Select(definicion => new ConceptoComparadoForecast
                    {
                        Codigo = definicion.Codigo,
                        Nombre = definicion.Nombre,
                        MontoBaseEscenario = grupo
                            .Where(item => item.IdForecastEscenario == idBase
                                && item.Concepto == definicion.Codigo)
                            .Sum(item => item.Monto),
                        MontoAlternativo = grupo
                            .Where(item => item.IdForecastEscenario == idAlternativo
                                && item.Concepto == definicion.Codigo)
                            .Sum(item => item.Monto)
                    })
                    .ToList();
                var bruto = conceptos.Single(item => item.Codigo == ConceptosForecast.SalarioBruto);
                var deducciones = conceptos.Single(item => item.Codigo == ConceptosForecast.Deducciones);
                var neto = conceptos.Single(item => item.Codigo == ConceptosForecast.SalarioNeto);

                return new FilaComparacionForecast
                {
                    Clave = grupo.Key,
                    Codigo = encabezado.Codigo,
                    Etiqueta = encabezado.Etiqueta,
                    Detalle = encabezado.Detalle,
                    CantidadParticipantesBase = grupo
                        .Where(item => item.IdForecastEscenario == idBase)
                        .Select(item => item.CantidadParticipantes)
                        .DefaultIfEmpty()
                        .Max(),
                    CantidadParticipantesAlternativo = grupo
                        .Where(item => item.IdForecastEscenario == idAlternativo)
                        .Select(item => item.CantidadParticipantes)
                        .DefaultIfEmpty()
                        .Max(),
                    Valores = new ValoresComparacionForecast
                    {
                        SalarioBrutoBase = bruto.MontoBaseEscenario,
                        SalarioBrutoAlternativo = bruto.MontoAlternativo,
                        DeduccionesBase = deducciones.MontoBaseEscenario,
                        DeduccionesAlternativo = deducciones.MontoAlternativo,
                        SalarioNetoBase = neto.MontoBaseEscenario,
                        SalarioNetoAlternativo = neto.MontoAlternativo
                    },
                    Conceptos = conceptos
                };
            })
            .OrderBy(item => item.Clave.StartsWith("PER:", StringComparison.Ordinal)
                ? int.Parse(item.Clave.Split(':')[1])
                : int.MaxValue)
            .ThenBy(item => item.Etiqueta)
            .ThenBy(item => item.Codigo)
            .ToList();
    }

    private static string ObtenerClave(DetalleResultadoConsulta item, string dimension) => dimension switch
    {
        DimensionesResultadoForecast.Colaborador => item.IdColaborador.HasValue
            ? $"COL:{item.IdColaborador.Value}"
            : $"PREV:{item.CodigoParticipante}",
        DimensionesResultadoForecast.Departamento => $"DEP:{item.IdDepartamento}",
        DimensionesResultadoForecast.Proyecto => $"PRO:{item.IdProyecto}",
        DimensionesResultadoForecast.Periodo => $"PER:{item.NumeroOrdenPeriodo}:{item.TipoPeriodo}:{item.FechaInicioPeriodo:yyyyMMdd}:{item.FechaFinPeriodo:yyyyMMdd}",
        _ => throw new ValidationException("La dimensión de comparación no es válida.")
    };

    private static EncabezadoComparacion CrearEncabezado(
        DetalleResultadoConsulta item,
        string dimension) => dimension switch
    {
        DimensionesResultadoForecast.Colaborador => new(
            item.CodigoParticipante,
            item.EtiquetaParticipante,
            $"{item.NombreDepartamento} · {item.NombrePuesto}"),
        DimensionesResultadoForecast.Departamento => new(
            string.Empty,
            item.NombreDepartamento,
            null),
        DimensionesResultadoForecast.Proyecto => new(
            item.CodigoProyecto,
            item.NombreProyecto,
            null),
        DimensionesResultadoForecast.Periodo => new(
            $"P{item.NumeroOrdenPeriodo:00}",
            item.TipoPeriodo,
            $"{item.FechaInicioPeriodo:dd/MM/yyyy} – {item.FechaFinPeriodo:dd/MM/yyyy}"),
        _ => throw new ValidationException("La dimensión de comparación no es válida.")
    };

    private static ValoresComparacionForecast CrearTotales(
        IReadOnlyCollection<FilaComparacionForecast> filas) => new()
    {
        SalarioBrutoBase = filas.Sum(item => item.Valores.SalarioBrutoBase),
        SalarioBrutoAlternativo = filas.Sum(item => item.Valores.SalarioBrutoAlternativo),
        DeduccionesBase = filas.Sum(item => item.Valores.DeduccionesBase),
        DeduccionesAlternativo = filas.Sum(item => item.Valores.DeduccionesAlternativo),
        SalarioNetoBase = filas.Sum(item => item.Valores.SalarioNetoBase),
        SalarioNetoAlternativo = filas.Sum(item => item.Valores.SalarioNetoAlternativo)
    };

    private static EscenarioComparadoForecast CrearResumenEscenario(
        ForecastEscenario escenario,
        IReadOnlyCollection<DetalleResultadoConsulta> detalles)
    {
        var propios = detalles.Where(item =>
            item.IdForecastEscenario == escenario.IdForecastEscenario).ToList();
        return new EscenarioComparadoForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            Nombre = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            FechaInicioProyeccion = escenario.FechaInicioProyeccion,
            FechaFinProyeccion = escenario.FechaFinProyeccion,
            FechaCalculo = escenario.FechaCalculo,
            MontoProyectadoTotal = escenario.MontoProyectadoTotal,
            CantidadPeriodos = propios.Select(item => item.NumeroOrdenPeriodo).Distinct().Count(),
            DeduccionesProyectadas = propios
                .Where(item => item.Concepto == ConceptosForecast.Deducciones)
                .Sum(item => item.MontoProyectado),
            SalarioNetoProyectado = propios
                .Where(item => item.Concepto == ConceptosForecast.SalarioNeto)
                .Sum(item => item.MontoProyectado)
        };
    }

    private sealed class PeriodoConsulta
    {
        public long IdForecastEscenario { get; init; }
        public int NumeroOrden { get; init; }
        public string TipoPeriodo { get; init; } = string.Empty;
        public DateTime FechaInicio { get; init; }
        public DateTime FechaFin { get; init; }
    }

    private sealed class ValorAgrupado
    {
        public long IdForecastEscenario { get; init; }
        public string Clave { get; init; } = string.Empty;
        public string Concepto { get; init; } = string.Empty;
        public decimal Monto { get; init; }
        public int CantidadParticipantes { get; init; }
        public EncabezadoComparacion Encabezado { get; init; } = new(string.Empty, string.Empty, null);
    }

    private sealed record EncabezadoComparacion(
        string Codigo,
        string Etiqueta,
        string? Detalle);
}

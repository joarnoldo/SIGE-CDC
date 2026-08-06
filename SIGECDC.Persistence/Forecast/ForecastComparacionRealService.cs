using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastComparacionRealService(ApplicationDbContext contexto)
    : IForecastComparacionRealService
{
    public async Task<DisponibilidadComparacionRealForecast?> ObtenerDisponibilidadAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario);

        var cargado = await CargarAsync(idForecastEscenario, cancellationToken);
        return cargado?.Disponibilidad;
    }

    public async Task<ComparacionForecastReal?> CompararAsync(
        long idForecastEscenario,
        SolicitudCompararForecastReal solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario);
        ArgumentNullException.ThrowIfNull(solicitud);
        Validator.ValidateObject(solicitud, new ValidationContext(solicitud), true);

        var cargado = await CargarAsync(idForecastEscenario, cancellationToken);
        if (cargado is null)
        {
            return null;
        }

        return solicitud.Dimension switch
        {
            DimensionesComparacionRealForecast.Periodo => CompararPorPeriodo(cargado),
            DimensionesComparacionRealForecast.Colaborador => await CompararPorColaboradorAsync(
                cargado,
                solicitud.IdForecastPeriodo!.Value,
                cancellationToken),
            DimensionesComparacionRealForecast.Departamento => await CompararPorDepartamentoAsync(
                cargado,
                solicitud.IdForecastPeriodo!.Value,
                cancellationToken),
            DimensionesComparacionRealForecast.Proyecto => CompararPorProyecto(
                cargado,
                solicitud.IdForecastPeriodo!.Value),
            _ => throw new ValidationException("La dimensión de comparación real no es válida.")
        };
    }

    private async Task<ContextoComparacionReal?> CargarAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var escenario = await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new EscenarioConsulta
            {
                IdForecastEscenario = item.IdForecastEscenario,
                Nombre = item.Nombre,
                EstadoEscenario = item.EstadoEscenario,
                FechaCalculo = item.FechaCalculo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (escenario is null)
        {
            return null;
        }

        if (escenario.EstadoEscenario is not (EstadosEscenarioForecast.Guardado
            or EstadosEscenarioForecast.Comparado)
            || !escenario.FechaCalculo.HasValue)
        {
            throw new InvalidOperationException(
                "Solo se puede comparar con la planilla real un escenario guardado con resultados calculados.");
        }

        var periodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.NumeroOrden)
            .ThenBy(item => item.IdForecastPeriodo)
            .Select(item => new PeriodoForecastConsulta
            {
                IdForecastPeriodo = item.IdForecastPeriodo,
                NumeroOrden = item.NumeroOrden,
                TipoPeriodo = item.TipoPeriodo,
                FechaInicio = item.FechaInicio,
                FechaFin = item.FechaFin
            })
            .ToListAsync(cancellationToken);

        if (periodos.Count == 0)
        {
            throw new InvalidOperationException(
                "El escenario no conserva períodos activos para comparar.");
        }

        var detallesForecast = await ForecastResultadoConsultaInterna
            .Crear(contexto, [idForecastEscenario])
            .ToListAsync(cancellationToken);
        if (detallesForecast.Count == 0)
        {
            throw new InvalidOperationException(
                "El escenario no conserva resultados activos para comparar.");
        }

        var fechaMinima = periodos.Min(item => item.FechaInicio).Date;
        var fechaMaxima = periodos.Max(item => item.FechaFin).Date;
        var planillas = await contexto.Planillas
            .AsNoTracking()
            .Where(item => item.EstadoRegistro == EstadosRegistro.Activo
                && item.PeriodoPlanilla!.EstadoRegistro == EstadosRegistro.Activo
                && item.PeriodoPlanilla.FechaInicio >= fechaMinima
                && item.PeriodoPlanilla.FechaFin <= fechaMaxima)
            .Select(item => new PlanillaCoincidenciaConsulta
            {
                IdPlanilla = item.IdPlanilla,
                CodigoPeriodo = item.PeriodoPlanilla!.CodigoPeriodo,
                TipoPeriodo = item.PeriodoPlanilla.TipoPeriodo,
                FechaInicio = item.PeriodoPlanilla.FechaInicio,
                FechaFin = item.PeriodoPlanilla.FechaFin,
                EstadoPlanilla = item.EstadoPlanilla!.Nombre,
                EstadoPeriodo = item.PeriodoPlanilla.EstadoPlanilla!.Nombre,
                SalarioBruto = item.SalarioBrutoTotal,
                Deducciones = item.DeduccionesTotal,
                SalarioNeto = item.SalarioNetoTotal
            })
            .ToListAsync(cancellationToken);

        var idsPlanillas = planillas.Select(item => item.IdPlanilla).ToList();
        List<SumaDetalleRealConsulta> sumasDetalles = idsPlanillas.Count == 0
            ? []
            : await contexto.DetallesPlanilla
                .AsNoTracking()
                .Where(item => idsPlanillas.Contains(item.IdPlanilla))
                .GroupBy(item => item.IdPlanilla)
                .Select(grupo => new SumaDetalleRealConsulta
                {
                    IdPlanilla = grupo.Key,
                    Cantidad = grupo.Count(),
                    SalarioBruto = grupo.Sum(item => item.SalarioBruto),
                    Deducciones = grupo.Sum(item => item.TotalDeducciones),
                    SalarioNeto = grupo.Sum(item => item.SalarioNeto)
                })
                .ToListAsync(cancellationToken);
        var sumasPorPlanilla = sumasDetalles.ToDictionary(item => item.IdPlanilla);

        var valoresForecast = detallesForecast
            .GroupBy(item => item.IdForecastPeriodo)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => new ValoresForecastPeriodo(
                    grupo.Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                        .Sum(item => item.MontoProyectado),
                    grupo.Where(item => item.Concepto == ConceptosForecast.Deducciones)
                        .Sum(item => item.MontoProyectado),
                    grupo.Where(item => item.Concepto == ConceptosForecast.SalarioNeto)
                        .Sum(item => item.MontoProyectado)));

        var periodosDisponibilidad = new List<PeriodoComparacionRealForecast>(periodos.Count);
        var planillasDisponibles = new Dictionary<long, PlanillaCoincidenciaConsulta>();
        foreach (var periodo in periodos)
        {
            var coincidencias = planillas.Where(item =>
                    item.TipoPeriodo == periodo.TipoPeriodo
                    && item.FechaInicio.Date == periodo.FechaInicio.Date
                    && item.FechaFin.Date == periodo.FechaFin.Date)
                .ToList();
            var elegibles = coincidencias.Where(EsPlanillaElegible).ToList();
            var esAmbigua = elegibles.Count > 1;
            var planilla = elegibles.Count == 1 ? elegibles[0] : null;
            var disponible = planilla is not null;
            if (planilla is not null)
            {
                planillasDisponibles[periodo.IdForecastPeriodo] = planilla;
            }

            valoresForecast.TryGetValue(periodo.IdForecastPeriodo, out var proyectado);
            sumasPorPlanilla.TryGetValue(planilla?.IdPlanilla ?? 0, out var sumaReal);
            periodosDisponibilidad.Add(new PeriodoComparacionRealForecast
            {
                IdForecastPeriodo = periodo.IdForecastPeriodo,
                NumeroOrden = periodo.NumeroOrden,
                TipoPeriodo = periodo.TipoPeriodo,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin,
                EstaDisponible = disponible,
                EsAmbigua = esAmbigua,
                IdPlanilla = planilla?.IdPlanilla,
                CodigoPeriodoReal = planilla?.CodigoPeriodo,
                EstadoPlanillaReal = planilla?.EstadoPlanilla,
                MensajeDisponibilidad = CrearMensajeDisponibilidad(
                    disponible,
                    esAmbigua,
                    coincidencias.Count > 0),
                EsConsistenteConDetalles = planilla is null
                    ? null
                    : EsConsistente(planilla, sumaReal),
                Valores = new ValoresForecastReal
                {
                    SalarioBrutoProyectado = proyectado?.SalarioBruto ?? 0m,
                    SalarioBrutoReal = planilla?.SalarioBruto,
                    DeduccionesProyectadas = proyectado?.Deducciones ?? 0m,
                    DeduccionesReales = planilla?.Deducciones,
                    SalarioNetoProyectado = proyectado?.SalarioNeto ?? 0m,
                    SalarioNetoReal = planilla?.SalarioNeto
                }
            });
        }

        var disponibilidad = new DisponibilidadComparacionRealForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            NombreEscenario = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            FechaCalculo = escenario.FechaCalculo,
            CantidadPeriodosDisponibles = periodosDisponibilidad.Count(item => item.EstaDisponible),
            CantidadPeriodosPendientes = periodosDisponibilidad.Count(item => !item.EstaDisponible),
            Periodos = periodosDisponibilidad
        };

        return new ContextoComparacionReal(
            disponibilidad,
            detallesForecast,
            planillasDisponibles);
    }

    private static ComparacionForecastReal CompararPorPeriodo(ContextoComparacionReal cargado)
    {
        var filas = cargado.Disponibilidad.Periodos
            .Select(periodo => new FilaComparacionForecastReal
            {
                Clave = $"PER:{periodo.IdForecastPeriodo}",
                Codigo = $"P{periodo.NumeroOrden:00}",
                Etiqueta = periodo.TipoPeriodo,
                Detalle = $"{periodo.FechaInicio:dd/MM/yyyy} – {periodo.FechaFin:dd/MM/yyyy}",
                TieneForecast = true,
                TieneReal = periodo.EstaDisponible,
                Valores = periodo.Valores
            })
            .ToList();

        return new ComparacionForecastReal
        {
            Disponibilidad = cargado.Disponibilidad,
            Dimension = DimensionesComparacionRealForecast.Periodo,
            TotalesComparables = SumarPeriodosDisponibles(cargado.Disponibilidad.Periodos),
            Filas = filas
        };
    }

    private async Task<ComparacionForecastReal> CompararPorColaboradorAsync(
        ContextoComparacionReal cargado,
        long idForecastPeriodo,
        CancellationToken cancellationToken)
    {
        var (periodo, planilla) = ObtenerPeriodoDisponible(cargado, idForecastPeriodo);

        var forecast = cargado.DetallesForecast
            .Where(item => item.IdForecastPeriodo == idForecastPeriodo)
            .GroupBy(item => item.IdColaborador.HasValue
                ? $"COL:{item.IdColaborador.Value}"
                : $"PREV:{item.CodigoParticipante}", StringComparer.Ordinal)
            .Select(grupo =>
            {
                var encabezado = grupo.First();
                return new FilaLadoForecast
                {
                    Clave = grupo.Key,
                    Codigo = encabezado.CodigoParticipante,
                    Etiqueta = encabezado.EtiquetaParticipante,
                    Detalle = $"{encabezado.NombreDepartamento} · {encabezado.NombrePuesto}",
                    EsContratacionPrevista = !encabezado.IdColaborador.HasValue,
                    SalarioBruto = grupo
                        .Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                        .Sum(item => item.MontoProyectado),
                    Deducciones = grupo
                        .Where(item => item.Concepto == ConceptosForecast.Deducciones)
                        .Sum(item => item.MontoProyectado),
                    SalarioNeto = grupo
                        .Where(item => item.Concepto == ConceptosForecast.SalarioNeto)
                        .Sum(item => item.MontoProyectado)
                };
            })
            .ToDictionary(item => item.Clave, StringComparer.Ordinal);

        var reales = await contexto.DetallesPlanilla
            .AsNoTracking()
            .Where(item => item.IdPlanilla == planilla.IdPlanilla)
            .Select(item => new FilaLadoReal
            {
                Clave = $"COL:{item.IdColaborador}",
                Codigo = item.Colaborador!.CodigoColaborador,
                Etiqueta = item.Colaborador!.Nombre + " "
                    + item.Colaborador!.PrimerApellido + " "
                    + (item.Colaborador!.SegundoApellido ?? string.Empty),
                SalarioBruto = item.SalarioBruto,
                Deducciones = item.TotalDeducciones,
                SalarioNeto = item.SalarioNeto
            })
            .ToListAsync(cancellationToken);
        var realesPorClave = reales.ToDictionary(item => item.Clave, StringComparer.Ordinal);

        var filas = CrearFilasComparacion(forecast, realesPorClave);

        return new ComparacionForecastReal
        {
            Disponibilidad = cargado.Disponibilidad,
            Dimension = DimensionesComparacionRealForecast.Colaborador,
            IdForecastPeriodo = idForecastPeriodo,
            TotalesComparables = periodo.Valores,
            Filas = filas
        };
    }

    private async Task<ComparacionForecastReal> CompararPorDepartamentoAsync(
        ContextoComparacionReal cargado,
        long idForecastPeriodo,
        CancellationToken cancellationToken)
    {
        var (periodo, planilla) = ObtenerPeriodoDisponible(cargado, idForecastPeriodo);
        var forecast = cargado.DetallesForecast
            .Where(item => item.IdForecastPeriodo == idForecastPeriodo)
            .GroupBy(item => new { item.IdDepartamento, item.NombreDepartamento })
            .Select(grupo => new FilaLadoForecast
            {
                Clave = $"DEP:{grupo.Key.IdDepartamento}",
                Etiqueta = grupo.Key.NombreDepartamento,
                Detalle = "Forecast: snapshot del escenario · Real: departamento vigente",
                SalarioBruto = grupo
                    .Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                    .Sum(item => item.MontoProyectado),
                Deducciones = grupo
                    .Where(item => item.Concepto == ConceptosForecast.Deducciones)
                    .Sum(item => item.MontoProyectado),
                SalarioNeto = grupo
                    .Where(item => item.Concepto == ConceptosForecast.SalarioNeto)
                    .Sum(item => item.MontoProyectado)
            })
            .ToDictionary(item => item.Clave, StringComparer.Ordinal);

        var departamentosReales = await contexto.DetallesPlanilla
            .AsNoTracking()
            .Where(item => item.IdPlanilla == planilla.IdPlanilla)
            .GroupBy(item => new
            {
                item.Colaborador!.IdDepartamento,
                item.Colaborador.Departamento!.Nombre
            })
            .Select(grupo => new DepartamentoRealConsulta
            {
                IdDepartamento = grupo.Key.IdDepartamento,
                Nombre = grupo.Key.Nombre,
                SalarioBruto = grupo.Sum(item => item.SalarioBruto),
                Deducciones = grupo.Sum(item => item.TotalDeducciones),
                SalarioNeto = grupo.Sum(item => item.SalarioNeto)
            })
            .ToListAsync(cancellationToken);
        var reales = departamentosReales.ToDictionary(
            item => $"DEP:{item.IdDepartamento}",
            item => new FilaLadoReal
            {
                Clave = $"DEP:{item.IdDepartamento}",
                Etiqueta = item.Nombre,
                Detalle = "Departamento vigente del colaborador en la planilla real",
                SalarioBruto = item.SalarioBruto,
                Deducciones = item.Deducciones,
                SalarioNeto = item.SalarioNeto
            },
            StringComparer.Ordinal);

        return new ComparacionForecastReal
        {
            Disponibilidad = cargado.Disponibilidad,
            Dimension = DimensionesComparacionRealForecast.Departamento,
            IdForecastPeriodo = idForecastPeriodo,
            TotalesComparables = periodo.Valores,
            Filas = CrearFilasComparacion(forecast, reales)
        };
    }

    private static ComparacionForecastReal CompararPorProyecto(
        ContextoComparacionReal cargado,
        long idForecastPeriodo)
    {
        var (periodo, _) = ObtenerPeriodoDisponible(cargado, idForecastPeriodo);
        var filas = cargado.DetallesForecast
            .Where(item => item.IdForecastPeriodo == idForecastPeriodo)
            .GroupBy(item => new
            {
                item.IdProyecto,
                item.CodigoProyecto,
                item.NombreProyecto
            })
            .Select(grupo => new FilaComparacionForecastReal
            {
                Clave = $"PRO:{grupo.Key.IdProyecto}",
                Codigo = grupo.Key.CodigoProyecto,
                Etiqueta = grupo.Key.NombreProyecto,
                Detalle = "La planilla real no conserva atribución de costo laboral por proyecto.",
                TieneForecast = true,
                TieneReal = false,
                Valores = new ValoresForecastReal
                {
                    SalarioBrutoProyectado = grupo
                        .Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                        .Sum(item => item.MontoProyectado),
                    DeduccionesProyectadas = grupo
                        .Where(item => item.Concepto == ConceptosForecast.Deducciones)
                        .Sum(item => item.MontoProyectado),
                    SalarioNetoProyectado = grupo
                        .Where(item => item.Concepto == ConceptosForecast.SalarioNeto)
                        .Sum(item => item.MontoProyectado)
                }
            })
            .OrderBy(item => item.Etiqueta)
            .ThenBy(item => item.Codigo)
            .ToList();

        return new ComparacionForecastReal
        {
            Disponibilidad = cargado.Disponibilidad,
            Dimension = DimensionesComparacionRealForecast.Proyecto,
            IdForecastPeriodo = idForecastPeriodo,
            TotalesComparables = SoloForecast(periodo.Valores),
            Filas = filas
        };
    }

    private static (
        PeriodoComparacionRealForecast Periodo,
        PlanillaCoincidenciaConsulta Planilla) ObtenerPeriodoDisponible(
            ContextoComparacionReal cargado,
            long idForecastPeriodo)
    {
        var periodo = cargado.Disponibilidad.Periodos.FirstOrDefault(item =>
            item.IdForecastPeriodo == idForecastPeriodo);
        if (periodo is null)
        {
            throw new ValidationException(
                "El período seleccionado no pertenece al escenario forecast.");
        }

        if (!periodo.EstaDisponible
            || !cargado.PlanillasDisponibles.TryGetValue(idForecastPeriodo, out var planilla))
        {
            throw new InvalidOperationException(periodo.MensajeDisponibilidad);
        }

        return (periodo, planilla);
    }

    private static IReadOnlyList<FilaComparacionForecastReal> CrearFilasComparacion(
        IReadOnlyDictionary<string, FilaLadoForecast> forecast,
        IReadOnlyDictionary<string, FilaLadoReal> reales)
    {
        var claves = forecast.Keys
            .Union(reales.Keys, StringComparer.Ordinal)
            .OrderBy(item => forecast.TryGetValue(item, out var filaForecast)
                ? filaForecast.Etiqueta
                : reales[item].Etiqueta)
            .ThenBy(item => item, StringComparer.Ordinal)
            .ToList();

        return claves.Select(clave =>
        {
            forecast.TryGetValue(clave, out var proyectado);
            reales.TryGetValue(clave, out var real);
            return new FilaComparacionForecastReal
            {
                Clave = clave,
                Codigo = proyectado?.Codigo ?? real?.Codigo ?? string.Empty,
                Etiqueta = proyectado?.Etiqueta ?? real?.Etiqueta.Trim() ?? string.Empty,
                Detalle = proyectado?.Detalle ?? real?.Detalle,
                TieneForecast = proyectado is not null,
                TieneReal = real is not null,
                EsContratacionPrevista = proyectado?.EsContratacionPrevista == true,
                Valores = new ValoresForecastReal
                {
                    SalarioBrutoProyectado = proyectado?.SalarioBruto,
                    SalarioBrutoReal = real?.SalarioBruto,
                    DeduccionesProyectadas = proyectado?.Deducciones,
                    DeduccionesReales = real?.Deducciones,
                    SalarioNetoProyectado = proyectado?.SalarioNeto,
                    SalarioNetoReal = real?.SalarioNeto
                }
            };
        }).ToList();
    }

    private static ValoresForecastReal SoloForecast(ValoresForecastReal valores) => new()
    {
        SalarioBrutoProyectado = valores.SalarioBrutoProyectado,
        DeduccionesProyectadas = valores.DeduccionesProyectadas,
        SalarioNetoProyectado = valores.SalarioNetoProyectado
    };

    private static ValoresForecastReal SumarPeriodosDisponibles(
        IEnumerable<PeriodoComparacionRealForecast> periodos)
    {
        var disponibles = periodos.Where(item => item.EstaDisponible).ToList();
        return new ValoresForecastReal
        {
            SalarioBrutoProyectado = disponibles.Sum(item => item.Valores.SalarioBrutoProyectado ?? 0m),
            SalarioBrutoReal = disponibles.Count == 0
                ? null
                : disponibles.Sum(item => item.Valores.SalarioBrutoReal ?? 0m),
            DeduccionesProyectadas = disponibles.Sum(item => item.Valores.DeduccionesProyectadas ?? 0m),
            DeduccionesReales = disponibles.Count == 0
                ? null
                : disponibles.Sum(item => item.Valores.DeduccionesReales ?? 0m),
            SalarioNetoProyectado = disponibles.Sum(item => item.Valores.SalarioNetoProyectado ?? 0m),
            SalarioNetoReal = disponibles.Count == 0
                ? null
                : disponibles.Sum(item => item.Valores.SalarioNetoReal ?? 0m)
        };
    }

    private static bool EsPlanillaElegible(PlanillaCoincidenciaConsulta planilla) =>
        EsEstadoRealElegible(planilla.EstadoPlanilla)
        && EsEstadoRealElegible(planilla.EstadoPeriodo);

    private static bool EsEstadoRealElegible(string estado) =>
        estado is EstadosPlanilla.Aprobada or EstadosPlanilla.Cerrada;

    private static bool EsConsistente(
        PlanillaCoincidenciaConsulta planilla,
        SumaDetalleRealConsulta? suma) =>
        planilla.SalarioBruto == (suma?.SalarioBruto ?? 0m)
        && planilla.Deducciones == (suma?.Deducciones ?? 0m)
        && planilla.SalarioNeto == (suma?.SalarioNeto ?? 0m);

    private static string CrearMensajeDisponibilidad(
        bool disponible,
        bool ambigua,
        bool existeCoincidencia)
    {
        if (disponible)
        {
            return "Planilla real aprobada disponible para comparar.";
        }

        if (ambigua)
        {
            return "Existe más de una planilla real aprobada para el mismo tipo y rango de fechas.";
        }

        return existeCoincidencia
            ? "Existe una planilla para el período, pero todavía no está aprobada o cerrada."
            : "La planilla real de este período todavía no está disponible.";
    }

    private static void ValidarIdentificador(long idForecastEscenario)
    {
        if (idForecastEscenario <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idForecastEscenario),
                "El escenario seleccionado no es válido.");
        }
    }

    private sealed record ContextoComparacionReal(
        DisponibilidadComparacionRealForecast Disponibilidad,
        IReadOnlyList<DetalleResultadoConsulta> DetallesForecast,
        IReadOnlyDictionary<long, PlanillaCoincidenciaConsulta> PlanillasDisponibles);

    private sealed class EscenarioConsulta
    {
        public long IdForecastEscenario { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string EstadoEscenario { get; init; } = string.Empty;
        public DateTime? FechaCalculo { get; init; }
    }

    private sealed class PeriodoForecastConsulta
    {
        public long IdForecastPeriodo { get; init; }
        public int NumeroOrden { get; init; }
        public string TipoPeriodo { get; init; } = string.Empty;
        public DateTime FechaInicio { get; init; }
        public DateTime FechaFin { get; init; }
    }

    private sealed class PlanillaCoincidenciaConsulta
    {
        public long IdPlanilla { get; init; }
        public string CodigoPeriodo { get; init; } = string.Empty;
        public string TipoPeriodo { get; init; } = string.Empty;
        public DateTime FechaInicio { get; init; }
        public DateTime FechaFin { get; init; }
        public string EstadoPlanilla { get; init; } = string.Empty;
        public string EstadoPeriodo { get; init; } = string.Empty;
        public decimal SalarioBruto { get; init; }
        public decimal Deducciones { get; init; }
        public decimal SalarioNeto { get; init; }
    }

    private sealed class SumaDetalleRealConsulta
    {
        public long IdPlanilla { get; init; }
        public int Cantidad { get; init; }
        public decimal SalarioBruto { get; init; }
        public decimal Deducciones { get; init; }
        public decimal SalarioNeto { get; init; }
    }

    private sealed record ValoresForecastPeriodo(
        decimal SalarioBruto,
        decimal Deducciones,
        decimal SalarioNeto);

    private sealed class FilaLadoForecast
    {
        public string Clave { get; init; } = string.Empty;
        public string Codigo { get; init; } = string.Empty;
        public string Etiqueta { get; init; } = string.Empty;
        public string? Detalle { get; init; }
        public bool EsContratacionPrevista { get; init; }
        public decimal SalarioBruto { get; init; }
        public decimal Deducciones { get; init; }
        public decimal SalarioNeto { get; init; }
    }

    private sealed class FilaLadoReal
    {
        public string Clave { get; init; } = string.Empty;
        public string Codigo { get; init; } = string.Empty;
        public string Etiqueta { get; init; } = string.Empty;
        public string? Detalle { get; init; }
        public decimal SalarioBruto { get; init; }
        public decimal Deducciones { get; init; }
        public decimal SalarioNeto { get; init; }
    }

    private sealed class DepartamentoRealConsulta
    {
        public int IdDepartamento { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public decimal SalarioBruto { get; init; }
        public decimal Deducciones { get; init; }
        public decimal SalarioNeto { get; init; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastCalculoService(ApplicationDbContext contexto)
    : IForecastCalculoService
{
    public async Task<ResultadoGeneracionForecast> GenerarForecastAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var escenario = await CargarEscenarioAsync(idForecastEscenario, cancellationToken);
            var periodos = await CargarPeriodosAsync(idForecastEscenario, cancellationToken);
            var participantes = await CargarParticipantesAsync(idForecastEscenario, cancellationToken);
            var fuentes = await CargarFuentesHistoricasAsync(idForecastEscenario, cancellationToken);
            if (escenario.PeriodosHistoricosConsiderados != fuentes.Count)
            {
                throw new ValidationException(
                    "La cantidad de fuentes historicas no coincide con la definicion del escenario.");
            }
            var parametros = await CargarParametrosAsync(idForecastEscenario, cancellationToken);
            var asignaciones = await CargarYValidarAsignacionesAsync(
                idForecastEscenario,
                periodos,
                participantes,
                cancellationToken);
            var historicos = await CargarHistoricosAsync(
                fuentes,
                participantes,
                cancellationToken);

            var detalles = CrearDetalles(
                escenario,
                periodos,
                participantes,
                parametros,
                asignaciones,
                historicos,
                out var montoProyectadoTotal);

            var detallesAnteriores = await contexto.ForecastDetalles
                .Where(detalle => detalle.IdForecastEscenario == idForecastEscenario)
                .ToListAsync(cancellationToken);
            contexto.ForecastDetalles.RemoveRange(detallesAnteriores);
            contexto.ForecastDetalles.AddRange(detalles);

            var ahora = DateTime.Now;
            escenario.EstadoEscenario = EstadosEscenarioForecast.Calculado;
            escenario.MontoProyectadoTotal = montoProyectadoTotal;
            escenario.MontoRealTotal = null;
            escenario.DiferenciaTotal = null;
            escenario.FechaCalculo = ahora;
            escenario.FechaModificacion = ahora;
            escenario.ModificadoPor = idActor;

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            return new ResultadoGeneracionForecast
            {
                IdForecastEscenario = escenario.IdForecastEscenario,
                EstadoEscenario = escenario.EstadoEscenario,
                FechaCalculo = ahora,
                MontoProyectadoTotal = montoProyectadoTotal,
                CantidadPeriodos = periodos.Count,
                CantidadParticipantes = participantes.Count,
                CantidadDetalles = detalles.Count
            };
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<ForecastEscenario> CargarEscenarioAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var escenario = await contexto.ForecastEscenarios
            .FirstOrDefaultAsync(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "El escenario solicitado no existe o no esta activo.");

        if (escenario.EstadoEscenario != EstadosEscenarioForecast.Borrador)
        {
            throw new InvalidOperationException(
                "El forecast solo puede generarse desde un escenario Borrador.");
        }

        return escenario;
    }

    private async Task<List<ForecastPeriodo>> CargarPeriodosAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var periodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.NumeroOrden)
            .ThenBy(item => item.IdForecastPeriodo)
            .ToListAsync(cancellationToken);

        if (periodos.Count == 0)
        {
            throw new ValidationException(
                "El escenario requiere al menos un periodo activo para generar el forecast.");
        }

        return periodos;
    }

    private async Task<List<ForecastParticipante>> CargarParticipantesAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var participantes = await contexto.ForecastParticipantes
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo
                && item.EstaIncluido)
            .OrderBy(item => item.IdForecastParticipante)
            .ToListAsync(cancellationToken);

        if (participantes.Count == 0)
        {
            throw new ValidationException(
                "El escenario requiere al menos un participante incluido para generar el forecast.");
        }

        return participantes;
    }

    private async Task<List<FuenteHistoricaConsulta>> CargarFuentesHistoricasAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var fuentes = await contexto.ForecastFuentesHistoricas
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario)
            .OrderBy(item => item.NumeroOrden)
            .Select(item => new FuenteHistoricaConsulta
            {
                IdPlanilla = item.IdPlanilla,
                PlanillaActiva = item.Planilla!.EstadoRegistro == EstadosRegistro.Activo,
                PeriodoActivo = item.Planilla.PeriodoPlanilla!.EstadoRegistro == EstadosRegistro.Activo,
                EstadoPlanilla = item.Planilla.EstadoPlanilla!.Nombre,
                EstadoPeriodo = item.Planilla.PeriodoPlanilla.EstadoPlanilla!.Nombre
            })
            .ToListAsync(cancellationToken);

        if (fuentes.Count == 0)
        {
            throw new ValidationException(
                "El escenario requiere al menos una planilla historica aprobada o cerrada.");
        }

        if (fuentes.Any(fuente => !fuente.PlanillaActiva
                || !fuente.PeriodoActivo
                || !EsEstadoHistoricoPermitido(fuente.EstadoPlanilla)
                || !EsEstadoHistoricoPermitido(fuente.EstadoPeriodo)))
        {
            throw new ValidationException(
                "Las fuentes historicas deben permanecer activas y aprobadas o cerradas.");
        }

        return fuentes;
    }

    private async Task<List<ParametroForecastCalculo>> CargarParametrosAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var parametros = await contexto.ForecastParametros
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo
                && (item.Codigo == CodigosParametroForecast.AjusteSalarial
                    || item.Codigo == CodigosParametroForecast.HorasExtraEstimadas
                    || item.Codigo == CodigosParametroForecast.BonosEstimados
                    || item.Codigo == CodigosParametroForecast.DeduccionesRecurrentes))
            .Select(item => new ParametroForecastConsulta
            {
                Codigo = item.Codigo,
                TipoParametro = item.TipoParametro,
                ValorDecimal = item.ValorDecimal
            })
            .ToListAsync(cancellationToken);

        return parametros.Select(parametro => new ParametroForecastCalculo(
                parametro.Codigo,
                parametro.TipoParametro,
                parametro.ValorDecimal ?? throw new ValidationException(
                    $"El parametro {parametro.Codigo} activo no tiene un valor decimal.")))
            .ToList();
    }

    private async Task<Dictionary<(long IdPeriodo, long IdParticipante), List<AsignacionProyectoForecastCalculo>>>
        CargarYValidarAsignacionesAsync(
            long idForecastEscenario,
            IReadOnlyCollection<ForecastPeriodo> periodos,
            IReadOnlyCollection<ForecastParticipante> participantes,
            CancellationToken cancellationToken)
    {
        var asignaciones = await contexto.ForecastAsignacionesProyecto
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new AsignacionForecastConsulta
            {
                IdForecastPeriodo = item.IdForecastPeriodo,
                IdForecastParticipante = item.IdForecastParticipante,
                IdProyecto = item.IdProyecto,
                Porcentaje = item.Porcentaje
            })
            .ToListAsync(cancellationToken);

        var idsPeriodos = periodos.Select(item => item.IdForecastPeriodo).ToHashSet();
        var idsParticipantes = participantes.Select(item => item.IdForecastParticipante).ToHashSet();
        var asignacionesEsperadas = asignaciones
            .Where(item => idsPeriodos.Contains(item.IdForecastPeriodo)
                && idsParticipantes.Contains(item.IdForecastParticipante))
            .ToList();
        var proyectosElegibles = await CargarProyectosElegiblesAsync(
            asignacionesEsperadas.Select(item => item.IdProyecto).ToHashSet(),
            cancellationToken);

        var resultado = new Dictionary<(long IdPeriodo, long IdParticipante), List<AsignacionProyectoForecastCalculo>>();
        foreach (var participante in participantes)
        {
            foreach (var periodo in periodos)
            {
                var celda = asignacionesEsperadas
                    .Where(item => item.IdForecastPeriodo == periodo.IdForecastPeriodo
                        && item.IdForecastParticipante == participante.IdForecastParticipante)
                    .OrderBy(item => item.IdProyecto)
                    .ToList();
                if (celda.Count == 0
                    || celda.Select(item => item.IdProyecto).Distinct().Count() != celda.Count
                    || celda.Sum(item => item.Porcentaje) != 100.0000m
                    || celda.Any(item => !proyectosElegibles.TryGetValue(item.IdProyecto, out var proyecto)
                        || !SeSuperpone(proyecto, periodo)))
                {
                    throw new ValidationException(
                        "Cada participante incluido debe tener una distribucion completa y vigente por periodo.");
                }

                resultado.Add(
                    (periodo.IdForecastPeriodo, participante.IdForecastParticipante),
                    celda.Select(item => new AsignacionProyectoForecastCalculo(
                        item.IdProyecto,
                        item.Porcentaje)).ToList());
            }
        }

        return resultado;
    }

    private async Task<Dictionary<long, ProyectoForecastConsulta>> CargarProyectosElegiblesAsync(
        IReadOnlySet<long> idsProyectos,
        CancellationToken cancellationToken)
    {
        if (idsProyectos.Count == 0)
        {
            return [];
        }

        return await contexto.Proyectos
            .AsNoTracking()
            .Where(item => idsProyectos.Contains(item.IdProyecto)
                && item.EstadoRegistro == EstadosRegistro.Activo
                && item.EstadoProyecto != null
                && item.EstadoProyecto.EstadoRegistro == EstadosRegistro.Activo
                && (item.EstadoProyecto.Nombre == EstadosProyecto.Planificado
                    || item.EstadoProyecto.Nombre == EstadosProyecto.EnEjecucion
                    || item.EstadoProyecto.Nombre == EstadosProyecto.Pausado))
            .Select(item => new ProyectoForecastConsulta
            {
                IdProyecto = item.IdProyecto,
                FechaInicio = item.FechaInicio,
                FechaFin = item.FechaFinReal ?? item.FechaFinEstimada
            })
            .ToDictionaryAsync(item => item.IdProyecto, cancellationToken);
    }

    private async Task<Dictionary<long, ComponentesHistoricosForecast>> CargarHistoricosAsync(
        IReadOnlyCollection<FuenteHistoricaConsulta> fuentes,
        IReadOnlyCollection<ForecastParticipante> participantes,
        CancellationToken cancellationToken)
    {
        var idsColaboradores = participantes
            .Where(item => item.IdColaborador.HasValue)
            .Select(item => item.IdColaborador!.Value)
            .ToHashSet();
        if (idsColaboradores.Count == 0)
        {
            return [];
        }

        var idsPlanillas = fuentes.Select(item => item.IdPlanilla).ToHashSet();
        var detalles = await contexto.DetallesPlanilla
            .AsNoTracking()
            .Where(item => idsPlanillas.Contains(item.IdPlanilla)
                && idsColaboradores.Contains(item.IdColaborador))
            .Select(item => new DetallePlanillaHistoricoConsulta
            {
                IdColaborador = item.IdColaborador,
                SalarioProporcional = item.SalarioProporcional,
                TotalHorasExtra = item.TotalHorasExtra,
                TotalBonos = item.TotalBonos,
                TotalBeneficiosConfigurables = item.TotalBeneficiosConfigurables,
                TotalAusencias = item.TotalAusencias,
                TotalDeducciones = item.TotalDeducciones
            })
            .ToListAsync(cancellationToken);

        return detalles
            .GroupBy(item => item.IdColaborador)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => new ComponentesHistoricosForecast(
                    grupo.Sum(item => item.SalarioProporcional) / fuentes.Count,
                    grupo.Sum(item => item.TotalHorasExtra) / fuentes.Count,
                    grupo.Sum(item => item.TotalBonos) / fuentes.Count,
                    grupo.Sum(item => item.TotalBeneficiosConfigurables) / fuentes.Count,
                    grupo.Sum(item => item.TotalAusencias) / fuentes.Count,
                    grupo.Sum(item => item.TotalDeducciones) / fuentes.Count));
    }

    private static List<ForecastDetalle> CrearDetalles(
        ForecastEscenario escenario,
        IReadOnlyCollection<ForecastPeriodo> periodos,
        IReadOnlyCollection<ForecastParticipante> participantes,
        IReadOnlyCollection<ParametroForecastCalculo> parametros,
        IReadOnlyDictionary<(long IdPeriodo, long IdParticipante), List<AsignacionProyectoForecastCalculo>> asignaciones,
        IReadOnlyDictionary<long, ComponentesHistoricosForecast> historicos,
        out decimal montoProyectadoTotal)
    {
        var detalles = new List<ForecastDetalle>();
        var total = 0m;
        foreach (var participante in participantes.OrderBy(item => item.IdForecastParticipante))
        {
            var historico = participante.IdColaborador.HasValue
                && historicos.TryGetValue(participante.IdColaborador.Value, out var historialParticipante)
                ? historialParticipante
                : ComponentesHistoricosVacios;
            foreach (var periodo in periodos.OrderBy(item => item.NumeroOrden))
            {
                var resultado = CalculadoraForecast.Calcular(new DatosCalculoForecast(
                    periodo.TipoPeriodo,
                    periodo.FechaInicio,
                    periodo.FechaFin,
                    participante.SalarioBaseMensual,
                    participante.FechaInicioAplicacion,
                    participante.FechaSalidaPrevista,
                    historico,
                    parametros));
                var detallesProyecto = CalculadoraForecast.DistribuirPorProyecto(
                    resultado.Conceptos,
                    asignaciones[(periodo.IdForecastPeriodo, participante.IdForecastParticipante)]);

                foreach (var detalle in detallesProyecto)
                {
                    detalles.Add(new ForecastDetalle
                    {
                        IdForecastEscenario = escenario.IdForecastEscenario,
                        IdForecastPeriodo = periodo.IdForecastPeriodo,
                        IdForecastParticipante = participante.IdForecastParticipante,
                        IdProyecto = detalle.IdProyecto,
                        Concepto = detalle.Concepto,
                        MontoBase = detalle.MontoBase,
                        MontoAjuste = detalle.MontoAjuste,
                        MontoProyectado = detalle.MontoProyectado,
                        MontoReal = null,
                        Diferencia = null,
                        Observaciones = null,
                        EstadoRegistro = EstadosRegistro.Activo
                    });
                }

                total += resultado.MontoProyectadoTotal;
            }
        }

        montoProyectadoTotal = CalculadoraForecast.Redondear(total);
        return detalles;
    }

    private static bool EsEstadoHistoricoPermitido(string? estado) =>
        estado == EstadosPlanilla.Aprobada || estado == EstadosPlanilla.Cerrada;

    private static bool SeSuperpone(
        ProyectoForecastConsulta proyecto,
        ForecastPeriodo periodo) =>
        (!proyecto.FechaInicio.HasValue
            || proyecto.FechaInicio.Value.Date <= periodo.FechaFin.Date)
        && (!proyecto.FechaFin.HasValue
            || proyecto.FechaFin.Value.Date >= periodo.FechaInicio.Date);

    private static void ValidarIdentificador(long identificador, string nombreParametro)
    {
        if (identificador <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                "El identificador solicitado no es valido.");
        }
    }

    private static readonly ComponentesHistoricosForecast ComponentesHistoricosVacios = new(
        0m,
        0m,
        0m,
        0m,
        0m,
        0m);

    private sealed class FuenteHistoricaConsulta
    {
        public long IdPlanilla { get; set; }
        public bool PlanillaActiva { get; set; }
        public bool PeriodoActivo { get; set; }
        public string? EstadoPlanilla { get; set; }
        public string? EstadoPeriodo { get; set; }
    }

    private sealed class ParametroForecastConsulta
    {
        public string Codigo { get; set; } = string.Empty;
        public string TipoParametro { get; set; } = string.Empty;
        public decimal? ValorDecimal { get; set; }
    }

    private sealed class AsignacionForecastConsulta
    {
        public long IdForecastPeriodo { get; set; }
        public long IdForecastParticipante { get; set; }
        public long IdProyecto { get; set; }
        public decimal Porcentaje { get; set; }
    }

    private sealed class ProyectoForecastConsulta
    {
        public long IdProyecto { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    private sealed class DetallePlanillaHistoricoConsulta
    {
        public long IdColaborador { get; set; }
        public decimal SalarioProporcional { get; set; }
        public decimal TotalHorasExtra { get; set; }
        public decimal TotalBonos { get; set; }
        public decimal TotalBeneficiosConfigurables { get; set; }
        public decimal TotalAusencias { get; set; }
        public decimal TotalDeducciones { get; set; }
    }
}

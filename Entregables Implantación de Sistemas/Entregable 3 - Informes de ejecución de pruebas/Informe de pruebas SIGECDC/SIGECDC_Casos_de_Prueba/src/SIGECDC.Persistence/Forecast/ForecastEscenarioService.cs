using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastEscenarioService(ApplicationDbContext contexto)
    : IForecastEscenarioService
{
    public async Task<IReadOnlyList<PlanillaHistoricaForecastOpcion>>
        ObtenerFuentesHistoricasDisponiblesAsync(
            string idUsuarioActual,
            CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);

        return await contexto.Planillas
            .AsNoTracking()
            .Where(p=>p.EjecucionVigente!=null && p.EjecucionVigente.SnapshotCompleto && p.EjecucionVigente.EstadoEjecucion=="Autorizada"
                && p.EjecucionVigente.EstadoReproducibilidad=="REPRODUCIBLE" && p.EjecucionVigente.IdPlanilla==p.IdPlanilla)
            .Where(planilla => planilla.EstadoRegistro == EstadosRegistro.Activo
                && planilla.PeriodoPlanilla != null
                && planilla.PeriodoPlanilla.EstadoRegistro == EstadosRegistro.Activo
                && planilla.EstadoPlanilla != null
                && (planilla.EstadoPlanilla.Nombre == EstadosPlanilla.Aprobada
                    || planilla.EstadoPlanilla.Nombre == EstadosPlanilla.Cerrada)
                && planilla.PeriodoPlanilla.EstadoPlanilla != null
                && (planilla.PeriodoPlanilla.EstadoPlanilla.Nombre == EstadosPlanilla.Aprobada
                    || planilla.PeriodoPlanilla.EstadoPlanilla.Nombre == EstadosPlanilla.Cerrada))
            .OrderByDescending(planilla => planilla.PeriodoPlanilla!.FechaFin)
            .ThenByDescending(planilla => planilla.IdPlanilla)
            .Select(planilla => new PlanillaHistoricaForecastOpcion(
                planilla.IdPlanilla,
                planilla.IdPeriodoPlanilla,
                planilla.PeriodoPlanilla!.CodigoPeriodo,
                planilla.PeriodoPlanilla.Nombre,
                planilla.PeriodoPlanilla.FechaInicio,
                planilla.PeriodoPlanilla.FechaFin,
                planilla.EstadoPlanilla!.Nombre))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CrearBorradorAsync(
        SolicitudCrearEscenarioForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        var datos = NormalizarSolicitud(solicitud);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            if(datos.IdPlanillasHistoricas.Count==0)
            {
                var tipos=datos.Periodos.Select(p=>p.TipoPeriodo).Distinct().ToArray();
                if(tipos.Length!=1)throw new ConfiguracionConceptoException("PERIODOS_FORECAST_NO_COMPARABLES");
                var tipo=tipos[0];var inicio=datos.Periodos[0].FechaInicio;
                var automaticas=await contexto.Planillas.AsNoTracking()
                    .Where(p=>p.EstadoRegistro==EstadosRegistro.Activo && p.PeriodoPlanilla!.EstadoRegistro==EstadosRegistro.Activo
                        && p.PeriodoPlanilla.TipoPeriodo==tipo && p.PeriodoPlanilla.FechaFin<inicio
                        && (p.EstadoPlanilla!.Nombre==EstadosPlanilla.Aprobada || p.EstadoPlanilla.Nombre==EstadosPlanilla.Cerrada)
                        && (p.PeriodoPlanilla.EstadoPlanilla!.Nombre==EstadosPlanilla.Aprobada || p.PeriodoPlanilla.EstadoPlanilla.Nombre==EstadosPlanilla.Cerrada)
                        && p.EjecucionVigente!=null && p.EjecucionVigente.SnapshotCompleto && p.EjecucionVigente.EstadoEjecucion=="Autorizada"
                        && p.EjecucionVigente.EstadoReproducibilidad=="REPRODUCIBLE" && p.EjecucionVigente.IdPlanilla==p.IdPlanilla)
                    .OrderByDescending(p=>p.PeriodoPlanilla!.FechaFin).ThenByDescending(p=>p.IdPlanilla).Take(3).Select(p=>p.IdPlanilla).ToArrayAsync(cancellationToken);
                datos=datos with{IdPlanillasHistoricas=automaticas};
            }
            var idsFuentesElegibles = await contexto.Planillas
                .AsNoTracking()
                .Where(planilla => datos.IdPlanillasHistoricas.Contains(planilla.IdPlanilla)
                    && planilla.EstadoRegistro == EstadosRegistro.Activo
                    && planilla.PeriodoPlanilla != null
                    && planilla.PeriodoPlanilla.EstadoRegistro == EstadosRegistro.Activo
                    && planilla.EstadoPlanilla != null
                    && (planilla.EstadoPlanilla.Nombre == EstadosPlanilla.Aprobada
                        || planilla.EstadoPlanilla.Nombre == EstadosPlanilla.Cerrada)
                    && planilla.PeriodoPlanilla.EstadoPlanilla != null
                    && (planilla.PeriodoPlanilla.EstadoPlanilla.Nombre == EstadosPlanilla.Aprobada
                        || planilla.PeriodoPlanilla.EstadoPlanilla.Nombre == EstadosPlanilla.Cerrada))
                .Select(planilla => planilla.IdPlanilla)
                .ToListAsync(cancellationToken);

            if (idsFuentesElegibles.Count != datos.IdPlanillasHistoricas.Count)
            {
                throw new ArgumentException(
                    "Una o más planillas históricas no existen, están inactivas o no se encuentran aprobadas o cerradas.",
                    nameof(solicitud));
            }

            var ahora = DateTime.Now;
            var escenario = new ForecastEscenario
            {
                Nombre = datos.Nombre,
                Descripcion = datos.Descripcion,
                FechaInicioProyeccion = datos.Periodos[0].FechaInicio,
                FechaFinProyeccion = datos.Periodos[^1].FechaFin,
                PeriodosHistoricosConsiderados = datos.IdPlanillasHistoricas.Count,
                TieneHistoricoLimitado = datos.IdPlanillasHistoricas.Count<3,
                EstadoEscenario = EstadosEscenarioForecast.Borrador,
                MontoProyectadoTotal = 0m,
                MontoRealTotal = null,
                DiferenciaTotal = null,
                FechaCalculo = null,
                FechaCreacion = ahora,
                CreadoPor = idActor,
                EstadoRegistro = EstadosRegistro.Activo
            };

            for (var indice = 0; indice < datos.Periodos.Count; indice++)
            {
                var periodo = datos.Periodos[indice];
                escenario.Periodos.Add(new ForecastPeriodo
                {
                    NumeroOrden = indice + 1,
                    TipoPeriodo = periodo.TipoPeriodo,
                    FechaInicio = periodo.FechaInicio,
                    FechaFin = periodo.FechaFin,
                    EstadoRegistro = EstadosRegistro.Activo
                });
            }

            for (var indice = 0; indice < datos.IdPlanillasHistoricas.Count; indice++)
            {
                escenario.FuentesHistoricas.Add(new ForecastFuenteHistorica
                {
                    IdPlanilla = datos.IdPlanillasHistoricas[indice],
                    NumeroOrden = indice + 1
                });
            }

            contexto.ForecastEscenarios.Add(escenario);
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return escenario.IdForecastEscenario;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<IReadOnlyList<EscenarioForecastResumen>> ObtenerEscenariosAsync(
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);

        return await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(escenario => escenario.EstadoRegistro == EstadosRegistro.Activo)
            .OrderByDescending(escenario => escenario.FechaCreacion)
            .ThenByDescending(escenario => escenario.IdForecastEscenario)
            .Select(escenario => new EscenarioForecastResumen
            {
                VersionMotor = escenario.VersionMotor,
                IdForecastEscenario = escenario.IdForecastEscenario,
                Nombre = escenario.Nombre,
                FechaInicioProyeccion = escenario.FechaInicioProyeccion,
                FechaFinProyeccion = escenario.FechaFinProyeccion,
                PeriodosHistoricosConsiderados = escenario.PeriodosHistoricosConsiderados,
                EstadoEscenario = escenario.EstadoEscenario,
                MontoProyectadoTotal = escenario.MontoProyectadoTotal,
                MontoRealTotal = escenario.MontoRealTotal,
                DiferenciaTotal = escenario.DiferenciaTotal,
                FechaCalculo = escenario.FechaCalculo,
                FechaCreacion = escenario.FechaCreacion,
                CantidadPeriodos = escenario.Periodos.Count(periodo =>
                    periodo.EstadoRegistro == EstadosRegistro.Activo),
                CantidadFuentesHistoricas = escenario.FuentesHistoricas.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<EscenarioForecastDetalle?> ObtenerDetalleAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);

        var detalle = await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(escenario => escenario.IdForecastEscenario == idForecastEscenario
                && escenario.EstadoRegistro == EstadosRegistro.Activo)
            .Select(escenario => new EscenarioForecastDetalle
            {
                IdForecastEscenario = escenario.IdForecastEscenario,
                Nombre = escenario.Nombre,
                Descripcion = escenario.Descripcion,
                VersionMotor = escenario.VersionMotor,
                FechaInicioProyeccion = escenario.FechaInicioProyeccion,
                FechaFinProyeccion = escenario.FechaFinProyeccion,
                PeriodosHistoricosConsiderados = escenario.PeriodosHistoricosConsiderados,
                EstadoEscenario = escenario.EstadoEscenario,
                MontoProyectadoTotal = escenario.MontoProyectadoTotal,
                MontoRealTotal = escenario.MontoRealTotal,
                DiferenciaTotal = escenario.DiferenciaTotal,
                FechaCalculo = escenario.FechaCalculo,
                FechaCreacion = escenario.FechaCreacion,
                CreadoPor = escenario.CreadoPor,
                FechaModificacion = escenario.FechaModificacion,
                ModificadoPor = escenario.ModificadoPor,
                EstadoRegistro = escenario.EstadoRegistro,
                CantidadPeriodos = escenario.Periodos.Count(periodo =>
                    periodo.EstadoRegistro == EstadosRegistro.Activo),
                CantidadFuentesHistoricas = escenario.FuentesHistoricas.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (detalle is null)
        {
            return null;
        }

        detalle.Periodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(periodo => periodo.IdForecastEscenario == idForecastEscenario
                && periodo.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(periodo => periodo.NumeroOrden)
            .Select(periodo => new PeriodoForecastResumen(
                periodo.IdForecastPeriodo,
                periodo.NumeroOrden,
                periodo.TipoPeriodo,
                periodo.FechaInicio,
                periodo.FechaFin,
                periodo.EstadoRegistro))
            .ToListAsync(cancellationToken);

        detalle.FuentesHistoricas = await contexto.ForecastFuentesHistoricas
            .AsNoTracking()
            .Where(fuente => fuente.IdForecastEscenario == idForecastEscenario)
            .OrderBy(fuente => fuente.NumeroOrden)
            .Select(fuente => new FuenteHistoricaForecastResumen(
                fuente.IdForecastFuenteHistorica,
                fuente.NumeroOrden,
                fuente.IdPlanilla,
                fuente.Planilla!.IdPeriodoPlanilla,
                fuente.Planilla.PeriodoPlanilla!.CodigoPeriodo,
                fuente.Planilla.PeriodoPlanilla.Nombre,
                fuente.Planilla.PeriodoPlanilla.FechaInicio,
                fuente.Planilla.PeriodoPlanilla.FechaFin,
                fuente.Planilla.EstadoPlanilla!.Nombre))
            .ToListAsync(cancellationToken);

        return detalle;
    }

    public async Task GuardarAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarAdministracionFinancieraAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);

        if (idForecastEscenario <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idForecastEscenario),
                "El escenario solicitado no es válido.");
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var escenario = await BloqueoEscenarioForecast.ObtenerAsync(contexto,idForecastEscenario,cancellationToken);

            if (escenario.EstadoEscenario is EstadosEscenarioForecast.Guardado
                or EstadosEscenarioForecast.Comparado)
            {
                await transaccion.CommitAsync(cancellationToken);
                contexto.ChangeTracker.Clear();
                return;
            }

            if (escenario.EstadoEscenario != EstadosEscenarioForecast.Calculado)
            {
                throw new InvalidOperationException(
                    "Solo se puede guardar un escenario calculado.");
            }

            if (!escenario.FechaCalculo.HasValue)
            {
                throw new InvalidOperationException(
                    "El escenario calculado no conserva su fecha de cálculo.");
            }

            var cantidadPeriodos = await contexto.ForecastPeriodos
                .AsNoTracking()
                .CountAsync(item => item.IdForecastEscenario == idForecastEscenario
                    && item.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken);
            var cantidadFuentes = await contexto.ForecastFuentesHistoricas
                .AsNoTracking()
                .CountAsync(item => item.IdForecastEscenario == idForecastEscenario,
                    cancellationToken);
            var cantidadDetalles = await contexto.ForecastDetalles
                .AsNoTracking()
                .CountAsync(item => item.IdForecastEscenario == idForecastEscenario
                    && item.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken);

            if (cantidadPeriodos == 0 || cantidadFuentes != escenario.PeriodosHistoricosConsiderados || cantidadDetalles == 0)
            {
                throw new InvalidOperationException(
                    "El escenario no conserva toda la información necesaria para guardarse.");
            }

            if(escenario.VersionMotor!=ProyeccionFinanciera.VersionMotor)
                throw new ConfiguracionConceptoException("FORECAST_LEGADO_NO_REPRODUCIBLE");
            await ForecastSnapshotEscritura.ConciliarAsync(contexto,idForecastEscenario,escenario.MontoProyectadoTotal,cancellationToken);

            escenario.EstadoEscenario = EstadosEscenarioForecast.Guardado;
            escenario.FechaModificacion = DateTime.Now;
            escenario.ModificadoPor = idActor;

            contexto.BitacoraAuditoria.Add(new(){IdUsuario=idActor,FechaHora=DateTime.UtcNow,Accion="GUARDAR_FORECAST",Entidad=nameof(ForecastEscenario),
                IdRegistro=idForecastEscenario.ToString(System.Globalization.CultureInfo.InvariantCulture),Observacion="Actor: "+idActor+"; snapshot FIN-FOR-V1 conservado; FIN-02 contenido."});

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private static DatosEscenarioLimpios NormalizarSolicitud(
        SolicitudCrearEscenarioForecast solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        Validator.ValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            validateAllProperties: true);

        var periodos = solicitud.Periodos.Select(periodo => new PeriodoLimpio(
            string.Equals(
                periodo.TipoPeriodo.Trim(),
                TiposPeriodoPlanilla.Mensual,
                StringComparison.OrdinalIgnoreCase)
                ? TiposPeriodoPlanilla.Mensual
                : TiposPeriodoPlanilla.Quincenal,
            periodo.FechaInicio!.Value.Date,
            periodo.FechaFin!.Value.Date)).ToList();

        return new DatosEscenarioLimpios(
            solicitud.Nombre.Trim(),
            string.IsNullOrWhiteSpace(solicitud.Descripcion)
                ? null
                : solicitud.Descripcion.Trim(),
            periodos,
            [.. solicitud.IdPlanillasHistoricas]);
    }

    private sealed record PeriodoLimpio(
        string TipoPeriodo,
        DateTime FechaInicio,
        DateTime FechaFin);

    private sealed record DatosEscenarioLimpios(
        string Nombre,
        string? Descripcion,
        IReadOnlyList<PeriodoLimpio> Periodos,
        IReadOnlyList<long> IdPlanillasHistoricas);
}

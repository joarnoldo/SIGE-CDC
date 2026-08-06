using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastAsignacionProyectoService(ApplicationDbContext contexto)
    : IForecastAsignacionProyectoService
{
    public async Task<ConfiguracionAsignacionesProyectoForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));

        var escenario = await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new EscenarioConsulta
            {
                IdForecastEscenario = item.IdForecastEscenario,
                Nombre = item.Nombre,
                EstadoEscenario = item.EstadoEscenario
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (escenario is null)
        {
            return null;
        }

        var periodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.NumeroOrden)
            .Select(item => new PeriodoForecastResumen(
                item.IdForecastPeriodo,
                item.NumeroOrden,
                item.TipoPeriodo,
                item.FechaInicio,
                item.FechaFin,
                item.EstadoRegistro))
            .ToListAsync(cancellationToken);

        var participantes = await contexto.ForecastParticipantes
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo
                && item.EstaIncluido)
            .OrderBy(item => item.Etiqueta)
            .ThenBy(item => item.CodigoParticipante)
            .Select(item => new ParticipanteAsignacionForecast
            {
                IdForecastParticipante = item.IdForecastParticipante,
                CodigoParticipante = item.CodigoParticipante,
                Etiqueta = item.Etiqueta,
                TipoParticipante = item.TipoParticipante,
                Departamento = item.Departamento!.Nombre,
                Puesto = item.Puesto!.Nombre
            })
            .ToListAsync(cancellationToken);

        var proyectosConsulta = await ObtenerProyectosCandidatosAsync(cancellationToken);
        var proyectos = proyectosConsulta
            .Select(proyecto => new ProyectoAsignableForecast
            {
                IdProyecto = proyecto.IdProyecto,
                CodigoProyecto = proyecto.CodigoProyecto,
                NombreProyecto = proyecto.NombreProyecto,
                EstadoProyecto = proyecto.EstadoProyecto,
                IdsPeriodosElegibles = periodos
                    .Where(periodo => SeSuperpone(proyecto, periodo))
                    .Select(periodo => periodo.IdForecastPeriodo)
                    .ToList()
            })
            .Where(proyecto => proyecto.IdsPeriodosElegibles.Count > 0)
            .OrderBy(proyecto => proyecto.NombreProyecto)
            .ThenBy(proyecto => proyecto.CodigoProyecto)
            .ToList();

        var asignacionesConsulta = await contexto.ForecastAsignacionesProyecto
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new AsignacionConsulta
            {
                IdForecastAsignacionProyecto = item.IdForecastAsignacionProyecto,
                IdForecastParticipante = item.IdForecastParticipante,
                IdForecastPeriodo = item.IdForecastPeriodo,
                IdProyecto = item.IdProyecto,
                CodigoProyecto = item.Proyecto!.CodigoProyecto,
                NombreProyecto = item.Proyecto.NombreProyecto,
                Porcentaje = item.Porcentaje
            })
            .ToListAsync(cancellationToken);

        var elegibilidad = proyectos
            .SelectMany(proyecto => proyecto.IdsPeriodosElegibles.Select(
                idPeriodo => (proyecto.IdProyecto, IdPeriodo: idPeriodo)))
            .ToHashSet();
        var asignaciones = asignacionesConsulta
            .Select(item => new AsignacionProyectoForecastResumen
            {
                IdForecastAsignacionProyecto = item.IdForecastAsignacionProyecto,
                IdForecastParticipante = item.IdForecastParticipante,
                IdForecastPeriodo = item.IdForecastPeriodo,
                IdProyecto = item.IdProyecto,
                CodigoProyecto = item.CodigoProyecto,
                NombreProyecto = item.NombreProyecto,
                Porcentaje = item.Porcentaje,
                EsProyectoElegible = elegibilidad.Contains((item.IdProyecto, item.IdForecastPeriodo))
            })
            .ToList();

        var celdas = participantes
            .SelectMany(participante => periodos.Select(periodo =>
            {
                var filas = asignaciones
                    .Where(item => item.IdForecastParticipante == participante.IdForecastParticipante
                        && item.IdForecastPeriodo == periodo.IdForecastPeriodo)
                    .ToList();
                var total = filas.Sum(item => item.Porcentaje);
                return new CeldaAsignacionForecastResumen
                {
                    IdForecastParticipante = participante.IdForecastParticipante,
                    IdForecastPeriodo = periodo.IdForecastPeriodo,
                    PorcentajeTotal = total,
                    EstaCompleta = filas.Count > 0
                        && total == 100.0000m
                        && filas.All(item => item.EsProyectoElegible)
                };
            }))
            .ToList();
        var completas = celdas.Count(item => item.EstaCompleta);

        return new ConfiguracionAsignacionesProyectoForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            NombreEscenario = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            PuedeEditar = escenario.EstadoEscenario == EstadosEscenarioForecast.Borrador,
            CeldasRequeridas = celdas.Count,
            CeldasCompletas = completas,
            EstaListaParaCalculo = celdas.Count > 0 && completas == celdas.Count,
            Periodos = periodos,
            Participantes = participantes,
            Proyectos = proyectos,
            Asignaciones = asignaciones,
            Celdas = celdas
        };
    }

    public async Task GuardarDistribucionAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodo,
        SolicitudGuardarDistribucionForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await AutorizarYValidarAsync(
            idForecastEscenario,
            idForecastParticipante,
            idForecastPeriodo,
            idUsuarioActual,
            cancellationToken);
        ArgumentNullException.ThrowIfNull(solicitud);
        Validator.ValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            validateAllProperties: true);
        var asignacionesSolicitadas = solicitud.Asignaciones
            ?? throw new ValidationException("Agregue al menos un proyecto a la distribución.");

        await EjecutarEnTransaccionAsync(async () =>
        {
            var datos = await CargarEdicionAsync(idForecastEscenario, cancellationToken);
            var celda = ValidarCelda(datos, idForecastParticipante, idForecastPeriodo);
            var proyectos = await ValidarProyectosAsync(
                celda.Periodo,
                asignacionesSolicitadas.Select(item => item.IdProyecto).ToHashSet(),
                cancellationToken);
            AplicarDistribucion(
                datos,
                celda.Participante,
                celda.Periodo,
                asignacionesSolicitadas,
                proyectos);
            ActualizarEscenario(datos.Escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task LimpiarDistribucionAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodo,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await AutorizarYValidarAsync(
            idForecastEscenario,
            idForecastParticipante,
            idForecastPeriodo,
            idUsuarioActual,
            cancellationToken);

        await EjecutarEnTransaccionAsync(async () =>
        {
            var datos = await CargarEdicionAsync(idForecastEscenario, cancellationToken);
            ValidarCelda(datos, idForecastParticipante, idForecastPeriodo);
            foreach (var asignacion in datos.Asignaciones.Where(item =>
                item.IdForecastParticipante == idForecastParticipante
                && item.IdForecastPeriodo == idForecastPeriodo
                && item.EstadoRegistro == EstadosRegistro.Activo))
            {
                asignacion.EstadoRegistro = EstadosRegistro.Inactivo;
            }

            ActualizarEscenario(datos.Escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task CopiarDistribucionAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodoOrigen,
        IReadOnlyList<long> idsPeriodosDestino,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await AutorizarYValidarAsync(
            idForecastEscenario,
            idForecastParticipante,
            idForecastPeriodoOrigen,
            idUsuarioActual,
            cancellationToken);
        ValidarDestinos(idsPeriodosDestino, idForecastPeriodoOrigen);

        await EjecutarEnTransaccionAsync(async () =>
        {
            var datos = await CargarEdicionAsync(idForecastEscenario, cancellationToken);
            var origen = ValidarCelda(
                datos,
                idForecastParticipante,
                idForecastPeriodoOrigen);
            var asignacionesOrigen = datos.Asignaciones
                .Where(item => item.IdForecastParticipante == idForecastParticipante
                    && item.IdForecastPeriodo == idForecastPeriodoOrigen
                    && item.EstadoRegistro == EstadosRegistro.Activo)
                .OrderBy(item => item.IdProyecto)
                .ToList();

            if (asignacionesOrigen.Count == 0
                || asignacionesOrigen.Sum(item => item.Porcentaje) != 100.0000m)
            {
                throw new ValidationException(
                    "La distribución de origen debe estar completa antes de copiarla.");
            }

            var idsProyectos = asignacionesOrigen.Select(item => item.IdProyecto).ToHashSet();
            await ValidarProyectosAsync(origen.Periodo, idsProyectos, cancellationToken);
            var destinos = datos.Escenario.Periodos
                .Where(item => idsPeriodosDestino.Contains(item.IdForecastPeriodo)
                    && item.EstadoRegistro == EstadosRegistro.Activo)
                .ToList();

            if (destinos.Count != idsPeriodosDestino.Count)
            {
                throw new ValidationException(
                    "Uno o varios períodos destino no pertenecen al escenario activo.");
            }

            var solicitudCopia = asignacionesOrigen
                .Select(item => new SolicitudAsignacionProyectoForecast
                {
                    IdProyecto = item.IdProyecto,
                    Porcentaje = item.Porcentaje
                })
                .ToList();

            foreach (var destino in destinos)
            {
                var proyectos = await ValidarProyectosAsync(
                    destino,
                    idsProyectos,
                    cancellationToken);
                AplicarDistribucion(
                    datos,
                    origen.Participante,
                    destino,
                    solicitudCopia,
                    proyectos);
            }

            ActualizarEscenario(datos.Escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    private async Task<string> AutorizarYValidarAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodo,
        string idUsuarioActual,
        CancellationToken cancellationToken)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));
        ValidarIdentificador(idForecastParticipante, nameof(idForecastParticipante));
        ValidarIdentificador(idForecastPeriodo, nameof(idForecastPeriodo));
        return idActor;
    }

    private async Task<DatosEdicion> CargarEdicionAsync(
        long idForecastEscenario,
        CancellationToken cancellationToken)
    {
        var escenario = await contexto.ForecastEscenarios
            .Include(item => item.Periodos)
            .Include(item => item.Participantes)
            .FirstOrDefaultAsync(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "El escenario solicitado no existe o no está activo.");

        if (escenario.EstadoEscenario != EstadosEscenarioForecast.Borrador)
        {
            throw new InvalidOperationException(
                "La distribución por proyecto solo puede modificarse en un escenario Borrador.");
        }

        var asignaciones = await contexto.ForecastAsignacionesProyecto
            .Where(item => item.IdForecastEscenario == idForecastEscenario)
            .ToListAsync(cancellationToken);
        return new DatosEdicion(escenario, asignaciones);
    }

    private static CeldaEdicion ValidarCelda(
        DatosEdicion datos,
        long idForecastParticipante,
        long idForecastPeriodo)
    {
        var participante = datos.Escenario.Participantes.FirstOrDefault(item =>
            item.IdForecastParticipante == idForecastParticipante
            && item.EstadoRegistro == EstadosRegistro.Activo
            && item.EstaIncluido)
            ?? throw new ValidationException(
                "El participante no pertenece a la composición incluida del escenario.");
        var periodo = datos.Escenario.Periodos.FirstOrDefault(item =>
            item.IdForecastPeriodo == idForecastPeriodo
            && item.EstadoRegistro == EstadosRegistro.Activo)
            ?? throw new ValidationException(
                "El período no pertenece al escenario activo.");
        return new CeldaEdicion(participante, periodo);
    }

    private async Task<IReadOnlyDictionary<long, ProyectoConsulta>> ValidarProyectosAsync(
        ForecastPeriodo periodo,
        IReadOnlySet<long> idsProyectos,
        CancellationToken cancellationToken)
    {
        var candidatos = await ObtenerProyectosCandidatosAsync(cancellationToken);
        var proyectos = candidatos
            .Where(item => idsProyectos.Contains(item.IdProyecto)
                && SeSuperpone(item, periodo.FechaInicio, periodo.FechaFin))
            .ToDictionary(item => item.IdProyecto);

        if (proyectos.Count != idsProyectos.Count)
        {
            throw new ValidationException(
                "Uno o varios proyectos no están activos, no tienen un estado permitido o no se superponen con el período.");
        }

        return proyectos;
    }

    private void AplicarDistribucion(
        DatosEdicion datos,
        ForecastParticipante participante,
        ForecastPeriodo periodo,
        IReadOnlyList<SolicitudAsignacionProyectoForecast> solicitudes,
        IReadOnlyDictionary<long, ProyectoConsulta> proyectos)
    {
        var existentes = datos.Asignaciones
            .Where(item => item.IdForecastParticipante == participante.IdForecastParticipante
                && item.IdForecastPeriodo == periodo.IdForecastPeriodo)
            .ToDictionary(item => item.IdProyecto);

        foreach (var existente in existentes.Values)
        {
            existente.EstadoRegistro = EstadosRegistro.Inactivo;
        }

        foreach (var solicitud in solicitudes)
        {
            _ = proyectos[solicitud.IdProyecto];
            if (!existentes.TryGetValue(solicitud.IdProyecto, out var asignacion))
            {
                asignacion = new ForecastAsignacionProyecto
                {
                    IdForecastEscenario = datos.Escenario.IdForecastEscenario,
                    IdForecastPeriodo = periodo.IdForecastPeriodo,
                    IdForecastParticipante = participante.IdForecastParticipante,
                    IdProyecto = solicitud.IdProyecto
                };
                datos.Asignaciones.Add(asignacion);
                contexto.ForecastAsignacionesProyecto.Add(asignacion);
            }

            asignacion.Porcentaje = solicitud.Porcentaje;
            asignacion.EstadoRegistro = EstadosRegistro.Activo;
        }
    }

    private async Task<IReadOnlyList<ProyectoConsulta>> ObtenerProyectosCandidatosAsync(
        CancellationToken cancellationToken)
    {
        return await contexto.Proyectos
            .AsNoTracking()
            .Where(item => item.EstadoRegistro == EstadosRegistro.Activo
                && item.EstadoProyecto != null
                && item.EstadoProyecto.EstadoRegistro == EstadosRegistro.Activo
                && (item.EstadoProyecto.Nombre == EstadosProyecto.Planificado
                    || item.EstadoProyecto.Nombre == EstadosProyecto.EnEjecucion
                    || item.EstadoProyecto.Nombre == EstadosProyecto.Pausado))
            .Select(item => new ProyectoConsulta
            {
                IdProyecto = item.IdProyecto,
                CodigoProyecto = item.CodigoProyecto,
                NombreProyecto = item.NombreProyecto,
                EstadoProyecto = item.EstadoProyecto!.Nombre,
                FechaInicio = item.FechaInicio,
                FechaFin = item.FechaFinReal ?? item.FechaFinEstimada
            })
            .ToListAsync(cancellationToken);
    }

    private static bool SeSuperpone(
        ProyectoConsulta proyecto,
        PeriodoForecastResumen periodo) =>
        SeSuperpone(proyecto, periodo.FechaInicio, periodo.FechaFin);

    private static bool SeSuperpone(
        ProyectoConsulta proyecto,
        DateTime fechaInicioPeriodo,
        DateTime fechaFinPeriodo) =>
        (!proyecto.FechaInicio.HasValue
            || proyecto.FechaInicio.Value.Date <= fechaFinPeriodo.Date)
        && (!proyecto.FechaFin.HasValue
            || proyecto.FechaFin.Value.Date >= fechaInicioPeriodo.Date);

    private static void ValidarDestinos(
        IReadOnlyList<long> idsPeriodosDestino,
        long idForecastPeriodoOrigen)
    {
        ArgumentNullException.ThrowIfNull(idsPeriodosDestino);
        if (idsPeriodosDestino.Count == 0)
        {
            throw new ValidationException(
                "Seleccione al menos un período destino.");
        }

        if (idsPeriodosDestino.Any(id => id <= 0)
            || idsPeriodosDestino.Contains(idForecastPeriodoOrigen)
            || idsPeriodosDestino.Distinct().Count() != idsPeriodosDestino.Count)
        {
            throw new ValidationException(
                "Los períodos destino deben ser válidos, únicos y distintos del origen.");
        }
    }

    private async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<Task<T>> operacion,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var resultado = await operacion();
            await transaccion.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private static void ActualizarEscenario(ForecastEscenario escenario, string idActor)
    {
        escenario.FechaModificacion = DateTime.Now;
        escenario.ModificadoPor = idActor;
    }

    private static void ValidarIdentificador(long identificador, string nombreParametro)
    {
        if (identificador <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                "El identificador solicitado no es válido.");
        }
    }

    private sealed class EscenarioConsulta
    {
        public long IdForecastEscenario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string EstadoEscenario { get; set; } = string.Empty;
    }

    private sealed class ProyectoConsulta
    {
        public long IdProyecto { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string NombreProyecto { get; set; } = string.Empty;
        public string EstadoProyecto { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    private sealed class AsignacionConsulta
    {
        public long IdForecastAsignacionProyecto { get; set; }
        public long IdForecastParticipante { get; set; }
        public long IdForecastPeriodo { get; set; }
        public long IdProyecto { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string NombreProyecto { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
    }

    private sealed record DatosEdicion(
        ForecastEscenario Escenario,
        List<ForecastAsignacionProyecto> Asignaciones);

    private sealed record CeldaEdicion(
        ForecastParticipante Participante,
        ForecastPeriodo Periodo);
}

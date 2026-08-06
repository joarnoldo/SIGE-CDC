using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastMovimientoPersonalService(ApplicationDbContext contexto)
    : IForecastMovimientoPersonalService
{
    public async Task<ConfiguracionMovimientosForecast?> ObtenerConfiguracionAsync(
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
                EstadoEscenario = item.EstadoEscenario,
                FechaInicioProyeccion = item.FechaInicioProyeccion,
                FechaFinProyeccion = item.FechaFinProyeccion
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

        var departamentos = await contexto.Departamentos
            .AsNoTracking()
            .Where(item => item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.Nombre)
            .Select(item => new OpcionCatalogo(item.IdDepartamento, item.Nombre, item.Descripcion))
            .ToListAsync(cancellationToken);

        var puestos = await contexto.Puestos
            .AsNoTracking()
            .Where(item => item.EstadoRegistro == EstadosRegistro.Activo
                && (!item.IdDepartamento.HasValue
                    || item.Departamento!.EstadoRegistro == EstadosRegistro.Activo))
            .OrderBy(item => item.Nombre)
            .Select(item => new PuestoForecastOpcion(
                item.IdPuesto,
                item.IdDepartamento,
                item.Nombre))
            .ToListAsync(cancellationToken);

        var contrataciones = await contexto.ForecastParticipantes
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.FechaInicioAplicacion)
            .ThenBy(item => item.CodigoParticipante)
            .Select(item => new ContratacionPrevistaForecastResumen
            {
                IdForecastParticipante = item.IdForecastParticipante,
                CodigoParticipante = item.CodigoParticipante,
                Etiqueta = item.Etiqueta,
                IdDepartamento = item.IdDepartamento,
                Departamento = item.Departamento!.Nombre,
                IdPuesto = item.IdPuesto,
                Puesto = item.Puesto!.Nombre,
                SalarioBaseMensual = item.SalarioBaseMensual,
                FechaInicioAplicacion = item.FechaInicioAplicacion
            })
            .ToListAsync(cancellationToken);

        var colaboradores = await contexto.ForecastParticipantes
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.TipoParticipante == TiposParticipanteForecast.Colaborador
                && item.IdColaborador.HasValue
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.Etiqueta)
            .ThenBy(item => item.CodigoParticipante)
            .Select(item => new ColaboradorSalidaPrevistaForecast
            {
                IdForecastParticipante = item.IdForecastParticipante,
                CodigoParticipante = item.CodigoParticipante,
                Etiqueta = item.Etiqueta,
                Departamento = item.Departamento!.Nombre,
                Puesto = item.Puesto!.Nombre,
                EstaIncluido = item.EstaIncluido,
                FechaSalidaPrevista = item.FechaSalidaPrevista
            })
            .ToListAsync(cancellationToken);

        return new ConfiguracionMovimientosForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            NombreEscenario = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            FechaInicioProyeccion = escenario.FechaInicioProyeccion,
            FechaFinProyeccion = escenario.FechaFinProyeccion,
            PuedeEditar = escenario.EstadoEscenario == EstadosEscenarioForecast.Borrador,
            Periodos = periodos,
            Departamentos = departamentos,
            Puestos = puestos,
            Contrataciones = contrataciones,
            Colaboradores = colaboradores
        };
    }

    public async Task<long> RegistrarContratacionPrevistaAsync(
        long idForecastEscenario,
        SolicitudContratacionPrevistaForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await AutorizarYValidarAsync(
            idForecastEscenario,
            solicitud,
            idUsuarioActual,
            cancellationToken);

        return await EjecutarEnTransaccionAsync(async () =>
        {
            var escenario = await CargarEscenarioEditableAsync(
                idForecastEscenario,
                cancellationToken);
            var catalogo = await ValidarCatalogosAsync(solicitud, cancellationToken);
            ValidarFechaEnPeriodo(escenario, solicitud.FechaInicioAplicacion);

            var participante = new ForecastParticipante
            {
                CodigoParticipante = $"PREV-{Guid.NewGuid():N}".ToUpperInvariant(),
                Etiqueta = catalogo.NombrePuesto,
                TipoParticipante = TiposParticipanteForecast.ContratacionPrevista,
                IdColaborador = null,
                IdDepartamento = solicitud.IdDepartamento,
                IdPuesto = solicitud.IdPuesto,
                SalarioBaseMensual = solicitud.SalarioBaseMensual,
                FechaInicioAplicacion = solicitud.FechaInicioAplicacion.Date,
                FechaSalidaPrevista = null,
                EstaIncluido = true,
                EstadoRegistro = EstadosRegistro.Activo
            };
            escenario.Participantes.Add(participante);
            ActualizarEscenario(escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return participante.IdForecastParticipante;
        }, cancellationToken);
    }

    public async Task ActualizarContratacionPrevistaAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        SolicitudContratacionPrevistaForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await AutorizarYValidarAsync(
            idForecastEscenario,
            solicitud,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastParticipante, nameof(idForecastParticipante));

        await EjecutarEnTransaccionAsync(async () =>
        {
            var escenario = await CargarEscenarioEditableAsync(
                idForecastEscenario,
                cancellationToken);
            var participante = ObtenerContratacionActiva(
                escenario,
                idForecastParticipante);
            var catalogo = await ValidarCatalogosAsync(solicitud, cancellationToken);
            ValidarFechaEnPeriodo(escenario, solicitud.FechaInicioAplicacion);

            participante.Etiqueta = catalogo.NombrePuesto;
            participante.IdDepartamento = solicitud.IdDepartamento;
            participante.IdPuesto = solicitud.IdPuesto;
            participante.SalarioBaseMensual = solicitud.SalarioBaseMensual;
            participante.FechaInicioAplicacion = solicitud.FechaInicioAplicacion.Date;
            participante.FechaSalidaPrevista = null;
            participante.EstaIncluido = true;
            ActualizarEscenario(escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task DesactivarContratacionPrevistaAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));
        ValidarIdentificador(idForecastParticipante, nameof(idForecastParticipante));

        await EjecutarEnTransaccionAsync(async () =>
        {
            var escenario = await CargarEscenarioEditableAsync(
                idForecastEscenario,
                cancellationToken);
            var participante = ObtenerContratacionActiva(
                escenario,
                idForecastParticipante);

            participante.EstaIncluido = false;
            participante.EstadoRegistro = EstadosRegistro.Inactivo;
            ActualizarEscenario(escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task GuardarSalidaPrevistaAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        DateTime? fechaSalidaPrevista,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));
        ValidarIdentificador(idForecastParticipante, nameof(idForecastParticipante));

        await EjecutarEnTransaccionAsync(async () =>
        {
            var escenario = await CargarEscenarioEditableAsync(
                idForecastEscenario,
                cancellationToken);
            var participante = escenario.Participantes.FirstOrDefault(item =>
                item.IdForecastParticipante == idForecastParticipante
                && item.TipoParticipante == TiposParticipanteForecast.Colaborador
                && item.IdColaborador.HasValue
                && item.EstadoRegistro == EstadosRegistro.Activo)
                ?? throw new KeyNotFoundException(
                    "El colaborador no pertenece a la composición activa del escenario.");

            if (fechaSalidaPrevista.HasValue)
            {
                if (!participante.EstaIncluido)
                {
                    throw new InvalidOperationException(
                        "Solo se puede registrar una salida para un colaborador incluido.");
                }

                var fecha = fechaSalidaPrevista.Value.Date;
                ValidarFechaEnPeriodo(escenario, fecha);
                if (fecha < participante.FechaInicioAplicacion.Date)
                {
                    throw new ValidationException(
                        "La salida prevista no puede ser anterior al inicio del participante.");
                }

                participante.FechaSalidaPrevista = fecha;
            }
            else
            {
                participante.FechaSalidaPrevista = null;
            }

            ActualizarEscenario(escenario, idActor);
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    private async Task<string> AutorizarYValidarAsync(
        long idForecastEscenario,
        SolicitudContratacionPrevistaForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));
        ArgumentNullException.ThrowIfNull(solicitud);
        Validator.ValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            validateAllProperties: true);
        return idActor;
    }

    private async Task<ForecastEscenario> CargarEscenarioEditableAsync(
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
                "Los cambios previstos solo pueden modificarse en un escenario Borrador.");
        }

        return escenario;
    }

    private async Task<CatalogoContratacion> ValidarCatalogosAsync(
        SolicitudContratacionPrevistaForecast solicitud,
        CancellationToken cancellationToken)
    {
        var departamento = await contexto.Departamentos
            .AsNoTracking()
            .Where(item => item.IdDepartamento == solicitud.IdDepartamento
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => item.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        if (departamento is null)
        {
            throw new ValidationException(
                "El departamento seleccionado no está disponible.");
        }

        var puesto = await contexto.Puestos
            .AsNoTracking()
            .Where(item => item.IdPuesto == solicitud.IdPuesto
                && item.EstadoRegistro == EstadosRegistro.Activo
                && (!item.IdDepartamento.HasValue
                    || item.IdDepartamento == solicitud.IdDepartamento))
            .Select(item => item.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        if (puesto is null)
        {
            throw new ValidationException(
                "El puesto seleccionado no está disponible para el departamento indicado.");
        }

        return new CatalogoContratacion(departamento, puesto);
    }

    private static ForecastParticipante ObtenerContratacionActiva(
        ForecastEscenario escenario,
        long idForecastParticipante)
    {
        return escenario.Participantes.FirstOrDefault(item =>
            item.IdForecastParticipante == idForecastParticipante
            && item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista
            && !item.IdColaborador.HasValue
            && item.EstadoRegistro == EstadosRegistro.Activo)
            ?? throw new KeyNotFoundException(
                "La contratación prevista no pertenece al escenario activo.");
    }

    private static void ValidarFechaEnPeriodo(
        ForecastEscenario escenario,
        DateTime fecha)
    {
        var fechaLimpia = fecha.Date;
        if (fechaLimpia < escenario.FechaInicioProyeccion.Date
            || fechaLimpia > escenario.FechaFinProyeccion.Date
            || !escenario.Periodos.Any(periodo =>
                periodo.EstadoRegistro == EstadosRegistro.Activo
                && fechaLimpia >= periodo.FechaInicio.Date
                && fechaLimpia <= periodo.FechaFin.Date))
        {
            throw new ValidationException(
                "La fecha debe pertenecer a uno de los períodos activos del escenario.");
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
        public DateTime FechaInicioProyeccion { get; set; }
        public DateTime FechaFinProyeccion { get; set; }
    }

    private sealed record CatalogoContratacion(
        string NombreDepartamento,
        string NombrePuesto);
}

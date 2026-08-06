using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastParticipanteService(ApplicationDbContext contexto)
    : IForecastParticipanteService
{
    private const string EstadoContratoActivo = "Activo";

    public async Task<ConfiguracionParticipantesForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario);

        var escenario = await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new EscenarioConsulta
            {
                IdForecastEscenario = item.IdForecastEscenario,
                Nombre = item.Nombre,
                EstadoEscenario = item.EstadoEscenario,
                FechaInicioProyeccion = item.FechaInicioProyeccion
            })
            .FirstOrDefaultAsync(cancellationToken);

        return escenario is null
            ? null
            : await ConstruirConfiguracionAsync(escenario, cancellationToken);
    }

    public async Task GuardarComposicionAsync(
        long idForecastEscenario,
        SolicitudGuardarParticipantesForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario);
        ValidarSolicitud(solicitud);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var escenario = await contexto.ForecastEscenarios
                .Include(item => item.Participantes)
                .FirstOrDefaultAsync(item => item.IdForecastEscenario == idForecastEscenario
                    && item.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    "El escenario solicitado no existe o no está activo.");

            if (escenario.EstadoEscenario != EstadosEscenarioForecast.Borrador)
            {
                throw new InvalidOperationException(
                    "Solo se puede modificar la composición de un escenario Borrador.");
            }

            var configuracion = await ConstruirConfiguracionAsync(
                new EscenarioConsulta
                {
                    IdForecastEscenario = escenario.IdForecastEscenario,
                    Nombre = escenario.Nombre,
                    EstadoEscenario = escenario.EstadoEscenario,
                    FechaInicioProyeccion = escenario.FechaInicioProyeccion
                },
                cancellationToken);
            var solicitudes = solicitud.Participantes!.ToDictionary(item => item.IdColaborador);
            var idsActuales = configuracion.Participantes
                .Select(item => item.IdColaborador)
                .OrderBy(id => id)
                .ToArray();
            var idsRecibidos = solicitudes.Keys.OrderBy(id => id).ToArray();

            if (!idsActuales.SequenceEqual(idsRecibidos))
            {
                throw new ValidationException(
                    "La lista de colaboradores cambió. Recargue la composición antes de guardarla.");
            }

            AplicarComposicion(escenario, configuracion.Participantes, solicitudes);
            escenario.FechaModificacion = DateTime.Now;
            escenario.ModificadoPor = idActor;

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

    private static void AplicarComposicion(
        ForecastEscenario escenario,
        IReadOnlyList<ParticipanteForecastConfigurado> candidatos,
        IReadOnlyDictionary<long, SolicitudParticipanteForecast> solicitudes)
    {
        var activos = escenario.Participantes
            .Where(item => item.TipoParticipante == TiposParticipanteForecast.Colaborador
                && item.IdColaborador.HasValue
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .ToDictionary(item => item.IdColaborador!.Value);
        var inactivos = escenario.Participantes
            .Where(item => item.TipoParticipante == TiposParticipanteForecast.Colaborador
                && item.IdColaborador.HasValue
                && item.EstadoRegistro != EstadosRegistro.Activo)
            .Select(item => item.IdColaborador!.Value)
            .ToHashSet();

        foreach (var candidato in candidatos)
        {
            var incluido = solicitudes[candidato.IdColaborador].EstaIncluido;
            if (activos.TryGetValue(candidato.IdColaborador, out var existente))
            {
                if (incluido && !candidato.EsElegible)
                {
                    throw CrearErrorElegibilidad(candidato, "ya no es elegible");
                }

                existente.EstaIncluido = incluido;
                continue;
            }

            if (!candidato.EsElegible)
            {
                if (incluido)
                {
                    throw CrearErrorElegibilidad(candidato, "no es elegible");
                }

                continue;
            }

            if (inactivos.Contains(candidato.IdColaborador))
            {
                throw new ValidationException(
                    $"El colaborador {candidato.CodigoParticipante} tiene una participación "
                    + "inactiva que debe revisarse antes de incluirlo.");
            }

            escenario.Participantes.Add(new ForecastParticipante
            {
                CodigoParticipante = candidato.CodigoParticipante,
                Etiqueta = candidato.Etiqueta,
                TipoParticipante = TiposParticipanteForecast.Colaborador,
                IdColaborador = candidato.IdColaborador,
                IdDepartamento = candidato.IdDepartamento,
                IdPuesto = candidato.IdPuesto,
                SalarioBaseMensual = candidato.SalarioBaseMensual!.Value,
                FechaInicioAplicacion = escenario.FechaInicioProyeccion,
                FechaSalidaPrevista = null,
                EstaIncluido = incluido,
                EstadoRegistro = EstadosRegistro.Activo
            });
        }
    }

    private static ValidationException CrearErrorElegibilidad(
        ParticipanteForecastConfigurado candidato,
        string condicion)
    {
        return new ValidationException(
            $"El colaborador {candidato.CodigoParticipante} {condicion}: "
            + candidato.MotivoNoElegible);
    }

    private async Task<ConfiguracionParticipantesForecast> ConstruirConfiguracionAsync(
        EscenarioConsulta escenario,
        CancellationToken cancellationToken)
    {
        var hoy = DateTime.Today;
        var persistidos = await contexto.ForecastParticipantes
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == escenario.IdForecastEscenario
                && item.TipoParticipante == TiposParticipanteForecast.Colaborador
                && item.IdColaborador.HasValue
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new ParticipantePersistidoConsulta
            {
                IdForecastParticipante = item.IdForecastParticipante,
                IdColaborador = item.IdColaborador!.Value,
                CodigoParticipante = item.CodigoParticipante,
                Etiqueta = item.Etiqueta,
                IdDepartamento = item.IdDepartamento,
                Departamento = item.Departamento!.Nombre,
                IdPuesto = item.IdPuesto,
                Puesto = item.Puesto!.Nombre,
                SalarioBaseMensual = item.SalarioBaseMensual,
                FechaInicioAplicacion = item.FechaInicioAplicacion,
                EstaIncluido = item.EstaIncluido
            })
            .ToListAsync(cancellationToken);

        var candidatos = await contexto.Colaboradores
            .AsNoTracking()
            .Where(colaborador =>
                colaborador.EstadoRegistro == EstadosRegistro.Activo
                || contexto.ForecastParticipantes.Any(participante =>
                    participante.IdForecastEscenario == escenario.IdForecastEscenario
                    && participante.TipoParticipante == TiposParticipanteForecast.Colaborador
                    && participante.IdColaborador == colaborador.IdColaborador
                    && participante.EstadoRegistro == EstadosRegistro.Activo))
            .Select(colaborador => new CandidatoConsulta
            {
                IdColaborador = colaborador.IdColaborador,
                CodigoColaborador = colaborador.CodigoColaborador,
                Nombre = colaborador.Nombre,
                PrimerApellido = colaborador.PrimerApellido,
                SegundoApellido = colaborador.SegundoApellido,
                EstadoRegistroActivo = colaborador.EstadoRegistro == EstadosRegistro.Activo,
                EstadoLaboralActivo = colaborador.EstadoLaboral != null
                    && colaborador.EstadoLaboral.Nombre == EstadosLaborales.Activo
                    && colaborador.EstadoLaboral.EstadoRegistro == EstadosRegistro.Activo,
                IdDepartamento = colaborador.IdDepartamento,
                Departamento = colaborador.Departamento!.Nombre,
                DepartamentoActivo = colaborador.Departamento != null
                    && colaborador.Departamento.EstadoRegistro == EstadosRegistro.Activo,
                IdPuesto = colaborador.IdPuesto,
                Puesto = colaborador.Puesto!.Nombre,
                PuestoActivo = colaborador.Puesto != null
                    && colaborador.Puesto.EstadoRegistro == EstadosRegistro.Activo,
                ContratosVigentes = contexto.Contratos.Count(contrato =>
                    contrato.IdColaborador == colaborador.IdColaborador
                    && contrato.EstadoRegistro == EstadosRegistro.Activo
                    && contrato.EstadoContrato == EstadoContratoActivo
                    && contrato.FechaInicio <= hoy
                    && (!contrato.FechaFin.HasValue || contrato.FechaFin.Value >= hoy)),
                SalarioVigente = contexto.Contratos
                    .Where(contrato => contrato.IdColaborador == colaborador.IdColaborador
                        && contrato.EstadoRegistro == EstadosRegistro.Activo
                        && contrato.EstadoContrato == EstadoContratoActivo
                        && contrato.FechaInicio <= hoy
                        && (!contrato.FechaFin.HasValue || contrato.FechaFin.Value >= hoy))
                    .OrderBy(contrato => contrato.IdContrato)
                    .Select(contrato => (decimal?)contrato.SalarioBase)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var persistidosPorColaborador = persistidos.ToDictionary(item => item.IdColaborador);
        var idsPersistidos = persistidosPorColaborador.Keys.ToHashSet();
        var filas = candidatos
            .Where(item => idsPersistidos.Contains(item.IdColaborador)
                || item.EstadoRegistroActivo)
            .Select(item => CrearFila(
                item,
                persistidosPorColaborador.GetValueOrDefault(item.IdColaborador),
                escenario.FechaInicioProyeccion))
            .OrderBy(item => item.Etiqueta, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.CodigoParticipante, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ConfiguracionParticipantesForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            NombreEscenario = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            PuedeEditar = escenario.EstadoEscenario == EstadosEscenarioForecast.Borrador,
            Participantes = filas
        };
    }

    private static ParticipanteForecastConfigurado CrearFila(
        CandidatoConsulta candidato,
        ParticipantePersistidoConsulta? persistido,
        DateTime fechaInicioEscenario)
    {
        var motivo = ObtenerMotivoNoElegible(candidato);
        var esElegible = motivo is null;

        return new ParticipanteForecastConfigurado
        {
            IdForecastParticipante = persistido?.IdForecastParticipante,
            IdColaborador = candidato.IdColaborador,
            CodigoParticipante = persistido?.CodigoParticipante ?? candidato.CodigoColaborador,
            Etiqueta = persistido?.Etiqueta ?? ConstruirNombreCompleto(candidato),
            IdDepartamento = persistido?.IdDepartamento ?? candidato.IdDepartamento,
            Departamento = persistido?.Departamento ?? candidato.Departamento,
            IdPuesto = persistido?.IdPuesto ?? candidato.IdPuesto,
            Puesto = persistido?.Puesto ?? candidato.Puesto,
            SalarioBaseMensual = persistido?.SalarioBaseMensual ?? candidato.SalarioVigente,
            FechaInicioAplicacion = persistido?.FechaInicioAplicacion ?? fechaInicioEscenario,
            EstaIncluido = persistido?.EstaIncluido ?? esElegible,
            EstaPersistido = persistido is not null,
            EsElegible = esElegible,
            MotivoNoElegible = motivo
        };
    }

    private static string? ObtenerMotivoNoElegible(CandidatoConsulta candidato)
    {
        if (!candidato.EstadoRegistroActivo || !candidato.EstadoLaboralActivo)
        {
            return "El expediente o el estado laboral no está activo.";
        }

        if (!candidato.DepartamentoActivo)
        {
            return "El departamento asignado no está activo.";
        }

        if (!candidato.PuestoActivo)
        {
            return "El puesto asignado no está activo.";
        }

        return candidato.ContratosVigentes switch
        {
            0 => "No posee un contrato activo vigente.",
            > 1 => "Posee más de un contrato activo vigente.",
            _ when !candidato.SalarioVigente.HasValue => "No fue posible determinar el salario vigente.",
            _ => null
        };
    }

    private static string ConstruirNombreCompleto(CandidatoConsulta candidato)
    {
        return string.Join(
            ' ',
            new[] { candidato.Nombre, candidato.PrimerApellido, candidato.SegundoApellido }
                .Where(valor => !string.IsNullOrWhiteSpace(valor)))
            .Trim();
    }

    private static void ValidarSolicitud(SolicitudGuardarParticipantesForecast solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        Validator.ValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            validateAllProperties: true);
    }

    private static void ValidarIdentificador(long idForecastEscenario)
    {
        if (idForecastEscenario <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idForecastEscenario),
                "El identificador del escenario no es válido.");
        }
    }

    private sealed class EscenarioConsulta
    {
        public long IdForecastEscenario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string EstadoEscenario { get; set; } = string.Empty;
        public DateTime FechaInicioProyeccion { get; set; }
    }

    private sealed class ParticipantePersistidoConsulta
    {
        public long IdForecastParticipante { get; set; }
        public long IdColaborador { get; set; }
        public string CodigoParticipante { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
        public string Departamento { get; set; } = string.Empty;
        public int IdPuesto { get; set; }
        public string Puesto { get; set; } = string.Empty;
        public decimal SalarioBaseMensual { get; set; }
        public DateTime FechaInicioAplicacion { get; set; }
        public bool EstaIncluido { get; set; }
    }

    private sealed class CandidatoConsulta
    {
        public long IdColaborador { get; set; }
        public string CodigoColaborador { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string? SegundoApellido { get; set; }
        public bool EstadoRegistroActivo { get; set; }
        public bool EstadoLaboralActivo { get; set; }
        public int IdDepartamento { get; set; }
        public string Departamento { get; set; } = string.Empty;
        public bool DepartamentoActivo { get; set; }
        public int IdPuesto { get; set; }
        public string Puesto { get; set; } = string.Empty;
        public bool PuestoActivo { get; set; }
        public int ContratosVigentes { get; set; }
        public decimal? SalarioVigente { get; set; }
    }
}

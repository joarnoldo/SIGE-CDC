using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Activos;

public sealed class AsignacionActivoProyectoService(ApplicationDbContext contexto)
    : IAsignacionActivoProyectoService
{
    public async Task<IReadOnlyList<AsignacionActivoProyectoResumen>> ObtenerAsignacionesAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        long? idProyecto = null,
        CancellationToken cancellationToken = default)
    {
        ReglasAsignacionActivo.ValidarRango(fechaInicio, fechaFin);
        var inicio = fechaInicio.Date;
        var fin = fechaFin.Date;

        var consulta = contexto.AsignacionesActivoProyecto
            .AsNoTracking()
            .Where(asignacion => asignacion.EstadoRegistro == EstadosRegistro.Activo
                && asignacion.FechaInicio <= fin
                && asignacion.FechaFin >= inicio);

        if (idProyecto.HasValue && idProyecto.Value > 0)
        {
            consulta = consulta.Where(asignacion => asignacion.IdProyecto == idProyecto.Value);
        }

        return await consulta
            .OrderBy(asignacion => asignacion.FechaInicio)
            .ThenBy(asignacion => asignacion.Activo!.CodigoActivo)
            .Select(asignacion => new AsignacionActivoProyectoResumen
            {
                IdAsignacionActivoProyecto = asignacion.IdAsignacionActivoProyecto,
                IdActivo = asignacion.IdActivo,
                CodigoActivo = asignacion.Activo == null ? string.Empty : asignacion.Activo.CodigoActivo,
                NombreActivo = asignacion.Activo == null ? string.Empty : asignacion.Activo.NombreActivo,
                EstadoActivo = asignacion.Activo == null || asignacion.Activo.EstadoActivo == null
                    ? "Sin estado"
                    : asignacion.Activo.EstadoActivo.Nombre,
                IdProyecto = asignacion.IdProyecto,
                CodigoProyecto = asignacion.Proyecto == null ? string.Empty : asignacion.Proyecto.CodigoProyecto,
                NombreProyecto = asignacion.Proyecto == null ? string.Empty : asignacion.Proyecto.NombreProyecto,
                FechaInicio = asignacion.FechaInicio,
                FechaFin = asignacion.FechaFin,
                Observaciones = asignacion.Observaciones,
                AsignadoPor = asignacion.AsignadoPor,
                FechaAsignacion = asignacion.FechaAsignacion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivoAsignacionOpcion>> ObtenerActivosDisponiblesAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        ReglasAsignacionActivo.ValidarRango(fechaInicio, fechaFin);
        var inicio = fechaInicio.Date;
        var fin = fechaFin.Date;

        return await CrearConsultaDisponibilidad(inicio, fin, idEstadoActivo: null)
            .Where(activo => (activo.EstadoActivo == EstadosActivo.Disponible
                    || activo.EstadoActivo == EstadosActivo.Asignado)
                && !activo.TieneRestriccionDeAsignacion
                && !activo.TieneMantenimientoEnProceso)
            .OrderBy(activo => activo.CodigoActivo)
            .Select(activo => new ActivoAsignacionOpcion
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                EstadoActivo = activo.EstadoActivo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DisponibilidadActivoResumen>> ConsultarDisponibilidadAsync(
        FiltroDisponibilidadActivo filtro,
        CancellationToken cancellationToken = default)
    {
        var datos = DatosFiltroDisponibilidad.DesdeFiltro(filtro);

        var activos = await CrearConsultaDisponibilidad(
                datos.FechaInicio,
                datos.FechaFin,
                datos.IdEstadoActivo)
            .OrderBy(activo => activo.CodigoActivo)
            .ToListAsync(cancellationToken);

        return activos
            .Select(activo => new DisponibilidadActivoResumen
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                TipoActivo = activo.TipoActivo,
                CategoriaActivo = activo.CategoriaActivo,
                IdEstadoActivo = activo.IdEstadoActivo,
                EstadoActivo = activo.EstadoActivo,
                UbicacionActual = activo.UbicacionActual,
                EstaDisponible = ReglasAsignacionActivo.EstaDisponible(
                    activo.EstadoActivo,
                    activo.TieneRestriccionDeAsignacion,
                    activo.TieneMantenimientoEnProceso),
                MotivoDisponibilidad = ReglasAsignacionActivo.DescribirDisponibilidad(
                    activo.EstadoActivo,
                    activo.TieneRestriccionDeAsignacion,
                    activo.TieneMantenimientoEnProceso)
            })
            .ToList();
    }

    public async Task<long> CrearAsignacionAsync(
        SolicitudAsignacionActivoProyecto solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var datos = DatosAsignacionLimpios.DesdeSolicitud(solicitud);
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var usuarioExiste = await contexto.Users
                .AsNoTracking()
                .AnyAsync(usuario => usuario.Id == idUsuario, cancellationToken);

            if (!usuarioExiste)
            {
                throw new InvalidOperationException("No se encontró al usuario responsable de la asignación.");
            }

            var proyecto = await contexto.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(registro => registro.IdProyecto == datos.IdProyecto
                    && registro.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el proyecto seleccionado.");

            var activo = await contexto.Activos
                .Include(registro => registro.EstadoActivo)
                .FirstOrDefaultAsync(registro => registro.IdActivo == datos.IdActivo
                    && registro.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el activo seleccionado.");

            var conflicto = await contexto.AsignacionesActivoProyecto
                .AsNoTracking()
                .Where(asignacion => asignacion.IdActivo == activo.IdActivo
                    && asignacion.EstadoRegistro == EstadosRegistro.Activo
                    && asignacion.FechaInicio <= datos.FechaFin
                    && asignacion.FechaFin >= datos.FechaInicio)
                .OrderBy(asignacion => asignacion.FechaInicio)
                .Select(asignacion => new
                {
                    asignacion.FechaInicio,
                    asignacion.FechaFin,
                    CodigoProyecto = asignacion.Proyecto == null
                        ? asignacion.IdProyecto.ToString()
                        : asignacion.Proyecto.CodigoProyecto
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conflicto is not null)
            {
                throw new InvalidOperationException(
                    $"El activo {activo.CodigoActivo} ya está asignado al proyecto {conflicto.CodigoProyecto} " +
                    $"del {conflicto.FechaInicio:dd/MM/yyyy} al {conflicto.FechaFin:dd/MM/yyyy}.");
            }

            var tieneMantenimientoEnProceso = await contexto.Mantenimientos
                .AsNoTracking()
                .AnyAsync(mantenimiento => mantenimiento.IdActivo == activo.IdActivo
                    && mantenimiento.EstadoRegistro == EstadosRegistro.Activo
                    && mantenimiento.EstadoMantenimiento != null
                    && mantenimiento.EstadoMantenimiento.Nombre == EstadosMantenimiento.EnProceso
                    && mantenimiento.EstadoMantenimiento.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken);

            if (tieneMantenimientoEnProceso)
            {
                throw new InvalidOperationException(
                    $"El activo {activo.CodigoActivo} tiene un mantenimiento en proceso y no puede asignarse.");
            }

            ReglasAsignacionActivo.ValidarEstadoReservable(activo.EstadoActivo?.Nombre);

            var fechaCambio = DateTime.Now;
            var estadoAnterior = activo.EstadoActivo?.Nombre;
            var asignacionVigenteHoy = datos.FechaInicio <= DateTime.Today
                && datos.FechaFin >= DateTime.Today;

            var asignacion = new AsignacionActivoProyecto
            {
                IdActivo = activo.IdActivo,
                IdProyecto = proyecto.IdProyecto,
                FechaInicio = datos.FechaInicio,
                FechaFin = datos.FechaFin,
                Observaciones = datos.Observaciones,
                AsignadoPor = idUsuario,
                FechaAsignacion = fechaCambio,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.AsignacionesActivoProyecto.Add(asignacion);

            if (asignacionVigenteHoy
                && !ReglasAsignacionActivo.EsEstadoCompatibleConAsignacionVigente(estadoAnterior))
            {
                var estadoAsignado = await contexto.EstadosActivo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(estado => estado.Nombre == EstadosActivo.Asignado
                        && estado.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
                    ?? throw new InvalidOperationException(
                        "No se encontró el estado oficial Asignado en la base de datos.");

                activo.IdEstadoActivo = estadoAsignado.IdEstadoActivo;
                activo.EstadoActivo = estadoAsignado;
                activo.FechaModificacion = fechaCambio;
                activo.ModificadoPor = idUsuario;

                contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
                {
                    IdUsuario = idUsuario,
                    FechaHora = fechaCambio,
                    Accion = "Actualización de estado por asignación",
                    Entidad = "Activo",
                    IdRegistro = activo.IdActivo.ToString(),
                    ValoresAnteriores = JsonSerializer.Serialize(new
                    {
                        Estado = estadoAnterior,
                        activo.UbicacionActual
                    }),
                    ValoresNuevos = JsonSerializer.Serialize(new
                    {
                        Estado = estadoAsignado.Nombre,
                        activo.UbicacionActual
                    }),
                    Observacion = $"Estado actualizado de {estadoAnterior ?? "Sin estado"} a {estadoAsignado.Nombre} "
                        + $"al asignar el activo al proyecto {proyecto.CodigoProyecto} "
                        + $"del {datos.FechaInicio:dd/MM/yyyy} al {datos.FechaFin:dd/MM/yyyy}."
                });
            }

            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(
                new BitacoraAuditoria
                {
                    IdUsuario = idUsuario,
                    FechaHora = fechaCambio,
                    Accion =
                        "Creación de asignación de activo",
                    Entidad =
                        "AsignacionActivoProyecto",
                    IdRegistro = asignacion
                        .IdAsignacionActivoProyecto
                        .ToString(),
                    ValoresNuevos = JsonSerializer.Serialize(new
                    {
                        asignacion.IdActivo,
                        asignacion.IdProyecto,
                        asignacion.FechaInicio,
                        asignacion.FechaFin,
                        asignacion.Observaciones
                    }),
                    Observacion =
                        $"Se asignó el activo {activo.CodigoActivo} al proyecto {proyecto.CodigoProyecto}."
                });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return asignacion.IdAsignacionActivoProyecto;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private static string LimpiarIdUsuarioObligatorio(string idUsuario)
    {
        return string.IsNullOrWhiteSpace(idUsuario)
            ? throw new ArgumentException("No se pudo identificar al usuario que realiza la asignación.")
            : idUsuario.Trim();
    }

    private IQueryable<DisponibilidadActivoDatos> CrearConsultaDisponibilidad(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idEstadoActivo)
    {
        var consulta = contexto.Activos
            .AsNoTracking()
            .Where(activo => activo.EstadoRegistro == EstadosRegistro.Activo
                && activo.EstadoActivo != null
                && activo.EstadoActivo.EstadoRegistro == EstadosRegistro.Activo);

        if (idEstadoActivo.HasValue)
        {
            consulta = consulta.Where(activo => activo.IdEstadoActivo == idEstadoActivo.Value);
        }

        return consulta.Select(activo => new DisponibilidadActivoDatos
        {
            IdActivo = activo.IdActivo,
            CodigoActivo = activo.CodigoActivo,
            NombreActivo = activo.NombreActivo,
            TipoActivo = activo.TipoActivo == null ? "Sin tipo" : activo.TipoActivo.Nombre,
            CategoriaActivo = activo.CategoriaActivo == null ? "Sin categoría" : activo.CategoriaActivo.Nombre,
            IdEstadoActivo = activo.IdEstadoActivo,
            EstadoActivo = activo.EstadoActivo == null ? "Sin estado" : activo.EstadoActivo.Nombre,
            UbicacionActual = activo.UbicacionActual,
            TieneRestriccionDeAsignacion = contexto.AsignacionesActivoProyecto.Any(asignacion =>
                asignacion.IdActivo == activo.IdActivo
                && asignacion.EstadoRegistro == EstadosRegistro.Activo
                && asignacion.FechaInicio <= fechaFin
                && asignacion.FechaFin >= fechaInicio),
            TieneMantenimientoEnProceso = contexto.Mantenimientos.Any(mantenimiento =>
                mantenimiento.IdActivo == activo.IdActivo
                && mantenimiento.EstadoRegistro == EstadosRegistro.Activo
                && mantenimiento.EstadoMantenimiento != null
                && mantenimiento.EstadoMantenimiento.Nombre == EstadosMantenimiento.EnProceso
                && mantenimiento.EstadoMantenimiento.EstadoRegistro == EstadosRegistro.Activo)
        });
    }

    private sealed class DisponibilidadActivoDatos
    {
        public long IdActivo { get; set; }

        public string CodigoActivo { get; set; } = string.Empty;

        public string NombreActivo { get; set; } = string.Empty;

        public string TipoActivo { get; set; } = string.Empty;

        public string CategoriaActivo { get; set; } = string.Empty;

        public int IdEstadoActivo { get; set; }

        public string EstadoActivo { get; set; } = string.Empty;

        public string? UbicacionActual { get; set; }

        public bool TieneRestriccionDeAsignacion { get; set; }

        public bool TieneMantenimientoEnProceso { get; set; }
    }

    private sealed record DatosFiltroDisponibilidad(
        DateTime FechaInicio,
        DateTime FechaFin,
        int? IdEstadoActivo)
    {
        public static DatosFiltroDisponibilidad DesdeFiltro(FiltroDisponibilidadActivo filtro)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            if (!filtro.FechaInicio.HasValue || !filtro.FechaFin.HasValue)
            {
                throw new ArgumentException("Las fechas inicial y final son obligatorias.");
            }

            if (filtro.IdEstadoActivo.HasValue && filtro.IdEstadoActivo.Value <= 0)
            {
                throw new ArgumentException("El estado seleccionado no es válido.");
            }

            ReglasAsignacionActivo.ValidarRango(filtro.FechaInicio.Value, filtro.FechaFin.Value);

            return new DatosFiltroDisponibilidad(
                filtro.FechaInicio.Value.Date,
                filtro.FechaFin.Value.Date,
                filtro.IdEstadoActivo);
        }
    }

    private sealed record DatosAsignacionLimpios(
        long IdActivo,
        long IdProyecto,
        DateTime FechaInicio,
        DateTime FechaFin,
        string? Observaciones)
    {
        public static DatosAsignacionLimpios DesdeSolicitud(SolicitudAsignacionActivoProyecto solicitud)
        {
            ArgumentNullException.ThrowIfNull(solicitud);

            if (solicitud.IdActivo <= 0)
            {
                throw new ArgumentException("Seleccione un activo.");
            }

            if (solicitud.IdProyecto <= 0)
            {
                throw new ArgumentException("Seleccione un proyecto.");
            }

            if (!solicitud.FechaInicio.HasValue || !solicitud.FechaFin.HasValue)
            {
                throw new ArgumentException("Las fechas inicial y final son obligatorias.");
            }

            ReglasAsignacionActivo.ValidarRango(solicitud.FechaInicio.Value, solicitud.FechaFin.Value);
            var observaciones = string.IsNullOrWhiteSpace(solicitud.Observaciones)
                ? null
                : solicitud.Observaciones.Trim();

            if (observaciones?.Length > 500)
            {
                throw new ArgumentException("Las observaciones no deben superar los 500 caracteres.");
            }

            return new DatosAsignacionLimpios(
                solicitud.IdActivo,
                solicitud.IdProyecto,
                solicitud.FechaInicio.Value.Date,
                solicitud.FechaFin.Value.Date,
                observaciones);
        }
    }
}

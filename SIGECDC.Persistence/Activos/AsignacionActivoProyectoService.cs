using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
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

        return await contexto.Activos
            .AsNoTracking()
            .Where(activo => activo.EstadoRegistro == EstadosRegistro.Activo
                && activo.EstadoActivo != null
                && activo.EstadoActivo.EstadoRegistro == EstadosRegistro.Activo
                && (activo.EstadoActivo.Nombre == EstadosActivo.Disponible
                    || activo.EstadoActivo.Nombre == EstadosActivo.Asignado)
                && !contexto.AsignacionesActivoProyecto.Any(asignacion =>
                    asignacion.IdActivo == activo.IdActivo
                    && asignacion.EstadoRegistro == EstadosRegistro.Activo
                    && asignacion.FechaInicio <= fin
                    && asignacion.FechaFin >= inicio))
            .OrderBy(activo => activo.CodigoActivo)
            .Select(activo => new ActivoAsignacionOpcion
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                EstadoActivo = activo.EstadoActivo == null ? "Sin estado" : activo.EstadoActivo.Nombre
            })
            .ToListAsync(cancellationToken);
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

            ReglasAsignacionActivo.ValidarEstadoAsignable(activo.EstadoActivo?.Nombre);

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

            var asignacion = new AsignacionActivoProyecto
            {
                IdActivo = activo.IdActivo,
                IdProyecto = proyecto.IdProyecto,
                FechaInicio = datos.FechaInicio,
                FechaFin = datos.FechaFin,
                Observaciones = datos.Observaciones,
                AsignadoPor = idUsuario,
                FechaAsignacion = DateTime.Now,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.AsignacionesActivoProyecto.Add(asignacion);
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return asignacion.IdAsignacionActivoProyecto;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static string LimpiarIdUsuarioObligatorio(string idUsuario)
    {
        return string.IsNullOrWhiteSpace(idUsuario)
            ? throw new ArgumentException("No se pudo identificar al usuario que realiza la asignación.")
            : idUsuario.Trim();
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

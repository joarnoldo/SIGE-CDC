using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Operaciones;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Operaciones;

public sealed class ProyectoService(ApplicationDbContext contexto) : IProyectoService
{
    public async Task<IReadOnlyList<ProyectoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Proyectos
            .AsNoTracking()
            .Include(proyecto => proyecto.EstadoProyecto)
            .Where(proyecto => proyecto.EstadoRegistro == EstadosRegistro.Activo)
            .OrderByDescending(proyecto => proyecto.FechaCreacion)
            .Select(proyecto => new ProyectoResumen
            {
                IdProyecto = proyecto.IdProyecto,
                CodigoProyecto = proyecto.CodigoProyecto,
                NombreProyecto = proyecto.NombreProyecto,
                Descripcion = proyecto.Descripcion,
                FechaInicio = proyecto.FechaInicio,
                FechaFinEstimada = proyecto.FechaFinEstimada,
                FechaFinReal = proyecto.FechaFinReal,
                Responsable = proyecto.Responsable,
                Ubicacion = proyecto.Ubicacion,
                IdEstadoProyecto = proyecto.IdEstadoProyecto,
                EstadoProyecto = proyecto.EstadoProyecto == null ? "Sin estado" : proyecto.EstadoProyecto.Nombre,
                Observaciones = proyecto.Observaciones
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EstadoProyectoOpcion>> ObtenerEstadosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.EstadosProyecto
            .AsNoTracking()
            .Where(estado => estado.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(estado => estado.IdEstadoProyecto)
            .Select(estado => new EstadoProyectoOpcion
            {
                IdEstadoProyecto = estado.IdEstadoProyecto,
                Nombre = estado.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProyectoResumen?> ObtenerProyectoPorIdAsync(long idProyecto, CancellationToken cancellationToken = default)
    {
        return await contexto.Proyectos
            .AsNoTracking()
            .Include(proyecto => proyecto.EstadoProyecto)
            .Where(proyecto => proyecto.IdProyecto == idProyecto
                && proyecto.EstadoRegistro == EstadosRegistro.Activo)
            .Select(proyecto => new ProyectoResumen
            {
                IdProyecto = proyecto.IdProyecto,
                CodigoProyecto = proyecto.CodigoProyecto,
                NombreProyecto = proyecto.NombreProyecto,
                Descripcion = proyecto.Descripcion,
                FechaInicio = proyecto.FechaInicio,
                FechaFinEstimada = proyecto.FechaFinEstimada,
                FechaFinReal = proyecto.FechaFinReal,
                Responsable = proyecto.Responsable,
                Ubicacion = proyecto.Ubicacion,
                IdEstadoProyecto = proyecto.IdEstadoProyecto,
                EstadoProyecto = proyecto.EstadoProyecto == null ? "Sin estado" : proyecto.EstadoProyecto.Nombre,
                Observaciones = proyecto.Observaciones
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task GuardarProyectoAsync(SolicitudProyecto solicitud, CancellationToken cancellationToken = default)
    {
        var codigoProyecto = LimpiarObligatorio(solicitud.CodigoProyecto, "El codigo del proyecto es obligatorio.");
        var nombreProyecto = LimpiarObligatorio(solicitud.NombreProyecto, "El nombre del proyecto es obligatorio.");
        ValidarFechas(solicitud);

        var codigoDuplicado = await contexto.Proyectos
            .AsNoTracking()
            .AnyAsync(
                proyecto => proyecto.CodigoProyecto == codigoProyecto
                    && proyecto.IdProyecto != solicitud.IdProyecto,
                cancellationToken);

        if (codigoDuplicado)
        {
            throw new InvalidOperationException("Ya existe un proyecto con el codigo indicado.");
        }

        var idEstado = solicitud.IdEstadoProyecto ?? await ObtenerIdEstadoPlanificadoAsync(cancellationToken);
        await ValidarEstadoActivoAsync(idEstado, cancellationToken);

        if (solicitud.IdProyecto == 0)
        {
            var proyecto = new Proyecto
            {
                CodigoProyecto = codigoProyecto,
                NombreProyecto = nombreProyecto,
                Descripcion = LimpiarTextoOpcional(solicitud.Descripcion),
                FechaInicio = solicitud.FechaInicio,
                FechaFinEstimada = solicitud.FechaFinEstimada,
                FechaFinReal = solicitud.FechaFinReal,
                Responsable = LimpiarTextoOpcional(solicitud.Responsable),
                Ubicacion = LimpiarTextoOpcional(solicitud.Ubicacion),
                IdEstadoProyecto = idEstado,
                Observaciones = LimpiarTextoOpcional(solicitud.Observaciones),
                FechaCreacion = DateTime.Now,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.Proyectos.Add(proyecto);
        }
        else
        {
            var proyecto = await ObtenerProyectoActivoAsync(solicitud.IdProyecto, cancellationToken);

            proyecto.CodigoProyecto = codigoProyecto;
            proyecto.NombreProyecto = nombreProyecto;
            proyecto.Descripcion = LimpiarTextoOpcional(solicitud.Descripcion);
            proyecto.FechaInicio = solicitud.FechaInicio;
            proyecto.FechaFinEstimada = solicitud.FechaFinEstimada;
            proyecto.FechaFinReal = solicitud.FechaFinReal;
            proyecto.Responsable = LimpiarTextoOpcional(solicitud.Responsable);
            proyecto.Ubicacion = LimpiarTextoOpcional(solicitud.Ubicacion);
            proyecto.IdEstadoProyecto = idEstado;
            proyecto.Observaciones = LimpiarTextoOpcional(solicitud.Observaciones);
            proyecto.FechaModificacion = DateTime.Now;
        }

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsCodigoDuplicado(ex))
        {
            throw new InvalidOperationException("Ya existe un proyecto con el codigo indicado.", ex);
        }
    }

    private async Task<int> ObtenerIdEstadoPlanificadoAsync(CancellationToken cancellationToken)
    {
        var idEstado = await contexto.EstadosProyecto
            .Where(estado => estado.Nombre == EstadosProyecto.Planificado
                && estado.EstadoRegistro == EstadosRegistro.Activo)
            .Select(estado => estado.IdEstadoProyecto)
            .FirstOrDefaultAsync(cancellationToken);

        if (idEstado == 0)
        {
            throw new InvalidOperationException("No se encontro el estado Planificado en el catalogo EstadoProyecto.");
        }

        return idEstado;
    }

    private async Task ValidarEstadoActivoAsync(int idEstadoProyecto, CancellationToken cancellationToken)
    {
        var existe = await contexto.EstadosProyecto
            .AnyAsync(estado => estado.IdEstadoProyecto == idEstadoProyecto
                && estado.EstadoRegistro == EstadosRegistro.Activo, cancellationToken);

        if (!existe)
        {
            throw new InvalidOperationException("El estado seleccionado no esta disponible para proyectos.");
        }
    }

    private async Task<Proyecto> ObtenerProyectoActivoAsync(long idProyecto, CancellationToken cancellationToken)
    {
        return await contexto.Proyectos
            .FirstOrDefaultAsync(proyecto => proyecto.IdProyecto == idProyecto
                && proyecto.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el proyecto solicitado.");
    }

    private static string? LimpiarTextoOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static string LimpiarObligatorio(string valor, string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(mensajeError);
        }

        return valor.Trim();
    }

    private static void ValidarFechas(SolicitudProyecto solicitud)
    {
        if (solicitud.FechaInicio.HasValue
            && solicitud.FechaFinEstimada.HasValue
            && solicitud.FechaFinEstimada.Value.Date < solicitud.FechaInicio.Value.Date)
        {
            throw new ArgumentException("La fecha fin estimada no puede ser anterior a la fecha de inicio.");
        }

        if (solicitud.FechaInicio.HasValue
            && solicitud.FechaFinReal.HasValue
            && solicitud.FechaFinReal.Value.Date < solicitud.FechaInicio.Value.Date)
        {
            throw new ArgumentException("La fecha fin real no puede ser anterior a la fecha de inicio.");
        }
    }

    private static bool EsCodigoDuplicado(DbUpdateException exception)
    {
        var detalle = exception.GetBaseException().Message;
        return detalle.Contains("UX_Proyecto_CodigoProyecto", StringComparison.OrdinalIgnoreCase)
            || detalle.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
    }

}

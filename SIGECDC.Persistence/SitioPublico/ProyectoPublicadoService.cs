using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class ProyectoPublicadoService(ApplicationDbContext contexto) : IProyectoPublicadoService
{
    private const int LongitudMaximaEstadoVisual = 50;

    public async Task<IReadOnlyList<ProyectoPublicadoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.ProyectosPublicados
            .AsNoTracking()
            .Include(proyecto => proyecto.Proyecto)
            .Where(proyecto => proyecto.EstadoRegistro == EstadosRegistro.Activo)
            .OrderByDescending(proyecto => proyecto.FechaCreacion)
            .Select(proyecto => new ProyectoPublicadoResumen
            {
                IdProyectoPublicado = proyecto.IdProyectoPublicado,
                IdProyecto = proyecto.IdProyecto,
                CodigoProyecto = proyecto.Proyecto == null ? null : proyecto.Proyecto.CodigoProyecto,
                NombreProyecto = proyecto.Proyecto == null ? null : proyecto.Proyecto.NombreProyecto,
                Titulo = proyecto.Titulo,
                Descripcion = proyecto.Descripcion,
                EstadoVisual = proyecto.EstadoVisual,
                EstaPublicado = proyecto.EstaPublicado,
                FechaPublicacion = proyecto.FechaPublicacion,
                FechaCreacion = proyecto.FechaCreacion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProyectoPortafolioResumen>> ObtenerProyectosPublicadosAsync(
        string? estadoVisual = null,
        CancellationToken cancellationToken = default)
    {
        var estadoFiltrado = LimpiarTextoOpcional(estadoVisual);

        if (estadoFiltrado?.Length > LongitudMaximaEstadoVisual)
        {
            return [];
        }

        var consulta = contexto.ProyectosPublicados
            .AsNoTracking()
            .Where(proyecto => proyecto.EstadoRegistro == EstadosRegistro.Activo
                && proyecto.EstaPublicado);

        if (estadoFiltrado is not null)
        {
            consulta = consulta.Where(proyecto => proyecto.EstadoVisual != null
                && proyecto.EstadoVisual.Trim() == estadoFiltrado);
        }

        return await consulta
            .Select(proyecto => new ProyectoPortafolioResumen
            {
                IdProyectoPublicado = proyecto.IdProyectoPublicado,
                Titulo = proyecto.Titulo,
                Descripcion = proyecto.Descripcion,
                EstadoVisual = proyecto.EstadoVisual
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ObtenerEstadosPublicadosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.ProyectosPublicados
            .AsNoTracking()
            .Where(proyecto => proyecto.EstadoRegistro == EstadosRegistro.Activo
                && proyecto.EstaPublicado
                && proyecto.EstadoVisual != null
                && proyecto.EstadoVisual.Trim() != string.Empty)
            .Select(proyecto => proyecto.EstadoVisual!.Trim())
            .Distinct()
            .OrderBy(estado => estado)
            .ToListAsync(cancellationToken);
    }

    public async Task GuardarProyectoPublicadoAsync(SolicitudProyectoPublicado solicitud, CancellationToken cancellationToken = default)
    {
        if (solicitud.IdProyectoPublicado == 0)
        {
            var proyecto = new ProyectoPublicado
            {
                IdProyecto = solicitud.IdProyecto == 0 ? null : solicitud.IdProyecto,
                Titulo = solicitud.Titulo.Trim(),
                Descripcion = LimpiarTextoOpcional(solicitud.Descripcion),
                EstadoVisual = LimpiarTextoOpcional(solicitud.EstadoVisual),
                EstaPublicado = solicitud.EstaPublicado,
                FechaPublicacion = solicitud.EstaPublicado ? DateTime.Now : null,
                FechaCreacion = DateTime.Now,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.ProyectosPublicados.Add(proyecto);
        }
        else
        {
            var proyecto = await ObtenerProyectoPublicadoActivoAsync(solicitud.IdProyectoPublicado, cancellationToken);

            proyecto.IdProyecto = solicitud.IdProyecto == 0 ? null : solicitud.IdProyecto;
            proyecto.Titulo = solicitud.Titulo.Trim();
            proyecto.Descripcion = LimpiarTextoOpcional(solicitud.Descripcion);
            proyecto.EstadoVisual = LimpiarTextoOpcional(solicitud.EstadoVisual);
            proyecto.FechaModificacion = DateTime.Now;

            if (proyecto.EstaPublicado != solicitud.EstaPublicado)
            {
                proyecto.EstaPublicado = solicitud.EstaPublicado;
                proyecto.FechaPublicacion = solicitud.EstaPublicado ? DateTime.Now : null;
            }
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task CambiarPublicacionAsync(long idProyectoPublicado, bool estaPublicado, CancellationToken cancellationToken = default)
    {
        var proyecto = await ObtenerProyectoPublicadoActivoAsync(idProyectoPublicado, cancellationToken);

        proyecto.EstaPublicado = estaPublicado;
        proyecto.FechaPublicacion = estaPublicado ? DateTime.Now : null;
        proyecto.FechaModificacion = DateTime.Now;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProyectoPublicado> ObtenerProyectoPublicadoActivoAsync(long idProyectoPublicado, CancellationToken cancellationToken)
    {
        return await contexto.ProyectosPublicados
            .FirstOrDefaultAsync(proyecto => proyecto.IdProyectoPublicado == idProyectoPublicado
                && proyecto.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la tarjeta de proyecto solicitada.");
    }

    private static string? LimpiarTextoOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

}

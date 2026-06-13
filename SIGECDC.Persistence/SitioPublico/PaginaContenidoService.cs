using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class PaginaContenidoService(ApplicationDbContext contexto) : IPaginaContenidoService
{
    public async Task<IReadOnlyList<PaginaContenidoResumen>> ObtenerPaginasAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.PaginasContenido
            .AsNoTracking()
            .Where(pagina => pagina.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(pagina => pagina.CodigoPagina)
            .Select(pagina => new PaginaContenidoResumen
            {
                IdPaginaContenido = pagina.IdPaginaContenido,
                CodigoPagina = pagina.CodigoPagina,
                Titulo = pagina.Titulo,
                Contenido = pagina.Contenido,
                EstaPublicado = pagina.EstaPublicado,
                FechaPublicacion = pagina.FechaPublicacion,
                FechaCreacion = pagina.FechaCreacion,
                FechaModificacion = pagina.FechaModificacion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginaContenidoResumen?> ObtenerPaginaPorIdAsync(long idPaginaContenido, CancellationToken cancellationToken = default)
    {
        return await contexto.PaginasContenido
            .AsNoTracking()
            .Where(pagina => pagina.IdPaginaContenido == idPaginaContenido
                && pagina.EstadoRegistro == EstadosRegistro.Activo)
            .Select(pagina => new PaginaContenidoResumen
            {
                IdPaginaContenido = pagina.IdPaginaContenido,
                CodigoPagina = pagina.CodigoPagina,
                Titulo = pagina.Titulo,
                Contenido = pagina.Contenido,
                EstaPublicado = pagina.EstaPublicado,
                FechaPublicacion = pagina.FechaPublicacion,
                FechaCreacion = pagina.FechaCreacion,
                FechaModificacion = pagina.FechaModificacion
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginaContenidoResumen?> ObtenerPaginaPublicadaAsync(string codigoPagina, CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = codigoPagina.Trim().ToUpperInvariant();

        return await contexto.PaginasContenido
            .AsNoTracking()
            .Where(pagina => pagina.CodigoPagina == codigoNormalizado
                && pagina.EstaPublicado
                && pagina.EstadoRegistro == EstadosRegistro.Activo)
            .Select(pagina => new PaginaContenidoResumen
            {
                IdPaginaContenido = pagina.IdPaginaContenido,
                CodigoPagina = pagina.CodigoPagina,
                Titulo = pagina.Titulo,
                Contenido = pagina.Contenido,
                EstaPublicado = pagina.EstaPublicado,
                FechaPublicacion = pagina.FechaPublicacion,
                FechaCreacion = pagina.FechaCreacion,
                FechaModificacion = pagina.FechaModificacion
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task GuardarPaginaAsync(SolicitudPaginaContenido solicitud, CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = solicitud.CodigoPagina.Trim().ToUpperInvariant();

        if (solicitud.IdPaginaContenido == 0)
        {
            var pagina = new PaginaContenido
            {
                CodigoPagina = codigoNormalizado,
                Titulo = solicitud.Titulo.Trim(),
                Contenido = LimpiarTextoOpcional(solicitud.Contenido),
                EstaPublicado = solicitud.EstaPublicado,
                FechaPublicacion = solicitud.EstaPublicado ? DateTime.Now : null,
                FechaCreacion = DateTime.Now,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.PaginasContenido.Add(pagina);
        }
        else
        {
            var pagina = await ObtenerPaginaActivaAsync(solicitud.IdPaginaContenido, cancellationToken);

            pagina.CodigoPagina = codigoNormalizado;
            pagina.Titulo = solicitud.Titulo.Trim();
            pagina.Contenido = LimpiarTextoOpcional(solicitud.Contenido);
            pagina.FechaModificacion = DateTime.Now;

            if (pagina.EstaPublicado != solicitud.EstaPublicado)
            {
                pagina.EstaPublicado = solicitud.EstaPublicado;
                pagina.FechaPublicacion = solicitud.EstaPublicado ? DateTime.Now : null;
            }
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task CambiarPublicacionAsync(long idPaginaContenido, bool estaPublicado, CancellationToken cancellationToken = default)
    {
        var pagina = await ObtenerPaginaActivaAsync(idPaginaContenido, cancellationToken);

        pagina.EstaPublicado = estaPublicado;
        pagina.FechaPublicacion = estaPublicado ? DateTime.Now : null;
        pagina.FechaModificacion = DateTime.Now;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<PaginaContenido> ObtenerPaginaActivaAsync(long idPaginaContenido, CancellationToken cancellationToken)
    {
        return await contexto.PaginasContenido
            .FirstOrDefaultAsync(pagina => pagina.IdPaginaContenido == idPaginaContenido
                && pagina.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la pagina de contenido solicitada.");
    }

    private static string? LimpiarTextoOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

}

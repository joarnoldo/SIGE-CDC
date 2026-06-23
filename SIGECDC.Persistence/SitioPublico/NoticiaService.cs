using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class NoticiaService(ApplicationDbContext contexto) : INoticiaService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";

    public async Task<IReadOnlyList<NoticiaResumen>> ObtenerTodasAdminAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Noticias
            .AsNoTracking()
            .OrderByDescending(noticia => noticia.FechaCreacion)
            .Select(noticia => Proyectar(noticia))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoticiaResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Noticias
            .AsNoTracking()
            .Where(noticia => noticia.EstadoRegistro == EstadoRegistroActivo && noticia.EstaPublicado)
            .OrderByDescending(noticia => noticia.FechaPublicacion ?? noticia.FechaCreacion)
            .Select(noticia => Proyectar(noticia))
            .ToListAsync(cancellationToken);
    }

    public async Task<NoticiaResumen?> ObtenerPorIdAsync(long idNoticia, CancellationToken cancellationToken = default)
    {
        return await contexto.Noticias
            .AsNoTracking()
            .Where(noticia => noticia.IdNoticia == idNoticia && noticia.EstadoRegistro == EstadoRegistroActivo)
            .Select(noticia => Proyectar(noticia))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long> RegistrarAsync(SolicitudNoticia solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var noticia = new Noticia
        {
            Titulo = LimpiarObligatorio(solicitud.Titulo, "El titulo es obligatorio."),
            Resumen = LimpiarOpcional(solicitud.Resumen),
            Contenido = LimpiarOpcional(solicitud.Contenido),
            EstaPublicado = false,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuarioActual,
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.Noticias.Add(noticia);
        await contexto.SaveChangesAsync(cancellationToken);

        return noticia.IdNoticia;
    }

    public async Task ActualizarAsync(long idNoticia, SolicitudNoticia solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var noticia = await ObtenerNoticiaEditableAsync(idNoticia, cancellationToken);

        noticia.Titulo = LimpiarObligatorio(solicitud.Titulo, "El titulo es obligatorio.");
        noticia.Resumen = LimpiarOpcional(solicitud.Resumen);
        noticia.Contenido = LimpiarOpcional(solicitud.Contenido);
        noticia.FechaModificacion = DateTime.Now;
        noticia.ModificadoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task PublicarAsync(long idNoticia, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var noticia = await ObtenerNoticiaEditableAsync(idNoticia, cancellationToken);
        noticia.EstaPublicado = true;
        noticia.FechaPublicacion ??= DateTime.Now;
        noticia.FechaModificacion = DateTime.Now;
        noticia.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DespublicarAsync(long idNoticia, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var noticia = await ObtenerNoticiaEditableAsync(idNoticia, cancellationToken);
        noticia.EstaPublicado = false;
        noticia.FechaModificacion = DateTime.Now;
        noticia.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(long idNoticia, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var noticia = await ObtenerNoticiaEditableAsync(idNoticia, cancellationToken);
        noticia.EstadoRegistro = EstadoRegistroInactivo;
        noticia.EstaPublicado = false;
        noticia.FechaModificacion = DateTime.Now;
        noticia.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<Noticia> ObtenerNoticiaEditableAsync(long idNoticia, CancellationToken cancellationToken)
    {
        return await contexto.Noticias
            .FirstOrDefaultAsync(noticia => noticia.IdNoticia == idNoticia && noticia.EstadoRegistro == EstadoRegistroActivo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la noticia solicitada.");
    }

    private static NoticiaResumen Proyectar(Noticia noticia)
    {
        return new NoticiaResumen
        {
            IdNoticia = noticia.IdNoticia,
            Titulo = noticia.Titulo,
            Resumen = noticia.Resumen,
            Contenido = noticia.Contenido,
            EstaPublicado = noticia.EstaPublicado,
            FechaPublicacion = noticia.FechaPublicacion,
            EstadoRegistro = noticia.EstadoRegistro
        };
    }

    private static string LimpiarObligatorio(string valor, string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(mensajeError);
        }

        return valor.Trim();
    }

    private static string? LimpiarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
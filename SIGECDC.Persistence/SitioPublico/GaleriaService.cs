using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class GaleriaService(ApplicationDbContext contexto) : IGaleriaService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";

    public async Task<IReadOnlyList<GaleriaResumen>> ObtenerTodasAdminAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Galerias
            .AsNoTracking()
            .OrderBy(galeria => galeria.Nombre)
            .Select(galeria => new GaleriaResumen
            {
                IdGaleria = galeria.IdGaleria,
                Nombre = galeria.Nombre,
                Descripcion = galeria.Descripcion,
                EstaPublicado = galeria.EstaPublicado,
                TotalImagenes = contexto.ImagenesGaleria.Count(imagen =>
                    imagen.IdGaleria == galeria.IdGaleria
                    && imagen.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = galeria.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GaleriaResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Galerias
            .AsNoTracking()
            .Where(galeria => galeria.EstadoRegistro == EstadoRegistroActivo && galeria.EstaPublicado)
            .OrderBy(galeria => galeria.Nombre)
            .Select(galeria => new GaleriaResumen
            {
                IdGaleria = galeria.IdGaleria,
                Nombre = galeria.Nombre,
                Descripcion = galeria.Descripcion,
                EstaPublicado = galeria.EstaPublicado,
                TotalImagenes = contexto.ImagenesGaleria.Count(imagen =>
                    imagen.IdGaleria == galeria.IdGaleria
                    && imagen.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = galeria.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GaleriaResumen?> ObtenerPorIdAsync(long idGaleria, CancellationToken cancellationToken = default)
    {
        return await contexto.Galerias
            .AsNoTracking()
            .Where(galeria => galeria.IdGaleria == idGaleria && galeria.EstadoRegistro == EstadoRegistroActivo)
            .Select(galeria => new GaleriaResumen
            {
                IdGaleria = galeria.IdGaleria,
                Nombre = galeria.Nombre,
                Descripcion = galeria.Descripcion,
                EstaPublicado = galeria.EstaPublicado,
                TotalImagenes = contexto.ImagenesGaleria.Count(imagen =>
                    imagen.IdGaleria == galeria.IdGaleria
                    && imagen.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = galeria.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long> RegistrarAsync(SolicitudGaleria solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var galeria = new Galeria
        {
            Nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre de la galeria es obligatorio."),
            Descripcion = LimpiarOpcional(solicitud.Descripcion),
            EstaPublicado = false,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuarioActual,
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.Galerias.Add(galeria);
        await contexto.SaveChangesAsync(cancellationToken);

        return galeria.IdGaleria;
    }

    public async Task ActualizarAsync(long idGaleria, SolicitudGaleria solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var galeria = await ObtenerGaleriaEditableAsync(idGaleria, cancellationToken);

        galeria.Nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre de la galeria es obligatorio.");
        galeria.Descripcion = LimpiarOpcional(solicitud.Descripcion);
        galeria.FechaModificacion = DateTime.Now;
        galeria.ModificadoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task PublicarAsync(long idGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var galeria = await ObtenerGaleriaEditableAsync(idGaleria, cancellationToken);
        galeria.EstaPublicado = true;
        galeria.FechaModificacion = DateTime.Now;
        galeria.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DespublicarAsync(long idGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var galeria = await ObtenerGaleriaEditableAsync(idGaleria, cancellationToken);
        galeria.EstaPublicado = false;
        galeria.FechaModificacion = DateTime.Now;
        galeria.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(long idGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var galeria = await ObtenerGaleriaEditableAsync(idGaleria, cancellationToken);
        galeria.EstadoRegistro = EstadoRegistroInactivo;
        galeria.EstaPublicado = false;
        galeria.FechaModificacion = DateTime.Now;
        galeria.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<Galeria> ObtenerGaleriaEditableAsync(long idGaleria, CancellationToken cancellationToken)
    {
        return await contexto.Galerias
            .FirstOrDefaultAsync(galeria => galeria.IdGaleria == idGaleria && galeria.EstadoRegistro == EstadoRegistroActivo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la galeria solicitada.");
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
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class GaleriaService(
    ApplicationDbContext contexto,
    IAlmacenamientoArchivosService almacenamientoArchivos) : IGaleriaService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";
    private const string EntidadGaleria = "Galeria";
    private const string TipoDocumentoImagenGaleria = "Imagen galería";
    private const long TamanoMaximoImagen = 10 * 1024 * 1024;

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

    public async Task<IReadOnlyList<GaleriaDetalle>> ObtenerPublicadasConImagenesAsync(CancellationToken cancellationToken = default)
    {
        var galerias = await contexto.Galerias
            .AsNoTracking()
            .Where(galeria => galeria.EstadoRegistro == EstadoRegistroActivo && galeria.EstaPublicado)
            .OrderBy(galeria => galeria.Nombre)
            .Select(galeria => new GaleriaDetalle
            {
                IdGaleria = galeria.IdGaleria,
                Nombre = galeria.Nombre,
                Descripcion = galeria.Descripcion,
                EstaPublicado = galeria.EstaPublicado
            })
            .ToListAsync(cancellationToken);

        var idsGaleria = galerias.Select(galeria => galeria.IdGaleria).ToArray();

        if (idsGaleria.Length == 0)
        {
            return galerias;
        }

        var imagenes = await contexto.ImagenesGaleria
            .AsNoTracking()
            .Include(imagen => imagen.DocumentoArchivo)
            .Where(imagen => idsGaleria.Contains(imagen.IdGaleria)
                && imagen.EstadoRegistro == EstadoRegistroActivo
                && imagen.DocumentoArchivo != null
                && imagen.DocumentoArchivo.EstadoRegistro == EstadoRegistroActivo)
            .OrderBy(imagen => imagen.Orden)
            .ThenBy(imagen => imagen.IdImagenGaleria)
            .Select(imagen => new ImagenGaleriaResumen
            {
                IdImagenGaleria = imagen.IdImagenGaleria,
                IdGaleria = imagen.IdGaleria,
                IdDocumentoArchivo = imagen.IdDocumentoArchivo,
                Titulo = imagen.Titulo,
                Descripcion = imagen.Descripcion,
                Orden = imagen.Orden,
                NombreOriginal = imagen.DocumentoArchivo == null ? string.Empty : imagen.DocumentoArchivo.NombreOriginal,
                MimeType = imagen.DocumentoArchivo == null ? null : imagen.DocumentoArchivo.MimeType,
                TamanoBytes = imagen.DocumentoArchivo == null ? null : imagen.DocumentoArchivo.TamanoBytes,
                FechaCarga = imagen.DocumentoArchivo == null ? DateTime.MinValue : imagen.DocumentoArchivo.FechaCarga
            })
            .ToListAsync(cancellationToken);

        var imagenesPorGaleria = imagenes
            .GroupBy(imagen => imagen.IdGaleria)
            .ToDictionary(grupo => grupo.Key, grupo => (IReadOnlyList<ImagenGaleriaResumen>)grupo.ToList());

        foreach (var galeria in galerias)
        {
            galeria.Imagenes = imagenesPorGaleria.GetValueOrDefault(galeria.IdGaleria) ?? [];
        }

        return galerias;
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

    public async Task<IReadOnlyList<ImagenGaleriaResumen>> ObtenerImagenesAsync(long idGaleria, CancellationToken cancellationToken = default)
    {
        return await contexto.ImagenesGaleria
            .AsNoTracking()
            .Include(imagen => imagen.DocumentoArchivo)
            .Where(imagen => imagen.IdGaleria == idGaleria
                && imagen.EstadoRegistro == EstadoRegistroActivo
                && imagen.DocumentoArchivo != null
                && imagen.DocumentoArchivo.EstadoRegistro == EstadoRegistroActivo)
            .OrderBy(imagen => imagen.Orden)
            .ThenBy(imagen => imagen.IdImagenGaleria)
            .Select(imagen => new ImagenGaleriaResumen
            {
                IdImagenGaleria = imagen.IdImagenGaleria,
                IdGaleria = imagen.IdGaleria,
                IdDocumentoArchivo = imagen.IdDocumentoArchivo,
                Titulo = imagen.Titulo,
                Descripcion = imagen.Descripcion,
                Orden = imagen.Orden,
                NombreOriginal = imagen.DocumentoArchivo == null ? string.Empty : imagen.DocumentoArchivo.NombreOriginal,
                MimeType = imagen.DocumentoArchivo == null ? null : imagen.DocumentoArchivo.MimeType,
                TamanoBytes = imagen.DocumentoArchivo == null ? null : imagen.DocumentoArchivo.TamanoBytes,
                FechaCarga = imagen.DocumentoArchivo == null ? DateTime.MinValue : imagen.DocumentoArchivo.FechaCarga
            })
            .ToListAsync(cancellationToken);
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

    public async Task<long> AdjuntarImagenAsync(
        SolicitudImagenGaleria solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        await ValidarGaleriaActivaAsync(solicitud.IdGaleria, cancellationToken);
        var idTipoDocumento = await ObtenerIdTipoDocumentoImagenAsync(cancellationToken);
        var nombreOriginal = LimpiarObligatorio(solicitud.NombreOriginal, "El nombre del archivo es obligatorio.");
        var mimeTypeSeguro = ObtenerMimeTypeImagen(nombreOriginal, solicitud.MimeType);

        if (solicitud.Orden < 0)
        {
            throw new ArgumentException("El orden no puede ser negativo.");
        }

        await using var contenidoValidado = await CopiarYValidarImagenAsync(
            solicitud.Contenido,
            solicitud.TamanoBytes,
            mimeTypeSeguro,
            cancellationToken);

        var archivo = await almacenamientoArchivos.GuardarAsync(
            contenidoValidado,
            nombreOriginal,
            mimeTypeSeguro,
            contenidoValidado.Length,
            $"galerias/{solicitud.IdGaleria}",
            cancellationToken);

        var documento = new DocumentoArchivo
        {
            EntidadRelacionada = EntidadGaleria,
            IdEntidadRelacionada = solicitud.IdGaleria,
            IdTipoDocumento = idTipoDocumento,
            NombreOriginal = nombreOriginal,
            NombreAlmacenado = archivo.NombreAlmacenado,
            RutaRelativa = archivo.RutaRelativa,
            MimeType = archivo.MimeType,
            TamanoBytes = archivo.TamanoBytes,
            FechaCarga = DateTime.Now,
            CargadoPor = idUsuarioActual,
            EstadoRegistro = EstadoRegistroActivo
        };

        var imagen = new ImagenGaleria
        {
            IdGaleria = solicitud.IdGaleria,
            DocumentoArchivo = documento,
            Titulo = LimpiarOpcional(solicitud.Titulo),
            Descripcion = LimpiarOpcional(solicitud.Descripcion),
            Orden = solicitud.Orden,
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.ImagenesGaleria.Add(imagen);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await almacenamientoArchivos.EliminarAsync(archivo.RutaRelativa, CancellationToken.None);
            }
            catch
            {
                // La limpieza es compensatoria; se conserva la excepción original del guardado de metadatos.
            }

            throw;
        }

        return imagen.IdImagenGaleria;
    }

    public async Task DesactivarImagenAsync(long idImagenGaleria, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var imagen = await contexto.ImagenesGaleria
            .Include(registro => registro.DocumentoArchivo)
            .FirstOrDefaultAsync(
                registro => registro.IdImagenGaleria == idImagenGaleria
                    && registro.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken)
            ?? throw new InvalidOperationException("No se encontró la imagen solicitada.");

        imagen.EstadoRegistro = EstadoRegistroInactivo;
        if (imagen.DocumentoArchivo is not null)
        {
            imagen.DocumentoArchivo.EstadoRegistro = EstadoRegistroInactivo;
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<ArchivoDescarga?> AbrirImagenPublicaAsync(long idImagenGaleria, CancellationToken cancellationToken = default)
    {
        return await AbrirImagenAsync(idImagenGaleria, exigirGaleriaPublicada: true, cancellationToken);
    }

    public async Task<ArchivoDescarga?> AbrirImagenAdministrativaAsync(long idImagenGaleria, CancellationToken cancellationToken = default)
    {
        return await AbrirImagenAsync(idImagenGaleria, exigirGaleriaPublicada: false, cancellationToken);
    }

    private async Task<ArchivoDescarga?> AbrirImagenAsync(
        long idImagenGaleria,
        bool exigirGaleriaPublicada,
        CancellationToken cancellationToken)
    {
        var imagen = await contexto.ImagenesGaleria
            .AsNoTracking()
            .Include(registro => registro.Galeria)
            .Include(registro => registro.DocumentoArchivo)
            .FirstOrDefaultAsync(
                registro => registro.IdImagenGaleria == idImagenGaleria
                    && registro.EstadoRegistro == EstadoRegistroActivo
                    && registro.Galeria != null
                    && registro.Galeria.EstadoRegistro == EstadoRegistroActivo
                    && (!exigirGaleriaPublicada || registro.Galeria.EstaPublicado)
                    && registro.DocumentoArchivo != null
                    && registro.DocumentoArchivo.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (imagen?.DocumentoArchivo is null)
        {
            return null;
        }

        try
        {
            var contenido = await almacenamientoArchivos.AbrirLecturaAsync(imagen.DocumentoArchivo.RutaRelativa, cancellationToken);
            return new ArchivoDescarga(contenido, imagen.DocumentoArchivo.NombreOriginal, imagen.DocumentoArchivo.MimeType);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private async Task<Galeria> ObtenerGaleriaEditableAsync(long idGaleria, CancellationToken cancellationToken)
    {
        return await contexto.Galerias
            .FirstOrDefaultAsync(galeria => galeria.IdGaleria == idGaleria && galeria.EstadoRegistro == EstadoRegistroActivo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la galeria solicitada.");
    }

    private async Task ValidarGaleriaActivaAsync(long idGaleria, CancellationToken cancellationToken)
    {
        var existe = await contexto.Galerias
            .AnyAsync(
                galeria => galeria.IdGaleria == idGaleria
                    && galeria.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!existe)
        {
            throw new ArgumentException("La galería seleccionada no está disponible.");
        }
    }

    private async Task<int> ObtenerIdTipoDocumentoImagenAsync(CancellationToken cancellationToken)
    {
        var idTipoDocumento = await contexto.TiposDocumento
            .Where(tipo => tipo.EstadoRegistro == EstadoRegistroActivo
                && (tipo.Nombre == TipoDocumentoImagenGaleria || tipo.Nombre == "Imagen galeria"))
            .Select(tipo => tipo.IdTipoDocumento)
            .FirstOrDefaultAsync(cancellationToken);

        if (idTipoDocumento == 0)
        {
            throw new InvalidOperationException("No existe el tipo de documento Imagen galería en el catálogo.");
        }

        return idTipoDocumento;
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

    private static string ObtenerMimeTypeImagen(string nombreOriginal, string? mimeTypeInformado)
    {
        var extension = Path.GetExtension(nombreOriginal);
        var mimeTypeEsperado = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new ArgumentException("Solo se permiten imágenes JPG o PNG.")
        };

        if (!string.IsNullOrWhiteSpace(mimeTypeInformado)
            && !string.Equals(mimeTypeInformado, mimeTypeEsperado, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El tipo declarado del archivo no coincide con su extensión.");
        }

        return mimeTypeEsperado;
    }

    private static async Task<MemoryStream> CopiarYValidarImagenAsync(
        Stream contenido,
        long tamanoInformado,
        string mimeType,
        CancellationToken cancellationToken)
    {
        if (contenido is null || contenido == Stream.Null)
        {
            throw new ArgumentException("La imagen es obligatoria.");
        }

        if (tamanoInformado <= 0 || tamanoInformado > TamanoMaximoImagen)
        {
            throw new ArgumentException("La imagen debe ser mayor a 0 bytes y no superar 10 MB.");
        }

        var copia = new MemoryStream((int)tamanoInformado);

        try
        {
            await contenido.CopyToAsync(copia, cancellationToken);

            if (copia.Length <= 0 || copia.Length > TamanoMaximoImagen)
            {
                throw new ArgumentException("La imagen debe ser mayor a 0 bytes y no superar 10 MB.");
            }

            var bytes = copia.GetBuffer();
            var firmaValida = mimeType == "image/png"
                ? TieneFirmaPng(bytes, copia.Length)
                : TieneFirmaJpeg(bytes, copia.Length);

            if (!firmaValida)
            {
                throw new ArgumentException("El contenido del archivo no corresponde a una imagen JPG o PNG válida.");
            }

            copia.Position = 0;
            return copia;
        }
        catch
        {
            await copia.DisposeAsync();
            throw;
        }
    }

    private static bool TieneFirmaPng(byte[] bytes, long longitud)
    {
        ReadOnlySpan<byte> firma = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return longitud >= firma.Length && bytes.AsSpan(0, firma.Length).SequenceEqual(firma);
    }

    private static bool TieneFirmaJpeg(byte[] bytes, long longitud)
    {
        return longitud >= 4
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[longitud - 2] == 0xFF
            && bytes[longitud - 1] == 0xD9;
    }
}

using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Activos;

public sealed class ActivoService(ApplicationDbContext contexto) : IActivoService
{
    public async Task<IReadOnlyList<ActivoResumen>> ObtenerActivosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Activos
            .AsNoTracking()
            .Where(activo => activo.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(activo => activo.CodigoActivo)
            .Select(activo => new ActivoResumen
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                TipoActivo = activo.TipoActivo == null ? string.Empty : activo.TipoActivo.Nombre,
                CategoriaActivo = activo.CategoriaActivo == null ? string.Empty : activo.CategoriaActivo.Nombre,
                Marca = activo.Marca,
                Modelo = activo.Modelo,
                NumeroSerie = activo.NumeroSerie,
                Placa = activo.Placa,
                Descripcion = activo.Descripcion,
                UbicacionActual = activo.UbicacionActual,
                IdEstadoActivo = activo.IdEstadoActivo,
                EstadoActivo = activo.EstadoActivo == null ? string.Empty : activo.EstadoActivo.Nombre,
                Observaciones = activo.Observaciones
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivoResumen?> ObtenerActivoPorIdAsync(long idActivo, CancellationToken cancellationToken = default)
    {
        return await contexto.Activos
            .AsNoTracking()
            .Where(activo => activo.IdActivo == idActivo && activo.EstadoRegistro == EstadosRegistro.Activo)
            .Select(activo => new ActivoResumen
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                TipoActivo = activo.TipoActivo == null ? string.Empty : activo.TipoActivo.Nombre,
                CategoriaActivo = activo.CategoriaActivo == null ? string.Empty : activo.CategoriaActivo.Nombre,
                Marca = activo.Marca,
                Modelo = activo.Modelo,
                NumeroSerie = activo.NumeroSerie,
                Placa = activo.Placa,
                Descripcion = activo.Descripcion,
                UbicacionActual = activo.UbicacionActual,
                IdEstadoActivo = activo.IdEstadoActivo,
                EstadoActivo = activo.EstadoActivo == null ? string.Empty : activo.EstadoActivo.Nombre,
                Observaciones = activo.Observaciones
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoActivoOpcion>> ObtenerTiposActivoAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.TiposActivo
            .AsNoTracking()
            .Where(tipo => tipo.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(tipo => tipo.Nombre)
            .Select(tipo => new TipoActivoOpcion
            {
                IdTipoActivo = tipo.IdTipoActivo,
                Nombre = tipo.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EstadoActivoOpcion>> ObtenerEstadosActivoAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado => estado.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(estado => estado.Nombre)
            .Select(estado => new EstadoActivoOpcion
            {
                IdEstadoActivo = estado.IdEstadoActivo,
                Nombre = estado.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoriaActivoOpcion>> ObtenerCategoriasPorTipoAsync(
        int idTipoActivo, CancellationToken cancellationToken = default)
    {
        return await contexto.CategoriasActivo
            .AsNoTracking()
            .Where(categoria => categoria.IdTipoActivo == idTipoActivo
                && categoria.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(categoria => categoria.Nombre)
            .Select(categoria => new CategoriaActivoOpcion
            {
                IdCategoriaActivo = categoria.IdCategoriaActivo,
                IdTipoActivo = categoria.IdTipoActivo,
                Nombre = categoria.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task GuardarActivoAsync(SolicitudActivo solicitud, CancellationToken cancellationToken = default)
    {
        var datos = DatosActivoLimpios.DesdeSolicitud(solicitud);

        if (datos.IdActivo > 0)
        {
            var activo = await contexto.Activos
                .FirstOrDefaultAsync(a => a.IdActivo == datos.IdActivo
                    && a.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el activo a modificar.");

            activo.CodigoActivo = datos.CodigoActivo;
            activo.NombreActivo = datos.NombreActivo;
            activo.IdTipoActivo = datos.IdTipoActivo;
            activo.IdCategoriaActivo = datos.IdCategoriaActivo;
            activo.Marca = datos.Marca;
            activo.Modelo = datos.Modelo;
            activo.NumeroSerie = datos.NumeroSerie;
            activo.Placa = datos.Placa;
            activo.Descripcion = datos.Descripcion;
            activo.FechaAdquisicion = datos.FechaAdquisicion;
            activo.ValorAdquisicion = datos.ValorAdquisicion;
            activo.UbicacionActual = datos.UbicacionActual;
            activo.Observaciones = datos.Observaciones;

            await contexto.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var existeCodigo = await contexto.Activos
                .AsNoTracking()
                .AnyAsync(a => a.CodigoActivo == datos.CodigoActivo
                    && a.EstadoRegistro == EstadosRegistro.Activo, cancellationToken);

            if (existeCodigo)
            {
                throw new InvalidOperationException(
                    $"Ya existe un activo con el código '{datos.CodigoActivo}'.");
            }

            var activo = new Activo
            {
                CodigoActivo = datos.CodigoActivo,
                NombreActivo = datos.NombreActivo,
                IdTipoActivo = datos.IdTipoActivo,
                IdCategoriaActivo = datos.IdCategoriaActivo,
                Marca = datos.Marca,
                Modelo = datos.Modelo,
                NumeroSerie = datos.NumeroSerie,
                Placa = datos.Placa,
                Descripcion = datos.Descripcion,
                FechaAdquisicion = datos.FechaAdquisicion,
                ValorAdquisicion = datos.ValorAdquisicion,
                UbicacionActual = datos.UbicacionActual,
                IdEstadoActivo = datos.IdEstadoActivo ?? 1,
                Observaciones = datos.Observaciones,
                FechaCreacion = DateTime.Now,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.Activos.Add(activo);
            await contexto.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record DatosActivoLimpios(
        long IdActivo,
        string CodigoActivo,
        string NombreActivo,
        int IdTipoActivo,
        int IdCategoriaActivo,
        string? Marca,
        string? Modelo,
        string? NumeroSerie,
        string? Placa,
        string? Descripcion,
        DateTime? FechaAdquisicion,
        decimal? ValorAdquisicion,
        string? UbicacionActual,
        int? IdEstadoActivo,
        string? Observaciones)
    {
        public static DatosActivoLimpios DesdeSolicitud(SolicitudActivo solicitud)
        {
            ArgumentNullException.ThrowIfNull(solicitud);

            if (string.IsNullOrWhiteSpace(solicitud.CodigoActivo))
            {
                throw new ArgumentException("El código del activo es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(solicitud.NombreActivo))
            {
                throw new ArgumentException("El nombre del activo es obligatorio.");
            }

            if (!solicitud.IdTipoActivo.HasValue || solicitud.IdTipoActivo.Value <= 0)
            {
                throw new ArgumentException("Seleccione un tipo de activo.");
            }

            if (!solicitud.IdCategoriaActivo.HasValue || solicitud.IdCategoriaActivo.Value <= 0)
            {
                throw new ArgumentException("Seleccione una categoría.");
            }

            return new DatosActivoLimpios(
                solicitud.IdActivo,
                solicitud.CodigoActivo.Trim(),
                solicitud.NombreActivo.Trim(),
                solicitud.IdTipoActivo.Value,
                solicitud.IdCategoriaActivo.Value,
                string.IsNullOrWhiteSpace(solicitud.Marca) ? null : solicitud.Marca.Trim(),
                string.IsNullOrWhiteSpace(solicitud.Modelo) ? null : solicitud.Modelo.Trim(),
                string.IsNullOrWhiteSpace(solicitud.NumeroSerie) ? null : solicitud.NumeroSerie.Trim(),
                string.IsNullOrWhiteSpace(solicitud.Placa) ? null : solicitud.Placa.Trim(),
                string.IsNullOrWhiteSpace(solicitud.Descripcion) ? null : solicitud.Descripcion.Trim(),
                solicitud.FechaAdquisicion,
                solicitud.ValorAdquisicion,
                string.IsNullOrWhiteSpace(solicitud.UbicacionActual) ? null : solicitud.UbicacionActual.Trim(),
                solicitud.IdEstadoActivo,
                string.IsNullOrWhiteSpace(solicitud.Observaciones) ? null : solicitud.Observaciones.Trim());
        }
    }
}

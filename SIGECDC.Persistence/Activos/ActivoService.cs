using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
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
                IdTipoActivo = activo.IdTipoActivo,
                TipoActivo = activo.TipoActivo == null ? string.Empty : activo.TipoActivo.Nombre,
                IdCategoriaActivo = activo.IdCategoriaActivo,
                CategoriaActivo = activo.CategoriaActivo == null ? string.Empty : activo.CategoriaActivo.Nombre,
                Marca = activo.Marca,
                Modelo = activo.Modelo,
                NumeroSerie = activo.NumeroSerie,
                Placa = activo.Placa,
                Descripcion = activo.Descripcion,
                FechaAdquisicion = activo.FechaAdquisicion,
                ValorAdquisicion = activo.ValorAdquisicion,
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
                IdTipoActivo = activo.IdTipoActivo,
                TipoActivo = activo.TipoActivo == null ? string.Empty : activo.TipoActivo.Nombre,
                IdCategoriaActivo = activo.IdCategoriaActivo,
                CategoriaActivo = activo.CategoriaActivo == null ? string.Empty : activo.CategoriaActivo.Nombre,
                Marca = activo.Marca,
                Modelo = activo.Modelo,
                NumeroSerie = activo.NumeroSerie,
                Placa = activo.Placa,
                Descripcion = activo.Descripcion,
                FechaAdquisicion = activo.FechaAdquisicion,
                ValorAdquisicion = activo.ValorAdquisicion,
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

    public async Task<IReadOnlyList<EstadoActivoOpcion>> ObtenerEstadosActivoAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado => estado.EstadoRegistro == EstadosRegistro.Activo
                && (estado.Nombre == EstadosActivo.Disponible
                    || estado.Nombre == EstadosActivo.Asignado
                    || estado.Nombre == EstadosActivo.EnMantenimiento
                    || estado.Nombre == EstadosActivo.FueraDeServicio
                    || estado.Nombre == EstadosActivo.DadoDeBaja))
            .OrderBy(estado => estado.IdEstadoActivo)
            .Select(estado => new EstadoActivoOpcion
            {
                IdEstadoActivo = estado.IdEstadoActivo,
                Nombre = estado.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<long> RegistrarActivoAsync(
        SolicitudRegistroActivo solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var datos = DatosRegistroActivoLimpios.DesdeSolicitud(solicitud);
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);

        var usuarioExiste = await contexto.Users
            .AsNoTracking()
            .AnyAsync(usuario => usuario.Id == idUsuario, cancellationToken);

        if (!usuarioExiste)
        {
            throw new InvalidOperationException("No se encontró al usuario responsable del registro.");
        }

        var tipoExiste = await contexto.TiposActivo
            .AsNoTracking()
            .AnyAsync(tipo => tipo.IdTipoActivo == datos.IdTipoActivo
                && tipo.EstadoRegistro == EstadosRegistro.Activo, cancellationToken);

        if (!tipoExiste)
        {
            throw new InvalidOperationException("No se encontró el tipo de activo seleccionado.");
        }

        var categoriaExiste = await contexto.CategoriasActivo
            .AsNoTracking()
            .AnyAsync(categoria => categoria.IdCategoriaActivo == datos.IdCategoriaActivo
                && categoria.IdTipoActivo == datos.IdTipoActivo
                && categoria.EstadoRegistro == EstadosRegistro.Activo, cancellationToken);

        if (!categoriaExiste)
        {
            throw new InvalidOperationException("La categoría seleccionada no pertenece al tipo de activo indicado.");
        }

        var existeCodigo = await contexto.Activos
            .AsNoTracking()
            .AnyAsync(activo => activo.CodigoActivo == datos.CodigoActivo, cancellationToken);

        if (existeCodigo)
        {
            throw new InvalidOperationException(
                $"Ya existe un activo con el código '{datos.CodigoActivo}'.");
        }

        var idEstadoDisponible = await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado => estado.Nombre == EstadosActivo.Disponible
                && estado.EstadoRegistro == EstadosRegistro.Activo)
            .Select(estado => (int?)estado.IdEstadoActivo)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el estado oficial Disponible en la base de datos.");

        var fechaRegistro = DateTime.Now;
        var activoNuevo = new Activo
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
            IdEstadoActivo = idEstadoDisponible,
            Observaciones = datos.Observaciones,
            FechaCreacion = fechaRegistro,
            CreadoPor = idUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            contexto.Activos.Add(activoNuevo);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(
                new BitacoraAuditoria
                {
                    IdUsuario = idUsuario,
                    FechaHora = fechaRegistro,
                    Accion = "Creación de activo",
                    Entidad = "Activo",
                    IdRegistro =
                        activoNuevo.IdActivo.ToString(),
                    ValoresNuevos = JsonSerializer.Serialize(new
                    {
                        activoNuevo.CodigoActivo,
                        activoNuevo.NombreActivo,
                        activoNuevo.IdTipoActivo,
                        activoNuevo.IdCategoriaActivo,
                        Estado = EstadosActivo.Disponible,
                        activoNuevo.UbicacionActual
                    }),
                    Observacion =
                        $"Se registró el activo {activoNuevo.CodigoActivo}."
                });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return activoNuevo.IdActivo;
        }
        catch
        {
            await transaccion.RollbackAsync(
                CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task ActualizarEstadoUbicacionAsync(
        SolicitudActualizacionEstadoUbicacion solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.IdActivo <= 0)
        {
            throw new ArgumentException("Seleccione un activo.");
        }

        if (solicitud.IdEstadoActivo <= 0)
        {
            throw new ArgumentException("Seleccione un estado.");
        }

        var ubicacionNueva = LimpiarTextoOpcional(solicitud.UbicacionActual, 150, "La ubicación");
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
                throw new InvalidOperationException("No se encontró al usuario responsable de la actualización.");
            }

            var activo = await contexto.Activos
                .Include(registro => registro.EstadoActivo)
                .FirstOrDefaultAsync(registro => registro.IdActivo == solicitud.IdActivo
                    && registro.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el activo seleccionado.");

            var estadoNuevo = await contexto.EstadosActivo
                .FirstOrDefaultAsync(estado => estado.IdEstadoActivo == solicitud.IdEstadoActivo
                    && estado.EstadoRegistro == EstadosRegistro.Activo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el estado seleccionado.");

            var tieneAsignacionNoFinalizada = await contexto.AsignacionesActivoProyecto
                .AsNoTracking()
                .AnyAsync(asignacion => asignacion.IdActivo == activo.IdActivo
                    && asignacion.EstadoRegistro == EstadosRegistro.Activo
                    && asignacion.FechaInicio <= DateTime.Today
                    && asignacion.FechaFin >= DateTime.Today, cancellationToken);

            var tieneMantenimientoEnProceso = await contexto.Mantenimientos
                .AsNoTracking()
                .AnyAsync(mantenimiento => mantenimiento.IdActivo == activo.IdActivo
                    && mantenimiento.EstadoRegistro == EstadosRegistro.Activo
                    && mantenimiento.EstadoMantenimiento != null
                    && mantenimiento.EstadoMantenimiento.Nombre == EstadosMantenimiento.EnProceso
                    && mantenimiento.EstadoMantenimiento.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken);

            var estadoAnterior = activo.EstadoActivo?.Nombre;
            var ubicacionAnterior = activo.UbicacionActual;
            ReglasEstadoActivo.ValidarTransicion(
                estadoAnterior,
                estadoNuevo.Nombre,
                tieneAsignacionNoFinalizada,
                tieneMantenimientoEnProceso);

            if (string.Equals(estadoAnterior, estadoNuevo.Nombre, StringComparison.OrdinalIgnoreCase)
                && string.Equals(ubicacionAnterior, ubicacionNueva, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("No se detectaron cambios de estado o ubicación.");
            }

            var fechaCambio = DateTime.Now;
            activo.IdEstadoActivo = estadoNuevo.IdEstadoActivo;
            activo.EstadoActivo = estadoNuevo;
            activo.UbicacionActual = ubicacionNueva;
            activo.FechaModificacion = fechaCambio;
            activo.ModificadoPor = idUsuario;

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = fechaCambio,
                Accion = "Actualización de estado y ubicación",
                Entidad = "Activo",
                IdRegistro = activo.IdActivo.ToString(),
                ValoresAnteriores = JsonSerializer.Serialize(new
                {
                    Estado = estadoAnterior,
                    Ubicacion = ubicacionAnterior
                }),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    Estado = estadoNuevo.Nombre,
                    Ubicacion = ubicacionNueva
                }),
                Observacion = ConstruirDescripcionCambio(
                    estadoAnterior,
                    estadoNuevo.Nombre,
                    ubicacionAnterior,
                    ubicacionNueva)
            });

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

    public async Task<IReadOnlyList<TrazabilidadActivoResumen>> ObtenerTrazabilidadActivoAsync(
        long idActivo,
        CancellationToken cancellationToken = default)
    {
        if (idActivo <= 0)
        {
            return [];
        }

        return await contexto.BitacoraAuditoria
            .AsNoTracking()
            .Where(registro => registro.Entidad == "Activo"
                && registro.IdRegistro == idActivo.ToString()
                && (registro.Accion == "Actualización de estado y ubicación"
                    || registro.Accion == "Actualización de estado por asignación"
                    || registro.Accion == "Actualización de estado por mantenimiento"))
            .OrderByDescending(registro => registro.FechaHora)
            .Take(100)
            .Select(registro => new TrazabilidadActivoResumen
            {
                FechaHora = registro.FechaHora,
                IdUsuario = registro.IdUsuario,
                DescripcionCambio = registro.Observacion ?? "Cambio de estado o ubicación registrado."
            })
            .ToListAsync(cancellationToken);
    }

    private static string LimpiarIdUsuarioObligatorio(string idUsuario)
    {
        return string.IsNullOrWhiteSpace(idUsuario)
            ? throw new ArgumentException("No se pudo identificar al usuario que registra el activo.")
            : idUsuario.Trim();
    }

    private static string? LimpiarTextoOpcional(string? valor, int longitudMaxima, string nombreCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = valor.Trim();
        if (limpio.Length > longitudMaxima)
        {
            throw new ArgumentException($"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
        }

        return limpio;
    }

    private static string ConstruirDescripcionCambio(
        string? estadoAnterior,
        string estadoNuevo,
        string? ubicacionAnterior,
        string? ubicacionNueva)
    {
        var anterior = string.IsNullOrWhiteSpace(estadoAnterior) ? "Sin estado" : estadoAnterior;
        var ubicacionPrev = string.IsNullOrWhiteSpace(ubicacionAnterior) ? "Sin ubicación" : ubicacionAnterior;
        var ubicacionActual = string.IsNullOrWhiteSpace(ubicacionNueva) ? "Sin ubicación" : ubicacionNueva;
        return $"Estado: {anterior} → {estadoNuevo}. Ubicación: {ubicacionPrev} → {ubicacionActual}.";
    }

    private sealed record DatosRegistroActivoLimpios(
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
        string? Observaciones)
    {
        public static DatosRegistroActivoLimpios DesdeSolicitud(SolicitudRegistroActivo solicitud)
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

            var codigo = LimpiarObligatorio(solicitud.CodigoActivo, 30, "El código del activo");
            var nombre = LimpiarObligatorio(solicitud.NombreActivo, 150, "El nombre del activo");

            return new DatosRegistroActivoLimpios(
                codigo,
                nombre,
                solicitud.IdTipoActivo.Value,
                solicitud.IdCategoriaActivo.Value,
                LimpiarOpcional(solicitud.Marca, 100, "La marca"),
                LimpiarOpcional(solicitud.Modelo, 100, "El modelo"),
                LimpiarOpcional(solicitud.NumeroSerie, 100, "El número de serie"),
                LimpiarOpcional(solicitud.Placa, 30, "La placa"),
                LimpiarOpcional(solicitud.Descripcion, 500, "La descripción"),
                solicitud.FechaAdquisicion,
                solicitud.ValorAdquisicion,
                LimpiarOpcional(solicitud.UbicacionActual, 150, "La ubicación"),
                LimpiarOpcional(solicitud.Observaciones, 500, "Las observaciones"));
        }

        private static string LimpiarObligatorio(string valor, int longitudMaxima, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException($"{nombreCampo} es obligatorio.");
            }

            var limpio = valor.Trim();
            if (limpio.Length > longitudMaxima)
            {
                throw new ArgumentException($"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
            }

            return limpio;
        }

        private static string? LimpiarOpcional(string? valor, int longitudMaxima, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var limpio = valor.Trim();
            if (limpio.Length > longitudMaxima)
            {
                throw new ArgumentException($"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
            }

            return limpio;
        }
    }
}

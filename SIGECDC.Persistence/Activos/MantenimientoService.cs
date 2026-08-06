using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Archivos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Activos;

public sealed class MantenimientoService(
    ApplicationDbContext contexto,
    IAlmacenamientoArchivosService almacenamientoArchivos)
    : IMantenimientoService
{
    private const string EntidadMantenimiento = "Mantenimiento";
    private const string TipoDocumentoEvidencia = "Evidencia mantenimiento";
    private const long TamanoMaximoEvidencia = 10 * 1024 * 1024;
    private const int CantidadMaximaEvidencias = 10;
    private const decimal ValorMaximoDecimal18_2 = 9999999999999999.99m;

    private static readonly IReadOnlyDictionary<string, string> MimeTypesPermitidos =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    public async Task<IReadOnlyList<MantenimientoResumen>> ObtenerMantenimientosAsync(
        CancellationToken cancellationToken = default)
    {
        var mantenimientos = await ProyectarMantenimientos(
                contexto.Mantenimientos
                    .AsNoTracking()
                    .Where(mantenimiento =>
                        mantenimiento.EstadoRegistro == EstadosRegistro.Activo))
            .OrderByDescending(mantenimiento => mantenimiento.FechaProgramada)
            .ThenByDescending(mantenimiento => mantenimiento.IdMantenimiento)
            .ToListAsync(cancellationToken);

        await CargarEvidenciasAsync(mantenimientos, cancellationToken);
        return mantenimientos;
    }

    public async Task<IReadOnlyList<MantenimientoResumen>>
        ObtenerMantenimientosPreventivosPendientesAsync(
            CancellationToken cancellationToken = default)
    {
        return await ProyectarMantenimientos(
                contexto.Mantenimientos
                    .AsNoTracking()
                    .Where(mantenimiento =>
                        mantenimiento.EstadoRegistro == EstadosRegistro.Activo
                        && mantenimiento.TipoMantenimiento == TiposMantenimiento.Preventivo
                        && mantenimiento.EstadoMantenimiento != null
                        && mantenimiento.EstadoMantenimiento.Nombre == EstadosMantenimiento.Programado
                        && mantenimiento.EstadoMantenimiento.EstadoRegistro == EstadosRegistro.Activo))
            .OrderBy(mantenimiento => mantenimiento.FechaProgramada)
            .ThenBy(mantenimiento => mantenimiento.IdMantenimiento)
            .ToListAsync(cancellationToken);
    }

    public async Task<MantenimientoResumen?> ObtenerMantenimientoPorIdAsync(
        long idMantenimiento,
        CancellationToken cancellationToken = default)
    {
        if (idMantenimiento <= 0)
        {
            return null;
        }

        var mantenimiento = await ProyectarMantenimientos(
                contexto.Mantenimientos
                    .AsNoTracking()
                    .Where(registro =>
                        registro.IdMantenimiento == idMantenimiento
                        && registro.EstadoRegistro == EstadosRegistro.Activo))
            .FirstOrDefaultAsync(cancellationToken);

        if (mantenimiento is not null)
        {
            await CargarEvidenciasAsync([mantenimiento], cancellationToken);
        }

        return mantenimiento;
    }

    public async Task<IReadOnlyList<EstadoMantenimientoOpcion>>
        ObtenerEstadosMantenimientoAsync(
            CancellationToken cancellationToken = default)
    {
        return await contexto.EstadosMantenimiento
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo
                && (estado.Nombre == EstadosMantenimiento.Programado
                    || estado.Nombre == EstadosMantenimiento.EnProceso
                    || estado.Nombre == EstadosMantenimiento.Finalizado
                    || estado.Nombre == EstadosMantenimiento.Cancelado))
            .OrderBy(estado => estado.IdEstadoMantenimiento)
            .Select(estado => new EstadoMantenimientoOpcion
            {
                IdEstadoMantenimiento = estado.IdEstadoMantenimiento,
                Nombre = estado.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<long> DefinirMantenimientoPreventivoAsync(
        SolicitudDefinirMantenimientoPreventivo solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.IdActivo <= 0)
        {
            throw new ArgumentException("Seleccione un activo.");
        }

        ReglasMantenimientoPreventivo.ValidarFechaProgramada(
            solicitud.FechaProgramada,
            DateTime.Today);

        var descripcion = LimpiarTextoOpcional(
            solicitud.Descripcion,
            500,
            "La descripción");
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);

        await ValidarUsuarioAsync(
            idUsuario,
            "No se encontró al usuario responsable de la definición.",
            cancellationToken);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var activo = await ObtenerActivoParaMantenimientoAsync(
                solicitud.IdActivo,
                cancellationToken);

            if (string.Equals(
                    activo.EstadoActivo?.Nombre,
                    EstadosActivo.DadoDeBaja,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "No se puede definir mantenimiento preventivo para un activo dado de baja.");
            }

            var idEstadoProgramado = await ObtenerIdEstadoMantenimientoAsync(
                EstadosMantenimiento.Programado,
                cancellationToken);
            var fechaRegistro = DateTime.Now;

            var mantenimiento = new Mantenimiento
            {
                IdActivo = activo.IdActivo,
                TipoMantenimiento = TiposMantenimiento.Preventivo,
                IdEstadoMantenimiento = idEstadoProgramado,
                FechaProgramada = solicitud.FechaProgramada.Date,
                Descripcion = descripcion,
                FechaCreacion = fechaRegistro,
                CreadoPor = idUsuario,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.Mantenimientos.Add(mantenimiento);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = fechaRegistro,
                Accion = "Definición de mantenimiento preventivo",
                Entidad = EntidadMantenimiento,
                IdRegistro = mantenimiento.IdMantenimiento.ToString(),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    mantenimiento.IdActivo,
                    mantenimiento.TipoMantenimiento,
                    Estado = EstadosMantenimiento.Programado,
                    mantenimiento.FechaProgramada,
                    mantenimiento.Descripcion
                }),
                Observacion =
                    $"Se definió mantenimiento preventivo para el activo {activo.CodigoActivo}."
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return mantenimiento.IdMantenimiento;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<long> CrearOrdenCorrectivaAsync(
        SolicitudCrearOrdenMantenimiento solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.IdActivo <= 0)
        {
            throw new ArgumentException("Seleccione un activo.");
        }

        if (solicitud.IdProyecto.HasValue && solicitud.IdProyecto.Value <= 0)
        {
            throw new ArgumentException("El proyecto seleccionado no es válido.");
        }

        if (solicitud.FechaProgramada == default)
        {
            throw new ArgumentException("La fecha programada es obligatoria.");
        }

        var descripcion = LimpiarTextoOpcional(
            solicitud.Descripcion,
            500,
            "La descripción");
        var responsable = LimpiarTextoOpcional(
            solicitud.Responsable,
            150,
            "El responsable");

        ValidarDecimalOpcional(
            solicitud.CostoEstimado,
            "El costo estimado");

        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        await ValidarUsuarioAsync(
            idUsuario,
            "No se encontró al usuario responsable del registro.",
            cancellationToken);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var activo = await ObtenerActivoParaMantenimientoAsync(
                solicitud.IdActivo,
                cancellationToken);

            if (string.Equals(
                    activo.EstadoActivo?.Nombre,
                    EstadosActivo.DadoDeBaja,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "No se puede crear una orden de mantenimiento para un activo dado de baja.");
            }

            if (solicitud.IdProyecto.HasValue)
            {
                var proyectoExiste = await contexto.Proyectos
                    .AsNoTracking()
                    .AnyAsync(
                        proyecto =>
                            proyecto.IdProyecto == solicitud.IdProyecto.Value
                            && proyecto.EstadoRegistro == EstadosRegistro.Activo,
                        cancellationToken);

                if (!proyectoExiste)
                {
                    throw new InvalidOperationException(
                        "No se encontró el proyecto seleccionado.");
                }
            }

            var idEstadoProgramado = await ObtenerIdEstadoMantenimientoAsync(
                EstadosMantenimiento.Programado,
                cancellationToken);
            var fechaRegistro = DateTime.Now;

            var mantenimiento = new Mantenimiento
            {
                IdActivo = activo.IdActivo,
                IdProyecto = solicitud.IdProyecto,
                TipoMantenimiento = TiposMantenimiento.Correctivo,
                IdEstadoMantenimiento = idEstadoProgramado,
                FechaProgramada = solicitud.FechaProgramada.Date,
                Descripcion = descripcion,
                CostoEstimado = solicitud.CostoEstimado,
                Responsable = responsable,
                FechaCreacion = fechaRegistro,
                CreadoPor = idUsuario,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.Mantenimientos.Add(mantenimiento);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = fechaRegistro,
                Accion = "Creación de orden de mantenimiento",
                Entidad = EntidadMantenimiento,
                IdRegistro = mantenimiento.IdMantenimiento.ToString(),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    mantenimiento.IdActivo,
                    mantenimiento.IdProyecto,
                    mantenimiento.TipoMantenimiento,
                    Estado = EstadosMantenimiento.Programado,
                    mantenimiento.FechaProgramada,
                    mantenimiento.Descripcion,
                    mantenimiento.CostoEstimado,
                    mantenimiento.Responsable
                }),
                Observacion =
                    $"Se creó una orden correctiva programada para el activo {activo.CodigoActivo}."
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return mantenimiento.IdMantenimiento;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task IniciarOrdenAsync(
        long idMantenimiento,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        if (idMantenimiento <= 0)
        {
            throw new ArgumentException("Seleccione una orden de mantenimiento.");
        }

        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        await ValidarUsuarioAsync(
            idUsuario,
            "No se encontró al usuario responsable del inicio.",
            cancellationToken);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var mantenimiento = await contexto.Mantenimientos
                .Include(registro => registro.EstadoMantenimiento)
                .Include(registro => registro.Activo)
                    .ThenInclude(activo => activo!.EstadoActivo)
                .FirstOrDefaultAsync(
                    registro =>
                        registro.IdMantenimiento == idMantenimiento
                        && registro.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "No se encontró la orden de mantenimiento seleccionada.");

            if (mantenimiento.Activo is null
                || mantenimiento.Activo.EstadoRegistro != EstadosRegistro.Activo)
            {
                throw new InvalidOperationException(
                    "No se encontró el activo asociado a la orden.");
            }

            ValidarEstadoMantenimientoActivo(
                mantenimiento.EstadoMantenimiento);
            ReglasOrdenMantenimiento.ValidarInicio(
                mantenimiento.EstadoMantenimiento!.Nombre);

            var activo = mantenimiento.Activo;
            var estadoMantenimientoAnterior =
                mantenimiento.EstadoMantenimiento?.Nombre;
            var estadoActivoAnterior = activo.EstadoActivo?.Nombre;
            var fechaInicioAnterior = mantenimiento.FechaInicio;
            var fechaFinAnterior = mantenimiento.FechaFin;
            var estadoActivoNuevo =
                ReglasOrdenMantenimiento.DeterminarEstadoActivoAlIniciar(
                    estadoActivoAnterior);

            var estadoEnProceso = await ObtenerEstadoMantenimientoAsync(
                EstadosMantenimiento.EnProceso,
                cancellationToken);
            var fechaInicio = DateTime.Now;

            mantenimiento.IdEstadoMantenimiento =
                estadoEnProceso.IdEstadoMantenimiento;
            mantenimiento.EstadoMantenimiento = estadoEnProceso;
            mantenimiento.FechaInicio = fechaInicio;
            mantenimiento.FechaFin = null;
            mantenimiento.FechaModificacion = fechaInicio;
            mantenimiento.ModificadoPor = idUsuario;

            var cambioEstadoActivo = !string.Equals(
                estadoActivoAnterior,
                estadoActivoNuevo,
                StringComparison.OrdinalIgnoreCase);

            if (cambioEstadoActivo)
            {
                var estadoActivoDestino = await ObtenerEstadoActivoAsync(
                    estadoActivoNuevo,
                    cancellationToken);
                activo.IdEstadoActivo =
                    estadoActivoDestino.IdEstadoActivo;
                activo.EstadoActivo = estadoActivoDestino;
                activo.FechaModificacion = fechaInicio;
                activo.ModificadoPor = idUsuario;
            }

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = fechaInicio,
                Accion = "Inicio de orden de mantenimiento",
                Entidad = EntidadMantenimiento,
                IdRegistro = mantenimiento.IdMantenimiento.ToString(),
                ValoresAnteriores = JsonSerializer.Serialize(new
                {
                    Estado = estadoMantenimientoAnterior,
                    FechaInicio = fechaInicioAnterior,
                    FechaFin = fechaFinAnterior
                }),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    Estado = EstadosMantenimiento.EnProceso,
                    FechaInicio = fechaInicio
                }),
                Observacion =
                    $"Se inició la orden de mantenimiento del activo {activo.CodigoActivo}."
            });

            if (cambioEstadoActivo)
            {
                contexto.BitacoraAuditoria.Add(CrearAuditoriaEstadoActivo(
                    activo,
                    idUsuario,
                    fechaInicio,
                    estadoActivoAnterior,
                    estadoActivoNuevo,
                    "Inicio de mantenimiento"));
            }

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

    public async Task FinalizarOrdenAsync(
        SolicitudCerrarOrdenMantenimiento solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.IdMantenimiento <= 0)
        {
            throw new ArgumentException("Seleccione una orden de mantenimiento.");
        }

        if (!solicitud.CostoReal.HasValue)
        {
            throw new ArgumentException("El costo real es obligatorio.");
        }

        if (!solicitud.TiempoFueraServicioHoras.HasValue)
        {
            throw new ArgumentException(
                "El tiempo fuera de servicio es obligatorio.");
        }

        ValidarDecimalObligatorio(solicitud.CostoReal.Value, "El costo real");
        ValidarDecimalObligatorio(
            solicitud.TiempoFueraServicioHoras.Value,
            "El tiempo fuera de servicio");

        var resultado = LimpiarTextoOpcional(
            solicitud.Resultado,
            500,
            "El resultado");

        if (solicitud.Evidencias is null || solicitud.Evidencias.Count == 0)
        {
            throw new ArgumentException(
                "Debe adjuntar al menos una evidencia para finalizar la orden.");
        }

        if (solicitud.Evidencias.Count > CantidadMaximaEvidencias)
        {
            throw new ArgumentException(
                $"Puede adjuntar como máximo {CantidadMaximaEvidencias} evidencias por cierre.");
        }

        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        await ValidarUsuarioAsync(
            idUsuario,
            "No se encontró al usuario responsable del cierre.",
            cancellationToken);

        var ordenPrevia = await contexto.Mantenimientos
            .AsNoTracking()
            .Where(registro =>
                registro.IdMantenimiento == solicitud.IdMantenimiento
                && registro.EstadoRegistro == EstadosRegistro.Activo)
            .Select(registro => new
            {
                Estado = registro.EstadoMantenimiento == null
                    ? null
                    : registro.EstadoMantenimiento.Nombre,
                EstadoCatalogo = registro.EstadoMantenimiento == null
                    ? null
                    : registro.EstadoMantenimiento.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró la orden de mantenimiento seleccionada.");

        ValidarEstadoMantenimientoActivo(
            ordenPrevia.Estado,
            ordenPrevia.EstadoCatalogo);
        ReglasOrdenMantenimiento.ValidarCierre(ordenPrevia.Estado);

        var idTipoDocumento = await ObtenerIdTipoDocumentoEvidenciaAsync(
            cancellationToken);
        List<EvidenciaPreparada> evidenciasPreparadas = [];
        List<EvidenciaGuardada> evidenciasGuardadas = [];

        try
        {
            evidenciasPreparadas = await PrepararEvidenciasAsync(
                solicitud.Evidencias,
                cancellationToken);
            evidenciasGuardadas = await GuardarEvidenciasAsync(
                solicitud.IdMantenimiento,
                evidenciasPreparadas,
                cancellationToken);

            await using var transaccion =
                await contexto.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                var mantenimiento = await contexto.Mantenimientos
                    .Include(registro => registro.EstadoMantenimiento)
                    .Include(registro => registro.Activo)
                        .ThenInclude(activo => activo!.EstadoActivo)
                    .FirstOrDefaultAsync(
                        registro =>
                            registro.IdMantenimiento == solicitud.IdMantenimiento
                            && registro.EstadoRegistro == EstadosRegistro.Activo,
                        cancellationToken)
                    ?? throw new InvalidOperationException(
                        "No se encontró la orden de mantenimiento seleccionada.");

                if (mantenimiento.Activo is null
                    || mantenimiento.Activo.EstadoRegistro != EstadosRegistro.Activo)
                {
                    throw new InvalidOperationException(
                        "No se encontró el activo asociado a la orden.");
                }

                ValidarEstadoMantenimientoActivo(
                    mantenimiento.EstadoMantenimiento);
                ReglasOrdenMantenimiento.ValidarCierre(
                    mantenimiento.EstadoMantenimiento!.Nombre);

                var fechaFinalizacion = DateTime.Now;
                if (!mantenimiento.FechaInicio.HasValue)
                {
                    throw new InvalidOperationException(
                        "La orden no posee una fecha de inicio válida.");
                }

                if (mantenimiento.FechaInicio.Value > fechaFinalizacion)
                {
                    throw new InvalidOperationException(
                        "La fecha de inicio de la orden es posterior al momento de cierre.");
                }

                var activo = mantenimiento.Activo;
                var estadoMantenimientoAnterior =
                    mantenimiento.EstadoMantenimiento?.Nombre;
                var estadoActivoAnterior = activo.EstadoActivo?.Nombre;
                var valoresAnteriores = new
                {
                    Estado = estadoMantenimientoAnterior,
                    mantenimiento.FechaInicio,
                    mantenimiento.FechaFin,
                    mantenimiento.CostoReal,
                    mantenimiento.TiempoFueraServicioHoras,
                    mantenimiento.Resultado
                };

                var existeOtroMantenimientoEnProceso =
                    await contexto.Mantenimientos
                        .AsNoTracking()
                        .AnyAsync(
                            registro =>
                                registro.IdActivo == activo.IdActivo
                                && registro.IdMantenimiento
                                    != mantenimiento.IdMantenimiento
                                && registro.EstadoRegistro
                                    == EstadosRegistro.Activo
                                && registro.EstadoMantenimiento != null
                                && registro.EstadoMantenimiento.Nombre
                                    == EstadosMantenimiento.EnProceso
                                && registro.EstadoMantenimiento.EstadoRegistro
                                    == EstadosRegistro.Activo,
                            cancellationToken);

                var tieneAsignacionNoFinalizada =
                    await contexto.AsignacionesActivoProyecto
                        .AsNoTracking()
                        .AnyAsync(
                            asignacion =>
                                asignacion.IdActivo == activo.IdActivo
                                && asignacion.EstadoRegistro
                                    == EstadosRegistro.Activo
                                && asignacion.FechaInicio <= DateTime.Today
                                && asignacion.FechaFin >= DateTime.Today,
                            cancellationToken);

                var estadoActivoNuevo =
                    ReglasOrdenMantenimiento.DeterminarEstadoActivoAlFinalizar(
                        estadoActivoAnterior,
                        existeOtroMantenimientoEnProceso,
                        tieneAsignacionNoFinalizada);
                var estadoFinalizado =
                    await ObtenerEstadoMantenimientoAsync(
                        EstadosMantenimiento.Finalizado,
                        cancellationToken);

                mantenimiento.IdEstadoMantenimiento =
                    estadoFinalizado.IdEstadoMantenimiento;
                mantenimiento.EstadoMantenimiento = estadoFinalizado;
                mantenimiento.FechaFin = fechaFinalizacion;
                mantenimiento.CostoReal = solicitud.CostoReal.Value;
                mantenimiento.TiempoFueraServicioHoras =
                    solicitud.TiempoFueraServicioHoras.Value;
                mantenimiento.Resultado = resultado;
                mantenimiento.FechaModificacion = fechaFinalizacion;
                mantenimiento.ModificadoPor = idUsuario;

                var cambioEstadoActivo = !string.Equals(
                    estadoActivoAnterior,
                    estadoActivoNuevo,
                    StringComparison.OrdinalIgnoreCase);

                if (cambioEstadoActivo)
                {
                    var estadoActivoDestino =
                        await ObtenerEstadoActivoAsync(
                        estadoActivoNuevo,
                        cancellationToken);
                    activo.IdEstadoActivo =
                        estadoActivoDestino.IdEstadoActivo;
                    activo.EstadoActivo = estadoActivoDestino;
                    activo.FechaModificacion = fechaFinalizacion;
                    activo.ModificadoPor = idUsuario;
                }

                foreach (var evidencia in evidenciasGuardadas)
                {
                    contexto.DocumentosArchivo.Add(new DocumentoArchivo
                    {
                        EntidadRelacionada = EntidadMantenimiento,
                        IdEntidadRelacionada = mantenimiento.IdMantenimiento,
                        IdTipoDocumento = idTipoDocumento,
                        NombreOriginal = evidencia.NombreOriginal,
                        NombreAlmacenado =
                            evidencia.ArchivoGuardado.NombreAlmacenado,
                        RutaRelativa =
                            evidencia.ArchivoGuardado.RutaRelativa,
                        MimeType = evidencia.MimeType,
                        TamanoBytes =
                            evidencia.ArchivoGuardado.TamanoBytes,
                        FechaCarga = fechaFinalizacion,
                        CargadoPor = idUsuario,
                        EstadoRegistro = EstadosRegistro.Activo
                    });
                }

                contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
                {
                    IdUsuario = idUsuario,
                    FechaHora = fechaFinalizacion,
                    Accion = "Finalización de orden de mantenimiento",
                    Entidad = EntidadMantenimiento,
                    IdRegistro = mantenimiento.IdMantenimiento.ToString(),
                    ValoresAnteriores =
                        JsonSerializer.Serialize(valoresAnteriores),
                    ValoresNuevos = JsonSerializer.Serialize(new
                    {
                        Estado = EstadosMantenimiento.Finalizado,
                        FechaFin = fechaFinalizacion,
                        mantenimiento.CostoReal,
                        mantenimiento.TiempoFueraServicioHoras,
                        mantenimiento.Resultado,
                        Evidencias = evidenciasGuardadas
                            .Select(evidencia => evidencia.NombreOriginal)
                            .ToArray()
                    }),
                    Observacion =
                        $"Se finalizó la orden de mantenimiento del activo {activo.CodigoActivo} con {evidenciasGuardadas.Count} evidencia(s)."
                });

                if (cambioEstadoActivo)
                {
                    contexto.BitacoraAuditoria.Add(CrearAuditoriaEstadoActivo(
                        activo,
                        idUsuario,
                        fechaFinalizacion,
                        estadoActivoAnterior,
                        estadoActivoNuevo,
                        "Finalización de mantenimiento"));
                }

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
        catch
        {
            await EliminarArchivosCompensatoriosAsync(evidenciasGuardadas);
            throw;
        }
        finally
        {
            foreach (var evidencia in evidenciasPreparadas)
            {
                evidencia.Dispose();
            }
        }
    }

    public async Task<IReadOnlyList<MantenimientoResumen>>
        ObtenerHistorialActivoAsync(
            long idActivo,
            CancellationToken cancellationToken = default)
    {
        if (idActivo <= 0)
        {
            return [];
        }

        var activoExiste = await contexto.Activos
            .AsNoTracking()
            .AnyAsync(
                activo =>
                    activo.IdActivo == idActivo
                    && activo.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken);

        if (!activoExiste)
        {
            return [];
        }

        var mantenimientos = await ProyectarMantenimientos(
                contexto.Mantenimientos
                    .AsNoTracking()
                    .Where(registro =>
                        registro.IdActivo == idActivo
                        && registro.EstadoRegistro == EstadosRegistro.Activo))
            .OrderByDescending(registro => registro.FechaProgramada)
            .ThenByDescending(registro => registro.IdMantenimiento)
            .ToListAsync(cancellationToken);

        await CargarEvidenciasAsync(mantenimientos, cancellationToken);
        return mantenimientos;
    }

    public async Task<ArchivoDescarga?> AbrirEvidenciaAsync(
        long idDocumentoArchivo,
        CancellationToken cancellationToken = default)
    {
        if (idDocumentoArchivo <= 0)
        {
            return null;
        }

        var evidencia = await contexto.DocumentosArchivo
            .AsNoTracking()
            .Where(documento =>
                documento.IdDocumentoArchivo == idDocumentoArchivo
                && documento.EntidadRelacionada == EntidadMantenimiento
                && documento.EstadoRegistro == EstadosRegistro.Activo
                && documento.TipoDocumento != null
                && documento.TipoDocumento.Nombre == TipoDocumentoEvidencia
                && documento.TipoDocumento.EstadoRegistro
                    == EstadosRegistro.Activo
                && contexto.Mantenimientos.Any(mantenimiento =>
                    mantenimiento.IdMantenimiento
                        == documento.IdEntidadRelacionada
                    && mantenimiento.EstadoRegistro
                        == EstadosRegistro.Activo))
            .Select(documento => new
            {
                documento.NombreOriginal,
                documento.RutaRelativa,
                documento.MimeType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (evidencia is null)
        {
            return null;
        }

        try
        {
            var contenido = await almacenamientoArchivos.AbrirLecturaAsync(
                evidencia.RutaRelativa,
                cancellationToken);
            return new ArchivoDescarga(
                contenido,
                evidencia.NombreOriginal,
                evidencia.MimeType);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    private static IQueryable<MantenimientoResumen> ProyectarMantenimientos(
        IQueryable<Mantenimiento> consulta)
    {
        return consulta.Select(mantenimiento => new MantenimientoResumen
        {
            IdMantenimiento = mantenimiento.IdMantenimiento,
            IdActivo = mantenimiento.IdActivo,
            CodigoActivo = mantenimiento.Activo == null
                ? string.Empty
                : mantenimiento.Activo.CodigoActivo,
            Activo = mantenimiento.Activo == null
                ? string.Empty
                : mantenimiento.Activo.NombreActivo,
            IdProyecto = mantenimiento.IdProyecto,
            Proyecto = mantenimiento.Proyecto == null
                ? null
                : mantenimiento.Proyecto.NombreProyecto,
            TipoMantenimiento = mantenimiento.TipoMantenimiento,
            IdEstadoMantenimiento =
                mantenimiento.IdEstadoMantenimiento,
            EstadoMantenimiento =
                mantenimiento.EstadoMantenimiento == null
                    ? string.Empty
                    : mantenimiento.EstadoMantenimiento.Nombre,
            FechaProgramada = mantenimiento.FechaProgramada,
            FechaInicio = mantenimiento.FechaInicio,
            FechaFin = mantenimiento.FechaFin,
            Descripcion = mantenimiento.Descripcion,
            CostoEstimado = mantenimiento.CostoEstimado,
            CostoReal = mantenimiento.CostoReal,
            TiempoFueraServicioHoras =
                mantenimiento.TiempoFueraServicioHoras,
            Resultado = mantenimiento.Resultado,
            Responsable = mantenimiento.Responsable
        });
    }

    private async Task CargarEvidenciasAsync(
        IReadOnlyList<MantenimientoResumen> mantenimientos,
        CancellationToken cancellationToken)
    {
        if (mantenimientos.Count == 0)
        {
            return;
        }

        var ids = mantenimientos
            .Select(mantenimiento => mantenimiento.IdMantenimiento)
            .Distinct()
            .ToArray();

        var evidencias = await contexto.DocumentosArchivo
            .AsNoTracking()
            .Where(documento =>
                documento.EntidadRelacionada == EntidadMantenimiento
                && ids.Contains(documento.IdEntidadRelacionada)
                && documento.EstadoRegistro == EstadosRegistro.Activo
                && documento.TipoDocumento != null
                && documento.TipoDocumento.Nombre == TipoDocumentoEvidencia
                && documento.TipoDocumento.EstadoRegistro
                    == EstadosRegistro.Activo)
            .OrderBy(documento => documento.FechaCarga)
            .ThenBy(documento => documento.IdDocumentoArchivo)
            .Select(documento => new EvidenciaMantenimientoResumen
            {
                IdDocumentoArchivo = documento.IdDocumentoArchivo,
                IdMantenimiento = documento.IdEntidadRelacionada,
                NombreOriginal = documento.NombreOriginal,
                MimeType = documento.MimeType,
                TamanoBytes = documento.TamanoBytes,
                FechaCarga = documento.FechaCarga
            })
            .ToListAsync(cancellationToken);

        var evidenciasPorMantenimiento = evidencias
            .GroupBy(evidencia => evidencia.IdMantenimiento)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<EvidenciaMantenimientoResumen>)
                    grupo.ToList());

        foreach (var mantenimiento in mantenimientos)
        {
            mantenimiento.Evidencias =
                evidenciasPorMantenimiento.GetValueOrDefault(
                    mantenimiento.IdMantenimiento,
                    []);
        }
    }

    private async Task<Activo> ObtenerActivoParaMantenimientoAsync(
        long idActivo,
        CancellationToken cancellationToken)
    {
        var activo = await contexto.Activos
            .Include(registro => registro.EstadoActivo)
            .FirstOrDefaultAsync(
                registro =>
                    registro.IdActivo == idActivo
                    && registro.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el activo seleccionado.");

        if (activo.EstadoActivo is null
            || activo.EstadoActivo.EstadoRegistro != EstadosRegistro.Activo
            || !ReglasEstadoActivo.EsEstadoOficial(
                activo.EstadoActivo.Nombre))
        {
            throw new InvalidOperationException(
                "El activo no posee un estado oficial activo.");
        }

        return activo;
    }

    private async Task<int> ObtenerIdEstadoMantenimientoAsync(
        string nombre,
        CancellationToken cancellationToken)
    {
        return await contexto.EstadosMantenimiento
            .AsNoTracking()
            .Where(estado =>
                estado.Nombre == nombre
                && estado.EstadoRegistro == EstadosRegistro.Activo)
            .Select(estado => (int?)estado.IdEstadoMantenimiento)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"No se encontró el estado oficial {nombre} en la base de datos.");
    }

    private static void ValidarEstadoMantenimientoActivo(
        EstadoMantenimiento? estado)
    {
        ValidarEstadoMantenimientoActivo(
            estado?.Nombre,
            estado?.EstadoRegistro);
    }

    private static void ValidarEstadoMantenimientoActivo(
        string? nombre,
        string? estadoRegistro)
    {
        if (string.IsNullOrWhiteSpace(nombre)
            || !string.Equals(
                estadoRegistro,
                EstadosRegistro.Activo,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La orden no posee un estado de mantenimiento oficial activo.");
        }
    }

    private async Task<EstadoMantenimiento> ObtenerEstadoMantenimientoAsync(
        string nombre,
        CancellationToken cancellationToken)
    {
        return await contexto.EstadosMantenimiento
            .FirstOrDefaultAsync(
                estado =>
                    estado.Nombre == nombre
                    && estado.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"No se encontró el estado oficial {nombre} en la base de datos.");
    }

    private async Task<EstadoActivo> ObtenerEstadoActivoAsync(
        string nombre,
        CancellationToken cancellationToken)
    {
        return await contexto.EstadosActivo
            .FirstOrDefaultAsync(
                estado =>
                    estado.Nombre == nombre
                    && estado.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"No se encontró el estado oficial {nombre} en la base de datos.");
    }

    private async Task<int> ObtenerIdTipoDocumentoEvidenciaAsync(
        CancellationToken cancellationToken)
    {
        return await contexto.TiposDocumento
            .AsNoTracking()
            .Where(tipo =>
                tipo.Nombre == TipoDocumentoEvidencia
                && tipo.EstadoRegistro == EstadosRegistro.Activo)
            .Select(tipo => (int?)tipo.IdTipoDocumento)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el tipo oficial Evidencia mantenimiento en la base de datos.");
    }

    private async Task ValidarUsuarioAsync(
        string idUsuario,
        string mensajeError,
        CancellationToken cancellationToken)
    {
        var usuarioExiste = await contexto.Users
            .AsNoTracking()
            .AnyAsync(
                usuario => usuario.Id == idUsuario,
                cancellationToken);

        if (!usuarioExiste)
        {
            throw new InvalidOperationException(mensajeError);
        }
    }

    private static BitacoraAuditoria CrearAuditoriaEstadoActivo(
        Activo activo,
        string idUsuario,
        DateTime fecha,
        string? estadoAnterior,
        string estadoNuevo,
        string motivo)
    {
        return new BitacoraAuditoria
        {
            IdUsuario = idUsuario,
            FechaHora = fecha,
            Accion = "Actualización de estado por mantenimiento",
            Entidad = "Activo",
            IdRegistro = activo.IdActivo.ToString(),
            ValoresAnteriores = JsonSerializer.Serialize(new
            {
                Estado = estadoAnterior,
                activo.UbicacionActual
            }),
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                Estado = estadoNuevo,
                activo.UbicacionActual
            }),
            Observacion =
                $"{motivo}: estado actualizado de {estadoAnterior ?? "Sin estado"} a {estadoNuevo}."
        };
    }

    private static string LimpiarIdUsuarioObligatorio(string idUsuario)
    {
        return string.IsNullOrWhiteSpace(idUsuario)
            ? throw new ArgumentException(
                "No se pudo identificar al usuario responsable.")
            : idUsuario.Trim();
    }

    private static string? LimpiarTextoOpcional(
        string? valor,
        int longitudMaxima,
        string nombreCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = valor.Trim();
        if (limpio.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
        }

        return limpio;
    }

    private static void ValidarDecimalOpcional(
        decimal? valor,
        string nombreCampo)
    {
        if (valor.HasValue)
        {
            ValidarDecimalObligatorio(valor.Value, nombreCampo);
        }
    }

    private static void ValidarDecimalObligatorio(
        decimal valor,
        string nombreCampo)
    {
        if (valor < 0)
        {
            throw new ArgumentException($"{nombreCampo} no puede ser negativo.");
        }

        if (valor > ValorMaximoDecimal18_2)
        {
            throw new ArgumentException(
                $"{nombreCampo} supera el máximo permitido.");
        }

        if (decimal.Round(valor, 2) != valor)
        {
            throw new ArgumentException(
                $"{nombreCampo} no puede tener más de dos decimales.");
        }
    }

    private static async Task<List<EvidenciaPreparada>>
        PrepararEvidenciasAsync(
            IReadOnlyList<SolicitudEvidenciaMantenimiento> evidencias,
            CancellationToken cancellationToken)
    {
        List<EvidenciaPreparada> preparadas = [];

        try
        {
            foreach (var evidencia in evidencias)
            {
                ArgumentNullException.ThrowIfNull(evidencia);

                var nombreOriginal = LimpiarNombreArchivo(
                    evidencia.NombreOriginal);
                var extension = Path.GetExtension(nombreOriginal);

                if (!MimeTypesPermitidos.TryGetValue(
                        extension,
                        out var mimeTypeEsperado))
                {
                    throw new ArgumentException(
                        $"El archivo {nombreOriginal} tiene un formato no permitido.");
                }

                if (!string.IsNullOrWhiteSpace(evidencia.MimeType)
                    && !string.Equals(
                        evidencia.MimeType.Trim(),
                        "application/octet-stream",
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        evidencia.MimeType.Trim(),
                        mimeTypeEsperado,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"El tipo declarado del archivo {nombreOriginal} no coincide con su extensión.");
                }

                if (evidencia.TamanoBytes <= 0
                    || evidencia.TamanoBytes > TamanoMaximoEvidencia)
                {
                    throw new ArgumentException(
                        $"El archivo {nombreOriginal} debe ser mayor a 0 bytes y no superar 10 MB.");
                }

                if (evidencia.Contenido is null
                    || evidencia.Contenido == Stream.Null)
                {
                    throw new ArgumentException(
                        $"El contenido del archivo {nombreOriginal} es obligatorio.");
                }

                var contenido = await CopiarContenidoLimitadoAsync(
                    evidencia.Contenido,
                    nombreOriginal,
                    cancellationToken);

                try
                {
                    if (contenido.Length != evidencia.TamanoBytes)
                    {
                        throw new ArgumentException(
                            $"El tamaño del archivo {nombreOriginal} no coincide con el contenido recibido.");
                    }

                    await ValidarFirmaArchivoAsync(
                        contenido,
                        extension,
                        nombreOriginal,
                        cancellationToken);

                    preparadas.Add(new EvidenciaPreparada(
                        nombreOriginal,
                        mimeTypeEsperado,
                        contenido));
                }
                catch
                {
                    contenido.Dispose();
                    throw;
                }
            }

            return preparadas;
        }
        catch
        {
            foreach (var preparada in preparadas)
            {
                preparada.Dispose();
            }

            throw;
        }
    }

    private async Task<List<EvidenciaGuardada>> GuardarEvidenciasAsync(
        long idMantenimiento,
        IReadOnlyList<EvidenciaPreparada> evidencias,
        CancellationToken cancellationToken)
    {
        List<EvidenciaGuardada> guardadas = [];

        try
        {
            foreach (var evidencia in evidencias)
            {
                evidencia.Contenido.Position = 0;
                var archivo = await almacenamientoArchivos.GuardarAsync(
                    evidencia.Contenido,
                    evidencia.NombreOriginal,
                    evidencia.MimeType,
                    evidencia.Contenido.Length,
                    $"mantenimientos/{idMantenimiento}",
                    cancellationToken);

                guardadas.Add(new EvidenciaGuardada(
                    evidencia.NombreOriginal,
                    evidencia.MimeType,
                    archivo));
            }

            return guardadas;
        }
        catch
        {
            await EliminarArchivosCompensatoriosAsync(guardadas);
            throw;
        }
    }

    private async Task EliminarArchivosCompensatoriosAsync(
        IReadOnlyList<EvidenciaGuardada> evidencias)
    {
        foreach (var evidencia in evidencias)
        {
            try
            {
                await almacenamientoArchivos.EliminarAsync(
                    evidencia.ArchivoGuardado.RutaRelativa,
                    CancellationToken.None);
            }
            catch
            {
                // La limpieza es compensatoria y no debe ocultar el error original.
            }
        }
    }

    private static async Task<MemoryStream> CopiarContenidoLimitadoAsync(
        Stream origen,
        string nombreOriginal,
        CancellationToken cancellationToken)
    {
        var destino = new MemoryStream();
        var buffer = new byte[81920];

        try
        {
            while (true)
            {
                var leidos = await origen.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken);

                if (leidos == 0)
                {
                    break;
                }

                if (destino.Length + leidos > TamanoMaximoEvidencia)
                {
                    throw new ArgumentException(
                        $"El archivo {nombreOriginal} supera el límite de 10 MB.");
                }

                await destino.WriteAsync(
                    buffer.AsMemory(0, leidos),
                    cancellationToken);
            }

            if (destino.Length == 0)
            {
                throw new ArgumentException(
                    $"El archivo {nombreOriginal} no puede estar vacío.");
            }

            destino.Position = 0;
            return destino;
        }
        catch
        {
            destino.Dispose();
            throw;
        }
    }

    private static async Task ValidarFirmaArchivoAsync(
        MemoryStream contenido,
        string extension,
        string nombreOriginal,
        CancellationToken cancellationToken)
    {
        var encabezado = new byte[8];
        contenido.Position = 0;
        var leidos = await contenido.ReadAsync(
            encabezado.AsMemory(0, encabezado.Length),
            cancellationToken);
        contenido.Position = 0;

        var firmaValida = extension.ToLowerInvariant() switch
        {
            ".pdf" => leidos >= 5
                && encabezado[0] == (byte)'%'
                && encabezado[1] == (byte)'P'
                && encabezado[2] == (byte)'D'
                && encabezado[3] == (byte)'F'
                && encabezado[4] == (byte)'-',
            ".jpg" or ".jpeg" => leidos >= 3
                && encabezado[0] == 0xFF
                && encabezado[1] == 0xD8
                && encabezado[2] == 0xFF,
            ".png" => leidos >= 8
                && encabezado.AsSpan(0, 8).SequenceEqual(
                    new byte[]
                    {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                    }),
            ".doc" => leidos >= 8
                && encabezado.AsSpan(0, 8).SequenceEqual(
                    new byte[]
                    {
                        0xD0, 0xCF, 0x11, 0xE0,
                        0xA1, 0xB1, 0x1A, 0xE1
                    }),
            ".docx" => leidos >= 4
                && encabezado[0] == 0x50
                && encabezado[1] == 0x4B
                && encabezado[2] == 0x03
                && encabezado[3] == 0x04,
            _ => false
        };

        if (!firmaValida)
        {
            throw new ArgumentException(
                $"El contenido del archivo {nombreOriginal} no coincide con su formato.");
        }
    }

    private static string LimpiarNombreArchivo(string nombreOriginal)
    {
        if (string.IsNullOrWhiteSpace(nombreOriginal))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.");
        }

        string nombreSeguro;
        try
        {
            nombreSeguro = Path.GetFileName(nombreOriginal.Trim());
        }
        catch (ArgumentException)
        {
            throw new ArgumentException(
                "El nombre del archivo no es válido.");
        }

        if (string.IsNullOrWhiteSpace(nombreSeguro))
        {
            throw new ArgumentException(
                "El nombre del archivo no es válido.");
        }

        if (nombreSeguro.Length > 255)
        {
            throw new ArgumentException(
                "El nombre del archivo no puede superar 255 caracteres.");
        }

        return nombreSeguro;
    }

    private sealed record EvidenciaGuardada(
        string NombreOriginal,
        string MimeType,
        ArchivoGuardado ArchivoGuardado);

    private sealed class EvidenciaPreparada(
        string nombreOriginal,
        string mimeType,
        MemoryStream contenido)
        : IDisposable
    {
        public string NombreOriginal { get; } = nombreOriginal;

        public string MimeType { get; } = mimeType;

        public MemoryStream Contenido { get; } = contenido;

        public void Dispose()
        {
            Contenido.Dispose();
        }
    }
}

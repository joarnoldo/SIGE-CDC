using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Activos;

public sealed class RegistroUsoActivoService(ApplicationDbContext contexto)
    : IRegistroUsoActivoService
{
    public async Task<IReadOnlyList<ActivoUsoOpcion>>
        ObtenerActivosParaUsoAsync(
            CancellationToken cancellationToken = default)
    {
        var activos = await contexto.Activos
            .AsNoTracking()
            .Where(activo =>
                activo.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(activo => activo.CodigoActivo)
            .ThenBy(activo => activo.IdActivo)
            .Select(activo => new ActivoUsoOpcion
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                EstadoActivo = activo.EstadoActivo == null
                    ? string.Empty
                    : activo.EstadoActivo.Nombre,
                IdTipoMedicionUso = activo.IdTipoMedicionUso,
                TipoMedicionUso = activo.TipoMedicionUso == null
                    ? null
                    : activo.TipoMedicionUso.Nombre,
                LecturaUsoActual = activo.LecturaUsoActual
            })
            .ToListAsync(cancellationToken);

        if (activos.Count == 0)
        {
            return activos;
        }

        var idsActivos = activos
            .Select(activo => activo.IdActivo)
            .ToArray();
        var idsConMantenimientoEnProceso = await contexto.Mantenimientos
            .AsNoTracking()
            .Where(mantenimiento =>
                idsActivos.Contains(mantenimiento.IdActivo)
                && mantenimiento.EstadoRegistro
                    == EstadosRegistro.Activo
                && mantenimiento.EstadoMantenimiento != null
                && mantenimiento.EstadoMantenimiento.Nombre
                    == EstadosMantenimiento.EnProceso
                && mantenimiento.EstadoMantenimiento.EstadoRegistro
                    == EstadosRegistro.Activo)
            .Select(mantenimiento => mantenimiento.IdActivo)
            .Distinct()
            .ToListAsync(cancellationToken);
        var bloqueadosPorMantenimiento =
            idsConMantenimientoEnProceso.ToHashSet();
        var idsConEstadoVigente =
            (await contexto.Activos
                .AsNoTracking()
                .Where(activo =>
                    idsActivos.Contains(activo.IdActivo)
                    && activo.EstadoActivo != null
                    && activo.EstadoActivo.EstadoRegistro
                        == EstadosRegistro.Activo)
                .Select(activo => activo.IdActivo)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var idsTiposMedicionActivos =
            (await contexto.TiposMedicionUso
                .AsNoTracking()
                .Where(tipo =>
                    tipo.EstadoRegistro
                        == EstadosRegistro.Activo)
                .Select(tipo => tipo.IdTipoMedicionUso)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var activo in activos)
        {
            if (!idsConEstadoVigente.Contains(activo.IdActivo))
            {
                activo.PuedeRegistrarUso = false;
                activo.MotivoNoDisponible =
                    "El estado del activo no está vigente.";
            }
            else if (!ReglasRegistroUsoActivo.EsEstadoUtilizable(
                    activo.EstadoActivo))
            {
                activo.PuedeRegistrarUso = false;
                activo.MotivoNoDisponible =
                    "El activo debe estar Disponible o Asignado para registrar uso.";
            }
            else if (bloqueadosPorMantenimiento.Contains(
                         activo.IdActivo))
            {
                activo.PuedeRegistrarUso = false;
                activo.MotivoNoDisponible =
                    "El activo tiene un mantenimiento en proceso.";
            }
            else if (activo.TipoMedicionUso is not null
                && !ReglasRegistroUsoActivo
                    .EsTipoMedicionRegistrable(
                        activo.TipoMedicionUso))
            {
                activo.PuedeRegistrarUso = false;
                activo.MotivoNoDisponible =
                    "El activo no utiliza una medición acumulada.";
            }
            else if (activo.IdTipoMedicionUso.HasValue
                && !idsTiposMedicionActivos.Contains(
                    activo.IdTipoMedicionUso.Value))
            {
                activo.PuedeRegistrarUso = false;
                activo.MotivoNoDisponible =
                    "El tipo de medición del activo no está vigente.";
            }
            else
            {
                activo.PuedeRegistrarUso = true;
                activo.MotivoNoDisponible = null;
            }
        }

        return activos;
    }

    public async Task<IReadOnlyList<TipoMedicionUsoOpcion>>
        ObtenerTiposMedicionUsoAsync(
            CancellationToken cancellationToken = default)
    {
        return await contexto.TiposMedicionUso
            .AsNoTracking()
            .Where(tipo =>
                tipo.EstadoRegistro == EstadosRegistro.Activo
                && (tipo.Nombre == TiposMedicionUso.Horas
                    || tipo.Nombre
                        == TiposMedicionUso.Kilometros
                    || tipo.Nombre
                        == TiposMedicionUso.Unidades))
            .OrderBy(tipo => tipo.Nombre)
            .ThenBy(tipo => tipo.IdTipoMedicionUso)
            .Select(tipo => new TipoMedicionUsoOpcion
            {
                IdTipoMedicionUso = tipo.IdTipoMedicionUso,
                Nombre = tipo.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<long> RegistrarUsoAsync(
        SolicitudRegistrarUsoActivo solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.IdActivo <= 0)
        {
            throw new ArgumentException("Seleccione un activo.");
        }

        if (!solicitud.FechaRegistro.HasValue)
        {
            throw new ArgumentException(
                "La fecha del registro es obligatoria.");
        }

        var fechaRegistro = solicitud.FechaRegistro.Value.Date;
        if (fechaRegistro > DateTime.Today)
        {
            throw new ArgumentException(
                "La fecha del registro no puede ser futura.");
        }

        if (!solicitud.LecturaNueva.HasValue)
        {
            throw new ArgumentException(
                "La lectura nueva es obligatoria.");
        }

        ReglasRegistroUsoActivo.ValidarLectura(
            solicitud.LecturaNueva.Value,
            "La lectura nueva");
        if (solicitud.LecturaInicial.HasValue)
        {
            ReglasRegistroUsoActivo.ValidarLectura(
                solicitud.LecturaInicial.Value,
                "La lectura inicial");
        }

        var observaciones = LimpiarTextoOpcional(
            solicitud.Observaciones,
            500,
            "Las observaciones");
        var idUsuario = LimpiarIdUsuarioObligatorio(
            idUsuarioActual);

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var usuarioActivo = await contexto.Users
                .AsNoTracking()
                .AnyAsync(
                    usuario =>
                        usuario.Id == idUsuario
                        && usuario.EstadoRegistro
                            == EstadosRegistro.Activo,
                    cancellationToken);

            if (!usuarioActivo)
            {
                throw new InvalidOperationException(
                    "No se encontró al usuario responsable del registro.");
            }

            var activo = await contexto.Activos
                .Include(registro => registro.EstadoActivo)
                .Include(registro => registro.TipoMedicionUso)
                .FirstOrDefaultAsync(
                    registro =>
                        registro.IdActivo == solicitud.IdActivo
                        && registro.EstadoRegistro
                            == EstadosRegistro.Activo,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "No se encontró el activo seleccionado.");

            var finDiaRegistro = fechaRegistro.AddDays(1);
            var mantenimientosEnLaFecha =
                await contexto.Mantenimientos
                    .AsNoTracking()
                    .Where(
                        mantenimiento =>
                            mantenimiento.IdActivo
                                == activo.IdActivo
                            && mantenimiento.EstadoRegistro
                                == EstadosRegistro.Activo
                            && mantenimiento.EstadoMantenimiento
                                != null
                            && mantenimiento.EstadoMantenimiento
                                .EstadoRegistro
                                == EstadosRegistro.Activo
                            && (mantenimiento.EstadoMantenimiento.Nombre
                                    == EstadosMantenimiento.EnProceso
                                || mantenimiento.EstadoMantenimiento.Nombre
                                    == EstadosMantenimiento.Finalizado)
                            && mantenimiento.FechaInicio.HasValue
                            && mantenimiento.FechaInicio.Value
                                < finDiaRegistro
                            && (!mantenimiento.FechaFin.HasValue
                                || mantenimiento.FechaFin.Value
                                    >= fechaRegistro))
                    .Select(mantenimiento => new
                    {
                        Estado =
                            mantenimiento.EstadoMantenimiento!.Nombre,
                        mantenimiento.FechaInicio,
                        mantenimiento.FechaFin
                    })
                    .ToListAsync(cancellationToken);
            var tieneMantenimientoEnLaFecha =
                mantenimientosEnLaFecha.Any(mantenimiento =>
                    ReglasRegistroUsoActivo
                        .EsFechaAfectadaPorMantenimiento(
                            fechaRegistro,
                            mantenimiento.Estado,
                            mantenimiento.FechaInicio,
                            mantenimiento.FechaFin));

            if (activo.EstadoActivo is null
                || activo.EstadoActivo.EstadoRegistro
                    != EstadosRegistro.Activo)
            {
                throw new InvalidOperationException(
                    "El estado actual del activo no está vigente.");
            }

            ReglasRegistroUsoActivo.ValidarEstadoUtilizable(
                activo.EstadoActivo?.Nombre,
                tieneMantenimientoEnLaFecha);

            if (activo.IdTipoMedicionUso.HasValue
                && solicitud.IdTipoMedicionUso.HasValue
                && activo.IdTipoMedicionUso.Value
                    != solicitud.IdTipoMedicionUso.Value)
            {
                throw new InvalidOperationException(
                    "El tipo de medición del activo no puede cambiar después del primer registro.");
            }

            var idTipoMedicion = activo.IdTipoMedicionUso
                ?? solicitud.IdTipoMedicionUso;
            if (!idTipoMedicion.HasValue
                || idTipoMedicion.Value <= 0)
            {
                throw new ArgumentException(
                    "Seleccione un tipo de medición de uso para el primer registro.");
            }

            var tipoMedicion = await contexto.TiposMedicionUso
                .FirstOrDefaultAsync(
                    tipo =>
                        tipo.IdTipoMedicionUso
                            == idTipoMedicion.Value
                        && tipo.EstadoRegistro
                            == EstadosRegistro.Activo,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "No se encontró el tipo de medición seleccionado.");

            ReglasRegistroUsoActivo.ValidarTipoMedicionRegistrable(
                tipoMedicion.Nombre);

            var ultimoRegistro = await contexto.RegistrosUsoActivo
                .AsNoTracking()
                .Where(registro =>
                    registro.IdActivo == activo.IdActivo
                    && registro.EstadoRegistro
                        == EstadosRegistro.Activo)
                .OrderByDescending(registro =>
                    registro.FechaRegistro)
                .ThenByDescending(registro =>
                    registro.IdRegistroUsoActivo)
                .Select(registro => new
                {
                    registro.FechaRegistro,
                    registro.LecturaNueva
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ultimoRegistro is not null
                && fechaRegistro
                    < ultimoRegistro.FechaRegistro.Date)
            {
                throw new InvalidOperationException(
                    "La fecha del registro no puede ser anterior al último uso registrado.");
            }

            if (ultimoRegistro is not null
                && (!activo.LecturaUsoActual.HasValue
                    || activo.LecturaUsoActual.Value
                        != ultimoRegistro.LecturaNueva))
            {
                throw new InvalidOperationException(
                    "La lectura actual del activo no coincide con su último registro de uso. Corrija la inconsistencia antes de registrar una nueva lectura.");
            }

            decimal lecturaAnterior;
            if (activo.LecturaUsoActual.HasValue)
            {
                lecturaAnterior = activo.LecturaUsoActual.Value;

                if (solicitud.LecturaInicial.HasValue
                    && solicitud.LecturaInicial.Value
                        != lecturaAnterior)
                {
                    throw new InvalidOperationException(
                        "La lectura inicial no coincide con la lectura actual del activo.");
                }
            }
            else
            {
                if (!solicitud.LecturaInicial.HasValue)
                {
                    throw new ArgumentException(
                        "La lectura inicial es obligatoria para el primer registro.");
                }

                lecturaAnterior = solicitud.LecturaInicial.Value;
            }

            var lecturaNueva = solicitud.LecturaNueva.Value;
            var cantidadUso =
                ReglasRegistroUsoActivo.CalcularCantidadUso(
                    lecturaAnterior,
                    lecturaNueva);

            var proyectosAsignados =
                await contexto.AsignacionesActivoProyecto
                    .AsNoTracking()
                    .Where(asignacion =>
                        asignacion.IdActivo == activo.IdActivo
                        && asignacion.EstadoRegistro
                            == EstadosRegistro.Activo
                        && asignacion.FechaInicio <= fechaRegistro
                        && asignacion.FechaFin >= fechaRegistro)
                    .OrderBy(asignacion =>
                        asignacion.IdAsignacionActivoProyecto)
                    .Select(asignacion => asignacion.IdProyecto)
                    .Take(2)
                    .ToListAsync(cancellationToken);

            if (proyectosAsignados.Count > 1)
            {
                throw new InvalidOperationException(
                    "Existe más de una asignación activa para el activo en la fecha indicada.");
            }

            var fechaCreacion = DateTime.Now;
            var registroUso = new RegistroUsoActivo
            {
                IdActivo = activo.IdActivo,
                IdProyecto = proyectosAsignados.Count == 1
                    ? proyectosAsignados[0]
                    : null,
                FechaRegistro = fechaRegistro,
                LecturaAnterior = lecturaAnterior,
                LecturaNueva = lecturaNueva,
                CantidadUso = cantidadUso,
                Observaciones = observaciones,
                RegistradoPor = idUsuario,
                FechaCreacion = fechaCreacion,
                EstadoRegistro = EstadosRegistro.Activo
            };

            contexto.RegistrosUsoActivo.Add(registroUso);

            var valoresActivoAnteriores = new
            {
                activo.IdTipoMedicionUso,
                activo.LecturaUsoActual
            };

            if (!activo.IdTipoMedicionUso.HasValue)
            {
                activo.IdTipoMedicionUso =
                    tipoMedicion.IdTipoMedicionUso;
                activo.TipoMedicionUso = tipoMedicion;
            }

            activo.LecturaUsoActual = lecturaNueva;
            activo.FechaModificacion = fechaCreacion;
            activo.ModificadoPor = idUsuario;

            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = fechaCreacion,
                Accion = "Registro de uso de activo",
                Entidad = "RegistroUsoActivo",
                IdRegistro =
                    registroUso.IdRegistroUsoActivo.ToString(),
                ValoresAnteriores = JsonSerializer.Serialize(
                    valoresActivoAnteriores),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    registroUso.IdActivo,
                    registroUso.IdProyecto,
                    registroUso.FechaRegistro,
                    registroUso.LecturaAnterior,
                    registroUso.LecturaNueva,
                    registroUso.CantidadUso,
                    activo.IdTipoMedicionUso,
                    activo.LecturaUsoActual
                }),
                Observacion =
                    $"Se registró uso para el activo {activo.CodigoActivo}."
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return registroUso.IdRegistroUsoActivo;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<UsoActivoAcumuladoResumen?>
        ObtenerResumenUsoActivoAsync(
            long idActivo,
            CancellationToken cancellationToken = default)
    {
        if (idActivo <= 0)
        {
            return null;
        }

        var resumen = await contexto.Activos
            .AsNoTracking()
            .Where(activo =>
                activo.IdActivo == idActivo
                && activo.EstadoRegistro
                    == EstadosRegistro.Activo)
            .Select(activo => new UsoActivoAcumuladoResumen
            {
                IdActivo = activo.IdActivo,
                CodigoActivo = activo.CodigoActivo,
                NombreActivo = activo.NombreActivo,
                TipoMedicionUso = activo.TipoMedicionUso == null
                    ? null
                    : activo.TipoMedicionUso.Nombre,
                LecturaUsoActual = activo.LecturaUsoActual
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (resumen is null)
        {
            return null;
        }

        resumen.Registros = await contexto.RegistrosUsoActivo
            .AsNoTracking()
            .Where(registro =>
                registro.IdActivo == idActivo
                && registro.EstadoRegistro
                    == EstadosRegistro.Activo)
            .OrderByDescending(registro =>
                registro.FechaRegistro)
            .ThenByDescending(registro =>
                registro.IdRegistroUsoActivo)
            .Select(registro => new RegistroUsoActivoResumen
            {
                IdRegistroUsoActivo =
                    registro.IdRegistroUsoActivo,
                IdProyecto = registro.IdProyecto,
                FechaRegistro = registro.FechaRegistro,
                LecturaAnterior = registro.LecturaAnterior,
                LecturaNueva = registro.LecturaNueva,
                CantidadUso = registro.CantidadUso,
                Proyecto = registro.Proyecto == null
                    ? null
                    : registro.Proyecto.NombreProyecto,
                Observaciones = registro.Observaciones,
                RegistradoPor = registro.RegistradoPor,
                FechaCreacion = registro.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        resumen.TotalUsoRegistrado = resumen.Registros.Sum(
            registro => registro.CantidadUso ?? 0m);

        resumen.PeriodosFueraServicio = await contexto.Mantenimientos
            .AsNoTracking()
            .Where(mantenimiento =>
                mantenimiento.IdActivo == idActivo
                && mantenimiento.EstadoRegistro
                    == EstadosRegistro.Activo
                && mantenimiento.FechaInicio.HasValue
                && mantenimiento.EstadoMantenimiento != null
                && mantenimiento.EstadoMantenimiento.EstadoRegistro
                    == EstadosRegistro.Activo
                && (mantenimiento.EstadoMantenimiento.Nombre
                        == EstadosMantenimiento.EnProceso
                    || mantenimiento.EstadoMantenimiento.Nombre
                        == EstadosMantenimiento.Finalizado))
            .OrderByDescending(mantenimiento =>
                mantenimiento.FechaInicio)
            .ThenByDescending(mantenimiento =>
                mantenimiento.IdMantenimiento)
            .Select(mantenimiento =>
                new PeriodoFueraServicioResumen
                {
                    IdMantenimiento =
                        mantenimiento.IdMantenimiento,
                    TipoMantenimiento =
                        mantenimiento.TipoMantenimiento,
                    EstadoMantenimiento =
                        mantenimiento.EstadoMantenimiento == null
                            ? string.Empty
                            : mantenimiento.EstadoMantenimiento.Nombre,
                    FechaInicio =
                        mantenimiento.FechaInicio!.Value,
                    FechaFin = mantenimiento.FechaFin,
                    TiempoFueraServicioHoras =
                        mantenimiento.TiempoFueraServicioHoras
                })
            .ToListAsync(cancellationToken);

        return resumen;
    }

    private static string LimpiarIdUsuarioObligatorio(
        string idUsuario)
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
}

using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlRegistroUsoActivoIntegrationTests
{
    [MySqlQaFact]
    public async Task RegistroInicialYPosterior_PersisteAcumuladoTipoYAuditoria()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new RegistroUsoActivoService(contexto);

        var idPrimerRegistro = await servicio.RegistrarUsoAsync(
            new SolicitudRegistrarUsoActivo
            {
                IdActivo = datos.IdActivoSinLectura,
                IdTipoMedicionUso = datos.IdTipoMedicionPrincipal,
                FechaRegistro = DateTime.Today.AddDays(-1),
                LecturaInicial = 100m,
                LecturaNueva = 105.25m,
                Observaciones = "  Jornada inicial QA  "
            },
            datos.IdUsuario);

        var idSegundoRegistro = await servicio.RegistrarUsoAsync(
            new SolicitudRegistrarUsoActivo
            {
                IdActivo = datos.IdActivoSinLectura,
                FechaRegistro = DateTime.Today,
                LecturaInicial = 105.25m,
                LecturaNueva = 110.75m,
                Observaciones = "Jornada posterior QA"
            },
            datos.IdUsuario);

        Assert.NotEqual(idPrimerRegistro, idSegundoRegistro);

        var registros = await contexto.RegistrosUsoActivo
            .AsNoTracking()
            .Where(registro =>
                registro.IdActivo == datos.IdActivoSinLectura)
            .OrderBy(registro => registro.FechaRegistro)
            .ThenBy(registro => registro.IdRegistroUsoActivo)
            .ToListAsync();

        Assert.Collection(
            registros,
            primero =>
            {
                Assert.Equal(idPrimerRegistro, primero.IdRegistroUsoActivo);
                Assert.Equal(100m, primero.LecturaAnterior);
                Assert.Equal(105.25m, primero.LecturaNueva);
                Assert.Equal(5.25m, primero.CantidadUso);
                Assert.Equal("Jornada inicial QA", primero.Observaciones);
                Assert.Null(primero.IdProyecto);
                Assert.Equal(datos.IdUsuario, primero.RegistradoPor);
            },
            segundo =>
            {
                Assert.Equal(idSegundoRegistro, segundo.IdRegistroUsoActivo);
                Assert.Equal(105.25m, segundo.LecturaAnterior);
                Assert.Equal(110.75m, segundo.LecturaNueva);
                Assert.Equal(5.50m, segundo.CantidadUso);
                Assert.Equal("Jornada posterior QA", segundo.Observaciones);
                Assert.Null(segundo.IdProyecto);
                Assert.Equal(datos.IdUsuario, segundo.RegistradoPor);
            });

        var activo = await contexto.Activos
            .AsNoTracking()
            .SingleAsync(registro =>
                registro.IdActivo == datos.IdActivoSinLectura);
        Assert.Equal(
            datos.IdTipoMedicionPrincipal,
            activo.IdTipoMedicionUso);
        Assert.Equal(110.75m, activo.LecturaUsoActual);
        Assert.Equal(datos.IdUsuario, activo.ModificadoPor);
        Assert.NotNull(activo.FechaModificacion);

        var auditorias = await contexto.BitacoraAuditoria
            .AsNoTracking()
            .Where(registro =>
                registro.IdUsuario == datos.IdUsuario
                && registro.Entidad == "RegistroUsoActivo"
                && registro.Accion
                    == "Registro de uso de activo"
                && (registro.IdRegistro
                        == idPrimerRegistro.ToString()
                    || registro.IdRegistro
                        == idSegundoRegistro.ToString()))
            .ToListAsync();
        Assert.Equal(2, auditorias.Count);

        var resumen = await servicio.ObtenerResumenUsoActivoAsync(
            datos.IdActivoSinLectura);
        Assert.NotNull(resumen);
        Assert.Equal(datos.NombreTipoMedicionPrincipal, resumen.TipoMedicionUso);
        Assert.Equal(110.75m, resumen.LecturaUsoActual);
        Assert.Equal(10.75m, resumen.TotalUsoRegistrado);
        Assert.Equal(2, resumen.Registros.Count);
        Assert.Equal(110.75m, resumen.Registros[0].LecturaNueva);
        Assert.Equal(105.25m, resumen.Registros[1].LecturaNueva);
    }

    [MySqlQaFact]
    public async Task RegistroConAsignacionUnica_AsociaProyectoAutomaticamente()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new RegistroUsoActivoService(contexto);

        var idRegistro = await servicio.RegistrarUsoAsync(
            new SolicitudRegistrarUsoActivo
            {
                IdActivo = datos.IdActivoAsignado,
                FechaRegistro = DateTime.Today,
                LecturaInicial = 50m,
                LecturaNueva = 57.50m,
                Observaciones = "Uso dentro de asignación QA"
            },
            datos.IdUsuario);

        var registro = await contexto.RegistrosUsoActivo
            .AsNoTracking()
            .SingleAsync(item =>
                item.IdRegistroUsoActivo == idRegistro);
        Assert.Equal(datos.IdProyecto, registro.IdProyecto);
        Assert.Equal(50m, registro.LecturaAnterior);
        Assert.Equal(57.50m, registro.LecturaNueva);
        Assert.Equal(7.50m, registro.CantidadUso);

        var resumen = await servicio.ObtenerResumenUsoActivoAsync(
            datos.IdActivoAsignado);
        Assert.NotNull(resumen);
        var registroResumen = Assert.Single(resumen.Registros);
        Assert.Equal(datos.NombreProyecto, registroResumen.Proyecto);
    }

    [MySqlQaFact]
    public async Task DatosInvalidosYAsignacionesAmbiguas_NoPersistenCambiosParciales()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new RegistroUsoActivoService(contexto);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.RegistrarUsoAsync(
                new SolicitudRegistrarUsoActivo
                {
                    IdActivo = datos.IdActivoSinLectura,
                    IdTipoMedicionUso =
                        datos.IdTipoMedicionPrincipal,
                    FechaRegistro = DateTime.Today,
                    LecturaInicial = 0m,
                    LecturaNueva = -1m
                },
                datos.IdUsuario));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.RegistrarUsoAsync(
                new SolicitudRegistrarUsoActivo
                {
                    IdActivo = datos.IdActivoConLectura,
                    FechaRegistro = DateTime.Today,
                    LecturaInicial = 30m,
                    LecturaNueva = 30m
                },
                datos.IdUsuario));

        var tipoDiferente =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoConLectura,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionSecundario,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 30m,
                        LecturaNueva = 31m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "no puede cambiar",
            tipoDiferente.Message,
            StringComparison.OrdinalIgnoreCase);

        var tipoInexistente =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoSinLectura,
                        IdTipoMedicionUso = int.MaxValue,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 0m,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "tipo de medici",
            tipoInexistente.Message,
            StringComparison.OrdinalIgnoreCase);

        var tipoNoAplica =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoSinLectura,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionNoAplica,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 0m,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "no permite registrar uso",
            tipoNoAplica.Message,
            StringComparison.OrdinalIgnoreCase);

        var activoInexistente =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = long.MaxValue,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionPrincipal,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 0m,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "activo seleccionado",
            activoInexistente.Message,
            StringComparison.OrdinalIgnoreCase);

        var activoNoUtilizable =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoDadoDeBaja,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionPrincipal,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 0m,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "Disponible o Asignado",
            activoNoUtilizable.Message,
            StringComparison.OrdinalIgnoreCase);

        var mantenimientoActivo =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo =
                            datos.IdActivoConMantenimiento,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 20m,
                        LecturaNueva = 21m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "mantenimiento",
            mantenimientoActivo.Message,
            StringComparison.OrdinalIgnoreCase);

        var asignacionAmbigua =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo =
                            datos.IdActivoConAsignacionAmbigua,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionPrincipal,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 10m,
                        LecturaNueva = 12m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "más de una asignación",
            asignacionAmbigua.Message,
            StringComparison.OrdinalIgnoreCase);

        contexto.ChangeTracker.Clear();

        Assert.False(await contexto.RegistrosUsoActivo
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.IdActivo == datos.IdActivoSinLectura
                || registro.IdActivo == datos.IdActivoConLectura
                || registro.IdActivo == datos.IdActivoDadoDeBaja
                || registro.IdActivo
                    == datos.IdActivoConMantenimiento
                || registro.IdActivo
                    == datos.IdActivoConAsignacionAmbigua));

        var activoAmbiguo = await contexto.Activos
            .AsNoTracking()
            .SingleAsync(activo =>
                activo.IdActivo
                    == datos.IdActivoConAsignacionAmbigua);
        Assert.Null(activoAmbiguo.IdTipoMedicionUso);
        Assert.Null(activoAmbiguo.LecturaUsoActual);
        Assert.Null(activoAmbiguo.ModificadoPor);
        Assert.Null(activoAmbiguo.FechaModificacion);

        Assert.False(await contexto.BitacoraAuditoria
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.Entidad == "RegistroUsoActivo"
                && registro.IdUsuario == datos.IdUsuario));
    }

    [MySqlQaFact]
    public async Task FechaYLecturaInicialInvalidas_SonRechazadasSinAlterarAcumulado()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new RegistroUsoActivoService(contexto);

        var lecturaInicialAusente =
            await Assert.ThrowsAsync<ArgumentException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoSinLectura,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionPrincipal,
                        FechaRegistro = DateTime.Today,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "lectura inicial",
            lecturaInicialAusente.Message,
            StringComparison.OrdinalIgnoreCase);

        var fechaFutura =
            await Assert.ThrowsAsync<ArgumentException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoSinLectura,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionPrincipal,
                        FechaRegistro = DateTime.Today.AddDays(1),
                        LecturaInicial = 0m,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "futura",
            fechaFutura.Message,
            StringComparison.OrdinalIgnoreCase);

        var idRegistro = await servicio.RegistrarUsoAsync(
            new SolicitudRegistrarUsoActivo
            {
                IdActivo = datos.IdActivoConLectura,
                FechaRegistro = DateTime.Today.AddDays(-1),
                LecturaInicial = 30m,
                LecturaNueva = 31m
            },
            datos.IdUsuario);

        var fechaAnterior =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoConLectura,
                        FechaRegistro = DateTime.Today.AddDays(-2),
                        LecturaInicial = 31m,
                        LecturaNueva = 32m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "anterior",
            fechaAnterior.Message,
            StringComparison.OrdinalIgnoreCase);

        contexto.ChangeTracker.Clear();

        var registro = await contexto.RegistrosUsoActivo
            .AsNoTracking()
            .SingleAsync(item =>
                item.IdRegistroUsoActivo == idRegistro);
        Assert.Equal(31m, registro.LecturaNueva);

        var activoSinLectura = await contexto.Activos
            .AsNoTracking()
            .SingleAsync(activo =>
                activo.IdActivo == datos.IdActivoSinLectura);
        Assert.Null(activoSinLectura.IdTipoMedicionUso);
        Assert.Null(activoSinLectura.LecturaUsoActual);

        var activoConLectura = await contexto.Activos
            .AsNoTracking()
            .SingleAsync(activo =>
                activo.IdActivo == datos.IdActivoConLectura);
        Assert.Equal(31m, activoConLectura.LecturaUsoActual);
    }

    [MySqlQaFact]
    public async Task MantenimientoHistoricoYLecturaDesalineada_RechazanRegistro()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new RegistroUsoActivoService(contexto);

        contexto.Mantenimientos.Add(CrearMantenimiento(
            datos.IdActivoSinLectura,
            datos.IdEstadoMantenimientoFinalizado,
            EstadosMantenimiento.Finalizado,
            DateTime.Today.AddDays(-6).AddHours(8),
            DateTime.Today.AddDays(-4).AddHours(17),
            57m,
            datos.IdUsuario));
        contexto.RegistrosUsoActivo.Add(new RegistroUsoActivo
        {
            IdActivo = datos.IdActivoConLectura,
            FechaRegistro = DateTime.Today.AddDays(-1),
            LecturaAnterior = 20m,
            LecturaNueva = 29m,
            CantidadUso = 9m,
            RegistradoPor = datos.IdUsuario,
            FechaCreacion = DateTime.Now.AddDays(-1),
            EstadoRegistro = EstadosRegistro.Activo
        });
        await contexto.SaveChangesAsync();

        var periodoFueraServicio =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoSinLectura,
                        IdTipoMedicionUso =
                            datos.IdTipoMedicionPrincipal,
                        FechaRegistro = DateTime.Today.AddDays(-5),
                        LecturaInicial = 0m,
                        LecturaNueva = 1m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "período fuera de servicio",
            periodoFueraServicio.Message,
            StringComparison.OrdinalIgnoreCase);

        var lecturaDesalineada =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.RegistrarUsoAsync(
                    new SolicitudRegistrarUsoActivo
                    {
                        IdActivo = datos.IdActivoConLectura,
                        FechaRegistro = DateTime.Today,
                        LecturaInicial = 30m,
                        LecturaNueva = 31m
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "no coincide",
            lecturaDesalineada.Message,
            StringComparison.OrdinalIgnoreCase);

        contexto.ChangeTracker.Clear();

        Assert.False(await contexto.RegistrosUsoActivo
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.IdActivo == datos.IdActivoSinLectura));
        var registroHistorico = Assert.Single(
            await contexto.RegistrosUsoActivo
                .AsNoTracking()
                .Where(registro =>
                    registro.IdActivo == datos.IdActivoConLectura)
                .ToListAsync());
        Assert.Equal(29m, registroHistorico.LecturaNueva);

        var activo = await contexto.Activos
            .AsNoTracking()
            .SingleAsync(registro =>
                registro.IdActivo == datos.IdActivoConLectura);
        Assert.Equal(30m, activo.LecturaUsoActual);
    }

    [MySqlQaFact]
    public async Task Resumen_IncluyeSoloPeriodosEnProcesoYFinalizados()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new RegistroUsoActivoService(contexto);

        await servicio.RegistrarUsoAsync(
            new SolicitudRegistrarUsoActivo
            {
                IdActivo = datos.IdActivoSinLectura,
                IdTipoMedicionUso =
                    datos.IdTipoMedicionPrincipal,
                FechaRegistro = DateTime.Today.AddDays(-10),
                LecturaInicial = 200m,
                LecturaNueva = 208m
            },
            datos.IdUsuario);

        var mantenimientos = new[]
        {
            CrearMantenimiento(
                datos.IdActivoSinLectura,
                datos.IdEstadoMantenimientoEnProceso,
                EstadosMantenimiento.EnProceso,
                DateTime.Today.AddDays(-1),
                fechaFin: null,
                tiempoFueraServicio: null,
                datos.IdUsuario),
            CrearMantenimiento(
                datos.IdActivoSinLectura,
                datos.IdEstadoMantenimientoFinalizado,
                EstadosMantenimiento.Finalizado,
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddDays(-4),
                24m,
                datos.IdUsuario),
            CrearMantenimiento(
                datos.IdActivoSinLectura,
                datos.IdEstadoMantenimientoProgramado,
                EstadosMantenimiento.Programado,
                fechaInicio: null,
                fechaFin: null,
                tiempoFueraServicio: null,
                datos.IdUsuario),
            CrearMantenimiento(
                datos.IdActivoSinLectura,
                datos.IdEstadoMantenimientoCancelado,
                EstadosMantenimiento.Cancelado,
                DateTime.Today.AddDays(-8),
                DateTime.Today.AddDays(-7),
                4m,
                datos.IdUsuario)
        };
        contexto.Mantenimientos.AddRange(mantenimientos);
        await contexto.SaveChangesAsync();

        var resumen = await servicio.ObtenerResumenUsoActivoAsync(
            datos.IdActivoSinLectura);

        Assert.NotNull(resumen);
        Assert.Equal(8m, resumen.TotalUsoRegistrado);
        Assert.Equal(2, resumen.PeriodosFueraServicio.Count);

        Assert.Collection(
            resumen.PeriodosFueraServicio,
            enProceso =>
            {
                Assert.Equal(
                    mantenimientos[0].IdMantenimiento,
                    enProceso.IdMantenimiento);
                Assert.Equal(
                    EstadosMantenimiento.EnProceso,
                    enProceso.EstadoMantenimiento);
                Assert.Null(enProceso.FechaFin);
                Assert.Null(enProceso.TiempoFueraServicioHoras);
            },
            finalizado =>
            {
                Assert.Equal(
                    mantenimientos[1].IdMantenimiento,
                    finalizado.IdMantenimiento);
                Assert.Equal(
                    EstadosMantenimiento.Finalizado,
                    finalizado.EstadoMantenimiento);
                Assert.Equal(
                    DateTime.Today.AddDays(-4),
                    finalizado.FechaFin);
                Assert.Equal(
                    24m,
                    finalizado.TiempoFueraServicioHoras);
            });
    }

    private static Mantenimiento CrearMantenimiento(
        long idActivo,
        int idEstado,
        string estado,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        decimal? tiempoFueraServicio,
        string idUsuario)
    {
        return new Mantenimiento
        {
            IdActivo = idActivo,
            TipoMantenimiento = TiposMantenimiento.Correctivo,
            IdEstadoMantenimiento = idEstado,
            FechaProgramada =
                (fechaInicio ?? DateTime.Today).Date,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Descripcion = $"Mantenimiento {estado} QA",
            TiempoFueraServicioHoras = tiempoFueraServicio,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(
            MySqlQaFactAttribute.VariableConexion);

        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains(
                "SIGE_CDC_DB_QA_MIEMBRO2",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de integración solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        var opciones =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySQL(cadena)
                .Options;
        return new ApplicationDbContext(opciones);
    }

    private static async Task<DatosQa> PrepararDatosAsync(
        ApplicationDbContext contexto)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..10];
        var catalogo = await contexto.CategoriasActivo
            .AsNoTracking()
            .Where(categoria =>
                categoria.EstadoRegistro == EstadosRegistro.Activo
                && categoria.TipoActivo != null
                && categoria.TipoActivo.EstadoRegistro
                    == EstadosRegistro.Activo)
            .OrderBy(categoria => categoria.IdCategoriaActivo)
            .Select(categoria => new
            {
                categoria.IdCategoriaActivo,
                categoria.IdTipoActivo
            })
            .FirstAsync();
        var estadosActivo = await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo)
            .ToDictionaryAsync(
                estado => estado.Nombre,
                estado => estado.IdEstadoActivo);
        var tiposMedicion = await contexto.TiposMedicionUso
            .AsNoTracking()
            .Where(tipo =>
                tipo.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(tipo => tipo.IdTipoMedicionUso)
            .ToListAsync();
        var estadosMantenimiento =
            await contexto.EstadosMantenimiento
                .AsNoTracking()
                .Where(estado =>
                    estado.EstadoRegistro
                        == EstadosRegistro.Activo)
                .ToDictionaryAsync(
                    estado => estado.Nombre,
                    estado => estado.IdEstadoMantenimiento);
        var idEstadoProyecto = await contexto.EstadosProyecto
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(estado => estado.IdEstadoProyecto)
            .Select(estado => estado.IdEstadoProyecto)
            .FirstAsync();

        Assert.Contains(EstadosActivo.Disponible, estadosActivo.Keys);
        Assert.Contains(EstadosActivo.Asignado, estadosActivo.Keys);
        Assert.Contains(EstadosActivo.DadoDeBaja, estadosActivo.Keys);
        Assert.Contains(
            EstadosMantenimiento.Programado,
            estadosMantenimiento.Keys);
        Assert.Contains(
            EstadosMantenimiento.EnProceso,
            estadosMantenimiento.Keys);
        Assert.Contains(
            EstadosMantenimiento.Finalizado,
            estadosMantenimiento.Keys);
        Assert.Contains(
            EstadosMantenimiento.Cancelado,
            estadosMantenimiento.Keys);

        var tipoNoAplica = Assert.Single(
            tiposMedicion,
            tipo => string.Equals(
                tipo.Nombre,
                TiposMedicionUso.NoAplica,
                StringComparison.OrdinalIgnoreCase));
        var tiposAcumulables = tiposMedicion
            .Where(tipo =>
                !string.Equals(
                    tipo.Nombre,
                    TiposMedicionUso.NoAplica,
                    StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        Assert.Equal(2, tiposAcumulables.Length);
        var tipoPrincipal = tiposAcumulables[0];
        var tipoSecundario = tiposAcumulables[1];

        var usuario = new ApplicationUser
        {
            Id = $"qa-ru-{sufijo}",
            UserName = $"qa-ru-{sufijo}@sige.local",
            NormalizedUserName = $"QA-RU-{sufijo}@SIGE.LOCAL",
            Email = $"qa-ru-{sufijo}@sige.local",
            NormalizedEmail = $"QA-RU-{sufijo}@SIGE.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = EstadosRegistro.Activo,
            FechaCreacion = DateTime.Now
        };
        var proyecto = new Proyecto
        {
            CodigoProyecto = $"QA-RU-{sufijo}",
            NombreProyecto = $"Proyecto uso {sufijo}",
            IdEstadoProyecto = idEstadoProyecto,
            FechaInicio = DateTime.Today.AddDays(-30),
            FechaFinEstimada = DateTime.Today.AddDays(30),
            FechaCreacion = DateTime.Now,
            CreadoPor = usuario.Id,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var activoSinLectura = CrearActivo(
            $"QA-RU-S-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estadosActivo[EstadosActivo.Disponible]);
        var activoAsignado = CrearActivo(
            $"QA-RU-A-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estadosActivo[EstadosActivo.Asignado],
            tipoPrincipal.IdTipoMedicionUso,
            50m);
        var activoConLectura = CrearActivo(
            $"QA-RU-L-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estadosActivo[EstadosActivo.Disponible],
            tipoPrincipal.IdTipoMedicionUso,
            30m);
        var activoDadoDeBaja = CrearActivo(
            $"QA-RU-B-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estadosActivo[EstadosActivo.DadoDeBaja]);
        var activoConMantenimiento = CrearActivo(
            $"QA-RU-M-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estadosActivo[EstadosActivo.Disponible],
            tipoPrincipal.IdTipoMedicionUso,
            20m);
        var activoConAsignacionAmbigua = CrearActivo(
            $"QA-RU-X-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estadosActivo[EstadosActivo.Asignado]);

        contexto.Users.Add(usuario);
        contexto.Proyectos.Add(proyecto);
        contexto.Activos.AddRange(
            activoSinLectura,
            activoAsignado,
            activoConLectura,
            activoDadoDeBaja,
            activoConMantenimiento,
            activoConAsignacionAmbigua);
        await contexto.SaveChangesAsync();

        contexto.AsignacionesActivoProyecto.AddRange(
            CrearAsignacion(
                activoAsignado.IdActivo,
                proyecto.IdProyecto,
                usuario.Id,
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddDays(5)),
            CrearAsignacion(
                activoConAsignacionAmbigua.IdActivo,
                proyecto.IdProyecto,
                usuario.Id,
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddDays(5)),
            CrearAsignacion(
                activoConAsignacionAmbigua.IdActivo,
                proyecto.IdProyecto,
                usuario.Id,
                DateTime.Today.AddDays(-2),
                DateTime.Today.AddDays(2)));

        contexto.Mantenimientos.Add(new Mantenimiento
        {
            IdActivo = activoConMantenimiento.IdActivo,
            TipoMantenimiento = TiposMantenimiento.Correctivo,
            IdEstadoMantenimiento =
                estadosMantenimiento[EstadosMantenimiento.EnProceso],
            FechaProgramada = DateTime.Today.AddDays(-1),
            FechaInicio = DateTime.Now.AddHours(-2),
            Descripcion = "Mantenimiento activo QA",
            FechaCreacion = DateTime.Now.AddHours(-2),
            CreadoPor = usuario.Id,
            EstadoRegistro = EstadosRegistro.Activo
        });
        await contexto.SaveChangesAsync();

        return new DatosQa(
            usuario.Id,
            proyecto.IdProyecto,
            proyecto.NombreProyecto,
            tipoPrincipal.IdTipoMedicionUso,
            tipoPrincipal.Nombre,
            tipoSecundario.IdTipoMedicionUso,
            tipoNoAplica.IdTipoMedicionUso,
            estadosMantenimiento[EstadosMantenimiento.Programado],
            estadosMantenimiento[EstadosMantenimiento.EnProceso],
            estadosMantenimiento[EstadosMantenimiento.Finalizado],
            estadosMantenimiento[EstadosMantenimiento.Cancelado],
            activoSinLectura.IdActivo,
            activoAsignado.IdActivo,
            activoConLectura.IdActivo,
            activoDadoDeBaja.IdActivo,
            activoConMantenimiento.IdActivo,
            activoConAsignacionAmbigua.IdActivo);
    }

    private static Activo CrearActivo(
        string codigo,
        string idUsuario,
        int idTipoActivo,
        int idCategoriaActivo,
        int idEstadoActivo,
        int? idTipoMedicionUso = null,
        decimal? lecturaUsoActual = null)
    {
        return new Activo
        {
            CodigoActivo = codigo,
            NombreActivo = $"Activo {codigo}",
            IdTipoActivo = idTipoActivo,
            IdCategoriaActivo = idCategoriaActivo,
            IdEstadoActivo = idEstadoActivo,
            IdTipoMedicionUso = idTipoMedicionUso,
            LecturaUsoActual = lecturaUsoActual,
            UbicacionActual = "Patio registro de uso QA",
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
    }

    private static AsignacionActivoProyecto CrearAsignacion(
        long idActivo,
        long idProyecto,
        string idUsuario,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        return new AsignacionActivoProyecto
        {
            IdActivo = idActivo,
            IdProyecto = idProyecto,
            FechaInicio = fechaInicio.Date,
            FechaFin = fechaFin.Date,
            AsignadoPor = idUsuario,
            FechaAsignacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
    }

    private sealed record DatosQa(
        string IdUsuario,
        long IdProyecto,
        string NombreProyecto,
        int IdTipoMedicionPrincipal,
        string NombreTipoMedicionPrincipal,
        int IdTipoMedicionSecundario,
        int IdTipoMedicionNoAplica,
        int IdEstadoMantenimientoProgramado,
        int IdEstadoMantenimientoEnProceso,
        int IdEstadoMantenimientoFinalizado,
        int IdEstadoMantenimientoCancelado,
        long IdActivoSinLectura,
        long IdActivoAsignado,
        long IdActivoConLectura,
        long IdActivoDadoDeBaja,
        long IdActivoConMantenimiento,
        long IdActivoConAsignacionAmbigua);
}

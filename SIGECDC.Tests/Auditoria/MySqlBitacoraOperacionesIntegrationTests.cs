using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Auditoria;
using SIGECDC.Application.Operaciones;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Auditoria;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Operaciones;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Auditoria;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlBitacoraOperacionesIntegrationTests
    : IAsyncLifetime
{
    private string? idUsuarioLimpieza;
    private string? tokenLimpieza;

    [MySqlQaFact]
    public async Task Operaciones_ReunenTrazabilidadDeLosCuatroModulos()
    {
        string sufijo;
        string idUsuario;
        long idActivo;
        long idProyecto;
        long idAsignacion;
        long idMantenimiento;
        long idRegistroUso;

        await using (var contexto = CrearContexto())
        {
            var datos = await PrepararDatosAsync(contexto);
            sufijo = datos.Sufijo;
            idUsuario = datos.IdUsuario;
            idUsuarioLimpieza = idUsuario;

            var activoService = new ActivoService(contexto);
            idActivo = await activoService.RegistrarActivoAsync(
                CrearSolicitudActivo(datos),
                idUsuario);

            var proyectoService =
                new ProyectoService(contexto);
            var solicitudProyecto =
                CrearSolicitudProyecto(datos);
            await proyectoService.GuardarProyectoAsync(
                solicitudProyecto,
                idUsuario);

            var proyecto = await contexto.Proyectos
                .AsNoTracking()
                .SingleAsync(registro =>
                    registro.CodigoProyecto
                        == solicitudProyecto.CodigoProyecto);
            idProyecto = proyecto.IdProyecto;

            solicitudProyecto.IdProyecto = idProyecto;
            solicitudProyecto.NombreProyecto +=
                " actualizado";
            solicitudProyecto.Descripcion =
                "Descripción actualizada para auditoría";
            await proyectoService.GuardarProyectoAsync(
                solicitudProyecto,
                idUsuario);

            var mantenimientoService =
                new MantenimientoService(
                    contexto,
                    new SIGECDC.Tests.Activos
                        .AlmacenamientoArchivosPrueba());
            idMantenimiento =
                await mantenimientoService
                    .DefinirMantenimientoPreventivoAsync(
                        new SolicitudDefinirMantenimientoPreventivo
                        {
                            IdActivo = idActivo,
                            FechaProgramada =
                                DateTime.Today.AddDays(20),
                            Descripcion =
                                $"Preventivo {sufijo}"
                        },
                        idUsuario);

            var asignacionService =
                new AsignacionActivoProyectoService(contexto);
            idAsignacion =
                await asignacionService.CrearAsignacionAsync(
                    new SolicitudAsignacionActivoProyecto
                    {
                        IdActivo = idActivo,
                        IdProyecto = idProyecto,
                        FechaInicio = DateTime.Today,
                        FechaFin =
                            DateTime.Today.AddDays(5),
                        Observaciones =
                            $"Asignación {sufijo}"
                    },
                    idUsuario);

            var usoService =
                new RegistroUsoActivoService(contexto);
            idRegistroUso = await usoService.RegistrarUsoAsync(
                new SolicitudRegistrarUsoActivo
                {
                    IdActivo = idActivo,
                    IdTipoMedicionUso =
                        datos.IdTipoMedicionUso,
                    FechaRegistro = DateTime.Today,
                    LecturaInicial = 0m,
                    LecturaNueva = 1m,
                    Observaciones = $"Uso {sufijo}"
                },
                idUsuario);
        }

        await using var contextoConsulta = CrearContexto();
        var historial =
            new HistorialSistemaService(contextoConsulta);
        var operaciones =
            await historial.ObtenerOperacionesAsync(
                new FiltroBitacoraOperaciones
                {
                    Busqueda = sufijo,
                    Cantidad = 250
                });

        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Activo
                && registro.Accion
                    == "Creación de activo"
                && registro.IdRegistro
                    == idActivo.ToString()
                && registro.IdUsuario == idUsuario
                && registro.ValoresNuevos != null);
        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Proyecto
                && registro.Accion
                    == "Creación de proyecto"
                && registro.IdRegistro
                    == idProyecto.ToString());
        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Proyecto
                && registro.Accion
                    == "Actualización de proyecto"
                && registro.ValoresAnteriores != null
                && registro.ValoresNuevos != null);
        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Asignacion
                && registro.Entidad
                    == "AsignacionActivoProyecto"
                && registro.IdRegistro
                    == idAsignacion.ToString());
        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Asignacion
                && registro.Entidad == "Activo"
                && registro.Accion
                    == "Actualización de estado por asignación");
        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Mantenimiento
                && registro.IdRegistro
                    == idMantenimiento.ToString());
        Assert.Contains(
            operaciones,
            registro =>
                registro.Categoria
                    == CategoriasBitacoraOperaciones.Activo
                && registro.Entidad
                    == "RegistroUsoActivo"
                && registro.Accion
                    == "Registro de uso de activo"
                && registro.IdRegistro
                    == idRegistroUso.ToString()
                && registro.IdUsuario == idUsuario);

        var operacionesActivo =
            await historial.ObtenerOperacionesAsync(
                new FiltroBitacoraOperaciones
                {
                    Categoria =
                        CategoriasBitacoraOperaciones.Activo,
                    Busqueda = sufijo,
                    Cantidad = 250
                });
        Assert.Contains(
            operacionesActivo,
            registro =>
                registro.Entidad == "RegistroUsoActivo"
                && registro.IdRegistro
                    == idRegistroUso.ToString());

        foreach (var categoria in new[]
        {
            CategoriasBitacoraOperaciones.Proyecto,
            CategoriasBitacoraOperaciones.Asignacion,
            CategoriasBitacoraOperaciones.Mantenimiento
        })
        {
            Assert.DoesNotContain(
                await historial.ObtenerOperacionesAsync(
                    new FiltroBitacoraOperaciones
                    {
                        Categoria = categoria,
                        Busqueda = sufijo,
                        Cantidad = 250
                    }),
                registro =>
                    registro.Entidad
                        == "RegistroUsoActivo");
        }
        Assert.All(
            operaciones,
            registro =>
                Assert.Equal(idUsuario, registro.IdUsuario));
        Assert.Empty(
            contextoConsulta.ChangeTracker.Entries());

        var proyectoAuditado =
            await contextoConsulta.Proyectos
                .AsNoTracking()
                .SingleAsync(registro =>
                    registro.IdProyecto == idProyecto);
        Assert.Equal(
            idUsuario,
            proyectoAuditado.CreadoPor);
        Assert.Equal(
            idUsuario,
            proyectoAuditado.ModificadoPor);
    }

    [MySqlQaFact]
    public async Task Consulta_RespetaFiltrosFechasYLecturaSinUsuario()
    {
        await using (var contexto = CrearContexto())
        {
            const string token = "AUDITORIA-FILTRO-QA";
            tokenLimpieza = token;
            contexto.BitacoraAuditoria.AddRange(
                CrearAuditoria(
                    new DateTime(2026, 7, 1),
                    "Activo",
                    "Actualización de estado por asignación",
                    token),
                CrearAuditoria(
                    new DateTime(
                        2026,
                        7,
                        31,
                        23,
                        59,
                        59),
                    "AsignacionActivoProyecto",
                    "Creación de asignación de activo",
                    token),
                CrearAuditoria(
                    new DateTime(
                        2026,
                        7,
                        31,
                        18,
                        0,
                        0),
                    "Mantenimiento",
                    "Finalización de orden de mantenimiento",
                    token),
                CrearAuditoria(
                    new DateTime(2026, 7, 15),
                    "Proyecto",
                    "Actualización de proyecto",
                    token),
                CrearAuditoria(
                    new DateTime(2026, 8, 1),
                    "Proyecto",
                    "Creación de proyecto",
                    token));
            await contexto.SaveChangesAsync();
        }

        await using var contextoConsulta = CrearContexto();
        var servicio =
            new HistorialSistemaService(contextoConsulta);
        var periodo = new FiltroBitacoraOperaciones
        {
            FechaDesde = new DateTime(
                2026,
                7,
                1,
                12,
                0,
                0),
            FechaHasta = new DateTime(
                2026,
                7,
                31,
                8,
                0,
                0),
            Busqueda = "AUDITORIA-FILTRO-QA",
            Cantidad = 999
        };

        periodo.Categoria =
            CategoriasBitacoraOperaciones.Asignacion;
        var asignaciones =
            await servicio.ObtenerOperacionesAsync(periodo);
        Assert.Equal(2, asignaciones.Count);
        Assert.All(
            asignaciones,
            registro => Assert.Equal(
                CategoriasBitacoraOperaciones.Asignacion,
                registro.Categoria));

        periodo.Categoria =
            CategoriasBitacoraOperaciones.Mantenimiento;
        var mantenimientos =
            await servicio.ObtenerOperacionesAsync(periodo);
        Assert.Single(mantenimientos);

        periodo.Categoria =
            CategoriasBitacoraOperaciones.Proyecto;
        var proyectos =
            await servicio.ObtenerOperacionesAsync(periodo);
        var proyecto = Assert.Single(proyectos);
        Assert.Null(proyecto.IdUsuario);
        Assert.Null(proyecto.Usuario);
        Assert.Equal(
            "Actualización de proyecto",
            proyecto.Accion);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.ObtenerOperacionesAsync(
                new FiltroBitacoraOperaciones
                {
                    FechaDesde =
                        new DateTime(2026, 8, 1),
                    FechaHasta =
                        new DateTime(2026, 7, 31)
                }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.ObtenerOperacionesAsync(
                new FiltroBitacoraOperaciones
                {
                    Categoria = "Auditor"
                }));
    }

    [MySqlQaFact]
    public async Task OperacionesFallidas_NoDejanCambiosParciales()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        idUsuarioLimpieza = datos.IdUsuario;
        var activoService = new ActivoService(contexto);
        var proyectoService =
            new ProyectoService(contexto);
        var codigoActivo =
            $"QA-AF-{datos.Sufijo}";
        var solicitudActivo =
            CrearSolicitudActivo(
                datos,
                codigoActivo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            activoService.RegistrarActivoAsync(
                solicitudActivo,
                "usuario-inexistente"));
        Assert.False(await contexto.Activos
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.CodigoActivo == codigoActivo));
        Assert.False(await contexto.BitacoraAuditoria
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.Observacion != null
                && registro.Observacion
                    .Contains(codigoActivo)));

        var solicitudProyecto =
            CrearSolicitudProyecto(datos);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            proyectoService.GuardarProyectoAsync(
                solicitudProyecto,
                "usuario-inexistente"));
        Assert.False(await contexto.Proyectos
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.CodigoProyecto
                    == solicitudProyecto.CodigoProyecto));

        var idActivo =
            await activoService.RegistrarActivoAsync(
                solicitudActivo,
                datos.IdUsuario);
        await proyectoService.GuardarProyectoAsync(
            solicitudProyecto,
            datos.IdUsuario);
        var idProyecto = await contexto.Proyectos
            .AsNoTracking()
            .Where(registro =>
                registro.CodigoProyecto
                    == solicitudProyecto.CodigoProyecto)
            .Select(registro => registro.IdProyecto)
            .SingleAsync();

        var asignacionService =
            new AsignacionActivoProyectoService(contexto);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            asignacionService.CrearAsignacionAsync(
                new SolicitudAsignacionActivoProyecto
                {
                    IdActivo = idActivo,
                    IdProyecto = idProyecto,
                    FechaInicio =
                        DateTime.Today.AddDays(1),
                    FechaFin =
                        DateTime.Today.AddDays(2)
                },
                "usuario-inexistente"));

        Assert.False(await contexto
            .AsignacionesActivoProyecto
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.IdActivo == idActivo
                && registro.IdProyecto == idProyecto));
        Assert.False(await contexto.BitacoraAuditoria
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.Entidad
                    == "AsignacionActivoProyecto"
                && registro.ValoresNuevos != null
                && registro.ValoresNuevos
                    .Contains(idActivo.ToString())));
    }

    public async Task InitializeAsync()
    {
        var cadena = Environment.GetEnvironmentVariable(
            MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena))
        {
            return;
        }

        await using var contexto = CrearContexto();
        await contexto.BitacoraAuditoria
            .Where(registro =>
                (registro.IdUsuario != null
                    && registro.IdUsuario.StartsWith("qa-aud-"))
                || registro.Observacion == "AUDITORIA-FILTRO-QA")
            .ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        if (idUsuarioLimpieza is null && tokenLimpieza is null)
        {
            return;
        }

        await using var contexto = CrearContexto();

        if (idUsuarioLimpieza is not null)
        {
            await contexto.BitacoraAuditoria
                .Where(registro => registro.IdUsuario == idUsuarioLimpieza)
                .ExecuteDeleteAsync();
        }

        if (tokenLimpieza is not null)
        {
            await contexto.BitacoraAuditoria
                .Where(registro => registro.Observacion == tokenLimpieza)
                .ExecuteDeleteAsync();
        }
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(
            SIGECDC.Tests.Activos.MySqlQaFactAttribute
                .VariableConexion);

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

    private static async Task<DatosAuditoriaQa>
        PrepararDatosAsync(ApplicationDbContext contexto)
    {
        var sufijo =
            Guid.NewGuid().ToString("N")[..10];
        var usuario = new ApplicationUser
        {
            Id = $"qa-aud-{sufijo}",
            UserName = $"qa-aud-{sufijo}@sige.local",
            NormalizedUserName =
                $"QA-AUD-{sufijo}@SIGE.LOCAL",
            Email = $"qa-aud-{sufijo}@sige.local",
            NormalizedEmail =
                $"QA-AUD-{sufijo}@SIGE.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = EstadosRegistro.Activo,
            FechaCreacion = DateTime.Now
        };
        contexto.Users.Add(usuario);

        var tipo = await contexto.TiposActivo
            .AsNoTracking()
            .Where(registro =>
                registro.EstadoRegistro
                    == EstadosRegistro.Activo)
            .OrderBy(registro =>
                registro.IdTipoActivo)
            .FirstAsync();
        var categoria =
            await contexto.CategoriasActivo
                .AsNoTracking()
                .Where(registro =>
                    registro.IdTipoActivo
                        == tipo.IdTipoActivo
                    && registro.EstadoRegistro
                        == EstadosRegistro.Activo)
                .OrderBy(registro =>
                    registro.IdCategoriaActivo)
                .FirstAsync();
        var tipoMedicion =
            await contexto.TiposMedicionUso
                .AsNoTracking()
                .Where(registro =>
                    registro.EstadoRegistro
                        == EstadosRegistro.Activo)
                .OrderBy(registro =>
                    registro.IdTipoMedicionUso)
                .FirstAsync();

        await contexto.SaveChangesAsync();

        return new DatosAuditoriaQa(
            sufijo,
            usuario.Id,
            tipo.IdTipoActivo,
            categoria.IdCategoriaActivo,
            tipoMedicion.IdTipoMedicionUso);
    }

    private static SolicitudRegistroActivo
        CrearSolicitudActivo(
            DatosAuditoriaQa datos,
            string? codigo = null)
    {
        codigo ??= $"QA-AA-{datos.Sufijo}";
        return new SolicitudRegistroActivo
        {
            CodigoActivo = codigo,
            NombreActivo =
                $"Activo auditoría {datos.Sufijo}",
            IdTipoActivo = datos.IdTipoActivo,
            IdCategoriaActivo =
                datos.IdCategoriaActivo,
            UbicacionActual = "Patio auditoría QA",
            Observaciones =
                $"Auditoría {datos.Sufijo}"
        };
    }

    private static SolicitudProyecto
        CrearSolicitudProyecto(
            DatosAuditoriaQa datos)
    {
        return new SolicitudProyecto
        {
            CodigoProyecto =
                $"QA-PA-{datos.Sufijo}",
            NombreProyecto =
                $"Proyecto auditoría {datos.Sufijo}",
            FechaInicio = DateTime.Today,
            FechaFinEstimada =
                DateTime.Today.AddMonths(1),
            Ubicacion = "Sitio auditoría QA",
            Observaciones =
                $"Auditoría {datos.Sufijo}"
        };
    }

    private static BitacoraAuditoria CrearAuditoria(
        DateTime fecha,
        string entidad,
        string accion,
        string token)
    {
        return new BitacoraAuditoria
        {
            FechaHora = fecha,
            Entidad = entidad,
            Accion = accion,
            IdRegistro =
                Guid.NewGuid().ToString("N")[..12],
            Observacion = token
        };
    }

    private sealed record DatosAuditoriaQa(
        string Sufijo,
        string IdUsuario,
        int IdTipoActivo,
        int IdCategoriaActivo,
        int IdTipoMedicionUso);
}

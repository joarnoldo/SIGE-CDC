using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlMantenimientoPreventivoIntegrationTests
{
    [MySqlQaFact]
    public async Task DefinirPreventivo_PersisteProgramadoAuditaYConservaDisponibilidad()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new MantenimientoService(
            contexto,
            new AlmacenamientoArchivosPrueba());
        var fechaProgramada = DateTime.Today.AddDays(10);
        var solicitud = new SolicitudDefinirMantenimientoPreventivo
        {
            IdActivo = datos.IdActivoDisponible,
            FechaProgramada = fechaProgramada,
            Descripcion = "  Inspección preventiva QA  "
        };

        var idMantenimiento = await servicio.DefinirMantenimientoPreventivoAsync(
            solicitud,
            datos.IdUsuario);

        var mantenimiento = await contexto.Mantenimientos
            .AsNoTracking()
            .Include(registro => registro.EstadoMantenimiento)
            .SingleAsync(registro =>
                registro.IdMantenimiento == idMantenimiento);

        Assert.Equal(datos.IdActivoDisponible, mantenimiento.IdActivo);
        Assert.Equal(TiposMantenimiento.Preventivo, mantenimiento.TipoMantenimiento);
        Assert.Equal(EstadosMantenimiento.Programado, mantenimiento.EstadoMantenimiento?.Nombre);
        Assert.Equal(fechaProgramada.Date, mantenimiento.FechaProgramada);
        Assert.Equal("Inspección preventiva QA", mantenimiento.Descripcion);
        Assert.Null(mantenimiento.IdProyecto);
        Assert.Null(mantenimiento.FechaInicio);
        Assert.Null(mantenimiento.FechaFin);
        Assert.Null(mantenimiento.CostoEstimado);
        Assert.Null(mantenimiento.CostoReal);
        Assert.Null(mantenimiento.Resultado);
        Assert.Null(mantenimiento.Responsable);
        Assert.Equal(datos.IdUsuario, mantenimiento.CreadoPor);

        var activo = await contexto.Activos
            .AsNoTracking()
            .Include(registro => registro.EstadoActivo)
            .SingleAsync(registro =>
                registro.IdActivo == datos.IdActivoDisponible);

        Assert.Equal(EstadosActivo.Disponible, activo.EstadoActivo?.Nombre);
        Assert.Equal("Patio QA preventivo", activo.UbicacionActual);
        Assert.Null(activo.FechaModificacion);
        Assert.Null(activo.ModificadoPor);

        Assert.True(await contexto.BitacoraAuditoria
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.IdUsuario == datos.IdUsuario
                && registro.Entidad == "Mantenimiento"
                && registro.IdRegistro == idMantenimiento.ToString()
                && registro.Accion
                    == "Definición de mantenimiento preventivo"));

        var idEstadoFinalizado = await contexto.EstadosMantenimiento
            .AsNoTracking()
            .Where(estado =>
                estado.Nombre == EstadosMantenimiento.Finalizado
                && estado.EstadoRegistro == EstadosRegistro.Activo)
            .Select(estado => estado.IdEstadoMantenimiento)
            .SingleAsync();

        var correctivoProgramado = new Mantenimiento
        {
            IdActivo = datos.IdActivoDisponible,
            TipoMantenimiento = TiposMantenimiento.Correctivo,
            IdEstadoMantenimiento =
                mantenimiento.IdEstadoMantenimiento,
            FechaProgramada = fechaProgramada.AddDays(1),
            FechaCreacion = DateTime.Now,
            CreadoPor = datos.IdUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var preventivoFinalizado = new Mantenimiento
        {
            IdActivo = datos.IdActivoDisponible,
            TipoMantenimiento = TiposMantenimiento.Preventivo,
            IdEstadoMantenimiento = idEstadoFinalizado,
            FechaProgramada = fechaProgramada.AddDays(2),
            FechaCreacion = DateTime.Now,
            CreadoPor = datos.IdUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var preventivoInactivo = new Mantenimiento
        {
            IdActivo = datos.IdActivoDisponible,
            TipoMantenimiento = TiposMantenimiento.Preventivo,
            IdEstadoMantenimiento =
                mantenimiento.IdEstadoMantenimiento,
            FechaProgramada = fechaProgramada.AddDays(3),
            FechaCreacion = DateTime.Now,
            CreadoPor = datos.IdUsuario,
            EstadoRegistro = EstadosRegistro.Inactivo
        };
        contexto.Mantenimientos.AddRange(
            correctivoProgramado,
            preventivoFinalizado,
            preventivoInactivo);
        await contexto.SaveChangesAsync();

        var pendientes =
            await servicio.ObtenerMantenimientosPreventivosPendientesAsync();
        var pendiente = Assert.Single(
            pendientes,
            registro => registro.IdMantenimiento == idMantenimiento);
        Assert.StartsWith("QA-MP-D-", pendiente.CodigoActivo);
        Assert.DoesNotContain(
            pendientes,
            registro =>
                registro.IdMantenimiento
                    == correctivoProgramado.IdMantenimiento
                || registro.IdMantenimiento
                    == preventivoFinalizado.IdMantenimiento
                || registro.IdMantenimiento
                    == preventivoInactivo.IdMantenimiento);

        var servicioDisponibilidad =
            new AsignacionActivoProyectoService(contexto);
        var disponibilidad =
            await servicioDisponibilidad.ObtenerActivosDisponiblesAsync(
                fechaProgramada,
                fechaProgramada);
        Assert.Contains(
            disponibilidad,
            registro => registro.IdActivo == datos.IdActivoDisponible);

        var consultaDisponibilidad =
            await servicioDisponibilidad.ConsultarDisponibilidadAsync(
                new FiltroDisponibilidadActivo
                {
                    FechaInicio = fechaProgramada,
                    FechaFin = fechaProgramada
                });
        Assert.True(Assert.Single(
            consultaDisponibilidad,
            registro => registro.IdActivo == datos.IdActivoDisponible)
            .EstaDisponible);

        var segundoId =
            await servicio.DefinirMantenimientoPreventivoAsync(
                solicitud,
                datos.IdUsuario);
        Assert.NotEqual(idMantenimiento, segundoId);
    }

    [MySqlQaFact]
    public async Task DefinirPreventivo_RechazaFechaPasadaYActivoDadoDeBaja()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new MantenimientoService(
            contexto,
            new AlmacenamientoArchivosPrueba());

        var fechaPasada = await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.DefinirMantenimientoPreventivoAsync(
                new SolicitudDefinirMantenimientoPreventivo
                {
                    IdActivo = datos.IdActivoDisponible,
                    FechaProgramada = DateTime.Today.AddDays(-1)
                },
                datos.IdUsuario));
        Assert.Contains(
            "anterior",
            fechaPasada.Message,
            StringComparison.OrdinalIgnoreCase);

        var activoDadoDeBaja =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.DefinirMantenimientoPreventivoAsync(
                    new SolicitudDefinirMantenimientoPreventivo
                    {
                        IdActivo = datos.IdActivoDadoDeBaja,
                        FechaProgramada = DateTime.Today.AddDays(1)
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "dado de baja",
            activoDadoDeBaja.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(await contexto.Mantenimientos
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.IdActivo == datos.IdActivoDisponible
                || registro.IdActivo == datos.IdActivoDadoDeBaja));
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

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(cadena)
            .Options;

        return new ApplicationDbContext(opciones);
    }

    private static async Task<DatosQa> PrepararDatosAsync(
        ApplicationDbContext contexto)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..10];
        var usuario = new ApplicationUser
        {
            Id = $"qa-mp-{sufijo}",
            UserName = $"qa-mp-{sufijo}@sige.local",
            NormalizedUserName = $"QA-MP-{sufijo}@SIGE.LOCAL",
            Email = $"qa-mp-{sufijo}@sige.local",
            NormalizedEmail = $"QA-MP-{sufijo}@SIGE.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = EstadosRegistro.Activo,
            FechaCreacion = DateTime.Now
        };

        var catalogo = await contexto.CategoriasActivo
            .AsNoTracking()
            .Where(categoria =>
                categoria.EstadoRegistro == EstadosRegistro.Activo
                && categoria.TipoActivo != null
                && categoria.TipoActivo.EstadoRegistro
                    == EstadosRegistro.Activo)
            .OrderBy(categoria =>
                categoria.IdCategoriaActivo)
            .Select(categoria => new
            {
                categoria.IdCategoriaActivo,
                categoria.IdTipoActivo
            })
            .FirstAsync();

        var estados = await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo
                && (estado.Nombre == EstadosActivo.Disponible
                    || estado.Nombre == EstadosActivo.DadoDeBaja))
            .ToDictionaryAsync(
                estado => estado.Nombre,
                estado => estado.IdEstadoActivo);

        Assert.True(estados.ContainsKey(EstadosActivo.Disponible));
        Assert.True(estados.ContainsKey(EstadosActivo.DadoDeBaja));

        var activoDisponible = CrearActivo(
            $"QA-MP-D-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estados[EstadosActivo.Disponible],
            "Patio QA preventivo");

        var activoDadoDeBaja = CrearActivo(
            $"QA-MP-B-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estados[EstadosActivo.DadoDeBaja],
            "Bodega de baja QA");

        contexto.Users.Add(usuario);
        contexto.Activos.AddRange(
            activoDisponible,
            activoDadoDeBaja);
        await contexto.SaveChangesAsync();

        return new DatosQa(
            usuario.Id,
            activoDisponible.IdActivo,
            activoDadoDeBaja.IdActivo);
    }

    private static Activo CrearActivo(
        string codigo,
        string idUsuario,
        int idTipoActivo,
        int idCategoriaActivo,
        int idEstadoActivo,
        string ubicacion)
    {
        return new Activo
        {
            CodigoActivo = codigo,
            NombreActivo = $"Activo {codigo}",
            IdTipoActivo = idTipoActivo,
            IdCategoriaActivo = idCategoriaActivo,
            IdEstadoActivo = idEstadoActivo,
            UbicacionActual = ubicacion,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
    }

    private sealed record DatosQa(
        string IdUsuario,
        long IdActivoDisponible,
        long IdActivoDadoDeBaja);
}

using System.Text;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlOrdenMantenimientoIntegrationTests
{
    [MySqlQaFact]
    public async Task CicloCompleto_PersisteEvidenciaAuditaYRecuperaDisponibilidad()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var almacenamiento = new AlmacenamientoArchivosPrueba();
        var servicio = new MantenimientoService(
            contexto,
            almacenamiento);

        var dadoDeBaja =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.CrearOrdenCorrectivaAsync(
                    new SolicitudCrearOrdenMantenimiento
                    {
                        IdActivo = datos.IdActivoDadoDeBaja,
                        FechaProgramada = DateTime.Today
                    },
                    datos.IdUsuario));
        Assert.Contains(
            "dado de baja",
            dadoDeBaja.Message,
            StringComparison.OrdinalIgnoreCase);

        var idMantenimiento = await servicio.CrearOrdenCorrectivaAsync(
            new SolicitudCrearOrdenMantenimiento
            {
                IdActivo = datos.IdActivoDisponible,
                FechaProgramada = DateTime.Today,
                Descripcion = "  Reparación hidráulica QA  ",
                CostoEstimado = 1250.50m,
                Responsable = "  Taller QA  "
            },
            datos.IdUsuario);

        var creado = await ObtenerMantenimientoAsync(
            contexto,
            idMantenimiento);
        Assert.Equal(TiposMantenimiento.Correctivo, creado.TipoMantenimiento);
        Assert.Equal(
            EstadosMantenimiento.Programado,
            creado.EstadoMantenimiento?.Nombre);
        Assert.Null(creado.FechaInicio);
        Assert.Null(creado.FechaFin);
        Assert.Null(creado.CostoReal);
        Assert.Null(creado.TiempoFueraServicioHoras);

        Assert.Equal(
            EstadosActivo.Disponible,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoDisponible));

        await servicio.IniciarOrdenAsync(
            idMantenimiento,
            datos.IdUsuario);

        var iniciado = await ObtenerMantenimientoAsync(
            contexto,
            idMantenimiento);
        Assert.Equal(
            EstadosMantenimiento.EnProceso,
            iniciado.EstadoMantenimiento?.Nombre);
        Assert.NotNull(iniciado.FechaInicio);
        Assert.Null(iniciado.FechaFin);
        Assert.Equal(
            EstadosActivo.EnMantenimiento,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoDisponible));

        var disponibilidadService =
            new AsignacionActivoProyectoService(contexto);
        var disponibilidad =
            await disponibilidadService.ConsultarDisponibilidadAsync(
                new FiltroDisponibilidadActivo
                {
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today
                });
        var activoNoDisponible = Assert.Single(
            disponibilidad,
            activo => activo.IdActivo == datos.IdActivoDisponible);
        Assert.False(activoNoDisponible.EstaDisponible);
        Assert.Contains(
            "mantenimiento en proceso",
            activoNoDisponible.MotivoDisponibilidad,
            StringComparison.OrdinalIgnoreCase);

        using (var cierre = CrearSolicitudCierre(
                   idMantenimiento,
                   "evidencia-cierre.pdf",
                   costoReal: 1100.25m,
                   tiempoFueraServicio: 3.5m))
        {
            await servicio.FinalizarOrdenAsync(
                cierre,
                datos.IdUsuario);
        }

        var finalizado = await ObtenerMantenimientoAsync(
            contexto,
            idMantenimiento);
        Assert.Equal(
            EstadosMantenimiento.Finalizado,
            finalizado.EstadoMantenimiento?.Nombre);
        Assert.NotNull(finalizado.FechaFin);
        Assert.Equal(1100.25m, finalizado.CostoReal);
        Assert.Equal(3.5m, finalizado.TiempoFueraServicioHoras);
        Assert.Equal("Trabajo finalizado QA", finalizado.Resultado);
        Assert.Equal(
            EstadosActivo.Disponible,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoDisponible));

        var documento = await contexto.DocumentosArchivo
            .AsNoTracking()
            .Include(registro => registro.TipoDocumento)
            .SingleAsync(registro =>
                registro.EntidadRelacionada == "Mantenimiento"
                && registro.IdEntidadRelacionada == idMantenimiento);
        Assert.Equal(
            "Evidencia mantenimiento",
            documento.TipoDocumento?.Nombre);
        Assert.Equal("evidencia-cierre.pdf", documento.NombreOriginal);
        Assert.Equal(datos.IdUsuario, documento.CargadoPor);
        Assert.True(almacenamiento.Existe(documento.RutaRelativa));

        var historial =
            await servicio.ObtenerHistorialActivoAsync(
                datos.IdActivoDisponible);
        var ordenHistorial = Assert.Single(
            historial,
            orden => orden.IdMantenimiento == idMantenimiento);
        var evidenciaHistorial =
            Assert.Single(ordenHistorial.Evidencias);
        Assert.Equal(
            documento.IdDocumentoArchivo,
            evidenciaHistorial.IdDocumentoArchivo);

        var descarga = await servicio.AbrirEvidenciaAsync(
            documento.IdDocumentoArchivo);
        Assert.NotNull(descarga);
        await using (descarga!.Contenido)
        {
            using var lector = new StreamReader(
                descarga.Contenido,
                Encoding.ASCII);
            Assert.StartsWith(
                "%PDF-",
                await lector.ReadToEndAsync());
        }

        Assert.True(await contexto.BitacoraAuditoria
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.Entidad == "Mantenimiento"
                && registro.IdRegistro == idMantenimiento.ToString()
                && registro.Accion
                    == "Inicio de orden de mantenimiento"));
        Assert.True(await contexto.BitacoraAuditoria
            .AsNoTracking()
            .AnyAsync(registro =>
                registro.Entidad == "Mantenimiento"
                && registro.IdRegistro == idMantenimiento.ToString()
                && registro.Accion
                    == "Finalización de orden de mantenimiento"));

        using var segundoCierre = CrearSolicitudCierre(
            idMantenimiento,
            "cierre-duplicado.pdf");
        var cierreDuplicado =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.FinalizarOrdenAsync(
                    segundoCierre,
                    datos.IdUsuario));
        Assert.Contains(
            "en proceso",
            cierreDuplicado.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, almacenamiento.CantidadArchivos);
    }

    [MySqlQaFact]
    public async Task Cierre_RecalculaOtroMantenimientoYAsignacionSinAlterarla()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var almacenamiento = new AlmacenamientoArchivosPrueba();
        var servicio = new MantenimientoService(
            contexto,
            almacenamiento);

        var idPrimero = await servicio.CrearOrdenCorrectivaAsync(
            new SolicitudCrearOrdenMantenimiento
            {
                IdActivo = datos.IdActivoAsignado,
                FechaProgramada = DateTime.Today,
                Descripcion = "Primer mantenimiento QA"
            },
            datos.IdUsuario);
        var idSegundo = await servicio.CrearOrdenCorrectivaAsync(
            new SolicitudCrearOrdenMantenimiento
            {
                IdActivo = datos.IdActivoAsignado,
                FechaProgramada = DateTime.Today,
                Descripcion = "Segundo mantenimiento QA"
            },
            datos.IdUsuario);

        await servicio.IniciarOrdenAsync(idPrimero, datos.IdUsuario);
        await servicio.IniciarOrdenAsync(idSegundo, datos.IdUsuario);

        Assert.Equal(
            EstadosActivo.EnMantenimiento,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoAsignado));

        using (var cierrePrimero = CrearSolicitudCierre(
                   idPrimero,
                   "primero.pdf"))
        {
            await servicio.FinalizarOrdenAsync(
                cierrePrimero,
                datos.IdUsuario);
        }

        Assert.Equal(
            EstadosActivo.EnMantenimiento,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoAsignado));

        using (var cierreSegundo = CrearSolicitudCierre(
                   idSegundo,
                   "segundo.pdf"))
        {
            await servicio.FinalizarOrdenAsync(
                cierreSegundo,
                datos.IdUsuario);
        }

        Assert.Equal(
            EstadosActivo.Asignado,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoAsignado));
        Assert.True(await contexto.AsignacionesActivoProyecto
            .AsNoTracking()
            .AnyAsync(asignacion =>
                asignacion.IdAsignacionActivoProyecto
                    == datos.IdAsignacion));
    }

    [MySqlQaFact]
    public async Task Cierre_SiFallaArchivo_NoModificaOrdenNiActivo()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var almacenamiento = new AlmacenamientoArchivosPrueba
        {
            FallarAlGuardar = true
        };
        var servicio = new MantenimientoService(
            contexto,
            almacenamiento);

        var idMantenimiento = await servicio.CrearOrdenCorrectivaAsync(
            new SolicitudCrearOrdenMantenimiento
            {
                IdActivo = datos.IdActivoDisponible,
                FechaProgramada = DateTime.Today
            },
            datos.IdUsuario);
        await servicio.IniciarOrdenAsync(
            idMantenimiento,
            datos.IdUsuario);

        using var cierre = CrearSolicitudCierre(
            idMantenimiento,
            "fallo.pdf");
        await Assert.ThrowsAsync<IOException>(() =>
            servicio.FinalizarOrdenAsync(
                cierre,
                datos.IdUsuario));

        Assert.Equal(
            EstadosMantenimiento.EnProceso,
            (await ObtenerMantenimientoAsync(
                contexto,
                idMantenimiento)).EstadoMantenimiento?.Nombre);
        Assert.Equal(
            EstadosActivo.EnMantenimiento,
            await ObtenerEstadoActivoAsync(
                contexto,
                datos.IdActivoDisponible));
        Assert.Equal(0, almacenamiento.CantidadArchivos);
        Assert.False(await contexto.DocumentosArchivo
            .AsNoTracking()
            .AnyAsync(documento =>
                documento.EntidadRelacionada == "Mantenimiento"
                && documento.IdEntidadRelacionada == idMantenimiento));
    }

    private static SolicitudCerrarOrdenMantenimiento CrearSolicitudCierre(
        long idMantenimiento,
        string nombreArchivo,
        decimal costoReal = 0,
        decimal tiempoFueraServicio = 0)
    {
        var contenido = Encoding.ASCII.GetBytes(
            "%PDF-1.4\n1 0 obj\n%%EOF");

        return new SolicitudCerrarOrdenMantenimiento
        {
            IdMantenimiento = idMantenimiento,
            CostoReal = costoReal,
            TiempoFueraServicioHoras = tiempoFueraServicio,
            Resultado = "Trabajo finalizado QA",
            Evidencias =
            [
                new SolicitudEvidenciaMantenimiento
                {
                    NombreOriginal = nombreArchivo,
                    MimeType = "application/octet-stream",
                    TamanoBytes = contenido.Length,
                    Contenido = new MemoryStream(
                        contenido,
                        writable: false)
                }
            ]
        };
    }

    private static async Task<Mantenimiento> ObtenerMantenimientoAsync(
        ApplicationDbContext contexto,
        long idMantenimiento)
    {
        return await contexto.Mantenimientos
            .AsNoTracking()
            .Include(registro => registro.EstadoMantenimiento)
            .SingleAsync(registro =>
                registro.IdMantenimiento == idMantenimiento);
    }

    private static async Task<string?> ObtenerEstadoActivoAsync(
        ApplicationDbContext contexto,
        long idActivo)
    {
        return await contexto.Activos
            .AsNoTracking()
            .Where(activo => activo.IdActivo == idActivo)
            .Select(activo => activo.EstadoActivo == null
                ? null
                : activo.EstadoActivo.Nombre)
            .SingleAsync();
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
        var estados = await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo)
            .ToDictionaryAsync(
                estado => estado.Nombre,
                estado => estado.IdEstadoActivo);
        var idEstadoProyecto = await contexto.EstadosProyecto
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(estado => estado.IdEstadoProyecto)
            .Select(estado => estado.IdEstadoProyecto)
            .FirstAsync();

        Assert.Contains(EstadosActivo.Disponible, estados.Keys);
        Assert.Contains(EstadosActivo.Asignado, estados.Keys);
        Assert.Contains(EstadosActivo.DadoDeBaja, estados.Keys);

        var usuario = new ApplicationUser
        {
            Id = $"qa-om-{sufijo}",
            UserName = $"qa-om-{sufijo}@sige.local",
            NormalizedUserName = $"QA-OM-{sufijo}@SIGE.LOCAL",
            Email = $"qa-om-{sufijo}@sige.local",
            NormalizedEmail = $"QA-OM-{sufijo}@SIGE.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = EstadosRegistro.Activo,
            FechaCreacion = DateTime.Now
        };
        var proyecto = new Proyecto
        {
            CodigoProyecto = $"QA-OM-{sufijo}",
            NombreProyecto = $"Proyecto mantenimiento {sufijo}",
            IdEstadoProyecto = idEstadoProyecto,
            FechaInicio = DateTime.Today,
            FechaFinEstimada = DateTime.Today.AddDays(30),
            FechaCreacion = DateTime.Now,
            CreadoPor = usuario.Id,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var activoDisponible = CrearActivo(
            $"QA-OM-D-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estados[EstadosActivo.Disponible]);
        var activoAsignado = CrearActivo(
            $"QA-OM-A-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estados[EstadosActivo.Asignado]);
        var activoDadoDeBaja = CrearActivo(
            $"QA-OM-B-{sufijo}",
            usuario.Id,
            catalogo.IdTipoActivo,
            catalogo.IdCategoriaActivo,
            estados[EstadosActivo.DadoDeBaja]);

        contexto.Users.Add(usuario);
        contexto.Proyectos.Add(proyecto);
        contexto.Activos.AddRange(
            activoDisponible,
            activoAsignado,
            activoDadoDeBaja);
        await contexto.SaveChangesAsync();

        var asignacion = new AsignacionActivoProyecto
        {
            IdActivo = activoAsignado.IdActivo,
            IdProyecto = proyecto.IdProyecto,
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(10),
            AsignadoPor = usuario.Id,
            FechaAsignacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.AsignacionesActivoProyecto.Add(asignacion);
        await contexto.SaveChangesAsync();

        return new DatosQa(
            usuario.Id,
            activoDisponible.IdActivo,
            activoAsignado.IdActivo,
            activoDadoDeBaja.IdActivo,
            asignacion.IdAsignacionActivoProyecto);
    }

    private static Activo CrearActivo(
        string codigo,
        string idUsuario,
        int idTipoActivo,
        int idCategoriaActivo,
        int idEstadoActivo)
    {
        return new Activo
        {
            CodigoActivo = codigo,
            NombreActivo = $"Activo {codigo}",
            IdTipoActivo = idTipoActivo,
            IdCategoriaActivo = idCategoriaActivo,
            IdEstadoActivo = idEstadoActivo,
            UbicacionActual = "Patio mantenimiento QA",
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
    }

    private sealed record DatosQa(
        string IdUsuario,
        long IdActivoDisponible,
        long IdActivoAsignado,
        long IdActivoDadoDeBaja,
        long IdAsignacion);
}

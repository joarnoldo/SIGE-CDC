using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlActivoIntegrationTests
{
    private static readonly string[] EstadosOficiales =
    [
        EstadosActivo.Disponible,
        EstadosActivo.Asignado,
        EstadosActivo.EnMantenimiento,
        EstadosActivo.FueraDeServicio,
        EstadosActivo.DadoDeBaja
    ];

    [MySqlQaFact]
    public async Task RegistrarActivo_ValidaCatalogosDuplicadosYEstadoInicial()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new ActivoService(contexto);
        var codigo = NuevoCodigo("REG");
        var solicitud = CrearSolicitudRegistro(codigo, datos);

        var idActivo = await servicio.RegistrarActivoAsync(solicitud, datos.IdUsuario);
        var registrado = await servicio.ObtenerActivoPorIdAsync(idActivo);

        Assert.NotNull(registrado);
        Assert.Equal(codigo, registrado.CodigoActivo);
        Assert.Equal(EstadosActivo.Disponible, registrado.EstadoActivo);
        Assert.Equal(datos.IdTipoActivo, registrado.IdTipoActivo);
        Assert.Equal(datos.IdCategoriaActivo, registrado.IdCategoriaActivo);
        Assert.Equal("Patio QA", registrado.UbicacionActual);

        var duplicado = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.RegistrarActivoAsync(CrearSolicitudRegistro(codigo, datos), datos.IdUsuario));
        Assert.Contains("Ya existe", duplicado.Message, StringComparison.OrdinalIgnoreCase);

        var categoriaInvalida = CrearSolicitudRegistro(NuevoCodigo("CAT"), datos);
        categoriaInvalida.IdCategoriaActivo = datos.IdCategoriaOtroTipo;
        var errorCategoria = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.RegistrarActivoAsync(categoriaInvalida, datos.IdUsuario));
        Assert.Contains("no pertenece", errorCategoria.Message, StringComparison.OrdinalIgnoreCase);

        var sinCodigo = CrearSolicitudRegistro(string.Empty, datos);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.RegistrarActivoAsync(sinCodigo, datos.IdUsuario));
    }

    [MySqlQaFact]
    public async Task ActualizarEstadoUbicacion_PreservaAdquisicionAuditaYAplicaRestricciones()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio = new ActivoService(contexto);
        var idActivo = await servicio.RegistrarActivoAsync(
            CrearSolicitudRegistro(NuevoCodigo("UPD"), datos),
            datos.IdUsuario);

        await servicio.ActualizarEstadoUbicacionAsync(new SolicitudActualizacionEstadoUbicacion
        {
            IdActivo = idActivo,
            IdEstadoActivo = datos.Estados[EstadosActivo.EnMantenimiento],
            UbicacionActual = "Taller QA"
        }, datos.IdUsuario);

        var actualizado = await contexto.Activos.AsNoTracking()
            .SingleAsync(activo => activo.IdActivo == idActivo);
        Assert.Equal(datos.Estados[EstadosActivo.EnMantenimiento], actualizado.IdEstadoActivo);
        Assert.Equal("Taller QA", actualizado.UbicacionActual);
        Assert.Equal(new DateTime(2026, 1, 15), actualizado.FechaAdquisicion);
        Assert.Equal(12500.75m, actualizado.ValorAdquisicion);
        Assert.Equal(datos.IdUsuario, actualizado.ModificadoPor);

        var trazabilidad = await servicio.ObtenerTrazabilidadActivoAsync(idActivo);
        Assert.Contains(trazabilidad, cambio => cambio.DescripcionCambio.Contains("Taller QA"));

        await servicio.ActualizarEstadoUbicacionAsync(new SolicitudActualizacionEstadoUbicacion
        {
            IdActivo = idActivo,
            IdEstadoActivo = datos.Estados[EstadosActivo.DadoDeBaja],
            UbicacionActual = "Bodega de baja QA"
        }, datos.IdUsuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarEstadoUbicacionAsync(new SolicitudActualizacionEstadoUbicacion
            {
                IdActivo = idActivo,
                IdEstadoActivo = datos.Estados[EstadosActivo.Disponible],
                UbicacionActual = "Patio QA"
            }, datos.IdUsuario));

        var idAsignado = await servicio.RegistrarActivoAsync(
            CrearSolicitudRegistro(NuevoCodigo("BLQ"), datos),
            datos.IdUsuario);
        var activoAsignado = await contexto.Activos.SingleAsync(activo => activo.IdActivo == idAsignado);
        activoAsignado.IdEstadoActivo = datos.Estados[EstadosActivo.Asignado];
        activoAsignado.EstadoActivo = null;
        var asignacion = new AsignacionActivoProyecto
        {
            IdActivo = idAsignado,
            IdProyecto = datos.IdProyecto,
            FechaInicio = DateTime.Today.AddDays(1),
            FechaFin = DateTime.Today.AddDays(5),
            AsignadoPor = datos.IdUsuario,
            FechaAsignacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.AsignacionesActivoProyecto.Add(asignacion);
        await contexto.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarEstadoUbicacionAsync(new SolicitudActualizacionEstadoUbicacion
            {
                IdActivo = idAsignado,
                IdEstadoActivo = datos.Estados[EstadosActivo.Disponible],
                UbicacionActual = "Patio QA"
            }, datos.IdUsuario));

        asignacion.FechaInicio = DateTime.Today.AddDays(-5);
        asignacion.FechaFin = DateTime.Today.AddDays(-1);
        await contexto.SaveChangesAsync();

        await servicio.ActualizarEstadoUbicacionAsync(new SolicitudActualizacionEstadoUbicacion
        {
            IdActivo = idAsignado,
            IdEstadoActivo = datos.Estados[EstadosActivo.Disponible],
            UbicacionActual = "Patio QA"
        }, datos.IdUsuario);

        Assert.Equal(
            datos.Estados[EstadosActivo.Disponible],
            (await contexto.Activos.AsNoTracking().SingleAsync(activo => activo.IdActivo == idAsignado))
                .IdEstadoActivo);
    }

    [MySqlQaFact]
    public async Task ActualizarEstadoUbicacion_NoDuplicaEstadoRastreadoPorEfCore()
    {
        DatosQa datos;
        long idActivo;

        await using (var contextoRegistro = CrearContexto())
        {
            datos = await PrepararDatosAsync(contextoRegistro);
            var servicioRegistro = new ActivoService(contextoRegistro);
            idActivo = await servicioRegistro.RegistrarActivoAsync(
                CrearSolicitudRegistro(NuevoCodigo("TRK"), datos),
                datos.IdUsuario);
        }

        await using var contextoActualizacion = CrearContexto();
        var servicioActualizacion = new ActivoService(contextoActualizacion);

        await servicioActualizacion.ActualizarEstadoUbicacionAsync(
            new SolicitudActualizacionEstadoUbicacion
            {
                IdActivo = idActivo,
                IdEstadoActivo = datos.Estados[EstadosActivo.Disponible],
                UbicacionActual = "Bodega QA rastreo"
            },
            datos.IdUsuario);

        var actualizado = await contextoActualizacion.Activos
            .AsNoTracking()
            .SingleAsync(activo => activo.IdActivo == idActivo);
        Assert.Equal("Bodega QA rastreo", actualizado.UbicacionActual);
        Assert.True(await contextoActualizacion.BitacoraAuditoria.AsNoTracking().AnyAsync(registro =>
            registro.Entidad == "Activo"
            && registro.IdRegistro == idActivo.ToString()
            && registro.Accion == "Actualización de estado y ubicación"));
    }

    [MySqlQaFact]
    public async Task ConsultarDisponibilidad_AplicaCincoEstadosYAsignacionesVigentes()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var codigos = new Dictionary<string, string>();

        foreach (var estado in EstadosOficiales)
        {
            var codigo = NuevoCodigo(estado[..3].ToUpperInvariant());
            codigos[estado] = codigo;
            contexto.Activos.Add(CrearActivo(codigo, datos, datos.Estados[estado]));
        }

        var codigoRestringido = NuevoCodigo("RES");
        var activoRestringido = CrearActivo(
            codigoRestringido,
            datos,
            datos.Estados[EstadosActivo.Disponible]);
        contexto.Activos.Add(activoRestringido);
        await contexto.SaveChangesAsync();
        contexto.AsignacionesActivoProyecto.Add(new AsignacionActivoProyecto
        {
            IdActivo = activoRestringido.IdActivo,
            IdProyecto = datos.IdProyecto,
            FechaInicio = DateTime.Today.AddDays(10),
            FechaFin = DateTime.Today.AddDays(12),
            AsignadoPor = datos.IdUsuario,
            FechaAsignacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        });
        await contexto.SaveChangesAsync();

        var servicio = new AsignacionActivoProyectoService(contexto);
        var resultados = await servicio.ConsultarDisponibilidadAsync(new FiltroDisponibilidadActivo
        {
            FechaInicio = DateTime.Today.AddDays(20),
            FechaFin = DateTime.Today.AddDays(22)
        });

        foreach (var estado in EstadosOficiales)
        {
            var resultado = Assert.Single(resultados, item => item.CodigoActivo == codigos[estado]);
            Assert.Equal(estado == EstadosActivo.Disponible, resultado.EstaDisponible);
        }

        Assert.False(Assert.Single(resultados, item => item.CodigoActivo == codigoRestringido).EstaDisponible);

        var opciones = await servicio.ObtenerActivosDisponiblesAsync(
            DateTime.Today.AddDays(20),
            DateTime.Today.AddDays(22));
        Assert.Contains(opciones, opcion => opcion.CodigoActivo == codigos[EstadosActivo.Disponible]);
        Assert.DoesNotContain(opciones, opcion => opcion.CodigoActivo == codigos[EstadosActivo.Asignado]);
        Assert.DoesNotContain(opciones, opcion => opcion.CodigoActivo == codigoRestringido);

        var soloAsignados = await servicio.ConsultarDisponibilidadAsync(new FiltroDisponibilidadActivo
        {
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(1),
            IdEstadoActivo = datos.Estados[EstadosActivo.Asignado]
        });
        Assert.All(soloAsignados, activo =>
        {
            Assert.Equal(EstadosActivo.Asignado, activo.EstadoActivo);
            Assert.False(activo.EstaDisponible);
        });
    }

    [MySqlQaFact]
    public async Task CrearAsignacion_ActualizaEstadoAuditaEvitaConflictosYPermiteLiberacionManual()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicioActivo = new ActivoService(contexto);
        var servicioAsignacion = new AsignacionActivoProyectoService(contexto);
        var codigo = NuevoCodigo("ASG");
        var idActivo = await servicioActivo.RegistrarActivoAsync(
            CrearSolicitudRegistro(codigo, datos),
            datos.IdUsuario);
        var inicio = DateTime.Today.AddDays(2);
        var fin = DateTime.Today.AddDays(4);
        var solicitud = new SolicitudAsignacionActivoProyecto
        {
            IdActivo = idActivo,
            IdProyecto = datos.IdProyecto,
            FechaInicio = inicio,
            FechaFin = fin,
            Observaciones = "Asignación QA"
        };

        Assert.Contains(
            await servicioAsignacion.ObtenerActivosDisponiblesAsync(inicio, fin),
            activo => activo.IdActivo == idActivo);

        var idAsignacion = await servicioAsignacion.CrearAsignacionAsync(solicitud, datos.IdUsuario);

        var activoPersistido = await contexto.Activos.AsNoTracking()
            .Include(activo => activo.EstadoActivo)
            .SingleAsync(activo => activo.IdActivo == idActivo);
        Assert.Equal(EstadosActivo.Asignado, activoPersistido.EstadoActivo?.Nombre);
        Assert.Equal(datos.IdUsuario, activoPersistido.ModificadoPor);
        Assert.True(await contexto.BitacoraAuditoria.AsNoTracking().AnyAsync(registro =>
            registro.Entidad == "Activo"
            && registro.IdRegistro == idActivo.ToString()
            && registro.Accion == "Actualización de estado por asignación"));

        var calendario = await servicioAsignacion.ObtenerAsignacionesAsync(inicio, fin, datos.IdProyecto);
        Assert.Contains(calendario, asignacion => asignacion.IdAsignacionActivoProyecto == idAsignacion);

        var conflicto = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicioAsignacion.CrearAsignacionAsync(solicitud, datos.IdUsuario));
        Assert.Contains("ya está asignado", conflicto.Message, StringComparison.OrdinalIgnoreCase);

        var rangoSinTraslape = new SolicitudAsignacionActivoProyecto
        {
            IdActivo = idActivo,
            IdProyecto = datos.IdProyecto,
            FechaInicio = fin.AddDays(10),
            FechaFin = fin.AddDays(12)
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicioAsignacion.CrearAsignacionAsync(rangoSinTraslape, datos.IdUsuario));

        Assert.DoesNotContain(
            await servicioAsignacion.ObtenerActivosDisponiblesAsync(inicio, fin),
            activo => activo.IdActivo == idActivo);

        var asignacionPersistida = await contexto.AsignacionesActivoProyecto
            .SingleAsync(asignacion => asignacion.IdAsignacionActivoProyecto == idAsignacion);
        asignacionPersistida.FechaInicio = DateTime.Today.AddDays(-4);
        asignacionPersistida.FechaFin = DateTime.Today.AddDays(-1);
        await contexto.SaveChangesAsync();

        await servicioActivo.ActualizarEstadoUbicacionAsync(new SolicitudActualizacionEstadoUbicacion
        {
            IdActivo = idActivo,
            IdEstadoActivo = datos.Estados[EstadosActivo.Disponible],
            UbicacionActual = "Patio liberado QA"
        }, datos.IdUsuario);

        Assert.Contains(
            await servicioAsignacion.ObtenerActivosDisponiblesAsync(inicio, fin),
            activo => activo.IdActivo == idActivo);
    }

    [MySqlQaFact]
    public async Task EsquemaOficial_ConservaFechasAsignacionYTablasDePlanilla()
    {
        await using var contexto = CrearContexto();
        var conexion = contexto.Database.GetDbConnection();
        await conexion.OpenAsync();

        Assert.Equal(2, await EjecutarConteoAsync(conexion, """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'AsignacionActivoProyecto'
              AND column_name IN ('FechaInicio', 'FechaFin')
              AND data_type = 'date';
            """));
        Assert.Equal(2, await EjecutarConteoAsync(conexion, """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name IN ('TipoActivo', 'CategoriaActivo');
            """));
        Assert.Equal(4, await EjecutarConteoAsync(conexion, """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name IN ('PeriodoPlanilla', 'Planilla', 'DetallePlanilla', 'IncidenciaPlanilla');
            """));
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de integración solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(cadena)
            .Options;
        return new ApplicationDbContext(opciones);
    }

    private static async Task<DatosQa> PrepararDatosAsync(ApplicationDbContext contexto)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..10];
        var usuario = new ApplicationUser
        {
            Id = $"qa-{sufijo}",
            UserName = $"qa-{sufijo}@sige.local",
            NormalizedUserName = $"QA-{sufijo}@SIGE.LOCAL",
            Email = $"qa-{sufijo}@sige.local",
            NormalizedEmail = $"QA-{sufijo}@SIGE.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = EstadosRegistro.Activo,
            FechaCreacion = DateTime.Now
        };
        contexto.Users.Add(usuario);

        var tipos = await contexto.TiposActivo.AsNoTracking()
            .OrderBy(tipo => tipo.IdTipoActivo)
            .Take(2)
            .ToListAsync();
        Assert.Equal(2, tipos.Count);
        var categoria = await contexto.CategoriasActivo.AsNoTracking()
            .FirstAsync(item => item.IdTipoActivo == tipos[0].IdTipoActivo);
        var categoriaOtroTipo = await contexto.CategoriasActivo.AsNoTracking()
            .FirstAsync(item => item.IdTipoActivo == tipos[1].IdTipoActivo);
        var estados = await contexto.EstadosActivo.AsNoTracking()
            .ToDictionaryAsync(estado => estado.Nombre, estado => estado.IdEstadoActivo);
        Assert.All(EstadosOficiales, estado => Assert.True(estados.ContainsKey(estado)));
        var idEstadoProyecto = await contexto.EstadosProyecto.AsNoTracking()
            .Where(estado => estado.Nombre == "Planificado")
            .Select(estado => estado.IdEstadoProyecto)
            .SingleAsync();
        var proyecto = new Proyecto
        {
            CodigoProyecto = $"QA-{sufijo}",
            NombreProyecto = $"Proyecto QA {sufijo}",
            FechaInicio = DateTime.Today,
            FechaFinEstimada = DateTime.Today.AddMonths(1),
            IdEstadoProyecto = idEstadoProyecto,
            FechaCreacion = DateTime.Now,
            CreadoPor = usuario.Id,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.Proyectos.Add(proyecto);
        await contexto.SaveChangesAsync();

        return new DatosQa(
            usuario.Id,
            tipos[0].IdTipoActivo,
            categoria.IdCategoriaActivo,
            categoriaOtroTipo.IdCategoriaActivo,
            proyecto.IdProyecto,
            estados);
    }

    private static SolicitudRegistroActivo CrearSolicitudRegistro(string codigo, DatosQa datos)
    {
        return new SolicitudRegistroActivo
        {
            CodigoActivo = codigo,
            NombreActivo = $"Activo {codigo}",
            IdTipoActivo = datos.IdTipoActivo,
            IdCategoriaActivo = datos.IdCategoriaActivo,
            Marca = "Marca QA",
            Modelo = "Modelo QA",
            NumeroSerie = $"SER-{codigo}",
            Placa = $"P-{codigo[^Math.Min(8, codigo.Length)..]}",
            Descripcion = "Activo de prueba aislada",
            FechaAdquisicion = new DateTime(2026, 1, 15),
            ValorAdquisicion = 12500.75m,
            UbicacionActual = "Patio QA",
            Observaciones = "QA Miembro 2"
        };
    }

    private static Activo CrearActivo(string codigo, DatosQa datos, int idEstado)
    {
        return new Activo
        {
            CodigoActivo = codigo,
            NombreActivo = $"Activo {codigo}",
            IdTipoActivo = datos.IdTipoActivo,
            IdCategoriaActivo = datos.IdCategoriaActivo,
            IdEstadoActivo = idEstado,
            UbicacionActual = "Patio QA",
            FechaCreacion = DateTime.Now,
            CreadoPor = datos.IdUsuario,
            EstadoRegistro = EstadosRegistro.Activo
        };
    }

    private static string NuevoCodigo(string prefijo)
    {
        return $"QA-{prefijo}-{Guid.NewGuid():N}"[..20];
    }

    private static async Task<int> EjecutarConteoAsync(
        System.Data.Common.DbConnection conexion,
        string consulta)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = consulta;
        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private sealed record DatosQa(
        string IdUsuario,
        int IdTipoActivo,
        int IdCategoriaActivo,
        int IdCategoriaOtroTipo,
        long IdProyecto,
        IReadOnlyDictionary<string, int> Estados);
}

[CollectionDefinition("MySQL QA Miembro 2", DisableParallelization = true)]
public sealed class MySqlQaMiembro2Collection;

public sealed class MySqlQaFactAttribute : FactAttribute
{
    public const string VariableConexion = "SIGECDC_QA_CONNECTION";

    public MySqlQaFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableConexion)))
        {
            Skip = "Requiere la base MySQL QA aislada del Miembro 2.";
        }
    }
}

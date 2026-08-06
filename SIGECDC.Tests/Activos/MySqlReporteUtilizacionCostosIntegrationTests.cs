using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Activos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Activos;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlReporteUtilizacionCostosIntegrationTests
{
    [MySqlQaFact]
    public async Task ReporteProyecto_FiltraPeriodoAgrupaUsoYTotalizaCostos()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var desde = new DateTime(2026, 7, 1);
        var hasta = new DateTime(2026, 7, 31);

        contexto.RegistrosUsoActivo.AddRange(
            CrearRegistroUso(
                datos.IdActivoHoras,
                datos.IdProyecto,
                desde,
                5m),
            CrearRegistroUso(
                datos.IdActivoKilometros,
                datos.IdProyecto,
                hasta,
                7m),
            CrearRegistroUso(
                datos.IdActivoHoras,
                datos.IdProyecto,
                desde.AddDays(-1),
                100m),
            CrearRegistroUso(
                datos.IdActivoHoras,
                datos.IdProyecto,
                hasta.AddDays(1),
                100m),
            CrearRegistroUso(
                datos.IdActivoHoras,
                datos.IdProyecto,
                desde.AddDays(5),
                50m,
                EstadosRegistro.Inactivo),
            CrearRegistroUso(
                datos.IdActivoHoras,
                datos.IdOtroProyecto,
                desde.AddDays(5),
                50m));

        var fechaFinFinalizado =
            new DateTime(2026, 7, 10, 12, 30, 0);
        var fechaInicioEnProceso =
            new DateTime(2026, 7, 31, 15, 0, 0);
        var fechaProgramada =
            new DateTime(2026, 7, 2);
        var fechaCancelado =
            new DateTime(2026, 7, 31);

        contexto.Mantenimientos.AddRange(
            CrearMantenimiento(
                datos,
                datos.IdActivoHoras,
                datos.IdProyecto,
                EstadosMantenimiento.Finalizado,
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 5, 8, 0, 0),
                fechaFinFinalizado,
                100m,
                90m),
            CrearMantenimiento(
                datos,
                datos.IdActivoHoras,
                datos.IdProyecto,
                EstadosMantenimiento.EnProceso,
                new DateTime(2026, 6, 15),
                fechaInicioEnProceso,
                fechaFin: null,
                50m,
                costoReal: null),
            CrearMantenimiento(
                datos,
                datos.IdActivoKilometros,
                datos.IdProyecto,
                EstadosMantenimiento.Programado,
                fechaProgramada,
                fechaInicio: null,
                fechaFin: null,
                30m,
                costoReal: null),
            CrearMantenimiento(
                datos,
                datos.IdActivoKilometros,
                datos.IdProyecto,
                EstadosMantenimiento.Cancelado,
                fechaCancelado,
                new DateTime(2026, 6, 20, 8, 0, 0),
                new DateTime(2026, 6, 20, 9, 0, 0),
                20m,
                5m),
            CrearMantenimiento(
                datos,
                datos.IdActivoHoras,
                datos.IdProyecto,
                EstadosMantenimiento.Programado,
                desde.AddDays(3),
                fechaInicio: null,
                fechaFin: null,
                500m,
                costoReal: null,
                EstadosRegistro.Inactivo),
            CrearMantenimiento(
                datos,
                datos.IdActivoHoras,
                datos.IdOtroProyecto,
                EstadosMantenimiento.Programado,
                desde.AddDays(3),
                fechaInicio: null,
                fechaFin: null,
                600m,
                costoReal: null));
        await contexto.SaveChangesAsync();

        var servicio =
            new ReporteUtilizacionCostosService(contexto);
        var reporte = await servicio.GenerarPorProyectoAsync(
            datos.IdProyecto,
            new FiltroPeriodoReporte
            {
                FechaDesde = desde.AddHours(8),
                FechaHasta = hasta.AddHours(18)
            });

        Assert.Equal("Proyecto", reporte.Encabezado.TipoReporte);
        Assert.Equal(datos.IdProyecto, reporte.Encabezado.IdReferencia);
        Assert.Equal(datos.CodigoProyecto, reporte.Encabezado.CodigoReferencia);
        Assert.Equal(desde, reporte.FechaDesde);
        Assert.Equal(hasta, reporte.FechaHasta);

        Assert.Equal(2, reporte.RegistrosUso.Count);
        Assert.Contains(
            reporte.RegistrosUso,
            registro => registro.FechaRegistro == desde);
        Assert.Contains(
            reporte.RegistrosUso,
            registro => registro.FechaRegistro == hasta);

        var totalHoras = Assert.Single(
            reporte.TotalesUtilizacion,
            total => total.TipoMedicion == datos.TipoMedicionHoras);
        Assert.Equal(5m, totalHoras.CantidadUsoTotal);
        Assert.Equal(1, totalHoras.CantidadRegistros);

        var totalKilometros = Assert.Single(
            reporte.TotalesUtilizacion,
            total =>
                total.TipoMedicion
                    == datos.TipoMedicionKilometros);
        Assert.Equal(7m, totalKilometros.CantidadUsoTotal);
        Assert.Equal(1, totalKilometros.CantidadRegistros);

        Assert.Equal(4, reporte.Mantenimientos.Count);
        Assert.Equal(200m, reporte.CostoEstimadoTotal);
        Assert.Equal(95m, reporte.CostoRealTotal);
        Assert.All(
            reporte.Mantenimientos,
            mantenimiento =>
                Assert.Equal(
                    datos.IdProyecto,
                    mantenimiento.IdProyecto));

        Assert.Equal(
            fechaFinFinalizado,
            Assert.Single(
                reporte.Mantenimientos,
                mantenimiento =>
                    mantenimiento.EstadoMantenimiento
                        == EstadosMantenimiento.Finalizado)
                .FechaEfectiva);
        Assert.Equal(
            fechaInicioEnProceso,
            Assert.Single(
                reporte.Mantenimientos,
                mantenimiento =>
                    mantenimiento.EstadoMantenimiento
                        == EstadosMantenimiento.EnProceso)
                .FechaEfectiva);
        Assert.Equal(
            fechaProgramada,
            Assert.Single(
                reporte.Mantenimientos,
                mantenimiento =>
                    mantenimiento.EstadoMantenimiento
                        == EstadosMantenimiento.Programado)
                .FechaEfectiva);
        Assert.Equal(
            fechaCancelado,
            Assert.Single(
                reporte.Mantenimientos,
                mantenimiento =>
                    mantenimiento.EstadoMantenimiento
                        == EstadosMantenimiento.Cancelado)
                .FechaEfectiva);
    }

    [MySqlQaFact]
    public async Task ReporteActivo_IncluyeProyectosYNulosYDevuelveVacioValido()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);

        contexto.RegistrosUsoActivo.AddRange(
            CrearRegistroUso(
                datos.IdActivoHoras,
                datos.IdProyecto,
                new DateTime(2026, 6, 10),
                4m),
            CrearRegistroUso(
                datos.IdActivoHoras,
                idProyecto: null,
                new DateTime(2026, 6, 11),
                6m),
            CrearRegistroUso(
                datos.IdActivoKilometros,
                datos.IdProyecto,
                new DateTime(2026, 6, 10),
                99m));

        contexto.Mantenimientos.AddRange(
            CrearMantenimiento(
                datos,
                datos.IdActivoHoras,
                datos.IdProyecto,
                EstadosMantenimiento.Finalizado,
                new DateTime(2026, 6, 10),
                new DateTime(2026, 6, 10, 8, 0, 0),
                new DateTime(2026, 6, 10, 10, 0, 0),
                80m,
                75m),
            CrearMantenimiento(
                datos,
                datos.IdActivoHoras,
                idProyecto: null,
                EstadosMantenimiento.Programado,
                new DateTime(2026, 6, 20),
                fechaInicio: null,
                fechaFin: null,
                20m,
                costoReal: null),
            CrearMantenimiento(
                datos,
                datos.IdActivoKilometros,
                datos.IdProyecto,
                EstadosMantenimiento.Programado,
                new DateTime(2026, 6, 20),
                fechaInicio: null,
                fechaFin: null,
                900m,
                costoReal: null));
        await contexto.SaveChangesAsync();

        var servicio =
            new ReporteUtilizacionCostosService(contexto);
        var reporte = await servicio.GenerarPorActivoAsync(
            datos.IdActivoHoras,
            new FiltroPeriodoReporte());

        Assert.Equal("Activo", reporte.Encabezado.TipoReporte);
        Assert.Equal(datos.IdActivoHoras, reporte.Encabezado.IdReferencia);
        Assert.Equal(2, reporte.RegistrosUso.Count);
        Assert.Contains(
            reporte.RegistrosUso,
            registro => registro.IdProyecto == datos.IdProyecto);
        Assert.Contains(
            reporte.RegistrosUso,
            registro => registro.IdProyecto is null);
        Assert.Equal(
            10m,
            Assert.Single(reporte.TotalesUtilizacion)
                .CantidadUsoTotal);

        Assert.Equal(2, reporte.Mantenimientos.Count);
        Assert.Contains(
            reporte.Mantenimientos,
            mantenimiento =>
                mantenimiento.IdProyecto == datos.IdProyecto);
        Assert.Contains(
            reporte.Mantenimientos,
            mantenimiento => mantenimiento.IdProyecto is null);
        Assert.Equal(100m, reporte.CostoEstimadoTotal);
        Assert.Equal(75m, reporte.CostoRealTotal);

        var vacio = await servicio.GenerarPorActivoAsync(
            datos.IdActivoVacio,
            new FiltroPeriodoReporte
            {
                FechaDesde = new DateTime(2026, 1, 1)
            });

        Assert.Equal(datos.IdActivoVacio, vacio.Encabezado.IdReferencia);
        Assert.Empty(vacio.RegistrosUso);
        Assert.Empty(vacio.TotalesUtilizacion);
        Assert.Empty(vacio.Mantenimientos);
        Assert.Equal(0m, vacio.CostoEstimadoTotal);
        Assert.Equal(0m, vacio.CostoRealTotal);
    }

    [MySqlQaFact]
    public async Task Reportes_RechazanIdentificadoresInvalidosOInactivos()
    {
        await using var contexto = CrearContexto();
        var datos = await PrepararDatosAsync(contexto);
        var servicio =
            new ReporteUtilizacionCostosService(contexto);
        var filtro = new FiltroPeriodoReporte();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.GenerarPorProyectoAsync(0, filtro));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.GenerarPorActivoAsync(0, filtro));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.GenerarPorProyectoAsync(
                datos.IdProyectoInactivo,
                filtro));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.GenerarPorActivoAsync(
                datos.IdActivoInactivo,
                filtro));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.GenerarPorProyectoAsync(
                long.MaxValue,
                filtro));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.GenerarPorActivoAsync(
                long.MaxValue,
                filtro));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.GenerarPorProyectoAsync(
                datos.IdProyecto,
                new FiltroPeriodoReporte
                {
                    FechaDesde = new DateTime(2026, 8, 1),
                    FechaHasta = new DateTime(2026, 7, 31)
                }));
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

    private static async Task<DatosReporteQa> PrepararDatosAsync(
        ApplicationDbContext contexto)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..10];
        var categoria = await contexto.CategoriasActivo
            .AsNoTracking()
            .Where(item =>
                item.EstadoRegistro == EstadosRegistro.Activo
                && item.TipoActivo != null
                && item.TipoActivo.EstadoRegistro
                    == EstadosRegistro.Activo)
            .OrderBy(item => item.IdCategoriaActivo)
            .Select(item => new
            {
                item.IdCategoriaActivo,
                item.IdTipoActivo
            })
            .FirstAsync();
        var idEstadoActivo = await contexto.EstadosActivo
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(estado => estado.IdEstadoActivo)
            .Select(estado => estado.IdEstadoActivo)
            .FirstAsync();
        var idEstadoProyecto = await contexto.EstadosProyecto
            .AsNoTracking()
            .Where(estado =>
                estado.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(estado => estado.IdEstadoProyecto)
            .Select(estado => estado.IdEstadoProyecto)
            .FirstAsync();
        var tiposMedicion = await contexto.TiposMedicionUso
            .AsNoTracking()
            .Where(tipo =>
                tipo.EstadoRegistro == EstadosRegistro.Activo
                && tipo.Nombre != TiposMedicionUso.NoAplica)
            .OrderBy(tipo => tipo.IdTipoMedicionUso)
            .Take(2)
            .ToListAsync();
        var estadosMantenimiento =
            await contexto.EstadosMantenimiento
                .AsNoTracking()
                .Where(estado =>
                    estado.EstadoRegistro == EstadosRegistro.Activo)
                .ToDictionaryAsync(
                    estado => estado.Nombre,
                    estado => estado.IdEstadoMantenimiento);

        Assert.Equal(2, tiposMedicion.Count);
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

        var proyecto = CrearProyecto(
            $"QA-RP-{sufijo}",
            $"Proyecto reporte {sufijo}",
            idEstadoProyecto);
        var otroProyecto = CrearProyecto(
            $"QA-RP-O-{sufijo}",
            $"Otro proyecto {sufijo}",
            idEstadoProyecto);
        var proyectoInactivo = CrearProyecto(
            $"QA-RP-I-{sufijo}",
            $"Proyecto inactivo {sufijo}",
            idEstadoProyecto,
            EstadosRegistro.Inactivo);
        var activoHoras = CrearActivo(
            $"QA-RP-H-{sufijo}",
            categoria.IdTipoActivo,
            categoria.IdCategoriaActivo,
            idEstadoActivo,
            tiposMedicion[0].IdTipoMedicionUso);
        var activoKilometros = CrearActivo(
            $"QA-RP-K-{sufijo}",
            categoria.IdTipoActivo,
            categoria.IdCategoriaActivo,
            idEstadoActivo,
            tiposMedicion[1].IdTipoMedicionUso);
        var activoVacio = CrearActivo(
            $"QA-RP-V-{sufijo}",
            categoria.IdTipoActivo,
            categoria.IdCategoriaActivo,
            idEstadoActivo,
            tiposMedicion[0].IdTipoMedicionUso);
        var activoInactivo = CrearActivo(
            $"QA-RP-I-{sufijo}",
            categoria.IdTipoActivo,
            categoria.IdCategoriaActivo,
            idEstadoActivo,
            tiposMedicion[0].IdTipoMedicionUso,
            EstadosRegistro.Inactivo);

        contexto.Proyectos.AddRange(
            proyecto,
            otroProyecto,
            proyectoInactivo);
        contexto.Activos.AddRange(
            activoHoras,
            activoKilometros,
            activoVacio,
            activoInactivo);
        await contexto.SaveChangesAsync();

        return new DatosReporteQa(
            proyecto.IdProyecto,
            proyecto.CodigoProyecto,
            otroProyecto.IdProyecto,
            proyectoInactivo.IdProyecto,
            activoHoras.IdActivo,
            activoKilometros.IdActivo,
            activoVacio.IdActivo,
            activoInactivo.IdActivo,
            tiposMedicion[0].Nombre,
            tiposMedicion[1].Nombre,
            estadosMantenimiento);
    }

    private static Proyecto CrearProyecto(
        string codigo,
        string nombre,
        int idEstadoProyecto,
        string estadoRegistro = EstadosRegistro.Activo)
    {
        return new Proyecto
        {
            CodigoProyecto = codigo,
            NombreProyecto = nombre,
            IdEstadoProyecto = idEstadoProyecto,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = estadoRegistro
        };
    }

    private static Activo CrearActivo(
        string codigo,
        int idTipoActivo,
        int idCategoriaActivo,
        int idEstadoActivo,
        int idTipoMedicionUso,
        string estadoRegistro = EstadosRegistro.Activo)
    {
        return new Activo
        {
            CodigoActivo = codigo,
            NombreActivo = $"Activo {codigo}",
            IdTipoActivo = idTipoActivo,
            IdCategoriaActivo = idCategoriaActivo,
            IdEstadoActivo = idEstadoActivo,
            IdTipoMedicionUso = idTipoMedicionUso,
            UbicacionActual = "Patio reportes QA",
            FechaCreacion = DateTime.Now,
            EstadoRegistro = estadoRegistro
        };
    }

    private static RegistroUsoActivo CrearRegistroUso(
        long idActivo,
        long? idProyecto,
        DateTime fecha,
        decimal cantidad,
        string estadoRegistro = EstadosRegistro.Activo)
    {
        return new RegistroUsoActivo
        {
            IdActivo = idActivo,
            IdProyecto = idProyecto,
            FechaRegistro = fecha.Date,
            LecturaAnterior = 0m,
            LecturaNueva = cantidad,
            CantidadUso = cantidad,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = estadoRegistro
        };
    }

    private static Mantenimiento CrearMantenimiento(
        DatosReporteQa datos,
        long idActivo,
        long? idProyecto,
        string estado,
        DateTime fechaProgramada,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        decimal? costoEstimado,
        decimal? costoReal,
        string estadoRegistro = EstadosRegistro.Activo)
    {
        return new Mantenimiento
        {
            IdActivo = idActivo,
            IdProyecto = idProyecto,
            TipoMantenimiento = TiposMantenimiento.Correctivo,
            IdEstadoMantenimiento =
                datos.EstadosMantenimiento[estado],
            FechaProgramada = fechaProgramada.Date,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Descripcion = $"Mantenimiento {estado} QA",
            CostoEstimado = costoEstimado,
            CostoReal = costoReal,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = estadoRegistro
        };
    }

    private sealed record DatosReporteQa(
        long IdProyecto,
        string CodigoProyecto,
        long IdOtroProyecto,
        long IdProyectoInactivo,
        long IdActivoHoras,
        long IdActivoKilometros,
        long IdActivoVacio,
        long IdActivoInactivo,
        string TipoMedicionHoras,
        string TipoMedicionKilometros,
        IReadOnlyDictionary<string, int> EstadosMantenimiento);
}

using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Operaciones;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class ActivosAcademicoTests(ServidorAcademico servidor)
{
    private static readonly DateTime Inicio = EntornoAcademico.Inicio;
    [Fact(DisplayName = "ACT-001 — Registrar y clasificar activo"), Trait("HU", "HU-ACT-001")]
    public async Task Act001_RegistrarActivoDisponible()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ActivoAsync();
        var activo = Assert.IsType<ActivoResumen>(await e.Obtener<IActivoService>().ObtenerActivoPorIdAsync(id));
        Assert.Equal("ACT-ACA-01", activo.CodigoActivo); Assert.Equal("Disponible", activo.EstadoActivo);
        Assert.False(string.IsNullOrWhiteSpace(activo.TipoActivo)); Assert.False(string.IsNullOrWhiteSpace(activo.CategoriaActivo));
        await using var lectura = e.LecturaNueva(); Assert.Equal("Bodega académica", (await lectura.Activos.SingleAsync(a => a.IdActivo == id)).UbicacionActual);
    }

    [Fact(DisplayName = "ACT-002 — Estado, ubicación y trazabilidad"), Trait("HU", "HU-ACT-002")]
    public async Task Act002_ActualizarEstadoYUbicacion()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ActivoAsync(); var s = e.Obtener<IActivoService>();
        await s.ActualizarEstadoUbicacionAsync(new() { IdActivo = id, IdEstadoActivo = (await s.ObtenerEstadosActivoAsync()).Single(x => x.Nombre == "Fuera de servicio").IdEstadoActivo,
            UbicacionActual = "Taller académico" }, EntornoAcademico.Ops);
        var activo = Assert.IsType<ActivoResumen>(await s.ObtenerActivoPorIdAsync(id));
        Assert.Equal("Fuera de servicio", activo.EstadoActivo); Assert.Equal("Taller académico", activo.UbicacionActual);
        Assert.Contains(await s.ObtenerTrazabilidadActivoAsync(id), x => x.IdUsuario == EntornoAcademico.Ops && x.DescripcionCambio.Contains("Taller académico"));
    }

    [Fact(DisplayName = "ACT-003 — Disponibilidad según fechas y estado"), Trait("HU", "HU-ACT-003")]
    public async Task Act003_ConsultarDisponibilidadPorFechasYEstado()
    {
        await using var e = await servidor.NuevoAsync(); var libre = await e.ActivoAsync("ACT-LIBRE"); var reservado = await e.ActivoAsync("ACT-RESERVA");
        var fuera = await e.ActivoAsync("ACT-FUERA"); var proyecto = await e.ProyectoAsync();
        var activos = e.Obtener<IActivoService>(); var s = e.Obtener<IAsignacionActivoProyectoService>();
        var estadoFuera = (await activos.ObtenerEstadosActivoAsync()).Single(x => x.Nombre == "Fuera de servicio").IdEstadoActivo;
        await activos.ActualizarEstadoUbicacionAsync(new() { IdActivo = fuera, IdEstadoActivo = estadoFuera }, EntornoAcademico.Ops);
        await s.CrearAsignacionAsync(new() { IdActivo = reservado, IdProyecto = proyecto, FechaInicio = Inicio, FechaFin = Inicio.AddDays(4) }, EntornoAcademico.Ops);
        var filas = await s.ConsultarDisponibilidadAsync(new() { FechaInicio = Inicio, FechaFin = Inicio.AddDays(2) });
        Assert.True(filas.Single(x => x.IdActivo == libre).EstaDisponible); Assert.False(filas.Single(x => x.IdActivo == reservado).EstaDisponible);
        Assert.False(filas.Single(x => x.IdActivo == fuera).EstaDisponible);
        var posterior = await s.ConsultarDisponibilidadAsync(new() { FechaInicio = Inicio.AddDays(5), FechaFin = Inicio.AddDays(8) });
        Assert.True(posterior.Single(x => x.IdActivo == reservado).EstaDisponible);
        var filtrado = await s.ConsultarDisponibilidadAsync(new() { FechaInicio = Inicio, FechaFin = Inicio.AddDays(2), IdEstadoActivo = estadoFuera });
        Assert.Equal(fuera, Assert.Single(filtrado).IdActivo);
    }

    [Fact(DisplayName = "ACT-004 — Crear y actualizar proyecto"), Trait("HU", "HU-ACT-004")]
    public async Task Act004_CrearYActualizarProyecto()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ProyectoAsync(); var s = e.Obtener<IProyectoService>();
        var inicial = await s.ObtenerProyectoPorIdAsync(id); Assert.NotNull(inicial); Assert.Equal("Planificado", inicial.EstadoProyecto);
        var estado = (await s.ObtenerEstadosAsync()).Single(x => x.Nombre == "En ejecución");
        await s.GuardarProyectoAsync(new() { IdProyecto = id, CodigoProyecto = "PRO-ACA-01", NombreProyecto = "Proyecto actualizado QA",
            FechaInicio = Inicio, FechaFinEstimada = Inicio.AddMonths(2), IdEstadoProyecto = estado.IdEstadoProyecto,
            Responsable = "Responsable sintético", Ubicacion = "Zona académica" }, EntornoAcademico.Ops);
        var actual = await s.ObtenerProyectoPorIdAsync(id); Assert.NotNull(actual);
        Assert.Equal("Proyecto actualizado QA", actual.NombreProyecto); Assert.Equal("En ejecución", actual.EstadoProyecto);
        await using var lectura = e.LecturaNueva(); Assert.Equal("Zona académica", (await lectura.Proyectos.SingleAsync(p => p.IdProyecto == id)).Ubicacion);
    }

    [Fact(DisplayName = "ACT-005 — Asignar activo libre"), Trait("HU", "HU-ACT-005")]
    public async Task Act005_AsignarActivoDisponibleAProyecto()
    {
        await using var e = await servidor.NuevoAsync(); var activo = await e.ActivoAsync(); var proyecto = await e.ProyectoAsync(); var s = e.Obtener<IAsignacionActivoProyectoService>();
        var id = await s.CrearAsignacionAsync(new() { IdActivo = activo, IdProyecto = proyecto, FechaInicio = Inicio, FechaFin = Inicio.AddDays(4) }, EntornoAcademico.Ops);
        var fila = Assert.Single(await s.ObtenerAsignacionesAsync(Inicio, Inicio.AddDays(6), proyecto));
        Assert.Equal(id, fila.IdAsignacionActivoProyecto); Assert.Equal(activo, fila.IdActivo); Assert.Equal(Inicio.AddDays(4), fila.FechaFin);
        Assert.DoesNotContain(await s.ObtenerActivosDisponiblesAsync(Inicio, Inicio.AddDays(4)), x => x.IdActivo == activo);
    }

    [Fact(DisplayName = "ACT-006 — Rechazar asignación solapada"), Trait("HU", "HU-ACT-005")]
    public async Task Act006_RechazarAsignacionSolapada()
    {
        await using var e = await servidor.NuevoAsync(); var activo = await e.ActivoAsync(); var p1 = await e.ProyectoAsync(); var p2 = await e.ProyectoAsync("PRO-ACA-02");
        var s = e.Obtener<IAsignacionActivoProyectoService>();
        var original = await s.CrearAsignacionAsync(new() { IdActivo = activo, IdProyecto = p1, FechaInicio = Inicio, FechaFin = Inicio.AddDays(5) }, EntornoAcademico.Ops);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => s.CrearAsignacionAsync(new() { IdActivo = activo, IdProyecto = p2,
            FechaInicio = Inicio.AddDays(3), FechaFin = Inicio.AddDays(7) }, EntornoAcademico.Ops));
        Assert.False(string.IsNullOrWhiteSpace(error.Message)); var unica = Assert.Single(await s.ObtenerAsignacionesAsync(Inicio, Inicio.AddDays(10)));
        Assert.Equal(original, unica.IdAsignacionActivoProyecto); Assert.Equal(p1, unica.IdProyecto); Assert.Equal(Inicio.AddDays(5), unica.FechaFin);
    }

    [Fact(DisplayName = "ACT-007 — Programar mantenimiento preventivo"), Trait("HU", "HU-ACT-006")]
    public async Task Act007_DefinirMantenimientoPreventivo()
    {
        await using var e = await servidor.NuevoAsync(); var activo = await e.ActivoAsync(); var s = e.Obtener<IMantenimientoService>();
        var fecha = DateTime.Today.AddDays(30); // Relación futura requerida por el producto, sin hora ni esperas.
        var id = await s.DefinirMantenimientoPreventivoAsync(new() { IdActivo = activo, FechaProgramada = fecha, Descripcion = "Revisión académica" }, EntornoAcademico.Ops);
        var orden = Assert.Single(await s.ObtenerMantenimientosPreventivosPendientesAsync());
        Assert.Equal(id, orden.IdMantenimiento); Assert.Equal(activo, orden.IdActivo); Assert.Equal("Programado", orden.EstadoMantenimiento); Assert.Equal(fecha, orden.FechaProgramada);
    }

    [Fact(DisplayName = "ACT-008 — Completar mantenimiento correctivo"), Trait("HU", "HU-ACT-007")]
    public async Task Act008_CompletarOrdenCorrectivaConEvidencia()
    {
        await using var e = await servidor.NuevoAsync(); var activo = await e.ActivoAsync(); var s = e.Obtener<IMantenimientoService>();
        var id = await s.CrearOrdenCorrectivaAsync(new() { IdActivo = activo, FechaProgramada = DateTime.Today,
            Descripcion = "Reparación académica", CostoEstimado = 1800, Responsable = "Técnico sintético" }, EntornoAcademico.Ops);
        Assert.Equal("Programado", (await s.ObtenerMantenimientoPorIdAsync(id))!.EstadoMantenimiento);
        await s.IniciarOrdenAsync(id, EntornoAcademico.Ops); Assert.Equal("En proceso", (await s.ObtenerMantenimientoPorIdAsync(id))!.EstadoMantenimiento);
        var pdf = EntornoAcademico.PdfSintetico();
        using var cierre = new SolicitudCerrarOrdenMantenimiento { IdMantenimiento = id, CostoReal = 1750, TiempoFueraServicioHoras = 4,
            Resultado = "Equipo reparado", Evidencias = [new() { NombreOriginal = "reparacion.pdf", MimeType = "application/pdf", TamanoBytes = pdf.Length, Contenido = new MemoryStream(pdf) }] };
        await s.FinalizarOrdenAsync(cierre, EntornoAcademico.Ops);
        var final = Assert.Single(await s.ObtenerHistorialActivoAsync(activo)); Assert.Equal("Finalizado", final.EstadoMantenimiento);
        Assert.Equal(1750m, final.CostoReal); Assert.Equal(4m, final.TiempoFueraServicioHoras); Assert.Equal("Equipo reparado", final.Resultado);
        var evidencia = Assert.Single(final.Evidencias); var descarga = await s.AbrirEvidenciaAsync(evidencia.IdDocumentoArchivo); Assert.NotNull(descarga);
        using var bytes = new MemoryStream(); await descarga.Contenido.CopyToAsync(bytes); await descarga.Contenido.DisposeAsync(); Assert.Equal(pdf, bytes.ToArray());
    }

    [Fact(DisplayName = "ACT-009 — Acumular uso y consultar inactividad"), Trait("HU", "HU-ACT-008")]
    public async Task Act009_AcumularLecturasYConsultarTiempoFueraServicio()
    {
        await using var e = await servidor.NuevoAsync(); var activo = await e.ActivoAsync(); var s = e.Obtener<IRegistroUsoActivoService>();
        // Antecedente sintético del expediente, separado de las lecturas que se ejercitan.
        e.Db.Mantenimientos.Add(new() { IdActivo = activo, TipoMantenimiento = "Correctivo",
            IdEstadoMantenimiento = await e.Db.EstadosMantenimiento.Where(x => x.Nombre == "Finalizado").Select(x => x.IdEstadoMantenimiento).SingleAsync(),
            FechaProgramada = new(2024, 1, 1), FechaInicio = new(2024, 1, 1), FechaFin = new(2024, 1, 2), TiempoFueraServicioHoras = 4, CostoReal = 1750,
            FechaCreacion = new(2024, 1, 1), CreadoPor = EntornoAcademico.Ops, EstadoRegistro = "Activo" });
        await e.Db.SaveChangesAsync();
        var tipo = (await s.ObtenerTiposMedicionUsoAsync()).Single(x => x.Nombre == "Horas").IdTipoMedicionUso;
        await s.RegistrarUsoAsync(new() { IdActivo = activo, IdTipoMedicionUso = tipo, FechaRegistro = new(2024, 2, 1), LecturaInicial = 100, LecturaNueva = 112 }, EntornoAcademico.Ops);
        await s.RegistrarUsoAsync(new() { IdActivo = activo, FechaRegistro = new(2024, 2, 2), LecturaNueva = 120 }, EntornoAcademico.Ops);
        var resumen = await s.ObtenerResumenUsoActivoAsync(activo); Assert.NotNull(resumen);
        Assert.Equal(120m, resumen.LecturaUsoActual); Assert.Equal(20m, resumen.TotalUsoRegistrado); Assert.Equal(2, resumen.Registros.Count);
        Assert.Equal(4m, Assert.Single(resumen.PeriodosFueraServicio).TiempoFueraServicioHoras);
    }
}

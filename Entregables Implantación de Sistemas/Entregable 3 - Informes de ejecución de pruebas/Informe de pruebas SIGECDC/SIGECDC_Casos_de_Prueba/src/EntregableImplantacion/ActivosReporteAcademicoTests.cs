using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class ActivosReporteAcademicoTests(ServidorAcademico servidor)
{
    [Fact(DisplayName = "ACT-010 — Utilización y costos por proyecto, activo y período"), Trait("HU", "HU-ACT-009")]
    public async Task Act010_ReportesFiltradosConTotalesIndependientes()
    {
        await using var e = await servidor.NuevoAsync(); var a = await e.ActivoAsync("ACT-REPORTE-A"); var b = await e.ActivoAsync("ACT-REPORTE-B");
        var p = await e.ProyectoAsync("PRO-REPORTE-A"); var q = await e.ProyectoAsync("PRO-REPORTE-B");
        // Antecedentes históricos exclusivamente del fixture local. El SUT ejercitado es el reporte.
        var tipo = (await e.Obtener<IRegistroUsoActivoService>().ObtenerTiposMedicionUsoAsync()).Single(x => x.Nombre == "Horas").IdTipoMedicionUso;
        foreach (var id in new[] { a, b }) (await e.Db.Activos.SingleAsync(x => x.IdActivo == id)).IdTipoMedicionUso = tipo;
        foreach (var (activo, proyecto, fecha, anterior, nueva) in new[] {
            (a,p,new DateTime(2024,1,10),100m,110m), (a,p,new DateTime(2024,1,20),110m,125m),
            (a,p,new DateTime(2024,2,10),125m,140m), (b,q,new DateTime(2024,1,12),200m,220m) })
            e.Db.RegistrosUsoActivo.Add(new() { IdActivo = activo, IdProyecto = proyecto, FechaRegistro = fecha, LecturaAnterior = anterior,
                LecturaNueva = nueva, CantidadUso = nueva - anterior, RegistradoPor = EntornoAcademico.Ops, FechaCreacion = fecha, EstadoRegistro = "Activo" });
        var finalizado = await e.Db.EstadosMantenimiento.Where(x => x.Nombre == "Finalizado").Select(x => x.IdEstadoMantenimiento).SingleAsync();
        foreach (var (activo, proyecto, fecha, costo, estimado) in new[] {
            (a,p,new DateTime(2024,1,15),150m,200m), (a,p,new DateTime(2024,2,15),700m,900m), (b,q,new DateTime(2024,1,17),900m,1000m) })
            e.Db.Mantenimientos.Add(new() { IdActivo = activo, IdProyecto = proyecto, TipoMantenimiento = "Correctivo", IdEstadoMantenimiento = finalizado,
                FechaProgramada = fecha, FechaInicio = fecha, FechaFin = fecha, FechaCreacion = fecha, CostoReal = costo, CostoEstimado = estimado,
                CreadoPor = EntornoAcademico.Ops, EstadoRegistro = "Activo", Descripcion = "Antecedente ACT-010" });
        await e.Db.SaveChangesAsync(); var s = e.Obtener<IReporteUtilizacionCostosService>();
        var filtro = new FiltroPeriodoReporte { FechaDesde = new(2024,1,1), FechaHasta = new(2024,1,31) };
        var proyectoA = await s.GenerarPorProyectoAsync(p, filtro); var activoA = await s.GenerarPorActivoAsync(a, filtro);
        foreach (var reporte in new[] { proyectoA, activoA })
        {
            Assert.Equal(2, reporte.RegistrosUso.Count); Assert.All(reporte.RegistrosUso, x => Assert.Equal(a, x.IdActivo));
            Assert.Equal(25m, Assert.Single(reporte.TotalesUtilizacion).CantidadUsoTotal);
            Assert.Equal(150m, reporte.CostoRealTotal); Assert.Equal(200m, reporte.CostoEstimadoTotal);
            Assert.Equal(a, Assert.Single(reporte.Mantenimientos).IdActivo);
        }
        Assert.Equal(p, proyectoA.Encabezado.IdReferencia); Assert.Equal(a, activoA.Encabezado.IdReferencia);
        var otro = await s.GenerarPorProyectoAsync(q, filtro); Assert.Equal(20m, Assert.Single(otro.TotalesUtilizacion).CantidadUsoTotal); Assert.Equal(900m, otro.CostoRealTotal);
        var completo = await s.GenerarPorActivoAsync(a, new()); Assert.Equal(40m, Assert.Single(completo.TotalesUtilizacion).CantidadUsoTotal); Assert.Equal(850m, completo.CostoRealTotal);
        var vacio = await s.GenerarPorActivoAsync(a, new() { FechaDesde = new(2025,1,1), FechaHasta = new(2025,1,31) });
        Assert.Empty(vacio.RegistrosUso); Assert.Empty(vacio.Mantenimientos); Assert.Equal(0m, vacio.CostoRealTotal);
    }
}

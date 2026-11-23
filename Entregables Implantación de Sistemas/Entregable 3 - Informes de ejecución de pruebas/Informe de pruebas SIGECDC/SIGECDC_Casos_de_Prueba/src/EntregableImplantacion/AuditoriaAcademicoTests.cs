using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Auditoria;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class AuditoriaAcademicoTests(ServidorAcademico servidor)
{
    [Fact(DisplayName = "AUD-001 — Historial de acceso y contacto de solo lectura"), Trait("HU", "HU-INFO-008")]
    public async Task Aud001_ConsultarHistorialAccesosYContactoSinEdicion()
    {
        await using var e = await servidor.NuevoAsync(); await e.Obtener<IConsultaContactoService>().RegistrarConsultaAsync(new()
            { Nombre = "Visitante académico", CorreoElectronico = "historial@example.invalid", Mensaje = "Consulta AUD-001" });
        await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var login = await WebAcademica.IngresarAsync(cliente, EntornoAcademico.Admin); Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var s = e.Obtener<IHistorialSistemaService>();
        Assert.Contains(await s.ObtenerAccesosAsync(accion: "Acceso exitoso"), a => a.IdUsuario == EntornoAcademico.Admin);
        Assert.Single(await s.ObtenerFormulariosContactoAsync(busqueda: "Consulta AUD-001"));
        var antes = await e.Db.BitacoraAuditoria.CountAsync(); var html = await cliente.GetStringAsync("/admin/historial");
        Assert.Contains("Consulta AUD-001", WebUtility.HtmlDecode(html)); SoloLectura(html);
        Assert.Equal(antes, await e.Db.BitacoraAuditoria.CountAsync()); Assert.Equal(1, await e.Db.ConsultasContacto.CountAsync());
    }

    [Fact(DisplayName = "AUD-002 — Trazabilidad de RRHH y planilla"), Trait("HU", "HU-RH-010")]
    public async Task Aud002_ConsultarTrazabilidadDeRecursosHumanosYPlanilla()
    {
        await using var e = await servidor.NuevoAsync(); var d = new DatosFinancierosAcademicos(e); await d.PrepararAsync(); await d.CalcularAsync(aprobar: true);
        var datos = await e.DatosColaboradorAsync(); datos.Telefono = "22222222";
        await e.Obtener<IColaboradorService>().ActualizarColaboradorAsync(d.Colaboradores[0], datos, EntornoAcademico.Rrhh);
        var s = e.Obtener<IHistorialSistemaService>(); var filas = await s.ObtenerOperacionesRecursosHumanosAsync(new() { Cantidad = 100 });
        Assert.Contains(filas, f => f.Entidad == "Colaborador" && f.IdRegistro == d.Colaboradores[0].ToString() && f.IdUsuario == EntornoAcademico.Rrhh);
        Assert.Contains(filas, f => f.Entidad == "Planilla" && f.Accion == "APROBAR_PLANILLA" && f.IdRegistro == d.Planilla.ToString());
        Assert.All(filas, f => Assert.NotEqual(default, f.FechaHora));
        var filtradas = await s.ObtenerOperacionesRecursosHumanosAsync(new() { Categoria = CategoriasBitacoraRecursosHumanos.Planilla });
        Assert.NotEmpty(filtradas); Assert.All(filtradas, f => Assert.Equal("Planilla", f.Categoria));
        await ComprobarVistaAsync(e, "APROBAR_PLANILLA");
    }

    [Fact(DisplayName = "AUD-003 — Bitácora de operaciones"), Trait("HU", "HU-ACT-010")]
    public async Task Aud003_ConsultarTrazabilidadDeActivosProyectosYAsignaciones()
    {
        await using var e = await servidor.NuevoAsync(); var activo = await e.ActivoAsync(); var proyecto = await e.ProyectoAsync();
        await e.Obtener<IAsignacionActivoProyectoService>().CrearAsignacionAsync(new() { IdActivo = activo, IdProyecto = proyecto,
            FechaInicio = EntornoAcademico.Inicio, FechaFin = EntornoAcademico.Inicio.AddDays(3) }, EntornoAcademico.Ops);
        await e.Obtener<IMantenimientoService>().DefinirMantenimientoPreventivoAsync(new() { IdActivo = activo, FechaProgramada = DateTime.Today.AddDays(30), Descripcion = "Auditoría sintética" }, EntornoAcademico.Ops);
        var s = e.Obtener<IHistorialSistemaService>(); var filas = await s.ObtenerOperacionesAsync(new());
        foreach (var categoria in CategoriasBitacoraOperaciones.Todas)
        {
            Assert.Contains(filas, f => f.Categoria == categoria && f.IdUsuario == EntornoAcademico.Ops && f.FechaHora != default);
            var filtradas = await s.ObtenerOperacionesAsync(new() { Categoria = categoria }); Assert.NotEmpty(filtradas);
            Assert.All(filtradas, f => Assert.Equal(categoria, f.Categoria));
        }
        await ComprobarVistaAsync(e, "Auditoría sintética");
    }

    private static async Task ComprobarVistaAsync(EntornoAcademico e, string evidencia)
    {
        await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var login = await WebAcademica.IngresarAsync(cliente, EntornoAcademico.Admin); Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var antes = await e.Db.BitacoraAuditoria.AsNoTracking().OrderBy(x => x.IdBitacoraAuditoria).Select(x => new { x.IdBitacoraAuditoria, x.Accion, x.ValoresNuevos }).ToArrayAsync();
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/admin/historial")); Assert.Contains(evidencia, html); SoloLectura(html);
        var despues = await e.Db.BitacoraAuditoria.AsNoTracking().OrderBy(x => x.IdBitacoraAuditoria).Select(x => new { x.IdBitacoraAuditoria, x.Accion, x.ValoresNuevos }).ToArrayAsync();
        Assert.Equal(antes, despues);
    }

    private static void SoloLectura(string html)
    {
        // Se comprueban acciones visibles, sin confundir textos históricos con botones de edición.
        var controles = Regex.Matches(html, "<(?:button|a)\\b[^>]*>(.*?)</(?:button|a)>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        Assert.DoesNotContain(controles, c => Regex.IsMatch(c.Groups[1].Value, @"\b(Editar|Eliminar|Borrar|Guardar cambios)\b", RegexOptions.IgnoreCase));
    }
}

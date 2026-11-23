using System.Net;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class IdentidadAcademicoTests(ServidorAcademico servidor)
{
    private static readonly (string Usuario, string Ruta)[] Accesos = [(EntornoAcademico.Admin, "/admin/usuarios"),
        (EntornoAcademico.Rrhh, "/rrhh/colaboradores"), (EntornoAcademico.Ops, "/operaciones/activos"), (EntornoAcademico.Empleado, "/empleado/colillas")];

    [Fact(DisplayName = "AUTH-001 — Acceso válido con los cuatro roles"), Trait("HU", "Transversal")]
    public async Task Auth001_IniciarSesionConCredencialesValidas()
    {
        await using var e = await servidor.NuevoAsync(); await using var web = await WebAcademica.IniciarAsync(e);
        foreach (var acceso in Accesos)
        {
            using var cliente = web.Cliente(); using var login = await WebAcademica.IngresarAsync(cliente, acceso.Usuario);
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            using var pagina = await cliente.GetAsync(acceso.Ruta); Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
            Assert.DoesNotContain("Error: credenciales inválidas", WebUtility.HtmlDecode(await pagina.Content.ReadAsStringAsync()));
        }
        await using var lectura = e.LecturaNueva(); Assert.Equal(4, await lectura.BitacoraAuditoria.CountAsync(b => b.Accion == "Acceso exitoso"));
    }

    [Fact(DisplayName = "AUTH-002 — Contraseña inválida rechazada"), Trait("HU", "Transversal")]
    public async Task Auth002_RechazarCredencialesInvalidas()
    {
        await using var e = await servidor.NuevoAsync(); await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var login = await WebAcademica.IngresarAsync(cliente, EntornoAcademico.Admin, "ClaveIncorrecta!42");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode); Assert.Contains("credenciales inválidas", WebUtility.HtmlDecode(await login.Content.ReadAsStringAsync()));
        using var protegida = await cliente.GetAsync("/admin/usuarios"); ExigirLogin(protegida);
        await using var lectura = e.LecturaNueva(); Assert.False(await lectura.BitacoraAuditoria.AnyAsync(b => b.Accion == "Acceso exitoso"));
        Assert.True(await lectura.BitacoraAuditoria.AnyAsync(b => b.Accion == "Acceso fallido"));
    }

    [Fact(DisplayName = "AUTH-003 — Cierre de sesión efectivo"), Trait("HU", "Transversal")]
    public async Task Auth003_CerrarSesionYBloquearReingresoDirecto()
    {
        await using var e = await servidor.NuevoAsync(); await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        using var ingreso = await WebAcademica.IngresarAsync(cliente, EntornoAcademico.Admin); Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);
        using var pagina = await cliente.GetAsync("/admin/usuarios"); Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        var ocultos = WebAcademica.Ocultos(await pagina.Content.ReadAsStringAsync()); ocultos["returnUrl"] = "";
        using var salida = await cliente.PostAsync("/Account/Logout", new FormUrlEncodedContent(ocultos)); Assert.Equal(HttpStatusCode.Redirect, salida.StatusCode);
        using var protegida = await cliente.GetAsync("/admin/usuarios"); ExigirLogin(protegida);
    }

    [Fact(DisplayName = "AUTH-004 — Separación representativa de permisos"), Trait("HU", "Transversal")]
    public async Task Auth004_AutorizarFuncionesPropiasYDenegarOtroRol()
    {
        await using var e = await servidor.NuevoAsync(); await using var web = await WebAcademica.IniciarAsync(e);
        foreach (var (usuario, permitidas, prohibidas) in new[]
        {
            (EntornoAcademico.Admin, new[] { "/admin/usuarios", "/admin/contenido-paginas", "/admin/historial" }, Array.Empty<string>()),
            (EntornoAcademico.Rrhh, new[] { "/rrhh/colaboradores", "/rrhh/periodos-planilla", "/rrhh/forecast" }, Array.Empty<string>()),
            (EntornoAcademico.Ops, new[] { "/operaciones/activos", "/operaciones/proyectos" }, new[] { "/rrhh/periodos-planilla", "/rrhh/forecast" }),
            (EntornoAcademico.Empleado, new[] { "/empleado/colillas" }, new[] { "/admin/usuarios", "/admin/historial" })
        })
        {
            using var cliente = web.Cliente(); using var login = await WebAcademica.IngresarAsync(cliente, usuario); Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            foreach (var ruta in permitidas) { using var respuesta = await cliente.GetAsync(ruta); Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); }
            foreach (var ruta in prohibidas)
            {
                using var respuesta = await cliente.GetAsync(ruta);
                Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden || respuesta.StatusCode == HttpStatusCode.Redirect &&
                    respuesta.Headers.Location?.ToString().Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) == true, $"El servidor permitió o no resolvió correctamente la ruta protegida {ruta}.");
            }
        }
    }

    private static void ExigirLogin(HttpResponseMessage respuesta)
    { Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode); Assert.Contains("/Account/Login", respuesta.Headers.Location?.ToString()); }
}

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Tests.EntregableImplantacion.Soporte;
using SIGECDC.Web.Components.Pages;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class PublicoAcademicoTests(ServidorAcademico servidor)
{
    [Fact(DisplayName = "PUB-001 — Páginas institucionales anónimas"), Trait("HU", "HU-INFO-001")]
    public async Task Pub001_ConsultarPaginasInstitucionalesSinSesion()
    {
        if (ConexionRevisionAcademica.EsRemoto)
        {
            using var remoto = ConexionRevisionAcademica.ClienteRemoto();
            await ComprobarPaginasAsync(remoto);
            return;
        }
        await using var e = await servidor.NuevoAsync(); await using var web = await WebAcademica.IniciarAsync(e);
        using var cliente = web.Cliente();
        await ComprobarPaginasAsync(cliente);
    }

    private static async Task ComprobarPaginasAsync(HttpClient cliente)
    {
        foreach (var (ruta, texto) in new[] { ("/", "Construimos soluciones"), ("/quienes-somos", "Nuestra identidad"),
            ("/servicios", "Construcción de puentes"), ("/contacto", "Enviar consulta") })
        {
            using var respuesta = await cliente.GetAsync(ruta); Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()); Assert.Contains(texto, html);
            foreach (var enlace in new[] { "quienes-somos", "servicios", "contacto" }) Assert.Contains($"href=\"{enlace}\"", html);
        }
        using var inicio = await cliente.GetAsync("/"); Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
    }

    [Fact(DisplayName = "PUB-002 — Filtro y detalle del portafolio"), Trait("HU", "HU-INFO-002")]
    public async Task Pub002_FiltrarPortafolioYConsultarDetalle()
    {
        await using var e = await servidor.NuevoAsync(); var s = e.Obtener<IProyectoPublicadoService>();
        foreach (var (titulo, estado, publicado) in new[] { ("Obra Alfa", "En ejecución", true), ("Obra Beta", "Finalizado", true), ("Obra Oculta", "Finalizado", false) })
            await s.GuardarProyectoPublicadoAsync(new() { Titulo = titulo, Descripcion = "Detalle " + titulo, EstadoVisual = estado, EstaPublicado = publicado });
        await using var ui = new ComponenteAcademico(e.Servicios); await ui.MostrarAsync<Portafolio>();
        Assert.Contains("Obra Alfa", await ui.TextoAsync()); Assert.DoesNotContain("Obra Oculta", await ui.TextoAsync());
        await ui.EventoAsync("select", "onchange", new ChangeEventArgs { Value = "Finalizado" });
        Assert.Contains("Obra Beta", await ui.TextoAsync()); Assert.DoesNotContain("Obra Alfa", await ui.TextoAsync());
        await ui.EventoAsync("button", "onclick", new MouseEventArgs());
        var detalle = await ui.TextoAsync(); Assert.Contains("Detalle público del proyecto", detalle); Assert.Contains("Detalle Obra Beta", detalle);
        await ui.EventoAsync("select", "onchange", new ChangeEventArgs { Value = "" });
        Assert.Contains("Obra Alfa", await ui.TextoAsync()); Assert.Contains("Obra Beta", await ui.TextoAsync()); Assert.DoesNotContain("Obra Oculta", await ui.TextoAsync());
    }

    [Fact(DisplayName = "PUB-003 — Envío válido de contacto"), Trait("HU", "HU-INFO-003")]
    public async Task Pub003_EnviarConsultaContactoValida()
    {
        await using var e = await servidor.NuevoAsync(); await using var ui = new ComponenteAcademico(e.Servicios);
        await ui.MostrarAsync<Contacto>();
        await ui.EventoAsync("input", "oninput", new ChangeEventArgs { Value = "Visitante sintético" }, "nombre");
        await ui.EventoAsync("input", "oninput", new ChangeEventArgs { Value = "visitante@example.invalid" }, "correo");
        await ui.EventoAsync("textarea", "oninput", new ChangeEventArgs { Value = "Consulta académica sobre drenajes" }, "mensaje");
        await ui.EventoAsync("form", "onsubmit", EventArgs.Empty);
        Assert.Contains("Hemos recibido su consulta", await ui.TextoAsync());
        await using var lectura = e.LecturaNueva(); var guardada = await lectura.ConsultasContacto.SingleAsync();
        Assert.Equal("Visitante sintético", guardada.Nombre); Assert.Equal("visitante@example.invalid", guardada.CorreoElectronico);
        Assert.Equal("Consulta académica sobre drenajes", guardada.Mensaje);
    }

    [Fact(DisplayName = "PUB-004 — Contacto vacío rechazado"), Trait("HU", "HU-INFO-003")]
    public async Task Pub004_RechazarContactoConObligatoriosVacios()
    {
        await using var e = await servidor.NuevoAsync(); await using var ui = new ComponenteAcademico(e.Servicios);
        await ui.MostrarAsync<Contacto>(); await ui.EventoAsync("form", "onsubmit", EventArgs.Empty);
        var texto = await ui.TextoAsync(); Assert.Contains("obligatorio", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hemos recibido su consulta", texto);
        await using var lectura = e.LecturaNueva(); Assert.Empty(await lectura.ConsultasContacto.ToListAsync());
    }

    [Fact(DisplayName = "PUB-005 — Publicación de contenido institucional"), Trait("HU", "HU-INFO-004")]
    public async Task Pub005_CrearActualizarYPublicarContenido()
    {
        await using var e = await servidor.NuevoAsync(); var s = e.Obtener<IPaginaContenidoService>();
        // Liberar la sección que la carga de referencia trae ocupada, solo en esta base desechable.
        var referencia = (await s.ObtenerPaginasAsync()).Single(p => p.CodigoPagina == "SERVICIOS");
        await s.GuardarPaginaAsync(new() { IdPaginaContenido = referencia.IdPaginaContenido, CodigoPagina = "SERVICIOS-BASE",
            Titulo = referencia.Titulo, Contenido = referencia.Contenido, EstaPublicado = false });
        await s.GuardarPaginaAsync(new() { CodigoPagina = "SERVICIOS", Titulo = "Título inicial académico", Contenido = "Texto inicial académico" });
        var pagina = (await s.ObtenerPaginasAsync()).Single(p => p.CodigoPagina == "SERVICIOS");
        Assert.Null(await s.ObtenerPaginaPublicadaAsync("SERVICIOS"));
        await s.GuardarPaginaAsync(new() { IdPaginaContenido = pagina.IdPaginaContenido, CodigoPagina = "SERVICIOS", Titulo = "Servicios académicos actualizados", Contenido = "Drenaje sintético actualizado" });
        await s.CambiarPublicacionAsync(pagina.IdPaginaContenido, true);
        await using var web = await WebAcademica.IniciarAsync(e); using var cliente = web.Cliente();
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/servicios"));
        Assert.Contains("Servicios académicos actualizados", html); Assert.Contains("Drenaje sintético actualizado", html); Assert.DoesNotContain("Texto inicial académico", html);
        await using var lectura = e.LecturaNueva(); Assert.Equal("Drenaje sintético actualizado", (await lectura.PaginasContenido.SingleAsync(p => p.IdPaginaContenido == pagina.IdPaginaContenido)).Contenido);
    }

    [Fact(DisplayName = "PUB-006 — Mostrar y ocultar tarjeta pública"), Trait("HU", "HU-INFO-007")]
    public async Task Pub006_PublicarYOcultarProyecto()
    {
        await using var e = await servidor.NuevoAsync(); var s = e.Obtener<IProyectoPublicadoService>();
        await s.GuardarProyectoPublicadoAsync(new() { Titulo = "Tarjeta académica", Descripcion = "Proyecto sintético", EstadoVisual = "Planificado" });
        var tarjeta = Assert.Single(await s.ObtenerProyectosAsync());
        Assert.Empty(await s.ObtenerProyectosPublicadosAsync());
        await s.CambiarPublicacionAsync(tarjeta.IdProyectoPublicado, true);
        Assert.Equal("Tarjeta académica", Assert.Single(await s.ObtenerProyectosPublicadosAsync()).Titulo);
        await using var web = await WebAcademica.IniciarAsync(e); using var anonimo = web.Cliente();
        Assert.Contains("Tarjeta académica", WebUtility.HtmlDecode(await anonimo.GetStringAsync("/portafolio")));
        await s.CambiarPublicacionAsync(tarjeta.IdProyectoPublicado, false);
        Assert.Empty(await s.ObtenerProyectosPublicadosAsync());
        Assert.DoesNotContain("Tarjeta académica", WebUtility.HtmlDecode(await anonimo.GetStringAsync("/portafolio")));
        Assert.Equal(tarjeta.IdProyectoPublicado, Assert.Single(await s.ObtenerProyectosAsync()).IdProyectoPublicado);
    }
}

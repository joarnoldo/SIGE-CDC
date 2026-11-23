using System.Net;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Tests.EntregableImplantacion.Soporte;
using SIGECDC.Web.Components.Pages;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class PublicoAdicionalAcademicoTests(ServidorAcademico servidor)
{
    [Fact(DisplayName = "PUB-007 — Crear, actualizar, publicar y ocultar noticia"), Trait("HU", "HU-INFO-005")]
    public async Task Pub007_NoticiaActualizadaYVisibilidadPublica()
    {
        await using var e = await servidor.NuevoAsync(); var s = e.Obtener<INoticiaService>();
        var id = await s.RegistrarAsync(new() { Titulo = "Noticia inicial PUB-007", Resumen = "Resumen inicial", Contenido = "Contenido anterior PUB-007" }, EntornoAcademico.Admin);
        var inicial = await s.ObtenerPorIdAsync(id); Assert.NotNull(inicial); Assert.Equal("Noticia inicial PUB-007", inicial.Titulo);
        Assert.Empty(await s.ObtenerPublicadasAsync());
        await s.ActualizarAsync(id, new() { Titulo = "Noticia vigente PUB-007", Resumen = "Resumen actualizado", Contenido = "Contenido actualizado PUB-007" }, EntornoAcademico.Admin);
        await s.PublicarAsync(id, EntornoAcademico.Admin);
        await using var web = await WebAcademica.IniciarAsync(e); using var anonimo = web.Cliente();
        var html = WebUtility.HtmlDecode(await anonimo.GetStringAsync("/noticias"));
        Assert.Contains("Noticia vigente PUB-007", html); Assert.Contains("Contenido actualizado PUB-007", html);
        Assert.DoesNotContain("Contenido anterior PUB-007", html);
        await s.DespublicarAsync(id, EntornoAcademico.Admin);
        Assert.DoesNotContain("Noticia vigente PUB-007", WebUtility.HtmlDecode(await anonimo.GetStringAsync("/noticias")));
        Assert.Equal(id, Assert.Single(await s.ObtenerTodasAdminAsync()).IdNoticia);
    }

    [Fact(DisplayName = "PUB-008 — Galería, archivos y asociación al proyecto"), Trait("HU", "HU-INFO-006")]
    public async Task Pub008_GaleriaPublicadaConImagenesActualizadasYProyecto()
    {
        await using var e = await servidor.NuevoAsync(); var proyecto = await e.ProyectoAsync("PRO-PUB008"); var s = e.Obtener<IGaleriaService>();
        var galeria = await s.RegistrarAsync(new() { Nombre = "Galería PUB-008", Descripcion = "Recursos de referencia del proyecto" }, EntornoAcademico.Admin);
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
        async Task<long> Adjuntar(string titulo)
        {
            using var solicitud = new SolicitudImagenGaleria { IdGaleria = galeria, NombreOriginal = titulo + ".png", Titulo = titulo,
                Descripcion = "Imagen sintética de QA", Orden = 1, MimeType = "image/png", TamanoBytes = png.Length, Contenido = new MemoryStream(png) };
            return await s.AdjuntarImagenAsync(solicitud, EntornoAcademico.Admin);
        }
        var primera = await Adjuntar("Primera PUB-008");
        Assert.Equal(primera, Assert.Single(await s.ObtenerImagenesAsync(galeria)).IdImagenGaleria);
        var segunda = await Adjuntar("Segunda PUB-008");
        await s.ActualizarImagenAsync(segunda, new() { Titulo = "Vigente PUB-008", Descripcion = "Metadatos vigentes", Orden = 2 }, EntornoAcademico.Admin);
        await s.DesactivarImagenAsync(primera, EntornoAcademico.Admin);
        await s.PublicarAsync(galeria, EntornoAcademico.Admin);
        var imagen = Assert.Single(await s.ObtenerImagenesAsync(galeria)); Assert.Equal(segunda, imagen.IdImagenGaleria);
        Assert.Equal("Metadatos vigentes", imagen.Descripcion); Assert.Equal(2, imagen.Orden);
        Assert.True(Directory.GetFiles(e.Archivos, "*", SearchOption.AllDirectories).Length > 0);
        await using var web = await WebAcademica.IniciarAsync(e); using var anonimo = web.Cliente();
        var html = WebUtility.HtmlDecode(await anonimo.GetStringAsync("/galeria"));
        Assert.Contains("Vigente PUB-008", html); Assert.DoesNotContain("Primera PUB-008", html);
        // El archivo se comprueba por el caso de uso real; el host de prueba no replica endpoints definidos en Program.
        var descarga = await s.AbrirImagenPublicaAsync(segunda); Assert.NotNull(descarga);
        using var contenido = new MemoryStream(); await descarga.Contenido.CopyToAsync(contenido); await descarga.Contenido.DisposeAsync();
        Assert.Equal(png, contenido.ToArray()); Assert.Null(await s.AbrirImagenPublicaAsync(primera));
        await using var lectura = e.LecturaNueva(); var persistida = await lectura.Galerias.AsNoTracking().SingleAsync(x => x.IdGaleria == galeria);
        // QA02 exige un vínculo persistido, no una mención del proyecto en el nombre.
        var propiedad = lectura.Model.FindEntityType(typeof(SIGECDC.Domain.SitioPublico.Galeria))!.FindProperty("IdProyecto");
        var vinculacion = propiedad is null ? null : lectura.Entry(persistida).Property("IdProyecto").CurrentValue;
        Assert.True(vinculacion is not null && Convert.ToInt64(vinculacion) == proyecto,
            "QA51-DEF-001: QA02 exige galería asociada a proyecto; el CMS no ofrece ni persiste IdProyecto. CRUD, archivos y publicación anteriores sí se comprobaron.");
    }

    [Fact(DisplayName = "PUB-009 — Políticas, error comprensible y retorno"), Trait("HU", "HU-INFO-009")]
    public async Task Pub009_PoliticasYRetornoDesdeRutaInexistente()
    {
        if (ConexionRevisionAcademica.EsRemoto)
        {
            using var remoto = ConexionRevisionAcademica.ClienteRemoto(); await ComprobarPoliticasYErrorAsync(remoto, null); return;
        }
        await using var e = await servidor.NuevoAsync(); var s = e.Obtener<IPaginaContenidoService>();
        var pagina = (await s.ObtenerPaginasAsync()).Single(p => p.CodigoPagina == "POLITICAS");
        await s.GuardarPaginaAsync(new() { IdPaginaContenido = pagina.IdPaginaContenido, CodigoPagina = "POLITICAS", Titulo = "Políticas PUB-009", Contenido = "Políticas vigentes PUB-009", EstaPublicado = true });
        await using var web = await WebAcademica.IniciarAsync(e); using var anonimo = web.Cliente();
        await ComprobarPoliticasYErrorAsync(anonimo, "Políticas vigentes PUB-009");
    }

    private static async Task ComprobarPoliticasYErrorAsync(HttpClient cliente, string? texto)
    {
        using var politicas = await cliente.GetAsync("/politicas"); Assert.Equal(HttpStatusCode.OK, politicas.StatusCode);
        var html = WebUtility.HtmlDecode(await politicas.Content.ReadAsStringAsync()); Assert.Contains("Políticas", html);
        if (texto is not null) Assert.Contains(texto, html);
        using var error = await cliente.GetAsync("/qa51-ruta-inexistente-7da573b5"); Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        var cuerpo = WebUtility.HtmlDecode(await error.Content.ReadAsStringAsync());
        Assert.Contains("La ruta solicitada no está disponible", cuerpo); Assert.Contains("Volver al inicio", cuerpo);
        Assert.DoesNotContain("Unhandled exception", cuerpo); Assert.DoesNotContain("StackTrace", cuerpo);
        using var vuelta = await cliente.GetAsync("/"); Assert.Equal(HttpStatusCode.OK, vuelta.StatusCode);
        Assert.Contains("Construimos soluciones", WebUtility.HtmlDecode(await vuelta.Content.ReadAsStringAsync()));
    }

    [Fact(DisplayName = "PUB-010 — Pregunta frecuente actualizada y publicada"), Trait("HU", "HU-INFO-010")]
    public async Task Pub010_ActualizarPreguntaYRespuestaPublicadas()
    {
        await using var e = await servidor.NuevoAsync(); var s = e.Obtener<IFaqService>();
        await s.RegistrarAsync(new() { Pregunta = "Pregunta inicial PUB-010", Respuesta = "Respuesta inicial PUB-010", Orden = 1 }, EntornoAcademico.Admin);
        var inicial = Assert.Single(await s.ObtenerTodosAdminAsync()); Assert.Equal("Respuesta inicial PUB-010", inicial.Respuesta);
        await s.ActualizarAsync(new() { IdFAQ = inicial.IdFAQ, Pregunta = "Pregunta vigente PUB-010", Respuesta = "Respuesta vigente PUB-010", Orden = 2 }, EntornoAcademico.Admin);
        await s.PublicarAsync(inicial.IdFAQ, EntornoAcademico.Admin);
        await using var web = await WebAcademica.IniciarAsync(e); using var anonimo = web.Cliente();
        var html = WebUtility.HtmlDecode(await anonimo.GetStringAsync("/preguntas-frecuentes"));
        Assert.Contains("Pregunta vigente PUB-010", html);
        await using var ui = new ComponenteAcademico(e.Servicios); await ui.MostrarAsync<PreguntasFrecuentes>();
        await ui.EventoAsync("button", "onclick", new MouseEventArgs());
        var expandida = await ui.TextoAsync(); Assert.Contains("Respuesta vigente PUB-010", expandida);
        Assert.DoesNotContain("Respuesta inicial PUB-010", expandida);
        Assert.Equal(inicial.IdFAQ, Assert.Single(await s.ObtenerTodosAdminAsync()).IdFAQ);
    }
}

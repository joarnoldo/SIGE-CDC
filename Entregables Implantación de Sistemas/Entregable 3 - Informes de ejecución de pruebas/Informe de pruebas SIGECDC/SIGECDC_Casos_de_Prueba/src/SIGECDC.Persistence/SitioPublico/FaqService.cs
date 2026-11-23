using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class FaqService(ApplicationDbContext contexto) : IFaqService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";

    public async Task RegistrarAsync(SolicitudRegistrarFaq solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var faq = new FAQ
        {
            Pregunta = LimpiarObligatorio(solicitud.Pregunta, "La pregunta es obligatoria."),
            Respuesta = LimpiarObligatorio(solicitud.Respuesta, "La respuesta es obligatoria."),
            Orden = Math.Max(0, solicitud.Orden),
            EstaPublicado = false,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuarioActual,
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.FAQs.Add(faq);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(SolicitudActualizarFaq solicitud, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var faq = await ObtenerFaqEditableAsync(solicitud.IdFAQ, cancellationToken);

        faq.Pregunta = LimpiarObligatorio(solicitud.Pregunta, "La pregunta es obligatoria.");
        faq.Respuesta = LimpiarObligatorio(solicitud.Respuesta, "La respuesta es obligatoria.");
        faq.Orden = Math.Max(0, solicitud.Orden);
        faq.FechaModificacion = DateTime.Now;
        faq.ModificadoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task PublicarAsync(long idFaq, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var faq = await ObtenerFaqEditableAsync(idFaq, cancellationToken);
        faq.EstaPublicado = true;
        faq.FechaModificacion = DateTime.Now;
        faq.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DespublicarAsync(long idFaq, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var faq = await ObtenerFaqEditableAsync(idFaq, cancellationToken);
        faq.EstaPublicado = false;
        faq.FechaModificacion = DateTime.Now;
        faq.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(long idFaq, string? idUsuarioActual = null, CancellationToken cancellationToken = default)
    {
        var faq = await ObtenerFaqEditableAsync(idFaq, cancellationToken);
        faq.EstadoRegistro = EstadoRegistroInactivo;
        faq.EstaPublicado = false;
        faq.FechaModificacion = DateTime.Now;
        faq.ModificadoPor = idUsuarioActual;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FaqResumen>> ObtenerTodosAdminAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.FAQs
            .AsNoTracking()
            .OrderBy(faq => faq.Orden)
            .ThenBy(faq => faq.IdFAQ)
            .Select(faq => new FaqResumen
            {
                IdFAQ = faq.IdFAQ,
                Pregunta = faq.Pregunta,
                Respuesta = faq.Respuesta,
                Orden = faq.Orden,
                EstaPublicado = faq.EstaPublicado,
                EstadoRegistro = faq.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FaqResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.FAQs
            .AsNoTracking()
            .Where(faq => faq.EstaPublicado && faq.EstadoRegistro == EstadoRegistroActivo)
            .OrderBy(faq => faq.Orden)
            .ThenBy(faq => faq.IdFAQ)
            .Select(faq => new FaqResumen
            {
                IdFAQ = faq.IdFAQ,
                Pregunta = faq.Pregunta,
                Respuesta = faq.Respuesta,
                Orden = faq.Orden,
                EstaPublicado = faq.EstaPublicado,
                EstadoRegistro = faq.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<FAQ> ObtenerFaqEditableAsync(long idFaq, CancellationToken cancellationToken)
    {
        return await contexto.FAQs
            .FirstOrDefaultAsync(faq => faq.IdFAQ == idFaq && faq.EstadoRegistro == EstadoRegistroActivo, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la pregunta frecuente solicitada.");
    }

    private static string LimpiarObligatorio(string valor, string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(mensajeError);
        }

        return valor.Trim();
    }
}
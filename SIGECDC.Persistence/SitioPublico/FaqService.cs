using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class FaqService(ApplicationDbContext contexto) : IFaqService
{
    public async Task RegistrarAsync(SolicitudRegistrarFaq solicitud, CancellationToken cancellationToken = default)
    {
        var faq = new FAQ
        {
            Pregunta = solicitud.Pregunta.Trim(),
            Respuesta = solicitud.Respuesta.Trim(),
            Orden = solicitud.Orden,
            EstaPublicado = false,
            EstadoRegistro = EstadosRegistro.Activo
        };

        contexto.FAQs.Add(faq);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(SolicitudActualizarFaq solicitud, CancellationToken cancellationToken = default)
    {
        var faq = await contexto.FAQs
            .FirstOrDefaultAsync(f => f.IdFAQ == solicitud.IdFAQ, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la FAQ con Id {solicitud.IdFAQ}.");

        faq.Pregunta = solicitud.Pregunta.Trim();
        faq.Respuesta = solicitud.Respuesta.Trim();
        faq.Orden = solicitud.Orden;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task PublicarAsync(long idFaq, CancellationToken cancellationToken = default)
    {
        var faq = await contexto.FAQs
            .FirstOrDefaultAsync(f => f.IdFAQ == idFaq, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la FAQ con Id {idFaq}.");

        faq.EstaPublicado = true;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesPublicarAsync(long idFaq, CancellationToken cancellationToken = default)
    {
        var faq = await contexto.FAQs
            .FirstOrDefaultAsync(f => f.IdFAQ == idFaq, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la FAQ con Id {idFaq}.");

        faq.EstaPublicado = false;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(long idFaq, CancellationToken cancellationToken = default)
    {
        var faq = await contexto.FAQs
            .FirstOrDefaultAsync(f => f.IdFAQ == idFaq, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la FAQ con Id {idFaq}.");

        faq.EstadoRegistro = EstadosRegistro.Inactivo;
        faq.EstaPublicado = false;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FaqResumen>> ObtenerTodosAdminAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.FAQs
            .AsNoTracking()
            .OrderBy(f => f.Orden)
            .ThenBy(f => f.IdFAQ)
            .Select(f => new FaqResumen
            {
                IdFAQ = f.IdFAQ,
                Pregunta = f.Pregunta,
                Respuesta = f.Respuesta,
                Orden = f.Orden,
                EstaPublicado = f.EstaPublicado,
                EstadoRegistro = f.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FaqResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.FAQs
            .AsNoTracking()
            .Where(f => f.EstaPublicado && f.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(f => f.Orden)
            .ThenBy(f => f.IdFAQ)
            .Select(f => new FaqResumen
            {
                IdFAQ = f.IdFAQ,
                Pregunta = f.Pregunta,
                Respuesta = f.Respuesta,
                Orden = f.Orden,
                EstaPublicado = f.EstaPublicado,
                EstadoRegistro = f.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }
}
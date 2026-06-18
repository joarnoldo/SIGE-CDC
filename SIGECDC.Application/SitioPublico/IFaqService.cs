using System;
using System.Collections.Generic;
using System.Text;

namespace SIGECDC.Application.SitioPublico;

public interface IFaqService
{
    Task RegistrarAsync(SolicitudRegistrarFaq solicitud, CancellationToken cancellationToken = default);

    Task ActualizarAsync(SolicitudActualizarFaq solicitud, CancellationToken cancellationToken = default);

    Task PublicarAsync(long idFaq, CancellationToken cancellationToken = default);

    Task DesPublicarAsync(long idFaq, CancellationToken cancellationToken = default);

    Task DesactivarAsync(long idFaq, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqResumen>> ObtenerTodosAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqResumen>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default);
}

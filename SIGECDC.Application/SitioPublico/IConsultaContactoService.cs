namespace SIGECDC.Application.SitioPublico;

public interface IConsultaContactoService
{
    Task RegistrarConsultaAsync(SolicitudConsultaContacto solicitud, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsultaContactoResumen>> ObtenerConsultasRecientesAsync(int cantidad = 50, CancellationToken cancellationToken = default);

    IReadOnlyList<EstadoConsultaContactoOpcion> ObtenerEstadosConsulta();

    Task ActualizarEstadoConsultaAsync(long idConsultaContacto, string estadoConsulta, CancellationToken cancellationToken = default);
}

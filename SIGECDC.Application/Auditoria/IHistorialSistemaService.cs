namespace SIGECDC.Application.Auditoria;

public interface IHistorialSistemaService
{
    Task RegistrarAccesoAsync(
        SolicitudRegistroAcceso solicitud,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccesoHistorialResumen>> ObtenerAccesosAsync(
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? accion = null,
        string? busqueda = null,
        int cantidad = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FormularioContactoHistorialResumen>> ObtenerFormulariosContactoAsync(
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estadoConsulta = null,
        string? busqueda = null,
        int cantidad = 100,
        CancellationToken cancellationToken = default);
}

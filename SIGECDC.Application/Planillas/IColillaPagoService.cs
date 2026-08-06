namespace SIGECDC.Application.Planillas;

public interface IColillaPagoService
{
    Task<int> GenerarPendientesAsync(
        long idPeriodoPlanilla,
        string idUsuarioActor,
        CancellationToken cancellationToken = default);

    Task<PortalColillasEmpleado> ObtenerPortalPropioAsync(
        string idUsuario,
        CancellationToken cancellationToken = default);

    Task<ColillaPagoDetalle?> ObtenerDetallePropioAsync(
        long idColillaPago,
        string idUsuario,
        CancellationToken cancellationToken = default);
}

namespace SIGECDC.Application.Activos;

public interface IRegistroUsoActivoService
{
    Task<IReadOnlyList<ActivoUsoOpcion>> ObtenerActivosParaUsoAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoMedicionUsoOpcion>> ObtenerTiposMedicionUsoAsync(
        CancellationToken cancellationToken = default);

    Task<long> RegistrarUsoAsync(
        SolicitudRegistrarUsoActivo solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<UsoActivoAcumuladoResumen?> ObtenerResumenUsoActivoAsync(
        long idActivo,
        CancellationToken cancellationToken = default);
}

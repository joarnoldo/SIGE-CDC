namespace SIGECDC.Application.RecursosHumanos;

public interface IColaboradorService
{
    Task<IReadOnlyList<ColaboradorResumen>> ObtenerColaboradoresAsync(
        string? busqueda = null,
        int? idEstadoLaboral = null,
        int? idDepartamento = null,
        CancellationToken cancellationToken = default);

    Task<ColaboradorDetalle?> ObtenerColaboradorPorIdAsync(long idColaborador, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpcionCatalogo>> ObtenerEstadosLaboralesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpcionCatalogo>> ObtenerDepartamentosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpcionCatalogo>> ObtenerPuestosAsync(int? idDepartamento = null, CancellationToken cancellationToken = default);

    Task<long> RegistrarColaboradorAsync(
        SolicitudColaborador solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task ActualizarColaboradorAsync(
        long idColaborador,
        SolicitudColaborador solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task DesactivarColaboradorAsync(
        long idColaborador,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task AsignarPuestoYDepartamentoAsync(
        SolicitudAsignarColaborador solicitud,
        CancellationToken cancellationToken = default);
}

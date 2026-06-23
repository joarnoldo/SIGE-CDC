using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.RecursosHumanos;

public interface IContratoDocumentoService
{
    Task<ExpedienteDocumental> ObtenerExpedienteDocumentalAsync(long idColaborador, CancellationToken cancellationToken = default);

    Task<ContratoDetalle?> ObtenerContratoPorIdAsync(long idContrato, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpcionCatalogo>> ObtenerTiposDocumentoAsync(CancellationToken cancellationToken = default);

    Task<long> RegistrarContratoAsync(
        SolicitudContrato solicitud,
        SolicitudArchivoDocumento? archivoContrato,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task ActualizarContratoAsync(
        long idContrato,
        SolicitudContrato solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task<long> AdjuntarDocumentoAsync(
        SolicitudArchivoDocumento solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task<long> ReemplazarDocumentoAsync(
        long idDocumentoAnterior,
        SolicitudArchivoDocumento solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default);

    Task<DocumentoArchivoDetalle?> ObtenerDocumentoPorIdAsync(long idDocumentoArchivo, CancellationToken cancellationToken = default);

    Task<ArchivoDescarga?> AbrirDocumentoAsync(long idDocumentoArchivo, CancellationToken cancellationToken = default);
}

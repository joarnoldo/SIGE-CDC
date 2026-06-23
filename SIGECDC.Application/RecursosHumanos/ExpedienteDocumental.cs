namespace SIGECDC.Application.RecursosHumanos;

public sealed class ExpedienteDocumental
{
    public ColaboradorDetalle? Colaborador { get; set; }

    public IReadOnlyList<ContratoResumen> Contratos { get; set; } = [];

    public IReadOnlyList<DocumentoArchivoResumen> Documentos { get; set; } = [];
}

using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public sealed record CapturaIncidenciaFinanciera(long IdPeriodo, long IdColaborador, int IdTipoIncidencia,
    long IdConceptoVersion, DateTime FechaHecho, string Unidad, decimal? Cantidad, decimal MontoManual, string ReferenciaHecho);
public sealed record ConfirmacionIncidenciaSolicitud(long IdIncidencia, int UltimaVersionEsperada, string ReferenciaSoporte,
    string? TipoDiaHE = null, bool RegistrarAutorizacion = false, string? ReferenciaAutorizacion = null,
    int? IdParametroPlanilla = null, ReferenciaSegmentoTemporal? BaseParametro = null);
public sealed record ResultadoCalculoIncidencias(ConciliacionLineas? Conciliacion, IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    public bool EsResoluble => Bloqueos.Count == 0 && Conciliacion is not null;
    public string Estado => EsResoluble ? "INCOMPLETO" : "BLOQUEADO";
    public bool EsAutorizable => false;
    public IReadOnlyList<string> Pendientes => Array.AsReadOnly(new[] { "G: CCSS/LPT", "H: renta", "I: RT/costo", "J: snapshot", "FIN-02" });
}

public sealed record IncidenciaExaminada(long IdIncidencia, long IdColaborador, DateTime Fecha,
    long? IdConceptoVersion, string? Unidad, long? IdCondicionLaboralVersion);
public sealed record DiagnosticoIncidencias(IReadOnlyList<IncidenciaExaminada> Incidencias, IReadOnlyList<BloqueoConcepto> Bloqueos)
{
    // Examinar no confirma hechos, no autoriza deducciones ni congela un cálculo de planilla.
    public bool EsAutorizable => false;
}

public interface IIncidenciasCalculadasService
{
    Task<long> CapturarIncidenciaFinancieraAsync(CapturaIncidenciaFinanciera solicitud, string referenciaRelacionOrdinaria,
        string actor, CancellationToken ct = default);
    Task<long> ConfirmarIncidenciaAsync(ConfirmacionIncidenciaSolicitud solicitud, string referenciaRelacionOrdinaria,
        string actor, CancellationToken ct = default);
    // Composición FIN-01: transacción Serializable del consumidor; mantiene locks hasta su commit/rollback.
    // Este componente no escribe snapshot ni cambia estados. No es una consulta que libere locks al retornar.
    Task<ResultadoCalculoIncidencias> PrepararIncidenciasEnTransaccionAsync(long idPeriodo,
        string referenciaRelacionOrdinaria, string actor, CancellationToken ct = default);
    Task<DiagnosticoIncidencias> ExaminarIncidenciasAsync(long idPeriodo, long? idColaborador,
        string referenciaRelacionOrdinaria, string actor, CancellationToken ct = default);
}

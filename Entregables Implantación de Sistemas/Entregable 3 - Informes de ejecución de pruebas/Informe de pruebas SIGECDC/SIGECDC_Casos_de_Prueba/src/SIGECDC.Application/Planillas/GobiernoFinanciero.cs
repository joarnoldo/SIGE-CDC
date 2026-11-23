using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public sealed record FuenteNormativa(string Codigo, string Institucion, string Nombre,
    string FuenteOficialUrl, string? IdentificadorPublicacion, DateTime? FechaPublicacion,
    VigenciaFinanciera Vigencia, string HuellaContenido);
public sealed record ValorNormativoCarga(string Codigo, string Nombre, string Sujeto, string Unidad,
    string CodigoBase, bool EsObligatorio, string ClaveAplicacion, decimal Valor,
    VigenciaFinanciera Vigencia, string? Observaciones = null);
public sealed record MinimoCarga(string CodigoOcupacion, string NombreOcupacion, string Periodicidad,
    string? TipoJornada, decimal Monto, VigenciaFinanciera Vigencia);
public sealed record TramoRentaCarga(int Orden, decimal Inferior, decimal? Superior, decimal Tasa);
public sealed record CreditoNormativoCarga(string CodigoTipo, string Nombre, decimal Monto,
    string Requisitos, VigenciaFinanciera Vigencia);
public sealed record PaqueteNormativo(FuenteNormativa Fuente, IReadOnlyList<ValorNormativoCarga> Valores,
    IReadOnlyList<MinimoCarga> Minimos, IReadOnlyList<TramoRentaCarga> Tramos,
    IReadOnlyList<CreditoNormativoCarga> Creditos);
public sealed record ClasificacionConcepto(string Clasificacion, bool EsSalarial, bool EsContributivoCCSS,
    bool EsTributario, bool EsPatronal, string TipoBase, string Unidad, bool PermiteMontoManual,
    bool RequiereAutorizacion);
public sealed record ConceptoCarga(string Codigo, string Nombre, VigenciaFinanciera Vigencia,
    ClasificacionConcepto? Clasificacion, string ReferenciaSoporte, string? TipoIncidenciaLegada = null);
public sealed record JornadaCarga(string Codigo, string Nombre, string Tipo, decimal HorasDia,
    decimal HorasSemana, VigenciaFinanciera Vigencia, string ReferenciaSoporte);
public sealed record RiesgoTrabajoCarga(string Poliza, string Actividad, decimal Tarifa, decimal Descuento,
    decimal Recargo, VigenciaFinanciera Vigencia, string ReferenciaDocumento, long? IdVersionNormativa = null);
public sealed record CreditoPersonalCarga(long IdColaborador, long IdCreditoNormativo, decimal Cantidad,
    VigenciaFinanciera Vigencia, string ReferenciaSoporte);
public sealed record ParametroInternoCarga(string Codigo, string Nombre, long IdConcepto,
    string Tipo, string Naturaleza, decimal? ValorDecimal, string? ValorTexto,
    VigenciaFinanciera Vigencia, string ReferenciaSoporte);
public sealed record RequisitosFinancieros(VigenciaFinanciera Periodo, IReadOnlyList<long> IdContratos,
    IReadOnlyList<string> Conceptos, string? ReferenciaAplicabilidadOrdinaria,
    string ClaveAplicacion = "ORDINARIO", string PeriodicidadMinimo = "Mensual",
    IReadOnlyList<string>? ParametrosInternos = null);
public sealed record FaltanteFinanciero(string Codigo, string Clave, DateTime Fecha, long? IdContrato = null);
public sealed record DatoFinancieroResuelto(string Tipo, string Clave, DateTime Fecha,
    long Id, long? IdVersionNormativa = null, decimal? Valor = null,
    string? Unidad = null, string? Sujeto = null, string? CodigoBase = null);
public sealed record ConfiguracionFinancieraResuelta(IReadOnlyList<FaltanteFinanciero> Faltantes,
    IReadOnlyList<DatoFinancieroResuelto> Datos)
{
    // Configuración resoluble no equivale a cálculo ni autorización para retirar FIN-02.
    public bool EstaCompleta => Faltantes.Count == 0;
}

public interface IGobiernoFinancieroService
{
    Task<long> ImportarNormativaAsync(PaqueteNormativo paquete, ReadOnlyMemory<byte> evidencia,
        string actor, CancellationToken ct = default);
    Task ActivarNormativaAsync(long idVersion, string huellaPaqueteRevisado, string revisor, CancellationToken ct = default);
    Task<string> ObtenerHuellaRevisionAsync(long idVersion, string actor, CancellationToken ct = default);
    Task<long> RegistrarConceptoAsync(ConceptoCarga datos, string actor, CancellationToken ct = default);
    Task<long> RegistrarJornadaAsync(JornadaCarga datos, string actor, CancellationToken ct = default);
    Task<long> RegistrarRiesgoTrabajoAsync(RiesgoTrabajoCarga datos, string actor, CancellationToken ct = default);
    Task<long> AcreditarCreditoAsync(CreditoPersonalCarga datos, string actor, CancellationToken ct = default);
    Task<int> RegistrarParametroInternoAsync(ParametroInternoCarga datos, string actor, CancellationToken ct = default);
    Task HomologarPuestoAsync(int idPuesto, long idOcupacion, string soporte, string actor, CancellationToken ct = default);
    Task AsociarContratoAsync(long idContrato, long idJornada, long idRiesgo, string soporte, string actor, CancellationToken ct = default);
    Task<ConfiguracionFinancieraResuelta> ResolverAsync(RequisitosFinancieros solicitud, string actor, CancellationToken ct = default);
}

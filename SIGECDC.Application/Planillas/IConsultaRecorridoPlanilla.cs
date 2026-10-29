using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

// Proyecciones administrativas, sin cálculos ni una segunda fuente financiera.
public sealed record OpcionFinanciera(long Id, string Nombre);
public sealed record TipoHechoOpcion(int Id, string Nombre, long IdConcepto);
public sealed record JornadaPlanillaOpcion(long Id, string Nombre, string Tipo, decimal HorasDia,
    decimal HorasSemana, VigenciaFinanciera Vigencia);
public sealed record RiesgoPlanillaOpcion(long Id, string Poliza, string Actividad, VigenciaFinanciera Vigencia);
public sealed record CatalogosRecorridoPlanilla(IReadOnlyList<ConceptoMonetarioVersion> Conceptos,
    IReadOnlyList<TipoHechoOpcion> Tipos, IReadOnlyList<JornadaPlanillaOpcion> Jornadas,
    IReadOnlyList<RiesgoPlanillaOpcion> Riesgos, IReadOnlyList<OpcionFinanciera> Ocupaciones,
    IReadOnlyList<OpcionFinanciera> Puestos, IReadOnlyList<OpcionFinanciera> Departamentos,
    IReadOnlyList<OpcionFinanciera> Colaboradores, IReadOnlyList<PeriodoPlanillaResumen> Periodos);
public sealed record ContratoRecorrido(long Id, long IdColaborador, string Colaborador, string Tipo,
    string Periodicidad, VigenciaFinanciera Vigencia, decimal Salario, int IdPuesto, int IdDepartamento,
    long? IdOcupacion, long? IdJornada, long? IdRiesgo);
public sealed record ParametroRecorrido(string Nombre, string Concepto, VersionParametroInterno Version,
    IReadOnlyList<OverrideParametroInterno> Asignaciones);
public sealed record IncidenciaRecorrido(long Id, string Colaborador, string Concepto,
    CapturaIncidenciaFinanciera Captura, int UltimaConfirmacion, string Estado, string EstadoPeriodo);
public sealed record VersionPlanillaOpcion(long Id, int Version, string Estado, DateTime Fecha, bool Vigente);
public sealed record PeriodoRecorrido(PeriodoPlanillaResumen Periodo, long? EjecucionVigente,
    IReadOnlyList<VersionPlanillaOpcion> Versiones, IReadOnlyList<ContratoRecorrido> Contratos,
    IReadOnlyList<OpcionFinanciera> EjecucionesPrevias);

public interface IConsultaRecorridoPlanilla
{
    Task<CatalogosRecorridoPlanilla> CatalogosAsync(string actor, CancellationToken ct = default);
    Task<ContratoRecorrido?> ContratoAsync(long id, string actor, CancellationToken ct = default);
    Task<IReadOnlyList<ParametroRecorrido>> ParametrosAsync(string actor, CancellationToken ct = default);
    Task<IReadOnlyList<IncidenciaRecorrido>> IncidenciasAsync(long? idPeriodo, string actor, CancellationToken ct = default);
    Task<PeriodoRecorrido?> PeriodoAsync(long id, string actor, CancellationToken ct = default);
}

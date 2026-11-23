using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Forecast;

public sealed record SolicitudCondicionForecast(long IdEscenario,long IdPeriodo,long IdParticipante,
    DateTime Inicio,DateTime Fin,string Origen,decimal SalarioMensual,int IdPuesto,int IdDepartamento,
    long IdJornada,long IdOcupacion,string TipoContrato,string Periodicidad,string Soporte,
    long? IdContrato=null,long? IdCondicion=null,long? IdConfiguracionRT=null,
    ModalidadBaseRiesgo ModalidadRT=ModalidadBaseRiesgo.Desconocida,string? SoporteRT=null);
public sealed record SolicitudOverrideForecast(long IdEscenario,long IdConceptoVersion,string Tipo,string Unidad,
    decimal Valor,string Soporte,string? ReferenciaAutorizacion=null);

public sealed record OpcionEvidenciaForecast(long Id,string Etiqueta);
public sealed record ConceptoEvidenciaForecast(long IdVersion,string Codigo,string Nombre,string Unidad,string TipoBase,bool PermiteMonto,bool RequiereAutorizacion);
public sealed record CondicionDisponibleForecast(string Etiqueta,SolicitudCondicionForecast Condicion);
public sealed record ConfiguracionEvidenciaForecast(bool PuedeEditar,DateTime Inicio,DateTime Fin,
    IReadOnlyList<OpcionEvidenciaForecast> Participantes,IReadOnlyList<PeriodoForecastResumen> Periodos,
    IReadOnlyList<OpcionEvidenciaForecast> Puestos,IReadOnlyList<OpcionEvidenciaForecast> Departamentos,
    IReadOnlyList<OpcionEvidenciaForecast> Jornadas,IReadOnlyList<OpcionEvidenciaForecast> Ocupaciones,
    IReadOnlyList<OpcionEvidenciaForecast> Polizas,IReadOnlyList<ConceptoEvidenciaForecast> Conceptos,
    IReadOnlyList<CondicionDisponibleForecast> CondicionesDisponibles,
    IReadOnlyList<SolicitudCondicionForecast> CondicionesRegistradas,IReadOnlyList<SolicitudOverrideForecast> Overrides);

public interface IForecastEvidenciaService
{
    Task<ConfiguracionEvidenciaForecast> ConsultarConfiguracionAsync(long escenario,string actor,CancellationToken ct=default);
    Task<IReadOnlyList<long>> RegistrarCondicionAsync(SolicitudCondicionForecast solicitud,string actor,CancellationToken ct=default);
    Task<long> RegistrarOverrideAsync(SolicitudOverrideForecast solicitud,string actor,CancellationToken ct=default);
}

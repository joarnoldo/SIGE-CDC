using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.Forecast;

public sealed record DestinoDistribucionReal(long? IdProyecto,bool EsSinAsignar,decimal Porcentaje);
public sealed record SolicitudDistribucionReal(long IdDetallePlanilla,IReadOnlyList<DestinoDistribucionReal> Destinos,string ReferenciaSoporte);
public sealed record ProyectoDistribucionReal(long Id,string Codigo,string Nombre);
public sealed record DistribucionRealConsulta(long IdDetalle,long IdEjecucion,int Version,string Colaborador,string Estado,decimal CostoLaboralTotal,bool Consumida,
    IReadOnlyList<DestinoDistribucionReal> Destinos,IReadOnlyList<ProyectoDistribucionReal> Proyectos);
public sealed record FuenteComparacion(long? IdForecastDetalle,long? IdDetallePlanilla,long? IdConceptoDetallePlanilla,long? IdDistribucionReal,string Lado);
public sealed record DimensionComparacion(string Lado,DateTime Inicio,DateTime Fin,string Puesto,string Departamento,string Jornada,decimal SalarioMensual);
public sealed record FilaComparacionPersistida(long Id,string Granularidad,string CodigoForecast,string NombreForecast,string CodigoReal,string NombreReal,
    bool TieneForecast,bool TieneReal,decimal? MontoForecast,decimal? MontoReal,decimal? DiferenciaFirmada,decimal? DiferenciaAbsoluta,
    decimal? DiferenciaPorcentual,string? Motivo,IReadOnlyList<FuenteComparacion> Fuentes)
{
    public IReadOnlyList<DimensionComparacion> Dimensiones { get; init; }=[];
}
public sealed record PeriodoComparacionPersistida(long IdComparacion,long IdPeriodo,string TipoPeriodo,DateTime Inicio,DateTime Fin,
    long? IdEjecucionReal,int? VersionReal,string? VersionMotorReal,string Disponibilidad,string? Motivo,string? EstadoReal,
    DateTime FechaEvaluacion,IReadOnlyList<FilaComparacionPersistida> Filas);
public sealed record ConsultaComparacionPersistida(long IdEscenario,string Nombre,string Estado,IReadOnlyList<PeriodoComparacionPersistida> Periodos);

public interface IForecastComparacionSnapshotService
{
    Task<DistribucionRealConsulta> ConsultarDistribucionAsync(long idDetalle,string actor,CancellationToken ct=default);
    Task RegistrarDistribucionAsync(SolicitudDistribucionReal solicitud,string actor,CancellationToken ct=default);
    Task<ConsultaComparacionPersistida> CompararAsync(long idEscenario,string actor,CancellationToken ct=default);
    Task<ConsultaComparacionPersistida> LeerAsync(long idEscenario,string actor,CancellationToken ct=default);
}
public interface IGeneradorComparacionForecast
{
    ArchivoDescarga Pdf(ConsultaComparacionPersistida consulta);
    ArchivoDescarga Excel(ConsultaComparacionPersistida consulta);
}

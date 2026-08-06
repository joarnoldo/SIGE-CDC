using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

internal static class ForecastResultadoConsultaInterna
{
    internal static IQueryable<DetalleResultadoConsulta> Crear(
        ApplicationDbContext contexto,
        IReadOnlyCollection<long> idsEscenarios,
        long? idForecastPeriodo = null)
    {
        var consulta = contexto.ForecastDetalles
            .AsNoTracking()
            .Where(item => idsEscenarios.Contains(item.IdForecastEscenario)
                && item.EstadoRegistro == EstadosRegistro.Activo
                && item.ForecastAsignacionProyecto!.EstadoRegistro == EstadosRegistro.Activo
                && item.ForecastAsignacionProyecto.ForecastPeriodo!.EstadoRegistro == EstadosRegistro.Activo
                && item.ForecastAsignacionProyecto.ForecastParticipante!.EstadoRegistro == EstadosRegistro.Activo
                && item.ForecastAsignacionProyecto.ForecastParticipante.EstaIncluido);

        if (idForecastPeriodo.HasValue)
        {
            consulta = consulta.Where(item => item.IdForecastPeriodo == idForecastPeriodo.Value);
        }

        return consulta.Select(item => new DetalleResultadoConsulta
        {
            IdForecastEscenario = item.IdForecastEscenario,
            IdForecastPeriodo = item.IdForecastPeriodo,
            NumeroOrdenPeriodo = item.ForecastAsignacionProyecto!.ForecastPeriodo!.NumeroOrden,
            TipoPeriodo = item.ForecastAsignacionProyecto.ForecastPeriodo.TipoPeriodo,
            FechaInicioPeriodo = item.ForecastAsignacionProyecto.ForecastPeriodo.FechaInicio,
            FechaFinPeriodo = item.ForecastAsignacionProyecto.ForecastPeriodo.FechaFin,
            IdForecastParticipante = item.IdForecastParticipante,
            IdColaborador = item.ForecastAsignacionProyecto.ForecastParticipante!.IdColaborador,
            CodigoParticipante = item.ForecastAsignacionProyecto.ForecastParticipante.CodigoParticipante,
            EtiquetaParticipante = item.ForecastAsignacionProyecto.ForecastParticipante.Etiqueta,
            TipoParticipante = item.ForecastAsignacionProyecto.ForecastParticipante.TipoParticipante,
            IdDepartamento = item.ForecastAsignacionProyecto.ForecastParticipante.IdDepartamento,
            NombreDepartamento = item.ForecastAsignacionProyecto.ForecastParticipante.Departamento!.Nombre,
            NombrePuesto = item.ForecastAsignacionProyecto.ForecastParticipante.Puesto!.Nombre,
            IdProyecto = item.IdProyecto,
            CodigoProyecto = item.ForecastAsignacionProyecto.Proyecto!.CodigoProyecto,
            NombreProyecto = item.ForecastAsignacionProyecto.Proyecto.NombreProyecto,
            Concepto = item.Concepto,
            MontoBase = item.MontoBase,
            MontoAjuste = item.MontoAjuste,
            MontoProyectado = item.MontoProyectado
        });
    }
}

internal sealed class DetalleResultadoConsulta
{
    public long IdForecastEscenario { get; init; }
    public long IdForecastPeriodo { get; init; }
    public int NumeroOrdenPeriodo { get; init; }
    public string TipoPeriodo { get; init; } = string.Empty;
    public DateTime FechaInicioPeriodo { get; init; }
    public DateTime FechaFinPeriodo { get; init; }
    public long IdForecastParticipante { get; init; }
    public long? IdColaborador { get; init; }
    public string CodigoParticipante { get; init; } = string.Empty;
    public string EtiquetaParticipante { get; init; } = string.Empty;
    public string TipoParticipante { get; init; } = string.Empty;
    public int IdDepartamento { get; init; }
    public string NombreDepartamento { get; init; } = string.Empty;
    public string NombrePuesto { get; init; } = string.Empty;
    public long IdProyecto { get; init; }
    public string CodigoProyecto { get; init; } = string.Empty;
    public string NombreProyecto { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public decimal MontoBase { get; init; }
    public decimal MontoAjuste { get; init; }
    public decimal MontoProyectado { get; init; }
}

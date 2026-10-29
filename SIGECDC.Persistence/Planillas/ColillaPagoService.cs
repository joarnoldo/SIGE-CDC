using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class ColillaPagoService(ApplicationDbContext contexto) : IColillaPagoService
{
    public async Task<int> GenerarPendientesAsync(long idPeriodoPlanilla, string idUsuarioActor,
        CancellationToken cancellationToken = default)
    {
        var reporte = await new LectorPublicacionPlanilla(contexto).ConsultarAsync(idPeriodoPlanilla, idUsuarioActor, false, cancellationToken)
            ?? throw new InvalidOperationException("La ejecución no está disponible para generar colillas.");
        return reporte.ColillasGeneradas;
    }

    public Task<ColillaPagoDetalle?> ObtenerDetallePropioAsync(long idColillaPago, string idUsuario, CancellationToken cancellationToken = default)
        => ObtenerAsync(idColillaPago, idUsuario, true, cancellationToken);

    public Task<ColillaPagoDetalle?> ObtenerRevisionAsync(long idColillaPago, string actor, CancellationToken cancellationToken = default)
        => ObtenerAsync(idColillaPago, actor, false, cancellationToken);

    private async Task<ColillaPagoDetalle?> ObtenerAsync(long id, string actor, bool propio, CancellationToken ct)
    {
        var r = await new LectorPublicacionPlanilla(contexto).ConsultarAsync(0, actor, propio, ct, id);
        var colilla = r?.Detalles.Cast<ColillaPagoDetalle>().SingleOrDefault(d => d.IdColillaPago == id);
        if (colilla is not null)
        {
            colilla.Lineas = colilla.Lineas.Where(l => l.Efecto != EfectoMonetario.CargaPatronal).ToArray();
            colilla.CargasPatronales = colilla.RiesgosTrabajo = colilla.OtrosCostosPatronales = colilla.CostoLaboralTotal = 0;
        }
        return colilla;
    }

    public async Task<PortalColillasEmpleado> ObtenerPortalPropioAsync(string idUsuario, CancellationToken cancellationToken = default)
    {
        var lector = new LectorPublicacionPlanilla(contexto);
        var colillas = new List<ColillaPagoDetalle>();
        foreach (var id in await lector.PeriodosAsync(idUsuario, true, cancellationToken))
        {
            var r = await lector.ConsultarAsync(id, idUsuario, true, cancellationToken);
            if (r is not null) colillas.AddRange(r.Detalles.Cast<ColillaPagoDetalle>());
        }
        var primera = colillas.FirstOrDefault();
        return new PortalColillasEmpleado { TieneColaboradorVinculado = true,
            CodigoColaborador = primera?.CodigoColaborador ?? "", NombreColaborador = primera?.NombreColaborador ?? "",
            Departamento = primera?.Departamento ?? "", Puesto = primera?.Puesto ?? "",
            Colillas = colillas.Select(c => new ColillaPagoResumen { IdColillaPago = c.IdColillaPago, IdPeriodoPlanilla = c.IdPeriodoPlanilla, CodigoColilla = c.CodigoColilla,
                CodigoPeriodo = c.CodigoPeriodo, NombrePeriodo = c.NombrePeriodo, TipoPeriodo = c.TipoPeriodo,
                FechaInicio = c.FechaInicio, FechaFin = c.FechaFin, FechaGeneracion = c.FechaGeneracion,
                EstadoPlanilla = c.EstadoPlanilla, SalarioBruto = c.SalarioBruto, TotalDeducciones = c.TotalDeducciones,
                SalarioNeto = c.SalarioNeto }).ToArray() };
    }
}

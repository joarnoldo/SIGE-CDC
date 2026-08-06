using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class ReportePlanillaService(ApplicationDbContext contexto) : IReportePlanillaService
{
    private const string EstadoActivo = "Activo";

    private static readonly string[] EstadosElegibles =
    [
        EstadosPlanilla.Calculada,
        EstadosPlanilla.Aprobada,
        EstadosPlanilla.Cerrada
    ];

    public async Task<IReadOnlyList<PeriodoReportePlanillaOpcion>> ObtenerPeriodosDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Where(periodo => periodo.EstadoRegistro == EstadoActivo
                && periodo.Planilla != null
                && periodo.Planilla.EstadoRegistro == EstadoActivo
                && periodo.Planilla.FechaCalculo != null
                && periodo.Planilla.EstadoPlanilla != null
                && EstadosElegibles.Contains(periodo.Planilla.EstadoPlanilla.Nombre))
            .OrderByDescending(periodo => periodo.FechaInicio)
            .ThenByDescending(periodo => periodo.IdPeriodoPlanilla)
            .Select(periodo => new PeriodoReportePlanillaOpcion
            {
                IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
                CodigoPeriodo = periodo.CodigoPeriodo,
                NombrePeriodo = periodo.Nombre,
                TipoPeriodo = periodo.TipoPeriodo,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin,
                EstadoPlanilla = periodo.Planilla!.EstadoPlanilla!.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportePlanilla?> ObtenerReporteAsync(
        long idPeriodoPlanilla,
        CancellationToken cancellationToken = default)
    {
        if (idPeriodoPlanilla <= 0)
        {
            return null;
        }

        var encabezado = await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Where(periodo => periodo.IdPeriodoPlanilla == idPeriodoPlanilla
                && periodo.EstadoRegistro == EstadoActivo
                && periodo.Planilla != null
                && periodo.Planilla.EstadoRegistro == EstadoActivo
                && periodo.Planilla.FechaCalculo != null
                && periodo.Planilla.EstadoPlanilla != null
                && EstadosElegibles.Contains(periodo.Planilla.EstadoPlanilla.Nombre))
            .Select(periodo => new ReportePlanilla
            {
                IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
                CodigoPeriodo = periodo.CodigoPeriodo,
                NombrePeriodo = periodo.Nombre,
                TipoPeriodo = periodo.TipoPeriodo,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin,
                EstadoPlanilla = periodo.Planilla!.EstadoPlanilla!.Nombre,
                FechaCalculo = periodo.Planilla.FechaCalculo!.Value,
                FechaAprobacion = periodo.Planilla.FechaAprobacion,
                FechaCierre = periodo.Planilla.FechaCierre,
                SalarioBrutoTotal = periodo.Planilla.SalarioBrutoTotal,
                DeduccionesTotal = periodo.Planilla.DeduccionesTotal,
                SalarioNetoTotal = periodo.Planilla.SalarioNetoTotal
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (encabezado is null)
        {
            return null;
        }

        var datos = await contexto.DetallesPlanilla
            .AsNoTracking()
            .Where(detalle => detalle.Planilla != null
                && detalle.Planilla.IdPeriodoPlanilla == idPeriodoPlanilla)
            .OrderBy(detalle => detalle.Colaborador!.PrimerApellido)
            .ThenBy(detalle => detalle.Colaborador!.Nombre)
            .ThenBy(detalle => detalle.Colaborador!.CodigoColaborador)
            .Select(detalle => new
            {
                CodigoColaborador = detalle.Colaborador != null
                    ? detalle.Colaborador.CodigoColaborador
                    : string.Empty,
                Nombre = detalle.Colaborador != null
                    ? detalle.Colaborador.Nombre
                    : string.Empty,
                PrimerApellido = detalle.Colaborador != null
                    ? detalle.Colaborador.PrimerApellido
                    : string.Empty,
                SegundoApellido = detalle.Colaborador != null
                    ? detalle.Colaborador.SegundoApellido
                    : null,
                Departamento = detalle.Colaborador != null
                    && detalle.Colaborador.Departamento != null
                        ? detalle.Colaborador.Departamento.Nombre
                        : string.Empty,
                detalle.SalarioProporcional,
                detalle.TotalHorasExtra,
                detalle.TotalBonos,
                detalle.TotalBeneficiosConfigurables,
                detalle.TotalAusencias,
                detalle.SalarioBruto,
                detalle.TotalDeducciones,
                detalle.SalarioNeto
            })
            .ToListAsync(cancellationToken);

        encabezado.Detalles = datos.Select(detalle => new DetalleReportePlanilla
        {
            CodigoColaborador = detalle.CodigoColaborador,
            NombreColaborador = ConstruirNombreCompleto(
                detalle.Nombre,
                detalle.PrimerApellido,
                detalle.SegundoApellido),
            Departamento = detalle.Departamento,
            SalarioProporcional = detalle.SalarioProporcional,
            TotalHorasExtra = detalle.TotalHorasExtra,
            TotalBonos = detalle.TotalBonos,
            TotalBeneficiosConfigurables = detalle.TotalBeneficiosConfigurables,
            TotalAusencias = detalle.TotalAusencias,
            SalarioBruto = detalle.SalarioBruto,
            TotalDeducciones = detalle.TotalDeducciones,
            SalarioNeto = detalle.SalarioNeto
        }).ToList();

        return encabezado;
    }

    private static string ConstruirNombreCompleto(
        string nombre,
        string primerApellido,
        string? segundoApellido)
    {
        return string.IsNullOrWhiteSpace(segundoApellido)
            ? $"{nombre} {primerApellido}"
            : $"{nombre} {primerApellido} {segundoApellido}";
    }
}

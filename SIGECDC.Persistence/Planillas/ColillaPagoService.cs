using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class ColillaPagoService(ApplicationDbContext contexto) : IColillaPagoService
{
    private const string EstadoActivo = "Activo";

    private static readonly string[] EstadosConCalculo =
    [
        EstadosPlanilla.Calculada,
        EstadosPlanilla.EnRevision,
        EstadosPlanilla.Aprobada,
        EstadosPlanilla.Cerrada
    ];

    public async Task<int> GenerarPendientesAsync(
        long idPeriodoPlanilla,
        string idUsuarioActor,
        CancellationToken cancellationToken = default)
    {
        if (idPeriodoPlanilla <= 0)
        {
            throw new ArgumentException("El período de planilla es obligatorio.");
        }

        var idUsuario = LimpiarIdUsuario(idUsuarioActor);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var planilla = await contexto.Planillas
                .Include(registro => registro.EstadoPlanilla)
                .FirstOrDefaultAsync(registro =>
                    registro.IdPeriodoPlanilla == idPeriodoPlanilla
                    && registro.EstadoRegistro == EstadoActivo,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "El período no tiene una planilla calculada.");

            if (planilla.FechaCalculo is null
                || planilla.EstadoPlanilla is null
                || !EstadosConCalculo.Contains(planilla.EstadoPlanilla.Nombre))
            {
                throw new InvalidOperationException(
                    "Solo se pueden generar colillas para una planilla calculada, en revisión, aprobada o cerrada.");
            }

            var cantidad = await GeneradorMetadatosColillaPago.AgregarPendientesAsync(
                contexto,
                planilla.IdPlanilla,
                idUsuario,
                DateTime.Now,
                cancellationToken);

            if (cantidad > 0)
            {
                await contexto.SaveChangesAsync(cancellationToken);
            }

            await transaccion.CommitAsync(cancellationToken);
            return cantidad;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<PortalColillasEmpleado> ObtenerPortalPropioAsync(
        string idUsuario,
        CancellationToken cancellationToken = default)
    {
        var idUsuarioLimpio = LimpiarIdUsuario(idUsuario);

        var colaborador = await contexto.Colaboradores
            .AsNoTracking()
            .Where(registro => registro.IdUsuario == idUsuarioLimpio
                && registro.EstadoRegistro == EstadoActivo)
            .Select(registro => new
            {
                registro.IdColaborador,
                registro.CodigoColaborador,
                registro.Nombre,
                registro.PrimerApellido,
                registro.SegundoApellido,
                Departamento = registro.Departamento != null
                    ? registro.Departamento.Nombre
                    : string.Empty,
                Puesto = registro.Puesto != null
                    ? registro.Puesto.Nombre
                    : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (colaborador is null)
        {
            return new PortalColillasEmpleado();
        }

        var colillas = await contexto.ColillasPago
            .AsNoTracking()
            .Where(colilla => colilla.EstadoRegistro == EstadoActivo
                && colilla.DetallePlanilla != null
                && colilla.DetallePlanilla.Colaborador != null
                && colilla.DetallePlanilla.Colaborador.IdUsuario == idUsuarioLimpio
                && colilla.DetallePlanilla.Planilla != null
                && colilla.DetallePlanilla.Planilla.FechaCalculo != null
                && colilla.DetallePlanilla.Planilla.EstadoPlanilla != null
                && EstadosConCalculo.Contains(colilla.DetallePlanilla.Planilla.EstadoPlanilla.Nombre)
                && colilla.DetallePlanilla.Planilla.PeriodoPlanilla != null)
            .OrderByDescending(colilla => colilla.DetallePlanilla!.Planilla!.PeriodoPlanilla!.FechaInicio)
            .ThenByDescending(colilla => colilla.IdColillaPago)
            .Select(colilla => new ColillaPagoResumen
            {
                IdColillaPago = colilla.IdColillaPago,
                IdPeriodoPlanilla = colilla.DetallePlanilla!.Planilla!.IdPeriodoPlanilla,
                CodigoColilla = colilla.CodigoColilla,
                CodigoPeriodo = colilla.DetallePlanilla.Planilla.PeriodoPlanilla!.CodigoPeriodo,
                NombrePeriodo = colilla.DetallePlanilla.Planilla.PeriodoPlanilla.Nombre,
                TipoPeriodo = colilla.DetallePlanilla.Planilla.PeriodoPlanilla.TipoPeriodo,
                FechaInicio = colilla.DetallePlanilla.Planilla.PeriodoPlanilla.FechaInicio,
                FechaFin = colilla.DetallePlanilla.Planilla.PeriodoPlanilla.FechaFin,
                EstadoPlanilla = colilla.DetallePlanilla.Planilla.EstadoPlanilla!.Nombre,
                FechaGeneracion = colilla.FechaGeneracion,
                SalarioBruto = colilla.DetallePlanilla.SalarioBruto,
                TotalDeducciones = colilla.DetallePlanilla.TotalDeducciones,
                SalarioNeto = colilla.DetallePlanilla.SalarioNeto
            })
            .ToListAsync(cancellationToken);

        var cantidadPeriodosPendientes = await contexto.DetallesPlanilla
            .AsNoTracking()
            .Where(detalle => detalle.IdColaborador == colaborador.IdColaborador
                && detalle.ColillaPago == null
                && detalle.Planilla != null
                && detalle.Planilla.FechaCalculo != null
                && detalle.Planilla.EstadoPlanilla != null
                && EstadosConCalculo.Contains(detalle.Planilla.EstadoPlanilla.Nombre))
            .Select(detalle => detalle.Planilla!.IdPeriodoPlanilla)
            .Distinct()
            .CountAsync(cancellationToken);

        return new PortalColillasEmpleado
        {
            TieneColaboradorVinculado = true,
            CodigoColaborador = colaborador.CodigoColaborador,
            NombreColaborador = ConstruirNombreCompleto(
                colaborador.Nombre,
                colaborador.PrimerApellido,
                colaborador.SegundoApellido),
            Departamento = colaborador.Departamento,
            Puesto = colaborador.Puesto,
            CantidadPeriodosPendientes = cantidadPeriodosPendientes,
            Colillas = colillas
        };
    }

    public async Task<ColillaPagoDetalle?> ObtenerDetallePropioAsync(
        long idColillaPago,
        string idUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idColillaPago <= 0)
        {
            return null;
        }

        var idUsuarioLimpio = LimpiarIdUsuario(idUsuario);

        var datos = await contexto.ColillasPago
            .AsNoTracking()
            .Where(colilla => colilla.IdColillaPago == idColillaPago
                && colilla.EstadoRegistro == EstadoActivo
                && colilla.DetallePlanilla != null
                && colilla.DetallePlanilla.Colaborador != null
                && colilla.DetallePlanilla.Colaborador.IdUsuario == idUsuarioLimpio
                && colilla.DetallePlanilla.Planilla != null
                && colilla.DetallePlanilla.Planilla.FechaCalculo != null
                && colilla.DetallePlanilla.Planilla.EstadoPlanilla != null
                && EstadosConCalculo.Contains(colilla.DetallePlanilla.Planilla.EstadoPlanilla.Nombre)
                && colilla.DetallePlanilla.Planilla.PeriodoPlanilla != null)
            .Select(colilla => new
            {
                colilla.IdColillaPago,
                colilla.CodigoColilla,
                colilla.FechaGeneracion,
                Periodo = colilla.DetallePlanilla!.Planilla!.PeriodoPlanilla!,
                EstadoPlanilla = colilla.DetallePlanilla.Planilla.EstadoPlanilla!.Nombre,
                Detalle = colilla.DetallePlanilla,
                Colaborador = colilla.DetallePlanilla.Colaborador!,
                Departamento = colilla.DetallePlanilla.Colaborador!.Departamento != null
                    ? colilla.DetallePlanilla.Colaborador.Departamento.Nombre
                    : string.Empty,
                Puesto = colilla.DetallePlanilla.Colaborador!.Puesto != null
                    ? colilla.DetallePlanilla.Colaborador.Puesto.Nombre
                    : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (datos is null)
        {
            return null;
        }

        return new ColillaPagoDetalle
        {
            IdColillaPago = datos.IdColillaPago,
            CodigoColilla = datos.CodigoColilla,
            CodigoPeriodo = datos.Periodo.CodigoPeriodo,
            NombrePeriodo = datos.Periodo.Nombre,
            TipoPeriodo = datos.Periodo.TipoPeriodo,
            FechaInicio = datos.Periodo.FechaInicio,
            FechaFin = datos.Periodo.FechaFin,
            EstadoPlanilla = datos.EstadoPlanilla,
            FechaGeneracion = datos.FechaGeneracion,
            CodigoColaborador = datos.Colaborador.CodigoColaborador,
            NombreColaborador = ConstruirNombreCompleto(
                datos.Colaborador.Nombre,
                datos.Colaborador.PrimerApellido,
                datos.Colaborador.SegundoApellido),
            Departamento = datos.Departamento,
            Puesto = datos.Puesto,
            SalarioBase = datos.Detalle.SalarioBase,
            SalarioProporcional = datos.Detalle.SalarioProporcional,
            TotalHorasExtra = datos.Detalle.TotalHorasExtra,
            TotalBonos = datos.Detalle.TotalBonos,
            TotalBeneficiosConfigurables = datos.Detalle.TotalBeneficiosConfigurables,
            TotalAusencias = datos.Detalle.TotalAusencias,
            SalarioBruto = datos.Detalle.SalarioBruto,
            TotalDeducciones = datos.Detalle.TotalDeducciones,
            SalarioNeto = datos.Detalle.SalarioNeto
        };
    }

    private static string LimpiarIdUsuario(string idUsuario)
    {
        if (string.IsNullOrWhiteSpace(idUsuario))
        {
            throw new ArgumentException("No fue posible identificar al usuario autenticado.");
        }

        var idUsuarioLimpio = idUsuario.Trim();
        if (idUsuarioLimpio.Length > 255)
        {
            throw new ArgumentException("El identificador del usuario no es válido.");
        }

        return idUsuarioLimpio;
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

using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

internal static class GeneradorMetadatosColillaPago
{
    private const string EstadoActivo = "Activo";

    public static async Task<int> AgregarPendientesAsync(
        ApplicationDbContext contexto,
        long idPlanilla,
        string? idUsuario,
        DateTime fechaGeneracion,
        CancellationToken cancellationToken)
    {
        var detallesPendientes = await contexto.DetallesPlanilla
            .Where(detalle => detalle.IdPlanilla == idPlanilla
                && detalle.ColillaPago == null)
            .Select(detalle => detalle.IdDetallePlanilla)
            .ToListAsync(cancellationToken);

        foreach (var idDetallePlanilla in detallesPendientes)
        {
            contexto.ColillasPago.Add(new ColillaPago
            {
                IdDetallePlanilla = idDetallePlanilla,
                CodigoColilla = $"COL-{Guid.NewGuid():N}".ToUpperInvariant(),
                RutaArchivo = null,
                FechaGeneracion = fechaGeneracion,
                GeneradoPor = idUsuario,
                EstadoRegistro = EstadoActivo
            });
        }

        return detallesPendientes.Count;
    }
}

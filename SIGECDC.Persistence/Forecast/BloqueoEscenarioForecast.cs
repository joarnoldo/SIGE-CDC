using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Forecast;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

internal static class BloqueoEscenarioForecast
{
    internal static async Task<ForecastEscenario> ObtenerAsync(ApplicationDbContext db,long id,CancellationToken ct)
    {
        var tx=db.Database.CurrentTransaction ?? throw new InvalidOperationException("Forecast requiere transacción.");
        if(tx.GetDbTransaction().IsolationLevel!=IsolationLevel.Serializable) throw new InvalidOperationException("Forecast requiere Serializable.");
        await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.Transaction=tx.GetDbTransaction();
        cmd.CommandText="SELECT IdForecastEscenario FROM ForecastEscenario WHERE IdForecastEscenario=@id FOR UPDATE";
        var p=cmd.CreateParameter();p.ParameterName="@id";p.DbType=DbType.Int64;p.Value=id;cmd.Parameters.Add(p);
        if(await cmd.ExecuteScalarAsync(ct) is null)throw new KeyNotFoundException("Escenario no disponible.");
        var escenario=await db.ForecastEscenarios.SingleAsync(e=>e.IdForecastEscenario==id,ct);
        if(escenario.EstadoRegistro!="Activo")throw new KeyNotFoundException("Escenario no disponible.");
        return escenario;
    }
}

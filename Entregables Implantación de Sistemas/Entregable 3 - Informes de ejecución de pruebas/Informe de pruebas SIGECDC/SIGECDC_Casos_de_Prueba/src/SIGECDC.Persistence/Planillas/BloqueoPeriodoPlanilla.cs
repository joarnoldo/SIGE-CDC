using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

internal static class BloqueoPeriodoPlanilla
{
    public static async Task<IReadOnlyDictionary<long, PeriodoPlanilla>> ObtenerAsync(
        ApplicationDbContext contexto,
        IEnumerable<long> idsPeriodos,
        CancellationToken cancellationToken)
    {
        var ids = idsPeriodos
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, PeriodoPlanilla>();
        }

        var transaccion = contexto.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "El bloqueo de un período de planilla requiere una transacción activa.");
        var conexion = contexto.Database.GetDbConnection();

        if (conexion.State != ConnectionState.Open)
        {
            await conexion.OpenAsync(cancellationToken);
        }

        foreach (var idPeriodo in ids)
        {
            await using var comando = conexion.CreateCommand();
            comando.Transaction = transaccion.GetDbTransaction();
            comando.CommandText = """
                SELECT IdPeriodoPlanilla
                FROM PeriodoPlanilla
                WHERE IdPeriodoPlanilla = @idPeriodo
                FOR UPDATE;
                """;
            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@idPeriodo";
            parametro.DbType = DbType.Int64;
            parametro.Value = idPeriodo;
            comando.Parameters.Add(parametro);
            await comando.ExecuteScalarAsync(cancellationToken);
        }

        return await contexto.PeriodosPlanilla
            .Include(periodo => periodo.EstadoPlanilla)
            .Where(periodo => ids.Contains(periodo.IdPeriodoPlanilla))
            .ToDictionaryAsync(periodo => periodo.IdPeriodoPlanilla, cancellationToken);
    }

    public static async Task<PeriodoPlanilla?> ObtenerAsync(
        ApplicationDbContext contexto,
        long idPeriodoPlanilla,
        CancellationToken cancellationToken)
    {
        var periodos = await ObtenerAsync(
            contexto,
            [idPeriodoPlanilla],
            cancellationToken);
        return periodos.GetValueOrDefault(idPeriodoPlanilla);
    }
}

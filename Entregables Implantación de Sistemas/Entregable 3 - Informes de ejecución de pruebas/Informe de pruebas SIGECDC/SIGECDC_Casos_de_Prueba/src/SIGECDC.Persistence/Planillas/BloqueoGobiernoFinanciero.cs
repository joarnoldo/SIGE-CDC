using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

// Un mutex MySQL por base evita carreras incluso cuando la clave aún no tiene una fila.
// Volumen de configuración pequeño; no requiere tabla nueva ni cambia el esquema aprobado.
internal sealed class BloqueoGobiernoFinanciero(ApplicationDbContext contexto) : IAsyncDisposable
{
    private bool adquirido;
    private bool abrir;
    public static async Task<BloqueoGobiernoFinanciero> AdquirirAsync(ApplicationDbContext contexto, CancellationToken ct)
    {
        var bloqueo = new BloqueoGobiernoFinanciero(contexto);
        var conexion = contexto.Database.GetDbConnection();
        bloqueo.abrir = conexion.State != ConnectionState.Open;
        if (bloqueo.abrir) await contexto.Database.OpenConnectionAsync(ct);
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT GET_LOCK(CONCAT('SIGECDC_FIN_GOB_',LEFT(SHA2(DATABASE(),256),40)),15)";
            bloqueo.adquirido = Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) == 1;
            if (!bloqueo.adquirido) throw new InvalidOperationException("Gobierno financiero ocupado; reintente la operación completa.");
            return bloqueo;
        }
        catch { await bloqueo.DisposeAsync(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (adquirido)
            {
                await using var comando = contexto.Database.GetDbConnection().CreateCommand();
                comando.CommandText = "SELECT RELEASE_LOCK(CONCAT('SIGECDC_FIN_GOB_',LEFT(SHA2(DATABASE(),256),40)))";
                await comando.ExecuteScalarAsync();
                adquirido = false;
            }
        }
        finally { if (abrir) await contexto.Database.CloseConnectionAsync(); }
    }
}

using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService(ApplicationDbContext contexto)
    : IGobiernoFinancieroService, IResolucionTemporalFinancieraService, ICondicionLaboralService, INucleoConceptosService, IIncidenciasCalculadasService, ICuotasSocialesService, IRentaSalarialService, ICostosPatronalesService, ISnapshotPlanillaService
{
    private const string Activo = "Activo";
    private const string Inactivo = "Inactivo";

    private async Task<T> EjecutarAsync<T>(string actor, Func<Task<T>> operacion, CancellationToken ct,
        IsolationLevel aislamiento = IsolationLevel.ReadCommitted)
    {
        if (contexto.ChangeTracker.HasChanges())
            throw new InvalidOperationException("El gobierno financiero requiere una unidad de trabajo sin cambios pendientes.");
        contexto.ChangeTracker.Clear();
        // El lock precede a la transacción y a toda lectura que determine una escritura.
        await using var bloqueo = await BloqueoGobiernoFinanciero.AdquirirAsync(contexto, ct);
        await using var transaccion = await contexto.Database.BeginTransactionAsync(aislamiento, ct);
        try
        {
            await AutorizarAsync(actor, ct);
            var resultado = await operacion();
            await contexto.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task AutorizarAsync(string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new UnauthorizedAccessException("Falta el actor del gobierno financiero.");
        var identidad = await (
            from usuario in contexto.Users
            join asignacion in contexto.UserRoles on usuario.Id equals asignacion.UserId
            join rol in contexto.Roles on asignacion.RoleId equals rol.Id
            where usuario.Id == actor && usuario.EstadoRegistro == Activo
                && (rol.Name == "Administrador" || rol.Name == "Recursos Humanos")
            select usuario.Id).FirstOrDefaultAsync(ct);
        // MySQL compara la FK sin distinguir case/acentos; la revisión dual compara identidades canónicas.
        if (!string.Equals(identidad, actor, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("El gobierno financiero requiere un actor activo Administrador o Recursos Humanos.");
    }

    private void Auditar(string actor, string accion, string entidad, object id, object datos)
        => contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = actor, FechaHora = DateTime.UtcNow, Accion = accion, Entidad = entidad,
            IdRegistro = Convert.ToString(id), ValoresNuevos = JsonSerializer.Serialize(datos),
            Observacion = "F1-FIN-02C: gobierno de configuración; FIN-02 continúa contenido."
        });

    private static string Texto(string? valor, int longitud, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor != valor.Trim() || valor.Length > longitud)
            throw new ArgumentException($"{campo}: texto obligatorio, sin espacios exteriores y máximo {longitud} caracteres.");
        return valor;
    }

    private static string Clave(string valor, int longitud = 100)
    {
        Texto(valor, longitud, "Clave");
        // Claves estables ASCII evitan equivalencias por acentos/case de la collation MySQL.
        if (valor.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-' or '.')))
            throw new ArgumentException("Las claves nuevas deben usar A-Z, 0-9, guion, punto o guion bajo.");
        return valor;
    }

    private static void Opcion(string valor, params string[] opciones)
    {
        if (!opciones.Contains(valor, StringComparer.Ordinal)) throw new ArgumentException("Valor de catálogo no admitido: " + valor);
    }
    private static void DecimalExacto(decimal valor, int escala, decimal minimo, decimal maximo)
    {
        if (valor < minimo || valor > maximo || decimal.Round(valor, escala) != valor)
            throw new ArgumentException("Valor fuera de rango o precisión de almacenamiento; no se redondea durante la carga.");
    }
    private static VigenciaFinanciera Vigencia(DateTime inicio, DateTime? fin) => new(inicio, fin);
    private static void SinSolape(VigenciaFinanciera nueva, IEnumerable<VigenciaFinanciera> existentes, string clave)
    {
        nueva.Validar();
        if (existentes.Any(nueva.Solapa)) throw new InvalidOperationException("Vigencias solapadas o ambiguas: " + clave);
    }
    private static void Dentro(VigenciaFinanciera padre, VigenciaFinanciera hija)
    {
        hija.Validar();
        if (!padre.Contiene(hija)) throw new ArgumentException("La vigencia del dato debe estar dentro de su versión normativa.");
    }
    private static bool NormativaUtilizable(VersionNormativa? version) => version is
        { EstadoRegistro: Activo, FechaActivacion: not null, CreadoPor: not null, ActivadoPor: not null }
        && version.CreadoPor != version.ActivadoPor && !string.IsNullOrWhiteSpace(version.HuellaContenido);
    private static string Huella(object datos) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datos))));
}

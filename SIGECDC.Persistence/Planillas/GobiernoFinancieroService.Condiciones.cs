using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<long> RegistrarCondicionAsync(NuevaCondicionLaboral solicitud, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            solicitud.Vigencia.Validar(); Texto(solicitud.ReferenciaSoporte, 500, "Evidencia de condición laboral");
            DecimalExacto(solicitud.SalarioBase, 2, 0.01m, 9999999999999999.99m);
            // El mutex de C serializa el conjunto; locks de filas coordinan además con actualizaciones legadas.
            await contexto.Database.ExecuteSqlInterpolatedAsync($"SELECT IdContrato FROM Contrato WHERE IdContrato={solicitud.IdContrato} FOR UPDATE", ct);
            var contrato = await contexto.Contratos.AsNoTracking().SingleAsync(c => c.IdContrato == solicitud.IdContrato, ct);
            await contexto.Database.ExecuteSqlInterpolatedAsync($"SELECT IdColaborador FROM Colaborador WHERE IdColaborador={contrato.IdColaborador} FOR UPDATE", ct);
            contrato = await contexto.Contratos.AsNoTracking().Include(c => c.Colaborador).ThenInclude(c => c!.Puesto)
                .SingleAsync(c => c.IdContrato == solicitud.IdContrato, ct);
            await contexto.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPuesto FROM Puesto WHERE IdPuesto={contrato.Colaborador!.IdPuesto} FOR UPDATE", ct);
            // Leer la homologación actual después del lock; solo se usa para detectar cambios legados.
            contrato.Colaborador.Puesto = await contexto.Puestos.AsNoTracking().SingleAsync(p => p.IdPuesto == contrato.Colaborador.IdPuesto, ct);
            var vigenciaContrato = Vigencia(contrato.FechaInicio, contrato.FechaFin);
            vigenciaContrato.Validar();
            if (!vigenciaContrato.Contiene(solicitud.Vigencia))
                throw new ArgumentException("La condición laboral debe estar dentro de las fechas demostradas del contrato.");
            if (contrato.EstadoContrato is not ("Activo" or "Finalizado")) throw new InvalidOperationException("Relación contractual sin vigencia remunerable demostrable.");
            if (!await contexto.Puestos.AnyAsync(p => p.IdPuesto == solicitud.IdPuesto, ct)
                || !await contexto.Departamentos.AnyAsync(d => d.IdDepartamento == solicitud.IdDepartamento, ct)
                || !await contexto.OcupacionesSalarioMinimo.AnyAsync(o => o.IdOcupacionSalarioMinimo == solicitud.IdOcupacionSalarioMinimo && o.EstadoRegistro == Activo, ct))
                throw new InvalidOperationException("Dimensiones laborales faltantes o no disponibles.");
            var jornada = await contexto.JornadasLaborales.AsNoTracking().SingleOrDefaultAsync(j => j.IdJornadaLaboral == solicitud.IdJornadaLaboral, ct);
            if (jornada is null || jornada.EstadoRegistro != Activo || string.IsNullOrWhiteSpace(jornada.CreadoPor)
                || !Vigencia(jornada.FechaVigenciaInicio, jornada.FechaVigenciaFin).Contiene(solicitud.Vigencia.Inicio))
                throw new InvalidOperationException("Jornada estructurada no disponible en la fecha efectiva.");

            var existentes = await contexto.CondicionesLaboralesVersion.Where(v => v.IdContrato == solicitud.IdContrato).ToListAsync(ct);
            var ultima = existentes.OrderByDescending(v => v.IdCondicionLaboralVersion).FirstOrDefault();
            if (ultima?.IdCondicionLaboralVersion != solicitud.IdUltimaVersionEsperada)
                throw new InvalidOperationException("Conflicto concurrente: consultar nuevamente las condiciones y su última versión.");
            foreach (var existente in existentes)
                if (!await CondicionConfirmadaAsync(existente, ct)) throw new InvalidOperationException("La historia confirmada fue alterada o carece de evidencia.");
            // Si una operación legada alteró la proyección, solo una condición explícita que la
            // confirme puede reconocer ese cambio. No fabricar vigencias desde la edición legada.
            if (ultima is not null && !await ProyeccionesCompatiblesAsync(contrato, ultima, ct) && !CoincideProyeccion(solicitud, contrato))
                throw new InvalidOperationException("La proyección legada cambió; la nueva condición debe confirmar explícitamente ese cambio con evidencia.");
            var abierta = existentes.SingleOrDefault(v => v.FechaVigenciaFin is null);
            if (abierta is not null && solicitud.Vigencia.Inicio > abierta.FechaVigenciaInicio)
            {
                if (abierta.EstadoRegistro != Activo) throw new InvalidOperationException("No sustituir una condición inactiva sin resolver su evidencia.");
                abierta.FechaVigenciaFin = solicitud.Vigencia.Inicio.AddDays(-1);
                AuditarCondicion(abierta, contrato, actor, "CerrarCondicion", finAnterior: null);
            }
            SinSolape(solicitud.Vigencia, existentes.Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), "CONDICION_LABORAL");
            var nueva = new CondicionLaboralVersion
            {
                IdContrato = solicitud.IdContrato, FechaVigenciaInicio = solicitud.Vigencia.Inicio, FechaVigenciaFin = solicitud.Vigencia.Fin,
                SalarioBase = solicitud.SalarioBase, IdPuesto = solicitud.IdPuesto, IdDepartamento = solicitud.IdDepartamento,
                IdOcupacionSalarioMinimo = solicitud.IdOcupacionSalarioMinimo, IdJornadaLaboral = solicitud.IdJornadaLaboral,
                ReferenciaSoporte = solicitud.ReferenciaSoporte, FechaCreacion = DateTime.UtcNow, CreadoPor = actor
            };
            contexto.CondicionesLaboralesVersion.Add(nueva);
            await contexto.SaveChangesAsync(ct);
            AuditarCondicion(nueva, contrato, actor, "RegistrarCondicion");
            return nueva.IdCondicionLaboralVersion;
        }, ct);

    public Task<IReadOnlyList<CondicionLaboralResumen>> ConsultarCondicionesAsync(long idContrato, string actor, CancellationToken ct = default)
        => LeerTemporalAsync<IReadOnlyList<CondicionLaboralResumen>>(actor, async () =>
        {
            var filas = await contexto.CondicionesLaboralesVersion.AsNoTracking().Where(v => v.IdContrato == idContrato)
                .OrderBy(v => v.FechaVigenciaInicio).ToListAsync(ct);
            var ultima = filas.Count == 0 ? (long?)null : filas.Max(v => v.IdCondicionLaboralVersion);
            return filas.Select(v => new CondicionLaboralResumen(v.IdCondicionLaboralVersion,
                new(v.IdContrato, Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin), v.SalarioBase, v.IdPuesto, v.IdDepartamento,
                    v.IdOcupacionSalarioMinimo, v.IdJornadaLaboral, v.ReferenciaSoporte, ultima), v.FechaCreacion, v.CreadoPor, v.EstadoRegistro)).ToArray();
        }, ct);

    private sealed record EvidenciaCondicion(string HuellaCondicion, string HuellaContrato, string HuellaColaborador, long IdColaborador, DateTime? FinAnterior);
    private static string HuellaCondicion(CondicionLaboralVersion v) => Huella(new
    {
        v.IdCondicionLaboralVersion, v.IdContrato,
        Inicio = v.FechaVigenciaInicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Fin = v.FechaVigenciaFin?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        SalarioBase = v.SalarioBase.ToString("F2", CultureInfo.InvariantCulture),
        v.IdPuesto, v.IdDepartamento, v.IdOcupacionSalarioMinimo, v.IdJornadaLaboral, v.ReferenciaSoporte, v.CreadoPor, v.EstadoRegistro
    });
    private static string HuellaProyeccionContrato(Contrato c) => Huella(new
    { c.IdColaborador, c.FechaInicio, c.FechaFin, c.SalarioBase, c.IdJornadaLaboral, c.Jornada, c.TipoContrato, c.PeriodicidadPago });
    private static string HuellaProyeccionColaborador(Colaborador c) => Huella(new
    { c.IdPuesto, c.IdDepartamento, c.Puesto?.IdOcupacionSalarioMinimo });
    private static bool CoincideProyeccion(NuevaCondicionLaboral s, Contrato c) => s.SalarioBase == c.SalarioBase
        && s.IdJornadaLaboral == c.IdJornadaLaboral && s.IdPuesto == c.Colaborador!.IdPuesto && s.IdDepartamento == c.Colaborador.IdDepartamento
        && s.IdOcupacionSalarioMinimo == c.Colaborador.Puesto?.IdOcupacionSalarioMinimo;

    private void AuditarCondicion(CondicionLaboralVersion v, Contrato c, string actor, string accion, DateTime? finAnterior = null)
        => contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = actor, FechaHora = DateTime.UtcNow, Accion = accion, Entidad = nameof(CondicionLaboralVersion),
            IdRegistro = v.IdCondicionLaboralVersion.ToString(),
            ValoresNuevos = JsonSerializer.Serialize(new EvidenciaCondicion(HuellaCondicion(v), HuellaProyeccionContrato(c),
                HuellaProyeccionColaborador(c.Colaborador!), c.IdColaborador, finAnterior)),
            Observacion = "F1-FIN-02D: condición efectiva confirmada; FIN-02 contenido. Huellas de proyección solo para detectar cambios, no como fuente histórica."
        });

    private async Task<EvidenciaCondicion?> EvidenciaCondicionAsync(long id, CancellationToken ct)
    {
        var registro = id.ToString();
        var json = await contexto.BitacoraAuditoria.AsNoTracking()
            .Where(b => b.Entidad == nameof(CondicionLaboralVersion) && b.IdRegistro == registro
                && (b.Accion == "RegistrarCondicion" || b.Accion == "CerrarCondicion"))
            .OrderByDescending(b => b.IdBitacoraAuditoria).Select(b => b.ValoresNuevos).FirstOrDefaultAsync(ct);
        try { return json is null ? null : JsonSerializer.Deserialize<EvidenciaCondicion>(json); }
        catch (JsonException) { return null; }
    }
    private async Task<bool> CondicionConfirmadaAsync(CondicionLaboralVersion v, CancellationToken ct)
        => v.EstadoRegistro == Activo && !string.IsNullOrWhiteSpace(v.ReferenciaSoporte)
            && !string.IsNullOrWhiteSpace(v.CreadoPor) && (await EvidenciaCondicionAsync(v.IdCondicionLaboralVersion, ct))?.HuellaCondicion == HuellaCondicion(v);

    private async Task<bool> ProyeccionesCompatiblesAsync(Contrato c, CondicionLaboralVersion ultimaContrato, CancellationToken ct)
    {
        var evidencia = await EvidenciaCondicionAsync(ultimaContrato.IdCondicionLaboralVersion, ct);
        if (evidencia?.HuellaContrato != HuellaProyeccionContrato(c)) return false;
        // La proyección del colaborador es compartida por sus contratos. Comparar su última
        // confirmación explícita, sin reinterpretar condiciones anteriores por el puesto actual.
        var ultimaPersona = await contexto.CondicionesLaboralesVersion.AsNoTracking()
            .Where(v => v.Contrato!.IdColaborador == c.IdColaborador).OrderByDescending(v => v.IdCondicionLaboralVersion).FirstAsync(ct);
        var persona = await EvidenciaCondicionAsync(ultimaPersona.IdCondicionLaboralVersion, ct);
        return persona?.IdColaborador == c.IdColaborador && persona.HuellaColaborador == HuellaProyeccionColaborador(c.Colaborador!);
    }
}

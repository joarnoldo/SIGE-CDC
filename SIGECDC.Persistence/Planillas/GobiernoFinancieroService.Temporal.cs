using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<ResultadoTemporalSalarial> ResolverPeriodoTemporalAsync(PeriodoSalarial periodo, string actor,
        string? referenciaAplicabilidadOrdinaria = null, CancellationToken ct = default)
        => LeerTemporalAsync(actor, () => ResolverTemporalEnTransaccionAsync(periodo, referenciaAplicabilidadOrdinaria, ct), ct);

    private async Task<ResultadoTemporalSalarial> ResolverTemporalEnTransaccionAsync(PeriodoSalarial periodo,
        string? referenciaAplicabilidadOrdinaria, CancellationToken ct)
        {
            periodo.Validar();
            // Incluye finalizados e inactivos: el estado actual no es evidencia de devengo histórico.
            var contratos = await contexto.Contratos.AsNoTracking()
                .Include(c => c.Colaborador).ThenInclude(c => c!.Puesto).Include(c => c.JornadaLaboral)
                .Include(c => c.CondicionesLaborales).ThenInclude(v => v.JornadaLaboral)
                .Include(c => c.CondicionesLaborales).ThenInclude(v => v.OcupacionSalarioMinimo)
                .Where(c => (c.FechaInicio <= periodo.Fin && (c.FechaFin == null || c.FechaFin >= periodo.Inicio))
                    || c.FechaFin < c.FechaInicio || c.CondicionesLaborales.Any(v => v.FechaVigenciaInicio <= periodo.Fin
                        && (v.FechaVigenciaFin == null || v.FechaVigenciaFin >= periodo.Inicio))).AsSplitQuery().ToListAsync(ct);
            var condiciones = contratos.SelectMany(c => c.CondicionesLaborales).ToArray();
            var jornadas = condiciones.Where(v => v.JornadaLaboral != null).Select(v => v.JornadaLaboral!.Codigo).Distinct().ToArray();
            var ocupaciones = condiciones.Select(v => v.IdOcupacionSalarioMinimo).Distinct().ToArray();
            var catalogos = await CatalogosTemporalesAsync(periodo, jornadas, ocupaciones, ct);
            var datos = new List<DatosTemporalesColaborador>();
            foreach (var grupo in contratos.GroupBy(c => c.IdColaborador))
            {
                var colaborador = grupo.First().Colaborador;
                var bloqueos = new List<BloqueoTemporal>();
                if (colaborador is null) bloqueos.Add(new("COLABORADOR_FALTANTE", periodo.Inicio));
                var tramos = new List<ContratoTemporal>();
                var ocupacionesPersona = new List<OcupacionTemporal>();
                foreach (var c in grupo)
                {
                    if (c.EstadoRegistro != Activo || c.EstadoContrato is not ("Activo" or "Finalizado")
                        || (c.EstadoContrato == "Finalizado" && c.FechaFin is null))
                        bloqueos.Add(new("ESTADO_CONTRACTUAL_SIN_VIGENCIA_DEMOSTRABLE", periodo.Inicio, c.IdContrato));
                    var ultima = c.CondicionesLaborales.OrderByDescending(v => v.IdCondicionLaboralVersion).FirstOrDefault();
                    if (ultima is not null && !await ProyeccionesCompatiblesAsync(c, ultima, ct))
                        bloqueos.Add(new("PROYECCION_MODIFICADA_SIN_CONDICION", periodo.Inicio, c.IdContrato));
                    var rangoContrato = Vigencia(c.FechaInicio, c.FechaFin);
                    try { rangoContrato.Validar(); }
                    catch (ArgumentException) { bloqueos.Add(new("CONTRATO_FECHAS_INVALIDAS", periodo.Inicio, c.IdContrato)); continue; }
                    var confirmadas = new HashSet<long>();
                    foreach (var v in c.CondicionesLaborales)
                    {
                        var vigencia = Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin);
                        try { vigencia.Validar(); }
                        catch (ArgumentException) { bloqueos.Add(new("CONDICION_FECHAS_INVALIDAS", periodo.Inicio, c.IdContrato)); continue; }
                        if (!rangoContrato.Contiene(vigencia)) bloqueos.Add(new("CONDICION_FUERA_CONTRATO", v.FechaVigenciaInicio, c.IdContrato));
                        if (await CondicionConfirmadaAsync(v, ct)) confirmadas.Add(v.IdCondicionLaboralVersion);
                        else bloqueos.Add(new("CONDICION_NO_CONFIRMADA", v.FechaVigenciaInicio, c.IdContrato));
                    }
                    var inicio = c.FechaInicio > periodo.Inicio ? c.FechaInicio : periodo.Inicio;
                    var fin = c.FechaFin is { } f && f < periodo.Fin ? f : periodo.Fin;
                    if (fin < inicio) continue;
                    var fronteras = c.CondicionesLaborales.SelectMany(v => v.FechaVigenciaFin is { } vf && vf < fin
                        ? new[] { v.FechaVigenciaInicio, vf.AddDays(1) } : new[] { v.FechaVigenciaInicio });
                    foreach (var tramo in CalendarioSalarial.Dividir(new(inicio, fin), fronteras))
                    {
                        var versiones = c.CondicionesLaborales.Where(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin).Contiene(tramo.Inicio)).ToArray();
                        if (versiones.Length != 1)
                        {
                            bloqueos.Add(new(versiones.Length == 0 ? "HISTORIA_NO_DEMOSTRABLE" : "CONDICIONES_SOLAPADAS", tramo.Inicio, c.IdContrato));
                            if (versiones.Length == 0)
                            {
                                bloqueos.Add(new("OCUPACION_HISTORICA_NO_DEMOSTRABLE", tramo.Inicio, c.IdContrato));
                                if (colaborador?.Puesto?.IdOcupacionSalarioMinimo is null) bloqueos.Add(new("OCUPACION_NO_HOMOLOGADA", tramo.Inicio, c.IdContrato));
                            }
                            tramos.Add(new(c.IdContrato, tramo, 0, null, referenciaAplicabilidadOrdinaria, false));
                            continue;
                        }
                        var v = versiones[0];
                        tramos.Add(new(c.IdContrato, tramo, v.SalarioBase, v.JornadaLaboral?.Codigo, referenciaAplicabilidadOrdinaria,
                            confirmadas.Contains(v.IdCondicionLaboralVersion), v.IdCondicionLaboralVersion, v.IdPuesto, v.IdDepartamento, v.IdJornadaLaboral));
                        ocupacionesPersona.Add(new(v.IdOcupacionSalarioMinimo, v.OcupacionSalarioMinimo?.CodigoOficial ?? "",
                            tramo, v.ReferenciaSoporte, c.IdContrato));
                    }
                }
                // La relación remunerable proviene del contrato; no de atributos actuales del colaborador.
                datos.Add(new(grupo.Key, new(grupo.Min(c => c.FechaInicio), null), tramos, ocupacionesPersona, bloqueos));
            }
            return ResolvedorTemporalSalarial.Resolver(periodo, datos, catalogos.Jornadas, catalogos.Minimos);
        }

    public Task<CatalogosTemporalesSalariales> ConsultarCatalogosTemporalesAsync(PeriodoSalarial periodo,
        IReadOnlyList<string> codigosJornada, IReadOnlyList<long> idsOcupacion, string actor, CancellationToken ct = default)
        => LeerTemporalAsync(actor, () => CatalogosTemporalesAsync(periodo, codigosJornada, idsOcupacion, ct), ct);

    private async Task<CatalogosTemporalesSalariales> CatalogosTemporalesAsync(PeriodoSalarial periodo,
        IReadOnlyList<string> codigosJornada, IReadOnlyList<long> idsOcupacion, CancellationToken ct)
    {
        periodo.Validar();
        var codigos = codigosJornada.ToArray();
        var ocupaciones = idsOcupacion.ToArray();
        var jornadas = await FiltrarClaves(contexto.JornadasLaborales.AsNoTracking(), j => j.Codigo, codigos)
            .Where(j => j.FechaVigenciaInicio <= periodo.Fin
                && (j.FechaVigenciaFin == null || j.FechaVigenciaFin >= periodo.Inicio)).ToListAsync(ct);
        var minimos = await FiltrarClaves(contexto.SalariosMinimosVersion.AsNoTracking(), m => m.IdOcupacionSalarioMinimo, ocupaciones)
            .Include(m => m.VersionNormativa)
            .Include(m => m.OcupacionSalarioMinimo)
            .Where(m => m.FechaVigenciaInicio <= periodo.Fin
                && (m.FechaVigenciaFin == null || m.FechaVigenciaFin >= periodo.Inicio)).ToListAsync(ct);
        var verificadas = new HashSet<long>();
        foreach (var norma in minimos.Select(m => m.VersionNormativa).Where(NormativaUtilizable).DistinctBy(n => n!.IdVersionNormativa))
        {
            var huella = await HuellaRevisionAsync(norma!.IdVersionNormativa, ct);
            if (huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "CargarNormativa", ct)
                && huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "ActivarNormativa", ct))
                verificadas.Add(norma.IdVersionNormativa);
        }
        return new(jornadas.Select(j => new JornadaTemporal(j.IdJornadaLaboral, j.Codigo, j.TipoJornada,
            j.HorasOrdinariasDia, j.HorasOrdinariasSemana, new(j.FechaVigenciaInicio, j.FechaVigenciaFin),
            j.EstadoRegistro == Activo && j.FechaModificacion is null && !string.IsNullOrWhiteSpace(j.CreadoPor))).ToArray(),
            minimos.Select(m => new MinimoTemporal(m.IdSalarioMinimoVersion, m.IdOcupacionSalarioMinimo,
                m.PeriodicidadReferencia, m.TipoJornadaAplicable, m.Monto, new(m.FechaVigenciaInicio, m.FechaVigenciaFin),
                m.IdVersionNormativa, new(m.VersionNormativa?.FechaVigenciaInicio ?? periodo.Inicio, m.VersionNormativa?.FechaVigenciaFin),
                verificadas.Contains(m.IdVersionNormativa) && m.OcupacionSalarioMinimo?.EstadoRegistro == Activo)).ToArray());
    }

    // El proveedor instalado no traduce las colecciones primitivas de estas consultas.
    // Disyunción de igualdades tipadas: una consulta por catálogo, sin SQL concatenado ni lectura global.
    private static IQueryable<T> FiltrarClaves<T, TClave>(IQueryable<T> consulta,
        Expression<Func<T, TClave>> selector, IEnumerable<TClave> claves)
    {
        Expression condicion = Expression.Constant(false);
        foreach (var clave in claves.Distinct())
            condicion = Expression.OrElse(condicion, Expression.Equal(selector.Body, Expression.Constant(clave, typeof(TClave))));
        return consulta.Where(Expression.Lambda<Func<T, bool>>(condicion, selector.Parameters));
    }

    private async Task<T> LeerTemporalAsync<T>(string actor, Func<Task<T>> leer, CancellationToken ct)
    {
        if (contexto.ChangeTracker.HasChanges())
            throw new InvalidOperationException("La resolución temporal requiere una unidad de trabajo sin cambios pendientes.");
        // Snapshot MVCC coherente incluso frente a escritores de RRHH que no usan el mutex de gobierno.
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        await AutorizarAsync(actor, ct);
        var resultado = await leer();
        await transaccion.CommitAsync(ct);
        return resultado; // Lectura pura de persistencia: sin SaveChanges ni escritura de snapshots.
    }
}

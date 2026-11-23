using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

// Frontera única de publicación K: estado, ownership y grafo se leen bajo el lock FIN-01.
internal sealed class LectorPublicacionPlanilla(ApplicationDbContext db)
{
    internal async Task<long?> AutorizarAsync(string actor, bool propio, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new UnauthorizedAccessException("Acceso no autorizado.");
        var filas = await (from u in db.Users.AsNoTracking()
            join ur in db.UserRoles on u.Id equals ur.UserId
            join r in db.Roles on ur.RoleId equals r.Id
            where u.Id == actor && u.EstadoRegistro == "Activo"
                && (propio ? r.Name == "Empleado" : r.Name == "Administrador" || r.Name == "Recursos Humanos")
            select u.Id).ToArrayAsync(ct);
        if (!filas.Any(id => string.Equals(id, actor, StringComparison.Ordinal)))
            throw new UnauthorizedAccessException("Acceso no autorizado.");
        if (!propio) return null;
        var vinculos = await db.Colaboradores.AsNoTracking().Where(c => c.IdUsuario == actor && c.EstadoRegistro == "Activo")
            .Select(c => new { c.IdColaborador, c.IdUsuario }).ToArrayAsync(ct);
        if (vinculos.Length != 1 || !string.Equals(vinculos[0].IdUsuario, actor, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Colilla no disponible.");
        return vinculos[0].IdColaborador;
    }

    internal async Task<IReadOnlyList<long>> PeriodosAsync(string actor, bool propio, CancellationToken ct)
    {
        var colaborador = await AutorizarAsync(actor, propio, ct);
        return await db.EjecucionesPlanilla.AsNoTracking()
            .Where(e => e.SnapshotCompleto && e.EstadoReproducibilidad == "REPRODUCIBLE"
                && e.Planilla != null && e.Planilla.EstadoRegistro == "Activo"
                && e.Planilla.IdEjecucionPlanillaVigente == e.IdEjecucionPlanilla
                && e.Planilla.EstadoPlanilla != null
                && (e.Planilla.EstadoPlanilla.Nombre == "Aprobada" || e.Planilla.EstadoPlanilla.Nombre == "Cerrada"
                    || !propio && e.Planilla.EstadoPlanilla.Nombre == "Calculada")
                && (!propio || e.Detalles.Any(d => d.IdColaborador == colaborador && d.SnapshotCompleto)))
            .OrderByDescending(e => e.FechaInicioSnapshot).ThenByDescending(e => e.IdEjecucionPlanilla)
            .Select(e => e.Planilla!.IdPeriodoPlanilla).ToArrayAsync(ct);
    }

    internal async Task<ReportePlanilla?> ConsultarAsync(long periodo, string actor, bool propio, CancellationToken ct,
        long? idColilla = null)
    {
        if (db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("La publicación requiere una unidad de lectura propia.");
        if (idColilla is { } cid)
        {
            var propietario = await AutorizarAsync(actor, propio, ct);
            // Solo localizar el lock, sin conservar locks compartidos de Planilla antes del período.
            // Ownership, metadata y ejecución se vuelven a comprobar dentro de la transacción.
            periodo = await db.ColillasPago.AsNoTracking().Where(c => c.IdColillaPago == cid && c.EstadoRegistro == "Activo"
                && c.DetallePlanilla != null && (!propio || c.DetallePlanilla.IdColaborador == propietario))
                .Select(c => c.DetallePlanilla!.Planilla!.IdPeriodoPlanilla).SingleOrDefaultAsync(ct);
            if (periodo <= 0) return null;
        }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var colaborador = await AutorizarAsync(actor, propio, ct);
            if (periodo <= 0) return null;
            await BloqueoPeriodoPlanilla.ObtenerAsync(db, periodo, ct);
            var p = await db.Planillas.AsNoTracking().Include(p => p.EstadoPlanilla)
                .Include(p => p.PeriodoPlanilla).ThenInclude(p => p!.EstadoPlanilla)
                .SingleOrDefaultAsync(p => p.IdPeriodoPlanilla == periodo && p.EstadoRegistro == "Activo", ct);
            if (p?.IdEjecucionPlanillaVigente is not { } eid) return null;
            var estado = p.EstadoPlanilla?.Nombre;
            if (estado is not ("Calculada" or "Aprobada" or "Cerrada") || propio && estado == "Calculada"
                || p.PeriodoPlanilla?.EstadoPlanilla?.Nombre != estado || p.PeriodoPlanilla.EstadoRegistro != "Activo") return null;
            if (idColilla is { } anterior && !await db.ColillasPago.AsNoTracking().AnyAsync(c => c.IdColillaPago == anterior
                && c.EstadoRegistro == "Activo" && c.DetallePlanilla!.IdEjecucionPlanilla == eid
                && (!propio || c.DetallePlanilla.IdColaborador == colaborador), ct)) return null;
            // Misma forma de grafo que la lectura histórica J; no invoca motores ni su conciliación matemática.
            var e = await db.EjecucionesPlanilla.AsNoTracking().Include(e => e.Detalles.Where(d => !propio || d.IdColaborador == colaborador))
                .ThenInclude(d => d.Segmentos)
                .Include(e => e.Detalles.Where(d => !propio || d.IdColaborador == colaborador)).ThenInclude(d => d.Conceptos)
                .ThenInclude(l => l.AplicacionesNormativas).AsSplitQuery()
                .SingleOrDefaultAsync(e => e.IdEjecucionPlanilla == eid, ct);
            if (e is null || !e.SnapshotCompleto || e.EstadoReproducibilidad != "REPRODUCIBLE" || e.IdPlanilla != p.IdPlanilla
                || e.EstadoEjecucion != (estado == "Calculada" ? "Calculada" : "Autorizada") || e.Detalles.Count == 0) return null;
            if (p.SalarioBrutoTotal != e.SalarioBrutoTotal || p.DeduccionesTotal != e.DeduccionesTotal
                || p.SalarioNetoTotal != e.SalarioNetoTotal || p.CostoPatronalEstimadoTotal != e.CargasPatronalesTotal)
                throw new InvalidOperationException("REPORTE_SNAPSHOT_NO_CONCILIA");
            if (!propio) PublicacionPlanilla.Conciliar(e);
            var ids = e.Detalles.SelectMany(d => d.Conceptos).Select(l => l.IdConceptoPlanillaVersion).Distinct().ToArray();
            var versiones = await db.ConceptosPlanillaVersion.AsNoTracking().Where(v => ids.Contains(v.IdConceptoPlanillaVersion))
                .ToDictionaryAsync(v => v.IdConceptoPlanillaVersion, ct);
            var detalleIds = e.Detalles.Select(d => d.IdDetallePlanilla).ToArray();
            var metadata = await db.ColillasPago.AsNoTracking().Where(c => detalleIds.Contains(c.IdDetallePlanilla))
                .ToDictionaryAsync(c => c.IdDetallePlanilla, ct);
            var generadas = 0;
            foreach (var d in e.Detalles.Where(d => !metadata.ContainsKey(d.IdDetallePlanilla)))
            {
                var m = new ColillaPago { IdDetallePlanilla = d.IdDetallePlanilla, CodigoColilla = $"COL-{Guid.NewGuid():N}".ToUpperInvariant(),
                    FechaGeneracion = DateTime.UtcNow, GeneradoPor = actor, EstadoRegistro = "Activo" };
                metadata.Add(d.IdDetallePlanilla, m); db.ColillasPago.Add(m); generadas++;
            }
            if (metadata.Values.Any(m => m.EstadoRegistro != "Activo")) return null;
            await db.SaveChangesAsync(ct); // Unique por detalle + lock de período: generación idempotente.
            var detalles = e.Detalles.OrderBy(d => d.CodigoColaborador, StringComparer.Ordinal).ThenBy(d => d.IdDetallePlanilla)
                .Select(d =>
                {
                    var colilla = PublicacionPlanilla.Proyectar(e, d, versiones, estado!, metadata[d.IdDetallePlanilla], !propio);
                    colilla.IdPeriodoPlanilla = periodo;
                    return colilla;
                }).ToArray();
            var r = new ReportePlanilla { ColillasGeneradas = generadas, IdPeriodoPlanilla = periodo, IdEjecucionPlanilla = eid, NumeroVersion = e.NumeroVersion,
                CodigoPeriodo = detalles[0].CodigoPeriodo, NombrePeriodo = e.TipoPeriodoSnapshot, TipoPeriodo = e.TipoPeriodoSnapshot,
                FechaInicio = e.FechaInicioSnapshot, FechaFin = e.FechaFinSnapshot, EstadoPlanilla = estado!,
                FechaCalculo = e.FechaCalculo, FechaAprobacion = p.FechaAprobacion, FechaCierre = p.FechaCierre,
                SalarioBrutoTotal = e.SalarioBrutoTotal, IngresosNoSalarialesTotal = e.IngresosNoSalarialesTotal,
                BrutoPagaderoTotal = e.SalarioBrutoTotal + e.IngresosNoSalarialesTotal,
                DeduccionesTotal = e.DeduccionesTotal, SalarioNetoTotal = e.SalarioNetoTotal,
                CargasPatronalesTotal = e.CargasPatronalesTotal, RiesgosTrabajoTotal = e.RiesgosTrabajoTotal,
                OtrosCostosPatronalesTotal = e.OtrosCostosPatronalesTotal, CostoLaboralTotal = e.CostoLaboralTotal, Detalles = detalles };
            await tx.CommitAsync(ct);
            return r;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
        finally { db.ChangeTracker.Clear(); }
    }
}

using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    // Identidad mantenida del baseline monetario D–I. No depende de Git ni del reloj.
    internal const string VersionMotorSnapshot = "FIN-DI-V1";

    internal Task AutorizarOperacionPlanillaAsync(string actor, CancellationToken ct) => AutorizarAsync(actor, ct);

    public async Task<long> CalcularSnapshotAsync(SolicitudSnapshotPlanilla solicitud, string actor, CancellationToken ct = default)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("J requiere una unidad de trabajo independiente sin cambios pendientes.");
        solicitud = solicitud with { Cuotas = solicitud.Cuotas.ToArray(), Riesgos = solicitud.Riesgos.ToArray(), OtrosCostos = solicitud.OtrosCostos.ToArray() };
        contexto.ChangeTracker.Clear();
        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await AutorizarAsync(actor, ct);
            // Orden común FIN-01, incluyendo el período fuente Q1 cuando exista.
            long? periodoPrevio = solicitud.IdEjecucionPrevia is { } previa
                ? await contexto.EjecucionesPlanilla.Where(e => e.IdEjecucionPlanilla == previa)
                    .Select(e => (long?)e.Planilla!.IdPeriodoPlanilla).SingleOrDefaultAsync(ct) : null;
            await BloqueoPeriodoPlanilla.ObtenerAsync(contexto,
                new[] { solicitud.IdPeriodo }.Concat(periodoPrevio is { } p ? new[] { p } : []), ct);
            var periodo = await PeriodoIncidenciasEditableAsync(solicitud.IdPeriodo, ct);
            var acumulado = await AcumuladoSnapshotAsync(solicitud.IdEjecucionPrevia, periodo, ct);
            var resultado = await PrepararCostoLaboralEnTransaccionAsync(solicitud.IdPeriodo, solicitud.ReferenciaRelacionOrdinaria,
                solicitud.Cuotas, solicitud.Riesgos, solicitud.OtrosCostos, actor, acumulado, ct);
            if (!resultado.EsResoluble) throw new ConfiguracionConceptoException(string.Join(";", resultado.Bloqueos.Select(b => b.Codigo).Distinct()));
            if (resultado.NetoPreliminar < 0) throw new ConfiguracionConceptoException("NETO_NEGATIVO");
            var periododb = await BloqueoPeriodoPlanilla.ObtenerAsync(contexto, solicitud.IdPeriodo, ct)
                ?? throw new ConfiguracionConceptoException("PERIODO_FALTANTE");
            var planilla = await contexto.Planillas.SingleOrDefaultAsync(p => p.IdPeriodoPlanilla == solicitud.IdPeriodo, ct);
            if (planilla is null)
            {
                planilla = new() { IdPeriodoPlanilla = solicitud.IdPeriodo, IdEstadoPlanilla = periododb.IdEstadoPlanilla,
                    FechaCreacion = DateTime.UtcNow, CreadoPor = actor };
                contexto.Planillas.Add(planilla);
                await contexto.SaveChangesAsync(ct);
            }
            if (planilla.IdEstadoPlanilla != periododb.IdEstadoPlanilla || planilla.IdEjecucionPlanillaVigente is not null)
                throw new ConfiguracionConceptoException("EJECUCION_VIGENTE_INCOMPATIBLE");
            var ejecucion = new EjecucionPlanilla { IdPlanilla = planilla.IdPlanilla,
                NumeroVersion = (await contexto.EjecucionesPlanilla.Where(e => e.IdPlanilla == planilla.IdPlanilla)
                    .MaxAsync(e => (int?)e.NumeroVersion, ct) ?? 0) + 1,
                SnapshotCompleto = true, VersionMotor = VersionMotorSnapshot, CalculadoPor = actor, FechaCalculo = DateTime.UtcNow,
                FechaInicioSnapshot = periodo.Inicio, FechaFinSnapshot = periodo.Fin, TipoPeriodoSnapshot = periodo.Tipo };
            await ConstruirSnapshotAsync(ejecucion, resultado, solicitud, ct);
            TotalizarSnapshot(ejecucion);
            contexto.EjecucionesPlanilla.Add(ejecucion);
            await contexto.SaveChangesAsync(ct);
            var leida = await CargarSnapshotAsync(ejecucion.IdEjecucionPlanilla, ct);
            await ConciliarSnapshotAsync(leida, ct);
            if (leida.SalarioBrutoTotal != resultado.BrutoSalarial || leida.SalarioNetoTotal != resultado.NetoPreliminar
                || leida.CostoLaboralTotal != resultado.CostoLaboralTotal || leida.CargasPatronalesTotal != resultado.CargaPatronal)
                throw new ConfiguracionConceptoException("SNAPSHOT_NO_CONCILIA");
            var calculada = await contexto.EstadosPlanilla.SingleAsync(e => e.Nombre == "Calculada", ct);
            periododb.IdEstadoPlanilla = planilla.IdEstadoPlanilla = calculada.IdEstadoPlanilla;
            planilla.IdEjecucionPlanillaVigente = ejecucion.IdEjecucionPlanilla;
            planilla.FechaCalculo = ejecucion.FechaCalculo;
            planilla.SalarioBrutoTotal = ejecucion.SalarioBrutoTotal; planilla.DeduccionesTotal = ejecucion.DeduccionesTotal;
            planilla.SalarioNetoTotal = ejecucion.SalarioNetoTotal; planilla.CostoPatronalEstimadoTotal = ejecucion.CargasPatronalesTotal;
            periododb.FechaModificacion = planilla.FechaModificacion = DateTime.UtcNow;
            periododb.ModificadoPor = planilla.ModificadoPor = actor;
            AuditarSnapshot(planilla, ejecucion, actor, "CALCULAR_PLANILLA", "Borrador", "Calculada", null);
            await contexto.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ejecucion.IdEjecucionPlanilla;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); contexto.ChangeTracker.Clear(); throw; }
    }

    public Task ReabrirSnapshotAsync(long idPeriodo, long idEjecucionEsperada, string motivo, string actor, CancellationToken ct = default)
        => TransicionSnapshotAsync(idPeriodo, idEjecucionEsperada, "Borrador", Texto(motivo, 1000, "motivo"), actor, ct);
    public Task AprobarSnapshotAsync(long idPeriodo, long idEjecucionEsperada, string actor, CancellationToken ct = default)
        => TransicionSnapshotAsync(idPeriodo, idEjecucionEsperada, "Aprobada", null, actor, ct);
    public Task CerrarSnapshotAsync(long idPeriodo, long idEjecucionEsperada, string actor, CancellationToken ct = default)
        => TransicionSnapshotAsync(idPeriodo, idEjecucionEsperada, "Cerrada", null, actor, ct);

    private async Task TransicionSnapshotAsync(long idPeriodo, long esperada, string destino, string? motivo, string actor, CancellationToken ct)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("J requiere una unidad de trabajo sin cambios pendientes.");
        contexto.ChangeTracker.Clear();
        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await AutorizarAsync(actor, ct);
            var periodo = await BloqueoPeriodoPlanilla.ObtenerAsync(contexto, idPeriodo, ct)
                ?? throw new ConfiguracionConceptoException("PERIODO_FALTANTE");
            var p = await contexto.Planillas.Include(x => x.EstadoPlanilla).SingleAsync(x => x.IdPeriodoPlanilla == idPeriodo, ct);
            var anterior = destino == "Cerrada" ? "Aprobada" : "Calculada";
            if (p.EstadoRegistro != Activo || periodo.EstadoRegistro != Activo || p.EstadoPlanilla!.Nombre != anterior
                || periodo.EstadoPlanilla!.Nombre != anterior || p.IdEjecucionPlanillaVigente != esperada)
                throw new ConfiguracionConceptoException("TRANSICION_O_EJECUCION_DESPLAZADA");
            var snapshot = await CargarSnapshotAsync(esperada, ct);
            await ConciliarSnapshotAsync(snapshot, ct);
            if (snapshot.IdPlanilla != p.IdPlanilla || snapshot.EstadoEjecucion != (destino == "Cerrada" ? "Autorizada" : "Calculada")
                || snapshot.Detalles.Any(d => d.SalarioNeto < 0) || p.SalarioBrutoTotal != snapshot.SalarioBrutoTotal
                || p.DeduccionesTotal != snapshot.DeduccionesTotal || p.SalarioNetoTotal != snapshot.SalarioNetoTotal
                || p.CostoPatronalEstimadoTotal != snapshot.CargasPatronalesTotal)
                throw new ConfiguracionConceptoException("SNAPSHOT_NO_CONCILIA");
            var e = await contexto.EjecucionesPlanilla.SingleAsync(x => x.IdEjecucionPlanilla == esperada, ct);
            var nuevo = await contexto.EstadosPlanilla.SingleAsync(x => x.Nombre == destino, ct);
            p.IdEstadoPlanilla = periodo.IdEstadoPlanilla = nuevo.IdEstadoPlanilla;
            p.FechaModificacion = periodo.FechaModificacion = DateTime.UtcNow; p.ModificadoPor = periodo.ModificadoPor = actor;
            string accion;
            if (destino == "Borrador")
            {
                e.EstadoEjecucion = "InvalidadaPorReapertura"; e.MotivoInvalidacion = motivo;
                e.FechaInvalidacion = DateTime.UtcNow; e.InvalidadoPor = actor;
                p.IdEjecucionPlanillaVigente = null; p.FechaCalculo = null;
                p.SalarioBrutoTotal = p.DeduccionesTotal = p.SalarioNetoTotal = p.CostoPatronalEstimadoTotal = 0;
                p.AprobadoPor = p.CerradoPor = null; p.FechaAprobacion = p.FechaCierre = null;
                accion = "REABRIR_PLANILLA";
            }
            else if (destino == "Aprobada")
            {
                e.EstadoEjecucion = "Autorizada"; p.AprobadoPor = actor; p.FechaAprobacion = DateTime.UtcNow; accion = "APROBAR_PLANILLA";
            }
            else { p.CerradoPor = actor; p.FechaCierre = DateTime.UtcNow; accion = "CERRAR_PLANILLA"; }
            AuditarSnapshot(p, snapshot, actor, accion, anterior, destino, motivo);
            await contexto.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); contexto.ChangeTracker.Clear(); throw; }
    }

    public async Task<EjecucionPlanilla> LeerSnapshotAsync(long idEjecucion, string actor, CancellationToken ct = default)
    {
        await AutorizarAsync(actor, ct);
        return await CargarSnapshotAsync(idEjecucion, ct);
    }

    private async Task<EjecucionPlanilla> CargarSnapshotAsync(long id, CancellationToken ct)
        => await contexto.EjecucionesPlanilla.AsNoTracking().Include(e => e.Detalles).ThenInclude(d => d.Segmentos)
            .Include(e => e.Detalles).ThenInclude(d => d.Conceptos).ThenInclude(l => l.AplicacionesNormativas)
            .AsSplitQuery().SingleAsync(e => e.IdEjecucionPlanilla == id, ct);

    private void AuditarSnapshot(Planilla p, EjecucionPlanilla e, string actor, string accion, string anterior, string nuevo, string? motivo)
        => contexto.BitacoraAuditoria.Add(new BitacoraAuditoria { IdUsuario = actor, FechaHora = DateTime.UtcNow,
            Accion = accion, Entidad = "Planilla", IdRegistro = p.IdPlanilla.ToString(CultureInfo.InvariantCulture),
            ValoresAnteriores = JsonSerializer.Serialize(new { Estado = anterior }),
            ValoresNuevos = JsonSerializer.Serialize(new { Actor = actor, Estado = nuevo, p.IdPlanilla, p.IdPeriodoPlanilla,
                e.IdEjecucionPlanilla, e.NumeroVersion, e.VersionMotor, Motivo = motivo, e.SalarioBrutoTotal, e.SalarioNetoTotal,
                e.DeduccionesTotal, e.CargasPatronalesTotal, e.CostoLaboralTotal, Detalles = e.Detalles.Count,
                Lineas = e.Detalles.Sum(d => d.Conceptos.Count) }), Observacion = "F1-FIN-02J; FIN-02 contenido. Sin habilitación de pagos." });
}

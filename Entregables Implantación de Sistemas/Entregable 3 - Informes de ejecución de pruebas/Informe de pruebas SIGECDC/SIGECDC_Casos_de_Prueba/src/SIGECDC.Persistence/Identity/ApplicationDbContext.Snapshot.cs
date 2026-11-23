using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Identity;

public partial class ApplicationDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (!CandidatosSnapshot().Any() && !CandidatosForecast().Any() && !CandidatosComparacion().Any()) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await using var propia = Database.CurrentTransaction is null && Database.IsRelational()
            ? await Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken) : null;
        try
        {
            await ProtegerSnapshotAsync(cancellationToken);
            await ProtegerForecastAsync(cancellationToken);
            await ProtegerComparacionAsync(cancellationToken);
            var resultado = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (propia is not null) await propia.CommitAsync(cancellationToken);
            return resultado;
        }
        catch { if (propia is not null) await propia.RollbackAsync(CancellationToken.None); throw; }
    }

    private IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> CandidatosSnapshot()
        => ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is DetallePlanilla or SegmentoCalculoPlanilla or ConceptoDetallePlanilla or AplicacionNormativaDetallePlanilla
                || e.State != EntityState.Added && e.Entity is EjecucionPlanilla or ConceptoPlanillaVersion or ConceptoPlanilla
                    or JornadaLaboral or SalarioMinimoVersion or CondicionLaboralVersion or ParametroPlanilla or ParametroPlanillaPeriodo
                    or ParametroPlanillaColaborador or ConfiguracionRiesgoTrabajo or ValorParametroNormativo or ParametroNormativo
                    or VersionNormativa or TramoImpuestoSalario or CreditoFiscalNormativo or CreditoFiscalColaborador);

    private async Task<bool> ExtensionSnapshotDisponibleAsync(CancellationToken ct)
    {
        if (!Database.IsRelational()) return true;
        await using var command = Database.GetDbConnection().CreateCommand();
        command.Transaction = Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='SegmentoCalculoPlanilla' AND COLUMN_NAME='IdCondicionLaboralVersion'";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == 1;
    }

    // Frontera de escritura de Persistence. Las migraciones SQL y QA de corrupción deliberada no son flujos de negocio.
    private async Task ProtegerSnapshotAsync(CancellationToken ct)
    {
        var cambios = CandidatosSnapshot().OrderBy(e => e.Metadata.GetTableName(), StringComparer.Ordinal).ToArray();
        // Compatibilidad con bases todavía anteriores al upgrade aditivo; allí no puede existir evidencia J.
        if (!await ExtensionSnapshotDisponibleAsync(ct)) return;
        foreach (var entrada in cambios)
        {
            var nueva = entrada.State == EntityState.Added;
            if (!nueva && Database.IsRelational())
            {
                // Bloquear la evidencia antes de consultar sus consumidores. El cálculo mantiene sus lecturas
                // Serializable hasta commit; una mutación tardía observa el snapshot recién creado y se rechaza.
                var tabla = entrada.Metadata.GetTableName()!;
                var store = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(tabla, entrada.Metadata.GetSchema());
                var pk = entrada.Metadata.FindPrimaryKey()!.Properties.Single();
                await using var command = Database.GetDbConnection().CreateCommand();
                command.Transaction = Database.CurrentTransaction!.GetDbTransaction();
                command.CommandText = $"SELECT `{pk.GetColumnName(store)}` FROM `{tabla}` WHERE `{pk.GetColumnName(store)}`=@id FOR UPDATE";
                var id = command.CreateParameter(); id.ParameterName = "@id"; id.Value = entrada.Property(pk.Name).OriginalValue!;
                command.Parameters.Add(id); await command.ExecuteScalarAsync(ct);
            }
            bool usada = entrada.Entity switch
            {
                DetallePlanilla d => nueva
                    ? await EjecucionesPlanilla.AnyAsync(e => e.IdEjecucionPlanilla == d.IdEjecucionPlanilla && e.SnapshotCompleto, ct)
                    : await DetallesPlanilla.AnyAsync(x => x.IdDetallePlanilla == d.IdDetallePlanilla && x.EjecucionPlanilla!.SnapshotCompleto, ct),
                SegmentoCalculoPlanilla s => await DetallesPlanilla.AnyAsync(d => d.IdDetallePlanilla == s.IdDetallePlanilla && d.EjecucionPlanilla!.SnapshotCompleto, ct),
                ConceptoDetallePlanilla l => await DetallesPlanilla.AnyAsync(d => d.IdDetallePlanilla == l.IdDetallePlanilla && d.EjecucionPlanilla!.SnapshotCompleto, ct),
                AplicacionNormativaDetallePlanilla a => await ConceptosDetallePlanilla.AnyAsync(l => l.IdConceptoDetallePlanilla == a.IdConceptoDetallePlanilla && l.DetallePlanilla!.EjecucionPlanilla!.SnapshotCompleto, ct),
                EjecucionPlanilla e when !nueva => await EjecucionesPlanilla.AnyAsync(x => x.IdEjecucionPlanilla == e.IdEjecucionPlanilla && x.SnapshotCompleto, ct)
                    && (entrada.State == EntityState.Deleted || entrada.Properties.Any(p => p.IsModified && p.Metadata.Name is not
                        (nameof(EjecucionPlanilla.EstadoEjecucion) or nameof(EjecucionPlanilla.MotivoInvalidacion) or nameof(EjecucionPlanilla.FechaInvalidacion) or nameof(EjecucionPlanilla.InvalidadoPor)))),
                ConceptoPlanillaVersion v when !nueva => await ConceptosDetallePlanilla.AnyAsync(l => l.IdConceptoPlanillaVersion == v.IdConceptoPlanillaVersion && l.DetallePlanilla!.SnapshotCompleto, ct),
                ConceptoPlanilla c when !nueva && (entrada.State == EntityState.Deleted || entrada.Property(nameof(c.Codigo)).IsModified)
                    => await ConceptosDetallePlanilla.AnyAsync(l => l.ConceptoPlanillaVersion!.IdConceptoPlanilla == c.IdConceptoPlanilla && l.DetallePlanilla!.SnapshotCompleto, ct),
                JornadaLaboral j when !nueva => await SegmentosCalculoPlanilla.AnyAsync(s => s.IdJornadaLaboral == j.IdJornadaLaboral, ct),
                SalarioMinimoVersion m when !nueva => await SegmentosCalculoPlanilla.AnyAsync(s => s.IdSalarioMinimoVersion == m.IdSalarioMinimoVersion, ct),
                CondicionLaboralVersion c when !nueva => await CondicionSnapshotProtegidaAsync(c, entrada.State == EntityState.Modified
                    && entrada.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(c.FechaVigenciaFin)), ct),
                ParametroPlanilla p when !nueva => await ConceptosDetallePlanilla.AnyAsync(l => l.IdParametroPlanilla == p.IdParametroPlanilla, ct),
                ParametroPlanillaPeriodo p when !nueva => await ConceptosDetallePlanilla.AnyAsync(l => l.IdParametroPlanillaPeriodo == p.IdParametroPlanillaPeriodo, ct),
                ParametroPlanillaColaborador p when !nueva => await ConceptosDetallePlanilla.AnyAsync(l => l.IdParametroPlanillaColaborador == p.IdParametroPlanillaColaborador, ct),
                ConfiguracionRiesgoTrabajo r when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.IdConfiguracionRiesgoTrabajo == r.IdConfiguracionRiesgoTrabajo || a.IdConfiguracionRTAncla == r.IdConfiguracionRiesgoTrabajo, ct),
                ValorParametroNormativo v when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.IdValorParametroNormativo == v.IdValorParametroNormativo, ct),
                ParametroNormativo p when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.ValorParametroNormativo!.IdParametroNormativo == p.IdParametroNormativo, ct),
                VersionNormativa v when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.ValorParametroNormativo!.IdVersionNormativa == v.IdVersionNormativa
                    || a.TramoImpuestoSalario!.IdVersionNormativa == v.IdVersionNormativa || a.CreditoFiscalNormativo!.IdVersionNormativa == v.IdVersionNormativa
                    || a.ConfiguracionRiesgoTrabajo!.IdVersionNormativa == v.IdVersionNormativa, ct)
                    || await SegmentosCalculoPlanilla.AnyAsync(s => SalariosMinimosVersion.Any(m => m.IdSalarioMinimoVersion == s.IdSalarioMinimoVersion
                        && m.IdVersionNormativa == v.IdVersionNormativa), ct),
                TramoImpuestoSalario t when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.IdTramoImpuestoSalario == t.IdTramoImpuestoSalario, ct),
                CreditoFiscalNormativo c when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.IdCreditoFiscalNormativo == c.IdCreditoFiscalNormativo, ct),
                CreditoFiscalColaborador c when !nueva => await AplicacionesNormativasDetallePlanilla.AnyAsync(a => a.IdCreditoFiscalColaborador == c.IdCreditoFiscalColaborador, ct),
                _ => false
            };
            if (usada) throw new InvalidOperationException("EVIDENCIA_SNAPSHOT_INMUTABLE: conserve la versión consumida.");
        }
    }

    private async Task<bool> CondicionSnapshotProtegidaAsync(CondicionLaboralVersion c, bool soloCierre, CancellationToken ct)
    {
        var ultimo = await SegmentosCalculoPlanilla.Where(s => s.IdCondicionLaboralVersion == c.IdCondicionLaboralVersion)
            .MaxAsync(s => (DateTime?)s.FechaFin, ct);
        return ultimo is not null && (!soloCierre || c.FechaVigenciaFin is null || c.FechaVigenciaFin < ultimo);
    }
}

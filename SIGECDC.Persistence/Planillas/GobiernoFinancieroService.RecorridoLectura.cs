using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService : IConsultaRecorridoPlanilla
{
    public Task<CatalogosRecorridoPlanilla> CatalogosAsync(string actor, CancellationToken ct = default)
        => LeerTemporalAsync(actor, async () =>
        {
            var codigos = await contexto.ConceptosPlanilla.AsNoTracking().Select(c => c.Codigo).ToArrayAsync(ct);
            // E sigue siendo la autoridad sobre clasificación y evidencia; la UI no certifica una fila activa.
            var conceptos = (await LeerConceptosAsync(codigos, ct)).Where(c => c.Activo && c.EvidenciaConfirmada).ToArray();
            var tipos = await contexto.TiposIncidenciaPlanilla.AsNoTracking()
                .Where(t => t.EstadoRegistro == Activo && t.IdConceptoPlanilla != null)
                .Select(t => new TipoHechoOpcion(t.IdTipoIncidenciaPlanilla, t.Nombre, t.IdConceptoPlanilla!.Value)).ToArrayAsync(ct);
            var jornadas = await contexto.JornadasLaborales.AsNoTracking().Where(j => j.EstadoRegistro == Activo)
                .OrderBy(j => j.Nombre).Select(j => new JornadaPlanillaOpcion(j.IdJornadaLaboral, j.Nombre, j.TipoJornada,
                    j.HorasOrdinariasDia, j.HorasOrdinariasSemana, new(j.FechaVigenciaInicio, j.FechaVigenciaFin))).ToArrayAsync(ct);
            var riesgos = await contexto.ConfiguracionesRiesgoTrabajo.AsNoTracking().Where(r => r.EstadoRegistro == Activo)
                .Select(r => new RiesgoPlanillaOpcion(r.IdConfiguracionRiesgoTrabajo, r.NumeroPoliza, r.CodigoActividad,
                    new(r.FechaVigenciaInicio, r.FechaVigenciaFin))).ToArrayAsync(ct);
            var ocupaciones = await contexto.OcupacionesSalarioMinimo.AsNoTracking().Where(o => o.EstadoRegistro == Activo)
                .OrderBy(o => o.Nombre).Select(o => new OpcionFinanciera(o.IdOcupacionSalarioMinimo, o.CodigoOficial + " · " + o.Nombre)).ToArrayAsync(ct);
            var puestos = await contexto.Puestos.AsNoTracking().Where(p => p.EstadoRegistro == Activo)
                .OrderBy(p => p.Nombre).Select(p => new OpcionFinanciera(p.IdPuesto, p.Nombre)).ToArrayAsync(ct);
            var departamentos = await contexto.Departamentos.AsNoTracking().Where(d => d.EstadoRegistro == Activo)
                .OrderBy(d => d.Nombre).Select(d => new OpcionFinanciera(d.IdDepartamento, d.Nombre)).ToArrayAsync(ct);
            var colaboradores = await contexto.Colaboradores.AsNoTracking().Where(c => c.EstadoRegistro == Activo)
                .OrderBy(c => c.PrimerApellido).Select(c => new OpcionFinanciera(c.IdColaborador,
                    c.CodigoColaborador + " · " + c.Nombre + " " + c.PrimerApellido + " " + (c.SegundoApellido ?? ""))).ToArrayAsync(ct);
            var periodos = await contexto.PeriodosPlanilla.AsNoTracking().Where(p => p.EstadoRegistro == Activo)
                .OrderByDescending(p => p.FechaInicio).Select(p => new PeriodoPlanillaResumen {
                    IdPeriodoPlanilla = p.IdPeriodoPlanilla, CodigoPeriodo = p.CodigoPeriodo, Nombre = p.Nombre,
                    FechaInicio = p.FechaInicio, FechaFin = p.FechaFin, TipoPeriodo = p.TipoPeriodo,
                    EstadoPlanilla = p.EstadoPlanilla!.Nombre }).ToArrayAsync(ct);
            return new CatalogosRecorridoPlanilla(conceptos, tipos, jornadas, riesgos, ocupaciones, puestos,
                departamentos, colaboradores, periodos);
        }, ct);

    private IQueryable<ContratoRecorrido> ContratosRecorrido(long[] ids)
        => contexto.Contratos.AsNoTracking().Where(c => c.EstadoRegistro == Activo && ids.Contains(c.IdContrato)).Select(c => new ContratoRecorrido(
            c.IdContrato, c.IdColaborador, c.Colaborador!.CodigoColaborador + " · " + c.Colaborador.Nombre + " " + c.Colaborador.PrimerApellido,
            c.TipoContrato, c.PeriodicidadPago, new(c.FechaInicio, c.FechaFin), c.SalarioBase,
            c.Colaborador.IdPuesto, c.Colaborador.IdDepartamento, c.Colaborador.Puesto!.IdOcupacionSalarioMinimo,
            c.IdJornadaLaboral, c.IdConfiguracionRiesgoTrabajo));

    public Task<ContratoRecorrido?> ContratoAsync(long id, string actor, CancellationToken ct = default)
        => LeerTemporalAsync(actor, () => ContratosRecorrido([id]).SingleOrDefaultAsync(ct), ct);

    public Task<IReadOnlyList<ParametroRecorrido>> ParametrosAsync(string actor, CancellationToken ct = default)
        => LeerTemporalAsync<IReadOnlyList<ParametroRecorrido>>(actor, async () =>
        {
            var claves = await contexto.ParametrosPlanilla.AsNoTracking().Where(p => !p.EsNormativo && p.IdConceptoPlanilla != null)
                .Select(p => p.Codigo).Distinct().ToArrayAsync(ct);
            var datos = await LeerInternosAsync(claves, ct);
            return datos.Versiones.Where(v => v.Gobernado && !v.EsNormativo).Select(v =>
            {
                var p = datos.Parametros.Single(p => p.IdParametroPlanilla == v.IdVersion);
                return new ParametroRecorrido(p.Nombre, p.ConceptoPlanilla!.Nombre, v,
                    datos.Overrides.Where(o => o.IdVersion == v.IdVersion).ToArray());
            }).OrderBy(p => p.Nombre).ThenBy(p => p.Version.Vigencia!.Inicio).ToArray();
        }, ct);

    public Task<IReadOnlyList<IncidenciaRecorrido>> IncidenciasAsync(long? idPeriodo, string actor, CancellationToken ct = default)
        => LeerTemporalAsync<IReadOnlyList<IncidenciaRecorrido>>(actor, async () =>
        {
            return await contexto.IncidenciasPlanilla.AsNoTracking().Where(h => idPeriodo == null || h.IdPeriodoPlanilla == idPeriodo)
                .OrderByDescending(h => h.FechaIncidencia).ThenBy(h => h.IdIncidenciaPlanilla)
                .Select(h => new IncidenciaRecorrido(h.IdIncidenciaPlanilla,
                    h.Colaborador!.CodigoColaborador + " · " + h.Colaborador.Nombre + " " + h.Colaborador.PrimerApellido,
                    h.TipoIncidenciaPlanilla!.Nombre,
                    new(h.IdPeriodoPlanilla, h.IdColaborador, h.IdTipoIncidenciaPlanilla, h.IdConceptoPlanillaVersion ?? 0,
                        h.FechaIncidencia, h.Unidad ?? "", h.Cantidad, h.Monto, h.Descripcion ?? ""),
                    contexto.ConfirmacionesIncidencias.Where(c => c.IdIncidenciaPlanilla == h.IdIncidenciaPlanilla)
                        .Max(c => (int?)c.NumeroVersion) ?? 0,
                    h.EstadoRegistro, h.PeriodoPlanilla!.EstadoPlanilla!.Nombre)).ToArrayAsync(ct);
        }, ct);

    public Task<PeriodoRecorrido?> PeriodoAsync(long id, string actor, CancellationToken ct = default)
        => LeerTemporalAsync<PeriodoRecorrido?>(actor, async () =>
        {
            var p = await contexto.PeriodosPlanilla.AsNoTracking().Include(p => p.EstadoPlanilla)
                .SingleOrDefaultAsync(p => p.IdPeriodoPlanilla == id && p.EstadoRegistro == Activo, ct);
            if (p is null) return null;
            var vigente = await contexto.Planillas.AsNoTracking().Where(x => x.IdPeriodoPlanilla == id)
                .Select(x => x.IdEjecucionPlanillaVigente).SingleOrDefaultAsync(ct);
            var versiones = await contexto.EjecucionesPlanilla.AsNoTracking().Where(e => e.Planilla!.IdPeriodoPlanilla == id)
                .OrderByDescending(e => e.NumeroVersion).Select(e => new VersionPlanillaOpcion(e.IdEjecucionPlanilla,
                    e.NumeroVersion, e.EstadoEjecucion, e.FechaCalculo, e.IdEjecucionPlanilla == vigente)).ToArrayAsync(ct);
            var ids = await contexto.Contratos.AsNoTracking().Where(c => c.FechaInicio <= p.FechaFin
                && (c.FechaFin == null || c.FechaFin >= p.FechaInicio) && c.EstadoRegistro == Activo)
                .Select(c => c.IdContrato).ToArrayAsync(ct);
            var contratos = await ContratosRecorrido(ids).ToArrayAsync(ct);
            var previas = await contexto.EjecucionesPlanilla.AsNoTracking().Where(e => e.SnapshotCompleto
                && (e.EstadoEjecucion == "Calculada" || e.EstadoEjecucion == "Autorizada") && e.Planilla!.IdEjecucionPlanillaVigente == e.IdEjecucionPlanilla
                && e.FechaInicioSnapshot.Year == p.FechaInicio.Year && e.FechaInicioSnapshot.Month == p.FechaInicio.Month
                && e.FechaFinSnapshot < p.FechaInicio).OrderByDescending(e => e.FechaCalculo)
                .Select(e => new { e.IdEjecucionPlanilla, e.NumeroVersion, e.FechaInicioSnapshot, e.FechaFinSnapshot }).ToArrayAsync(ct);
            return new(new() { IdPeriodoPlanilla = id, CodigoPeriodo = p.CodigoPeriodo, Nombre = p.Nombre,
                TipoPeriodo = p.TipoPeriodo, FechaInicio = p.FechaInicio, FechaFin = p.FechaFin, EstadoPlanilla = p.EstadoPlanilla!.Nombre },
                vigente, versiones, contratos, previas.Select(e => new OpcionFinanciera(e.IdEjecucionPlanilla,
                    $"{e.FechaInicioSnapshot:dd/MM/yyyy} – {e.FechaFinSnapshot:dd/MM/yyyy} · V{e.NumeroVersion}")).ToArray());
        }, ct);
}

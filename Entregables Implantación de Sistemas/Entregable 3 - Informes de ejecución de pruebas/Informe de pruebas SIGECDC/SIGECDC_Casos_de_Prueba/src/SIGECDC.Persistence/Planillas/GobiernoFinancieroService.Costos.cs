using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public async Task<ResultadoCostoLaboral> PrepararCostoLaboralEnTransaccionAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> cuotas, IReadOnlyList<AplicabilidadRiesgoTrabajo> riesgos,
        IReadOnlyList<HechoCostoPatronal> otrosCostos, string actor, AcumuladoMensualRenta? acumulado = null, CancellationToken ct = default)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
            throw new InvalidOperationException("I requiere la transacción Serializable del consumidor FIN-01, sin cambios pendientes.");
        riesgos = Array.AsReadOnly(riesgos.ToArray()); otrosCostos = Array.AsReadOnly(otrosCostos.ToArray());
        cuotas = Array.AsReadOnly(cuotas.ToArray()); acumulado = CongelarAcumuladoRenta(acumulado);
        contexto.ChangeTracker.Clear(); var periodo = await PeriodoIncidenciasEditableAsync(idPeriodo, ct); await AutorizarAsync(actor, ct);
        ResultadoCostoLaboral Falta(string codigo) => ProductorCostosPatronales.Fallido([new(codigo, "RT/COSTO", periodo.Inicio)]);
        try
        {
            if (riesgos.Any(r => r.ConfirmadoPor != actor) || otrosCostos.Any(h => h.RegistradoPor != actor
                || h.AutorizadoPor is not null && h.AutorizadoPor != actor)) return Falta("ACTOR_EVIDENCIA_PATRONAL_INCOMPATIBLE");
            var temporal = await ResolverTemporalEnTransaccionAsync(periodo, referenciaRelacionOrdinaria, ct);
            if (!temporal.EsResoluble)
                return ProductorCostosPatronales.Fallido(temporal.Poblacion.SelectMany(p => p.Bloqueos.Select(b =>
                    new BloqueoConcepto(b.Codigo, "TEMPORAL", b.Fecha, p.IdColaborador))).DefaultIfEmpty(new("BASE_RT_NO_DEMOSTRABLE", "RT", periodo.Inicio)));
            var segmentos = temporal.Poblacion.SelectMany(p => p.Segmentos).ToArray();
            foreach (var h in otrosCostos)
                if (segmentos.Count(s => s.IdColaborador == h.IdColaborador && s.Inicio <= h.FechaHecho && h.FechaHecho <= s.Fin) != 1)
                    return Falta("COSTO_PATRONAL_FUERA_RELACION");
            var contratos = await FiltrarClaves(contexto.Contratos.AsNoTracking().Include(c => c.ConfiguracionRiesgoTrabajo),
                c => c.IdContrato, segmentos.Select(s => s.IdContrato)).ToArrayAsync(ct);
            var asociaciones = new List<AsignacionRiesgoContrato>(); var versiones = new List<RiesgoTrabajoVersionado>();
            foreach (var contrato in contratos)
            {
                var (soporte, asociadoPor, demostrada) = await AsociacionRiesgoDemostradaAsync(contrato, ct);
                var ancla = contrato.ConfiguracionRiesgoTrabajo;
                asociaciones.Add(new(contrato.IdContrato, contrato.IdColaborador, new(contrato.FechaInicio, contrato.FechaFin),
                    contrato.IdConfiguracionRiesgoTrabajo, ancla?.NumeroPoliza ?? "", ancla?.CodigoActividad ?? "", soporte, asociadoPor, demostrada));
                if (ancla is null) continue;
                // La asociación explícita de C identifica la familia póliza/actividad. No se asigna una fila global ni otra familia.
                var filas = await contexto.ConfiguracionesRiesgoTrabajo.AsNoTracking().Include(r => r.VersionNormativa)
                    .Where(r => r.NumeroPoliza == ancla.NumeroPoliza && r.CodigoActividad == ancla.CodigoActividad).ToArrayAsync(ct);
                foreach (var r in filas.Where(r => versiones.All(v => v.IdConfiguracion != r.IdConfiguracionRiesgoTrabajo)))
                    versiones.Add(new(r.IdConfiguracionRiesgoTrabajo, r.NumeroPoliza, r.CodigoActividad, r.DescripcionActividad,
                        r.Tarifa, r.Descuento, r.Recargo, new(r.FechaVigenciaInicio, r.FechaVigenciaFin), r.ReferenciaDocumento,
                        r.IdVersionNormativa, r.CreadoPor ?? "", r.FechaCreacion, r.EstadoRegistro == Activo, await RiesgoDemostradoAsync(r, ct)));
            }
            var conceptos = await LeerConceptosAsync(otrosCostos.Select(c => c.CodigoConcepto).Append("RIESGO_TRABAJO").Distinct().ToArray(), ct);
            var fronteras = new Dictionary<long, IReadOnlyList<DateTime>>();
            foreach (var persona in temporal.Poblacion)
            {
                var asignadas = asociaciones.Where(a => a.IdColaborador == persona.IdColaborador).ToArray();
                var pertinentes = versiones.Where(v => asignadas.Any(a => a.NumeroPoliza == v.NumeroPoliza && a.CodigoActividad == v.CodigoActividad));
                var vigencias = pertinentes.Select(v => v.Vigencia)
                    .Concat(riesgos.Where(r => asignadas.Any(a => a.IdContrato == r.IdContrato)).Select(r => r.Vigencia))
                    .Concat(conceptos.Where(c => c.Codigo == "RIESGO_TRABAJO").Select(c => c.Vigencia));
                foreach (var v in vigencias) v.Validar();
                fronteras[persona.IdColaborador] = ResolucionValoresCuotas.Fronteras(vigencias, periodo.Fin).Distinct().Order().ToArray();
                // Resolver cada intervalo antes de producir líneas; gaps, solapes o modalidad desconocida bloquean íntegramente.
                foreach (var segmento in persona.Segmentos)
                foreach (var intervalo in CalendarioSalarial.Dividir(new(segmento.Inicio, segmento.Fin), fronteras[persona.IdColaborador]))
                    _ = ResolucionRiesgoTrabajo.Resolver(persona.IdColaborador, segmento.IdContrato, intervalo, asociaciones, versiones, riesgos);
            }
            var renta = await PrepararRentaConFronterasAsync(idPeriodo, referenciaRelacionOrdinaria, cuotas, actor, acumulado, fronteras, ct);
            return ProductorCostosPatronales.Producir(periodo, renta, asociaciones, versiones, riesgos, conceptos, otrosCostos, segmentos);
        }
        catch (ConfiguracionConceptoException e) { return Falta(e.Codigo); }
        catch (ArgumentException) { return Falta("VIGENCIA_RT_INVALIDA"); }
    }

    private async Task<(string Soporte, string Actor, bool Demostrada)> AsociacionRiesgoDemostradaAsync(Contrato c, CancellationToken ct)
    {
        var id = c.IdContrato.ToString(CultureInfo.InvariantCulture);
        var registros = await contexto.BitacoraAuditoria.AsNoTracking().Where(a => a.Entidad == nameof(Contrato)
            && a.Accion == "AsociarConfiguracion" && a.IdRegistro == id).OrderBy(a => a.IdBitacoraAuditoria)
            .Select(a => new { a.IdUsuario, a.ValoresNuevos }).ToArrayAsync(ct);
        if (registros.Length == 0 || c.IdConfiguracionRiesgoTrabajo is null) return ("", "", false);
        string soporte = "", actor = "";
        try
        {
            foreach (var a in registros)
            {
                using var json = JsonDocument.Parse(a.ValoresNuevos ?? "");
                if (json.RootElement.GetProperty("idRiesgo").GetInt64() != c.IdConfiguracionRiesgoTrabajo) return ("", "", false);
                soporte = json.RootElement.GetProperty("soporte").GetString() ?? ""; actor = a.IdUsuario ?? "";
                if (string.IsNullOrWhiteSpace(soporte) || string.IsNullOrWhiteSpace(actor)) return ("", "", false);
            }
            return (soporte, actor, true);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { return ("", "", false); }
    }

    private async Task<bool> RiesgoDemostradoAsync(ConfiguracionRiesgoTrabajo r, CancellationToken ct)
    {
        var id = r.IdConfiguracionRiesgoTrabajo.ToString(CultureInfo.InvariantCulture);
        var cargas = await contexto.BitacoraAuditoria.AsNoTracking().Where(a => a.Entidad == nameof(ConfiguracionRiesgoTrabajo)
            && a.Accion == "VersionarRT" && a.IdRegistro == id).Select(a => new { a.IdUsuario, a.ValoresNuevos }).ToArrayAsync(ct);
        if (cargas.Length != 1 || string.IsNullOrWhiteSpace(r.CreadoPor) || cargas[0].IdUsuario != r.CreadoPor) return false;
        try
        {
            var original = JsonSerializer.Deserialize<RiesgoTrabajoCarga>(cargas[0].ValoresNuevos ?? "");
            if (original != new RiesgoTrabajoCarga(r.NumeroPoliza, r.CodigoActividad, r.Tarifa, r.Descuento, r.Recargo,
                new(r.FechaVigenciaInicio, r.FechaVigenciaFin), r.ReferenciaDocumento, r.IdVersionNormativa)) return false;
        }
        catch (JsonException) { return false; }
        if (r.IdVersionNormativa is null) return true;
        var norma = r.VersionNormativa;
        if (!NormativaUtilizable(norma) || norma!.Institucion != "INS"
            || !new VigenciaFinanciera(norma.FechaVigenciaInicio, norma.FechaVigenciaFin).Contiene(new VigenciaFinanciera(r.FechaVigenciaInicio, r.FechaVigenciaFin))) return false;
        var huella = await HuellaRevisionAsync(norma.IdVersionNormativa, ct);
        return huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "CargarNormativa", ct)
            && huella == await HuellaAuditadaAsync(norma.IdVersionNormativa, "ActivarNormativa", ct);
    }
}

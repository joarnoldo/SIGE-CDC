using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<ResultadoCalculoIncidencias> PrepararIncidenciasEnTransaccionAsync(long idPeriodo,
        string referenciaRelacionOrdinaria, string actor, CancellationToken ct = default)
        => PrepararIncidenciasConFronterasAsync(idPeriodo, referenciaRelacionOrdinaria, actor, null, ct);

    private async Task<ResultadoCalculoIncidencias> PrepararIncidenciasConFronterasAsync(long idPeriodo,
        string referenciaRelacionOrdinaria, string actor, IReadOnlyDictionary<long, IReadOnlyList<DateTime>>? fronterasNormativas, CancellationToken ct)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
            throw new InvalidOperationException("La composición FIN-01 requiere una transacción Serializable sin cambios pendientes; el consumidor conserva los locks hasta terminar.");
        contexto.ChangeTracker.Clear();
        var rango = await PeriodoIncidenciasEditableAsync(idPeriodo, ct);
        await AutorizarAsync(actor, ct);
        var temporal = await ResolverTemporalEnTransaccionAsync(rango, referenciaRelacionOrdinaria, ct);
        var lectura = await LeerIncidenciasConfirmadasAsync(idPeriodo, null, rango, temporal, ct);
        var nucleo = await PrepararNucleoEnTransaccionAsync(new(rango, idPeriodo, referenciaRelacionOrdinaria, []), ct, fronterasNormativas);
        var incidencias = ProductorIncidencias.Producir(rango, lectura.Hechos, temporal, lectura.Conceptos, lectura.Parametros);
        var bloqueos = lectura.Bloqueos.Concat(nucleo.Bloqueos).Concat(incidencias.Bloqueos).Distinct().ToList();
        ConciliacionLineas? conciliacion = null;
        if (bloqueos.Count == 0)
        {
            try { conciliacion = AgregadorLineasMonetarias.Conciliar(nucleo.Conciliacion!.Lineas.Concat(incidencias.Lineas)); }
            catch (ConfiguracionConceptoException e) { bloqueos.Add(new(e.Codigo, "CONCILIACION", rango.Inicio)); }
            catch (OverflowException) { bloqueos.Add(new("DESBORDAMIENTO_MONETARIO", "CONCILIACION", rango.Inicio)); }
        }
        return new(conciliacion, OrdenarBloqueos(bloqueos));
    }

    public async Task<DiagnosticoIncidencias> ExaminarIncidenciasAsync(long idPeriodo, long? idColaborador,
        string referenciaRelacionOrdinaria, string actor, CancellationToken ct = default)
    {
        if (contexto.ChangeTracker.HasChanges()) throw new InvalidOperationException("La lectura requiere una unidad de trabajo sin cambios pendientes.");
        if (idPeriodo <= 0 || idColaborador is <= 0) throw new ArgumentException("Identificador inválido.");
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        // Mismo orden de FIN-01: período antes de leer entradas. No es un cálculo ni cambia su estado.
        var periodo = await BloqueoPeriodoPlanilla.ObtenerAsync(contexto, idPeriodo, ct);
        await AutorizarAsync(actor, ct);
        if (periodo is null || periodo.EstadoRegistro != Activo) throw new ConfiguracionConceptoException("PERIODO_INCOMPATIBLE");
        var rango = new PeriodoSalarial(periodo.FechaInicio, periodo.FechaFin, periodo.TipoPeriodo); rango.Validar();
        var temporal = await ResolverTemporalEnTransaccionAsync(rango, referenciaRelacionOrdinaria, ct);
        var lectura = await LeerIncidenciasConfirmadasAsync(idPeriodo, idColaborador, rango, temporal, ct);
        await transaccion.CommitAsync(ct);
        return new(lectura.Filas, OrdenarBloqueos(lectura.Bloqueos));
    }

    private sealed record LecturaIncidencias(IReadOnlyList<IncidenciaExaminada> Filas, IReadOnlyList<HechoIncidenciaCalculable> Hechos,
        IReadOnlyList<ConceptoMonetarioVersion> Conceptos, IReadOnlyDictionary<long, ParametroInternoResuelto> Parametros, IReadOnlyList<BloqueoConcepto> Bloqueos);

    private static IReadOnlyList<BloqueoConcepto> OrdenarBloqueos(IEnumerable<BloqueoConcepto> bloqueos)
        => Array.AsReadOnly(bloqueos.Distinct().OrderBy(b => b.IdColaborador).ThenBy(b => b.Fecha)
            .ThenBy(b => b.Clave, StringComparer.Ordinal).ThenBy(b => b.Codigo, StringComparer.Ordinal).ToArray());

    private async Task<LecturaIncidencias> LeerIncidenciasConfirmadasAsync(long idPeriodo, long? idColaborador,
        PeriodoSalarial rango, ResultadoTemporalSalarial temporal, CancellationToken ct)
    {
        var consulta = contexto.IncidenciasPlanilla.AsNoTracking().Include(i => i.TipoIncidenciaPlanilla)
            .ThenInclude(t => t!.ConceptoPlanilla).Where(i => i.IdPeriodoPlanilla == idPeriodo && i.EstadoRegistro == Activo);
        if (idColaborador is { } persona) consulta = consulta.Where(i => i.IdColaborador == persona);
        var hechos = await consulta.OrderBy(i => i.FechaIncidencia).ThenBy(i => i.IdIncidenciaPlanilla).ToListAsync(ct);
        var conceptos = await LeerConceptosAsync(hechos.Select(i => i.TipoIncidenciaPlanilla?.ConceptoPlanilla?.Codigo)
            .OfType<string>().Distinct().ToArray(), ct);
        var bloqueos = new List<BloqueoConcepto>(); var filas = new List<IncidenciaExaminada>();
        var confirmaciones = await FiltrarClaves(contexto.ConfirmacionesIncidencias.AsNoTracking(), c => c.IdIncidenciaPlanilla,
            hechos.Select(h => h.IdIncidenciaPlanilla)).ToArrayAsync(ct);
        var evidencias = await FiltrarClaves(contexto.BitacoraAuditoria.AsNoTracking(), a => a.IdRegistro,
            confirmaciones.Select(c => c.IdIncidenciaPlanillaConfirmacion.ToString(CultureInfo.InvariantCulture)))
            .Where(a => a.Entidad == nameof(IncidenciaPlanillaConfirmacion) && a.Accion == "ConfirmarIncidencia").ToArrayAsync(ct);
        var calculables = new List<HechoIncidenciaCalculable>(); var parametros = new Dictionary<long, ParametroInternoResuelto>();
        foreach (var h in hechos)
        {
            var clave = h.IdIncidenciaPlanilla.ToString(CultureInfo.InvariantCulture);
            void Falta(string codigo) => bloqueos.Add(new(codigo, clave, h.FechaIncidencia, h.IdColaborador));
            if (h.FechaIncidencia < rango.Inicio || h.FechaIncidencia > rango.Fin) Falta("INCIDENCIA_FUERA_PERIODO");
            var p = temporal.Poblacion.SingleOrDefault(p => p.IdColaborador == h.IdColaborador);
            var segmentos = p?.Segmentos.Where(s => s.Inicio <= h.FechaIncidencia && h.FechaIncidencia <= s.Fin).ToArray() ?? [];
            if (p is null || !p.EsResoluble || segmentos.Length != 1) Falta("TEMPORAL_BLOQUEADO");
            if (p is not null) foreach (var b in p.Bloqueos) Falta(b.Codigo);
            filas.Add(new(h.IdIncidenciaPlanilla, h.IdColaborador, h.FechaIncidencia, h.IdConceptoPlanillaVersion,
                h.Unidad, segmentos.Length == 1 ? segmentos[0].IdCondicionLaboralVersion : null));
            var tipo = h.TipoIncidenciaPlanilla;
            var ultima = confirmaciones.Where(c => c.IdIncidenciaPlanilla == h.IdIncidenciaPlanilla).OrderByDescending(c => c.NumeroVersion).FirstOrDefault();
            if (ultima is not null && ultima.HuellaIncidencia != HuellaIncidenciaConfirmada.Calcular(h, ultima))
            {
                Falta("INCIDENCIA_MODIFICADA_DESPUES_DE_CONFIRMACION");
                continue; // No resolver una versión diferente para justificar un hecho alterado.
            }
            if (tipo is null || tipo.EstadoRegistro != Activo || tipo.IdConceptoPlanilla is null) Falta("TIPO_INCIDENCIA_NO_CLASIFICADO");
            if (h.IdConceptoPlanillaVersion is null) Falta("INCIDENCIA_SIN_VERSION_FIJADA");
            if (string.IsNullOrWhiteSpace(h.Unidad)) Falta("INCIDENCIA_SIN_UNIDAD");
            if (tipo?.ConceptoPlanilla is { } catalogo)
            {
                try
                {
                    var c = ResolucionConceptos.Resolver(catalogo.Codigo, h.FechaIncidencia, conceptos);
                    if (c.IdVersion != h.IdConceptoPlanillaVersion) Falta("VERSION_FIJADA_INCOMPATIBLE");
                    var cantidad = c.TipoBase is "HORA_ORDINARIA" or "REDUCCION_DEVENGO";
                    if (cantidad && h.Cantidad is null or <= 0) Falta("CANTIDAD_INCIDENCIA_INVALIDA");
                    if (c.TipoBase == "REDUCCION_DEVENGO" ? h.Unidad is not ("Día" or "Hora") : h.Unidad != c.UnidadPermitida)
                        Falta("UNIDAD_INCOMPATIBLE");
                    if (c.TipoBase == "HORA_ORDINARIA" && ultima is null) Falta("TIPO_DIA_HE_NO_DEMOSTRABLE");
                    if (c.TipoBase == "REDUCCION_DEVENGO" && h.Unidad == "Día" && h.Cantidad > 1)
                        Falta("DISTRIBUCION_TEMPORAL_NO_DEMOSTRABLE");
                    if (c.RequiereAutorizacion && ultima is null) Falta("AUTORIZACION_INCIDENCIA_NO_DEMOSTRABLE");
                    if (ultima is not null)
                    {
                        if (!evidencias.Any(a => a.IdRegistro == ultima.IdIncidenciaPlanillaConfirmacion.ToString(CultureInfo.InvariantCulture)
                            && a.IdUsuario == ultima.ConfirmadoPor && LeerEvidencia<EvidenciaConfirmacion>(a.ValoresNuevos) == EvidenciaConfirmacion.De(ultima)))
                            throw new ConfiguracionConceptoException("CONFIRMACION_SIN_EVIDENCIA_DE_GOBIERNO");
                        ValidarHechoFinanciero(h, rango, conceptos);
                        ValidarTipoDia(c, ultima.TipoDiaHE);
                        var s = SegmentoIncidencia(h, temporal);
                        if (ultima.FechaHecho != h.FechaIncidencia || ultima.IdConceptoPlanillaVersion != h.IdConceptoPlanillaVersion
                            || ultima.Unidad != h.Unidad || ultima.Cantidad != h.Cantidad
                            || ultima.MontoManual is not null && ultima.MontoManual != h.Monto
                            || ultima.HuellaIncidencia != HuellaIncidenciaConfirmada.Calcular(h, ultima))
                            throw new ConfiguracionConceptoException("INCIDENCIA_MODIFICADA_DESPUES_DE_CONFIRMACION");
                        if (string.IsNullOrWhiteSpace(ultima.ReferenciaSoporte) || string.IsNullOrWhiteSpace(ultima.ConfirmadoPor)
                            || ultima.NumeroVersion <= 0 || ultima.FechaConfirmacion == default)
                            throw new ConfiguracionConceptoException("CONFIRMACION_INCIDENCIA_NO_DEMOSTRABLE");
                        var autorizada = !string.IsNullOrWhiteSpace(ultima.AutorizadoPor) && ultima.FechaAutorizacion is not null
                            && !string.IsNullOrWhiteSpace(ultima.ReferenciaAutorizacion);
                        if (ultima.AutorizacionRequerida != c.RequiereAutorizacion || c.RequiereAutorizacion && !autorizada
                            || !autorizada && (ultima.AutorizadoPor is not null || ultima.FechaAutorizacion is not null || ultima.ReferenciaAutorizacion is not null))
                            throw new ConfiguracionConceptoException("AUTORIZACION_INCIDENCIA_NO_DEMOSTRABLE");
                        ReferenciaSegmentoTemporal? baseParametro = ultima.IdCondicionBase is { } idCondicion && ultima.FechaBaseInicio is { } inicio && ultima.FechaBaseFin is { } fin
                            ? new(s.IdContrato, idCondicion, inicio, fin) : null;
                        var parametro = await ParametroIncidenciaAsync(h, c, ultima.IdParametroPlanilla, baseParametro, s, ct);
                        if (parametro is not null) parametros.Add(h.IdIncidenciaPlanilla, parametro);
                        var usaMonto = parametro is null && c.PermiteMontoManual && h.Unidad == "Monto";
                        if (usaMonto != (ultima.MontoManual is not null)) throw new ConfiguracionConceptoException("MODALIDAD_CONFIRMACION_INCOMPATIBLE");
                        var clase = c.Codigo switch
                        {
                            "HORA_EXTRA_ORDINARIA" => ultima.TipoDiaHE switch { "Ordinario" => ClaseHechoIncidencia.HoraExtraOrdinaria,
                                "Feriado" => ClaseHechoIncidencia.HoraExtraFeriado, "Descanso" => ClaseHechoIncidencia.HoraExtraDescanso, _ => ClaseHechoIncidencia.HoraExtraEspecial },
                            "AUSENCIA_SIN_GOCE" => ClaseHechoIncidencia.AusenciaSinGoce,
                            _ => c.Clasificacion == "DeduccionInterna" ? ClaseHechoIncidencia.DeduccionInterna : ClaseHechoIncidencia.Bono
                        };
                        calculables.Add(new(h.IdIncidenciaPlanilla, h.IdColaborador, c.IdConcepto, c.IdVersion, h.FechaIncidencia,
                            h.FechaIncidencia, clase, h.Cantidad, h.Unidad, ultima.MontoManual ?? 0, ultima.ReferenciaSoporte,
                            true, autorizada, ultima.ReferenciaAutorizacion, baseParametro, ultima.IdIncidenciaPlanillaConfirmacion, ultima.NumeroVersion));
                    }
                }
                catch (ConfiguracionConceptoException e) { Falta(e.Codigo); }
                catch (ArgumentException) { Falta("VIGENCIA_CONCEPTO_INVALIDA"); }
            }
            if (ultima is null) Falta("CONFIRMACION_INCIDENCIA_NO_DEMOSTRABLE");
        }
        return new(filas.AsReadOnly(), calculables.AsReadOnly(), conceptos, parametros, bloqueos.AsReadOnly());
    }
}

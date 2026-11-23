using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<ResultadoRentaSalarial> PrepararRentaEnTransaccionAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, string actor, AcumuladoMensualRenta? acumulado = null, CancellationToken ct = default)
        => PrepararRentaConFronterasAsync(idPeriodo, referenciaRelacionOrdinaria, aplicabilidades, actor, acumulado, null, ct);

    private async Task<ResultadoRentaSalarial> PrepararRentaConFronterasAsync(long idPeriodo, string referenciaRelacionOrdinaria,
        IReadOnlyList<AplicabilidadCuota> aplicabilidades, string actor, AcumuladoMensualRenta? acumulado,
        IReadOnlyDictionary<long, IReadOnlyList<DateTime>>? fronterasAdicionales, CancellationToken ct)
    {
        if (contexto.ChangeTracker.HasChanges() || contexto.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
            throw new InvalidOperationException("H requiere la transacción Serializable del consumidor FIN-01, sin cambios pendientes.");
        acumulado = CongelarAcumuladoRenta(acumulado);
        aplicabilidades = Array.AsReadOnly(aplicabilidades.ToArray());
        contexto.ChangeTracker.Clear();
        var periodo = await PeriodoIncidenciasEditableAsync(idPeriodo, ct);
        await AutorizarAsync(actor, ct);
        ResultadoRentaSalarial Falta(string codigo) => ProductorRentaSalarial.Fallido([new(codigo, "RENTA", periodo.Inicio)]);
        try
        {
            if (periodo.Tipo == TiposPeriodoPlanilla.Quincenal && periodo.Inicio.Day == 16)
            {
                if (acumulado?.CuotasSociales is not { } anteriorG) return Falta("ACUMULADO_MENSUAL_RENTA_NO_DEMOSTRABLE");
                if (acumulado.InicioMes != anteriorG.InicioMes || acumulado.Hasta != anteriorG.Hasta
                    || !acumulado.Colaboradores.Order().SequenceEqual((anteriorG.Colaboradores ?? []).Order())
                    || !ProductorRentaSalarial.MismasLineas(acumulado.Lineas, anteriorG.Lineas.Concat(anteriorG.CuotasPrevias.Select(a => a.Linea))))
                    return Falta("ACUMULADO_RENTA_CUOTAS_INCOMPATIBLE");
            }
            var g = await PrepararCuotasConFronterasAsync(idPeriodo, referenciaRelacionOrdinaria, aplicabilidades, actor, acumulado?.CuotasSociales, fronterasAdicionales, ct);
            if (!g.EsResoluble) return ProductorRentaSalarial.Fallido(g.Bloqueos);
            var inicioMes = new DateTime(periodo.Inicio.Year, periodo.Inicio.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);
            var fiscales = await LeerVersionesFiscalesAsync(inicioMes, finMes, ct);
            var conceptos = await LeerConceptosAsync(["IMPUESTO_SALARIO"], ct);
            var personas = g.Conciliacion!.Lineas.Select(l => l.Fuente.IdColaborador)
                .Concat(acumulado?.Colaboradores ?? []).Distinct().ToArray();
            var asignaciones = await FiltrarClaves(contexto.CreditosFiscalesColaborador.AsNoTracking(), c => c.IdColaborador, personas)
                .Where(c => c.FechaVigenciaInicio <= finMes && (c.FechaVigenciaFin == null || c.FechaVigenciaFin >= inicioMes)).ToArrayAsync(ct);
            var acreditaciones = new List<AcreditacionRenta>();
            foreach (var c in asignaciones)
                acreditaciones.Add(new(c.IdCreditoFiscalColaborador, c.IdColaborador, c.IdCreditoFiscalNormativo, c.CantidadAcreditada,
                    new(c.FechaVigenciaInicio, c.FechaVigenciaFin), c.ReferenciaSoporte, c.AprobadoPor, c.FechaAprobacion,
                    c.EstadoRegistro == Activo, await AcreditacionRentaDemostradaAsync(c, ct)));
            var resultado = ProductorRentaSalarial.Producir(periodo, g.Conciliacion.Lineas, fiscales, conceptos, acreditaciones, acumulado);
            return resultado.EsResoluble ? resultado with { CuotasSociales = g } : resultado;
        }
        catch (ConfiguracionConceptoException e) { return Falta(e.Codigo); }
    }

    private async Task<IReadOnlyList<VersionFiscalRenta>> LeerVersionesFiscalesAsync(DateTime inicio, DateTime fin, CancellationToken ct)
    {
        // Incluir también versiones inactivas/solapadas: no ocultar una configuración fiscal ambigua.
        var normas = await contexto.VersionesNormativas.AsNoTracking().Where(v => v.Institucion == "HACIENDA"
            && v.FechaVigenciaInicio <= fin && (v.FechaVigenciaFin == null || v.FechaVigenciaFin >= inicio)).ToArrayAsync(ct);
        var resultados = new List<VersionFiscalRenta>();
        foreach (var n in normas.OrderBy(n => n.IdVersionNormativa))
        {
            var tramos = await contexto.TramosImpuestoSalario.AsNoTracking().Where(t => t.IdVersionNormativa == n.IdVersionNormativa)
                .OrderBy(t => t.NumeroOrden).ToArrayAsync(ct);
            var creditos = await contexto.CreditosFiscalesNormativos.AsNoTracking().Where(c => c.IdVersionNormativa == n.IdVersionNormativa)
                .OrderBy(c => c.CodigoTipo).ToArrayAsync(ct);
            var revisada = false;
            if (NormativaUtilizable(n))
            {
                var huella = await HuellaRevisionAsync(n.IdVersionNormativa, ct);
                revisada = huella == await HuellaAuditadaAsync(n.IdVersionNormativa, "CargarNormativa", ct)
                    && huella == await HuellaAuditadaAsync(n.IdVersionNormativa, "ActivarNormativa", ct);
            }
            resultados.Add(new(n.IdVersionNormativa, n.Codigo, n.Institucion, new(n.FechaVigenciaInicio, n.FechaVigenciaFin),
                n.FuenteOficialUrl, n.HuellaContenido ?? "", n.EstadoRegistro == Activo, revisada,
                Array.AsReadOnly(tramos.Select(t => new TramoRentaVersionado(t.IdTramoImpuestoSalario, t.IdVersionNormativa,
                    t.NumeroOrden, t.LimiteInferior, t.LimiteSuperior, t.Tasa)).ToArray()),
                Array.AsReadOnly(creditos.Select(c => new CreditoRentaVersionado(c.IdCreditoFiscalNormativo, c.IdVersionNormativa,
                    c.CodigoTipo, c.MontoUnitario, new(c.FechaVigenciaInicio, c.FechaVigenciaFin), c.Requisitos, c.EstadoRegistro == Activo)).ToArray())));
        }
        return Array.AsReadOnly(resultados.ToArray());
    }

    private async Task<bool> AcreditacionRentaDemostradaAsync(CreditoFiscalColaborador c, CancellationToken ct)
    {
        var id = c.IdCreditoFiscalColaborador.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var registros = await contexto.BitacoraAuditoria.AsNoTracking().Where(b => b.Entidad == nameof(CreditoFiscalColaborador)
            && b.Accion == "AcreditarCredito" && b.IdRegistro == id).Select(b => new { b.IdUsuario, b.ValoresNuevos }).ToArrayAsync(ct);
        if (registros.Length != 1 || registros[0].IdUsuario != c.AprobadoPor) return false;
        try
        {
            using var json = JsonDocument.Parse(registros[0].ValoresNuevos ?? ""); var raiz = json.RootElement;
            var vigencia = raiz.GetProperty("Vigencia"); var fin = vigencia.GetProperty("Fin");
            return raiz.GetProperty("IdColaborador").GetInt64() == c.IdColaborador
                && raiz.GetProperty("IdCreditoNormativo").GetInt64() == c.IdCreditoFiscalNormativo
                && raiz.GetProperty("Cantidad").GetDecimal() == c.CantidadAcreditada
                && vigencia.GetProperty("Inicio").GetDateTime() == c.FechaVigenciaInicio
                && (fin.ValueKind == JsonValueKind.Null ? c.FechaVigenciaFin is null : fin.GetDateTime() == c.FechaVigenciaFin);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { return false; }
    }

    private static AcumuladoMensualRenta? CongelarAcumuladoRenta(AcumuladoMensualRenta? a)
    {
        if (a is null) return null;
        EvaluacionRenta Copiar(EvaluacionRenta e) => e with { Aportes = Array.AsReadOnly(e.Aportes.ToArray()),
            Tramos = Array.AsReadOnly(e.Tramos.ToArray()), Creditos = Array.AsReadOnly(e.Creditos.ToArray()),
            Version = e.Version with { Tramos = Array.AsReadOnly(e.Version.Tramos.ToArray()), Creditos = Array.AsReadOnly(e.Version.Creditos.ToArray()) } };
        return a with { Colaboradores = Array.AsReadOnly(a.Colaboradores.ToArray()), Lineas = Array.AsReadOnly(a.Lineas.ToArray()),
            Retenciones = Array.AsReadOnly(a.Retenciones.Select(r => r with { Evaluacion = Copiar(r.Evaluacion),
                EvaluacionPrevia = r.EvaluacionPrevia is { } p ? Copiar(p) : null }).ToArray()),
            CuotasSociales = a.CuotasSociales is { } g ? g with { Lineas = Array.AsReadOnly(g.Lineas.ToArray()),
                Colaboradores = g.Colaboradores is null ? null : Array.AsReadOnly(g.Colaboradores.ToArray()),
                CuotasPrevias = Array.AsReadOnly(g.CuotasPrevias.Select(c => c with { Aportes = Array.AsReadOnly(c.Aportes.ToArray()) }).ToArray()) } : null };
    }
}

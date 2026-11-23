using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;

namespace SIGECDC.Persistence.Forecast;

public sealed partial class ForecastComparacionEscenarioService
{
    private async Task<ComparacionEscenariosForecast> CompararSnapshotsAsync(ForecastEscenario a,
        ForecastEscenario b, SolicitudCompararEscenariosForecast solicitud, CancellationToken ct)
    {
        async Task<ConsultaResultadosForecast> Leer(ForecastEscenario e)
        {
            long? periodo = solicitud.NumeroOrdenPeriodo is {} orden
                ? await contexto.ForecastPeriodos.Where(p => p.IdForecastEscenario == e.IdForecastEscenario
                    && p.NumeroOrden == orden && p.EstadoRegistro == "Activo").Select(p => (long?)p.IdForecastPeriodo).SingleAsync(ct)
                : null;
            var consulta = await ForecastSnapshotConsulta.LeerAsync(contexto, e.IdForecastEscenario,
                new() { Dimension = solicitud.Dimension, IdForecastPeriodo = periodo }, ct);
            if (consulta is null || consulta.Filas.Count == 0)
                throw new ValidationException("El escenario no conserva un resultado financiero vigente para comparar.");
            return consulta;
        }
        var ca = await Leer(a);
        var cb = await Leer(b);
        var ids = new[] { a.IdForecastEscenario, b.IdForecastEscenario };
        var personas = await contexto.ForecastParticipantes.AsNoTracking()
            .Where(p => ids.Contains(p.IdForecastEscenario)).ToDictionaryAsync(p => p.IdForecastParticipante, ct);

        string Clave(ConsultaResultadosForecast c, FilaResultadoForecast f)
        {
            if (solicitud.Dimension == DimensionesResultadoForecast.Periodo)
            {
                var id = long.Parse(f.Clave.Split(':')[1], CultureInfo.InvariantCulture);
                return "PER:" + c.Periodos.Single(p => p.IdForecastPeriodo == id).NumeroOrden.ToString(CultureInfo.InvariantCulture);
            }
            if (solicitud.Dimension == DimensionesResultadoForecast.Colaborador)
            {
                var p = personas[long.Parse(f.Clave.Split(':')[1], CultureInfo.InvariantCulture)];
                // Identidad persistida, nunca nombre vivo ni ID local al escenario.
                return p.IdColaborador is {} id ? "COL:" + id.ToString(CultureInfo.InvariantCulture) : "PREV:" + p.CodigoParticipante;
            }
            return f.Clave;
        }
        var fa = ca.Filas.ToDictionary(f => Clave(ca, f), StringComparer.Ordinal);
        var fb = cb.Filas.ToDictionary(f => Clave(cb, f), StringComparer.Ordinal);
        var filas = fa.Keys.Union(fb.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(key =>
        {
            fa.TryGetValue(key, out var x); fb.TryGetValue(key, out var y);
            static string Concepto(ConceptoResultadoForecast c) => $"{c.Codigo}:{c.IdConceptoVersion}:{c.Efecto}";
            var xa = (x?.Conceptos ?? []).ToDictionary(Concepto, StringComparer.Ordinal);
            var xb = (y?.Conceptos ?? []).ToDictionary(Concepto, StringComparer.Ordinal);
            var conceptos = xa.Keys.Union(xb.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(k =>
            {
                xa.TryGetValue(k, out var u); xb.TryGetValue(k, out var v);
                var nombre = u is not null && v is not null && u.Nombre != v.Nombre
                    ? u.Nombre + " ↔ " + v.Nombre : u?.Nombre ?? v!.Nombre;
                return new ImporteComparadoForecast(nombre, u?.MontoProyectado, v?.MontoProyectado);
            }).ToArray();
            return new FilaSnapshotsComparadosForecast(key, x?.Etiqueta, y?.Etiqueta, x?.Detalle, y?.Detalle,
                Importes(x?.SalarioBrutoProyectado, y?.SalarioBrutoProyectado, x?.DeduccionesProyectadas, y?.DeduccionesProyectadas,
                    x?.SalarioNetoProyectado, y?.SalarioNetoProyectado, x?.CargasPatronalesProyectadas, y?.CargasPatronalesProyectadas,
                    x?.CostoLaboralTotalProyectado, y?.CostoLaboralTotalProyectado), conceptos);
        }).ToArray();
        var totales = Importes(ca.BrutoSalarialTotalConsulta, cb.BrutoSalarialTotalConsulta, ca.DeduccionesTotalConsulta, cb.DeduccionesTotalConsulta,
            ca.SalarioNetoTotalConsulta, cb.SalarioNetoTotalConsulta, ca.CargasPatronalesTotalConsulta, cb.CargasPatronalesTotalConsulta,
            ca.MontoProyectadoTotalConsulta, cb.MontoProyectadoTotalConsulta);
        // Validar límites antes de devolver el DTO, no durante el render de la pantalla.
        foreach (var importe in totales.Concat(filas.SelectMany(f => f.Importes.Concat(f.Conceptos))))
            _ = importe.Diferencia;
        return new()
        {
            EscenarioBase = Resumen(a), EscenarioAlternativo = Resumen(b), Dimension = solicitud.Dimension,
            NumeroOrdenPeriodo = solicitud.NumeroOrdenPeriodo,
            Periodos = ca.Periodos.Select(p => new PeriodoComparacionForecastOpcion(p.NumeroOrden, p.TipoPeriodo, p.FechaInicio, p.FechaFin)).ToArray(),
            Snapshot = new(totales, filas)
        };
        // Consulta pura. Comparado y su evidencia pertenecen a M; aquí no se transiciona ni escribe.
    }

    private static EscenarioComparadoForecast Resumen(ForecastEscenario e) => new()
    {
        IdForecastEscenario = e.IdForecastEscenario, Nombre = e.Nombre, VersionMotor = e.VersionMotor,
        EstadoEscenario = e.EstadoEscenario, FechaCalculo = e.FechaCalculo,
        FechaInicioProyeccion = e.FechaInicioProyeccion, FechaFinProyeccion = e.FechaFinProyeccion, MontoProyectadoTotal = e.MontoProyectadoTotal
    };
    private static ImporteComparadoForecast[] Importes(decimal? ba, decimal? bb, decimal? da, decimal? db,
        decimal? na, decimal? nb, decimal? pa, decimal? pb, decimal? ca, decimal? cb) =>
        [new("Bruto", ba, bb), new("Deducciones", da, db), new("Neto", na, nb), new("Carga patronal", pa, pb), new("Costo laboral total", ca, cb)];
}

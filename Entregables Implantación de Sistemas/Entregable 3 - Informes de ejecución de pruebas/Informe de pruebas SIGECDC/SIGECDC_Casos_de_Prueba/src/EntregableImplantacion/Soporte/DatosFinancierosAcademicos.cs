using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Application.Planillas;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Tests.EntregableImplantacion.Soporte;

// Dataset NUEVO y enteramente sintético. Sus tasas NO representan normativa real.
// Dos salarios mensuales: 240000 y 360000. Tres cuotas obreras de 1% = 18000;
// diez cuotas patronales de 1% + RT de 2% = 72000. Renta sintética exenta.
// Oráculos independientes: bruto 600000, neto 582000, costo total 672000.
public sealed class DatosFinancierosAcademicos(EntornoAcademico entorno)
{
    public const string Actor = EntornoAcademico.Rrhh;
    public const string Soporte = "Dataset universitario sintético; sin aplicabilidad empresarial ni normativa real";
    public static readonly VigenciaFinanciera Vigencia = new(new(2024, 1, 1), new(2035, 12, 31));
    public List<long> Colaboradores { get; } = [];
    public List<long> Contratos { get; } = [];
    public Dictionary<string, long> Conceptos { get; } = [];
    public long Periodo { get; private set; }
    public long Ejecucion { get; private set; }
    public long Planilla { get; private set; }
    public RecorridoPlanilla Recorrido => entorno.Obtener<RecorridoPlanilla>();

    public async Task PrepararAsync(bool conCondiciones = true, bool departamentosDistintos = false)
    {
        var gobierno = entorno.Obtener<IGobiernoFinancieroService>();
        await PaqueteAsync("CCSS", "ccss.sa.cr", CatalogoCuotasSociales.Componentes.Select(c =>
            new ValorNormativoCarga(c.Codigo, "QA " + c.Codigo, c.Sujeto, "Proporcion", c.CodigoBase, true, "ORDINARIO", .01m, Vigencia))
            .Concat(new[] { new ValorNormativoCarga("BMC_SEM", "QA BMC SEM", "Empresa", "CRC_Mensual", "BASE_SEM", true, "ORDINARIO", 100000m, Vigencia),
                new ValorNormativoCarga("BMC_IVM", "QA BMC IVM", "Empresa", "CRC_Mensual", "BASE_IVM", true, "ORDINARIO", 100000m, Vigencia) }).ToArray(), [], []);
        await PaqueteAsync("MTSS", "mtss.go.cr", [], [new("QA_OCUPACION", "Ocupación sintética", "Mensual", "Diurna", 100000, Vigencia)], []);
        await PaqueteAsync("HACIENDA", "hacienda.go.cr", [], [], [new(1, 0, null, 0)]);
        await ConceptoAsync("SALARIO_ORDINARIO", "IngresoSalarial", "SALARIO_DEVENGADO", "Monto", true);
        foreach (var cuota in CatalogoCuotasSociales.Componentes.Where(c => c.Sujeto != "Estado"))
            await ConceptoAsync(cuota.Codigo, cuota.Sujeto == "Patrono" ? "CargaPatronal" : "DeduccionLegal", cuota.CodigoBase, "Porcentaje", false);
        await ConceptoAsync("IMPUESTO_SALARIO", "DeduccionLegal", "RENTA_SALARIAL", "Monto", false);
        await ConceptoAsync("RIESGO_TRABAJO", "CargaPatronal", "BASE_RT_POLIZA", "Porcentaje", false);
        await ConceptoAsync("HORA_EXTRA_ORDINARIA", "IngresoSalarial", "HORA_ORDINARIA", "Hora", true, "Hora extra");
        await ConceptoAsync("AUSENCIA_SIN_GOCE", "IngresoSalarial", "REDUCCION_DEVENGO", "Cantidad", true, "Ausencia sin goce");
        await ConceptoAsync("BONO_ACADEMICO", "IngresoNoSalarial", "MONTO_AUTORIZADO", "Monto", false, "Bono", true);
        var jornada = await gobierno.RegistrarJornadaAsync(new("QA_DIURNA", "Jornada académica", "Diurna", 8, 48, Vigencia, Soporte), Actor);
        var riesgo = await gobierno.RegistrarRiesgoTrabajoAsync(new("QA-POLIZA", "QA-ACTIVIDAD", .02m, 0, 0, Vigencia, Soporte), Actor);
        var ocupacion = await entorno.Db.OcupacionesSalarioMinimo.Where(o => o.CodigoOficial == "QA_OCUPACION").Select(o => o.IdOcupacionSalarioMinimo).SingleAsync();
        foreach (var (codigo, salario) in new[] { ("ACA-01", 240000m), ("ACA-02", 360000m) })
        {
            var datos = await entorno.DatosColaboradorAsync(codigo);
            if (departamentosDistintos && codigo == "ACA-02")
            {
                var departamentos = entorno.Obtener<IDepartamentoService>();
                await departamentos.RegistrarAsync(new() { Nombre = "Taller académico" });
                datos.IdDepartamento = (await departamentos.ObtenerTodosAsync()).Single(x => x.Nombre == "Taller académico").IdDepartamento;
                var puestos = entorno.Obtener<IPuestoService>();
                await puestos.RegistrarAsync(new() { Nombre = "Mecánico académico", IdDepartamento = datos.IdDepartamento });
                datos.IdPuesto = (await puestos.ObtenerTodosAsync()).Single(x => x.Nombre == "Mecánico académico").IdPuesto;
            }
            var colaborador = await entorno.Obtener<IColaboradorService>().RegistrarColaboradorAsync(datos, Actor);
            Colaboradores.Add(colaborador);
            await gobierno.HomologarPuestoAsync(datos.IdPuesto, ocupacion, Soporte, Actor);
            var contrato = await entorno.Obtener<IContratoDocumentoService>().RegistrarContratoAsync(new() { IdColaborador = colaborador,
                TipoContrato = "Indefinido", FechaInicio = Vigencia.Inicio, FechaFin = Vigencia.Fin, SalarioBase = salario, Jornada = "Diurna", PeriodicidadPago = "Mensual" }, null, Actor);
            Contratos.Add(contrato); await gobierno.AsociarContratoAsync(contrato, jornada, riesgo, Soporte, Actor);
            if (conCondiciones) await entorno.Obtener<ICondicionLaboralService>().RegistrarCondicionAsync(new(contrato, Vigencia, salario,
                datos.IdPuesto, datos.IdDepartamento, ocupacion, jornada, Soporte, null), Actor);
        }
        Periodo = await CrearPeriodoAsync("ACA-ENERO", new(2032, 1, 1));
    }

    private async Task PaqueteAsync(string institucion, string dominio, IReadOnlyList<ValorNormativoCarga> valores,
        IReadOnlyList<MinimoCarga> minimos, IReadOnlyList<TramoRentaCarga> tramos)
    {
        // URL con host admitido por la validación del producto, marcada expresamente como QA.
        // No se descarga ni se presenta como una publicación normativa auténtica.
        var evidencia = Encoding.UTF8.GetBytes("SIMULACION ACADEMICA — " + institucion + " — " + Soporte);
        var fuente = new FuenteNormativa("QA_" + institucion, institucion, "Simulación académica " + institucion,
            "https://" + dominio + "/qa-sintetico-no-normativa", "QA-SINTETICO", new(2024, 1, 1), Vigencia,
            Convert.ToHexString(SHA256.HashData(evidencia)));
        var gobierno = entorno.Obtener<IGobiernoFinancieroService>();
        var version = await gobierno.ImportarNormativaAsync(new(fuente, valores, minimos, tramos, []), evidencia, Actor);
        var huella = await gobierno.ObtenerHuellaRevisionAsync(version, EntornoAcademico.Admin);
        await gobierno.ActivarNormativaAsync(version, huella, EntornoAcademico.Admin);
    }

    private async Task ConceptoAsync(string codigo, string clasificacion, string baseCalculo, string unidad, bool contributivo,
        string? tipo = null, bool manual = false)
    {
        var id = await entorno.Obtener<IGobiernoFinancieroService>().RegistrarConceptoAsync(new(codigo, "QA " + codigo, Vigencia,
            new(clasificacion, clasificacion == "IngresoSalarial", contributivo, contributivo, clasificacion == "CargaPatronal", baseCalculo, unidad, manual, false), Soporte, tipo), Actor);
        Conceptos[codigo] = await entorno.Db.ConceptosPlanillaVersion.Where(v => v.IdConceptoPlanilla == id).Select(v => v.IdConceptoPlanillaVersion).SingleAsync();
    }

    public Task<long> CrearPeriodoAsync(string codigo, DateTime inicio) => entorno.Obtener<IPlanillaService>().CrearPeriodoAsync(new()
    { CodigoPeriodo = codigo, Nombre = codigo, TipoPeriodo = "Mensual", FechaInicio = inicio, FechaFin = inicio.AddMonths(1).AddDays(-1) }, Actor);

    public static IReadOnlyList<AplicabilidadCuota> Cuotas(IEnumerable<long> personas) => personas.SelectMany(persona =>
        CatalogoCuotasSociales.Componentes.Where(c => c.Sujeto != "Estado").Select(c => new AplicabilidadCuota(persona, c.Codigo, "ORDINARIO", Vigencia,
            c.CodigoMinimo is null ? ReglaBaseCuota.RemuneracionContributiva : ReglaBaseCuota.MinimoMensualOrdinario, Soporte, Soporte, Actor))).ToArray();

    public SolicitudSnapshotPlanilla Solicitud(long? periodo = null) => new(periodo ?? Periodo, Soporte, Cuotas(Colaboradores),
        Contratos.Select(id => new AplicabilidadRiesgoTrabajo(id, "QA-POLIZA", "QA-ACTIVIDAD", Vigencia,
            ModalidadBaseRiesgo.RemuneracionSalarialOrdinaria, Soporte, Actor)).ToArray(), []);

    public async Task CalcularAsync(bool aprobar = false)
    {
        Ejecucion = await Recorrido.CalcularAsync(Solicitud(), Actor);
        Planilla = await entorno.Db.EjecucionesPlanilla.Where(x => x.IdEjecucionPlanilla == Ejecucion).Select(x => x.IdPlanilla).SingleAsync();
        if (aprobar) await Recorrido.AprobarAsync(Periodo, Ejecucion, Actor);
    }

    public async Task<long> CapturarAsync(string concepto, string unidad, decimal? cantidad, decimal? monto, int persona = 0, int? parametro = null)
    {
        var id = await Recorrido.CapturarAsync(new() { IdPeriodo = Periodo, IdColaborador = Colaboradores[persona], IdConceptoVersion = Conceptos[concepto],
            Fecha = new(2032, 1, 10), Unidad = unidad, Cantidad = cantidad, Monto = monto, ReferenciaHecho = Soporte, RelacionOrdinaria = Soporte }, Actor);
        await Recorrido.ConfirmarAsync(new() { IdIncidencia = id, VersionEsperada = 0, Soporte = Soporte, RelacionOrdinaria = Soporte,
            TipoDia = concepto == "HORA_EXTRA_ORDINARIA" ? "Ordinario" : null, IdParametro = parametro }, Actor);
        return id;
    }

    public async Task<long> EscenarioAsync(string nombre = "Escenario académico", DateTime? inicio = null, int meses = 1, bool condiciones = true)
    {
        var desde = inicio ?? new DateTime(2032, 2, 1);
        var escenario = await entorno.Obtener<IForecastEscenarioService>().CrearBorradorAsync(new() { Nombre = nombre, IdPlanillasHistoricas = [Planilla],
            Periodos = Enumerable.Range(0, meses).Select(i => new SolicitudPeriodoForecast { TipoPeriodo = "Mensual", FechaInicio = desde.AddMonths(i), FechaFin = desde.AddMonths(i + 1).AddDays(-1) }).ToList() }, Actor);
        await ComposicionAsync(escenario, true);
        var evidencia = entorno.Obtener<IForecastEvidenciaService>();
        var opciones = await evidencia.ConsultarConfiguracionAsync(escenario, Actor);
        var riesgo = await entorno.Db.ConfiguracionesRiesgoTrabajo.Select(r => r.IdConfiguracionRiesgoTrabajo).SingleAsync();
        foreach (var propuesta in opciones.CondicionesDisponibles)
        {
            if (condiciones) await evidencia.RegistrarCondicionAsync(propuesta.Condicion with { Soporte = Soporte, IdConfiguracionRT = riesgo,
                ModalidadRT = ModalidadBaseRiesgo.RemuneracionSalarialOrdinaria, SoporteRT = Soporte }, Actor);
            await entorno.Obtener<IForecastAsignacionProyectoService>().GuardarDistribucionAsync(escenario,
                propuesta.Condicion.IdParticipante, propuesta.Condicion.IdPeriodo,
                new() { Asignaciones = [new() { IdProyecto = null, Porcentaje = 100 }] }, Actor);
        }
        return escenario;
    }

    public Task ComposicionAsync(long escenario, bool incluirSegundo) => entorno.Obtener<IForecastParticipanteService>().GuardarComposicionAsync(escenario,
        new() { Participantes = Colaboradores.Select((id, i) => new SolicitudParticipanteForecast { IdColaborador = id, EstaIncluido = i == 0 || incluirSegundo }).ToArray() }, Actor);

    public Task<long> BonoProyectadoAsync(long escenario, decimal monto) => entorno.Obtener<IForecastEvidenciaService>()
        .RegistrarOverrideAsync(new(escenario, Conceptos["BONO_ACADEMICO"], "Monto", "Monto", monto, Soporte), Actor);

    public async Task<ResultadoGeneracionForecast> GenerarAsync(long escenario)
    {
        var participantes = await entorno.Db.ForecastParticipantes.Where(p => p.IdForecastEscenario == escenario && p.EstaIncluido).Select(p => p.IdForecastParticipante).ToArrayAsync();
        return await entorno.Obtener<IForecastCalculoService>().GenerarForecastAsync(new SolicitudCalculoFinancieroForecast(escenario, Cuotas(participantes)), Actor);
    }
}

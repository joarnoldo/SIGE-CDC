using System.ComponentModel.DataAnnotations;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public sealed class CapturaIncidenciaInterfaz
{
    [Range(1, long.MaxValue)] public long IdPeriodo { get; set; }
    [Range(1, long.MaxValue)] public long IdColaborador { get; set; }
    [Range(1, long.MaxValue)] public long IdConceptoVersion { get; set; }
    [Required] public DateTime? Fecha { get; set; }
    public string Unidad { get; set; } = "";
    public decimal? Cantidad { get; set; }
    public decimal? Monto { get; set; }
    [Required, StringLength(300)] public string ReferenciaHecho { get; set; } = "";
    [Required, StringLength(500)] public string RelacionOrdinaria { get; set; } = "";
}

public sealed class ConfirmacionIncidenciaInterfaz
{
    public long IdIncidencia { get; set; }
    public int VersionEsperada { get; set; }
    [Required, StringLength(500)] public string Soporte { get; set; } = "";
    [Required, StringLength(500)] public string RelacionOrdinaria { get; set; } = "";
    public string? TipoDia { get; set; }
    public bool Autorizar { get; set; }
    [StringLength(500)] public string? Autorizacion { get; set; }
    public int? IdParametro { get; set; }
    public bool ConfirmarBaseSalario { get; set; }
}

// Adaptación de la captura humana a los contratos certificados. No calcula importes,
// no escribe entidades ni ofrece una alternativa al motor J.
public sealed class RecorridoPlanilla(IConsultaRecorridoPlanilla consulta,
    IIncidenciasCalculadasService incidencias, IResolucionTemporalFinancieraService temporal,
    IGobiernoFinancieroService gobierno, INucleoConceptosService conceptos, ISnapshotPlanillaService snapshot,
    IIncidenciaPlanillaService correcciones)
{
    public static bool EsParametroInternoAdmitido(ConceptoMonetarioVersion c) => c.Activo && c.EvidenciaConfirmada
        && c.Clasificacion is "IngresoSalarial" or "IngresoNoSalarial" or "DeduccionInterna"
        && (c.TipoBase == "SALARIO_DEVENGADO" && c.UnidadPermitida == "Porcentaje"
            || c.TipoBase == "MONTO_AUTORIZADO" && c.UnidadPermitida == "Monto");

    public async Task<long> CapturarAsync(CapturaIncidenciaInterfaz datos, string actor, CancellationToken ct = default)
    {
        Validator.ValidateObject(datos, new(datos), true);
        var catalogos = await consulta.CatalogosAsync(actor, ct);
        var c = catalogos.Conceptos.SingleOrDefault(c => c.IdVersion == datos.IdConceptoVersion)
            ?? throw new ConfiguracionConceptoException("CONCEPTO_VERSION_FALTANTE");
        var tipos = catalogos.Tipos.Where(t => t.IdConcepto == c.IdConcepto).ToArray();
        if (tipos.Length != 1) throw new ConfiguracionConceptoException("TIPO_INCIDENCIA_NO_CLASIFICADO");
        ValidarCaptura(datos, c);
        return await incidencias.CapturarIncidenciaFinancieraAsync(new(datos.IdPeriodo, datos.IdColaborador,
            tipos[0].Id, c.IdVersion, datos.Fecha!.Value, datos.Unidad, datos.Cantidad, datos.Monto ?? 0, datos.ReferenciaHecho.Trim()),
            datos.RelacionOrdinaria.Trim(), actor, ct);
    }

    private static void ValidarCaptura(CapturaIncidenciaInterfaz datos, ConceptoMonetarioVersion c)
    {
        Validator.ValidateObject(datos, new(datos), true);
        if (!c.Vigencia.Contiene(datos.Fecha!.Value)) throw new ConfiguracionConceptoException("CONCEPTO_VIGENCIA_INCOMPATIBLE");
        var cantidad = c.Codigo is "HORA_EXTRA_ORDINARIA" or "AUSENCIA_SIN_GOCE";
        if (cantidad && (datos.Monto is not null || datos.Cantidad is null or <= 0))
            throw new ConfiguracionConceptoException("CAPTURA_REQUIERE_CANTIDAD_NO_MONTO");
        if (!cantidad && !EsParametroInternoAdmitido(c)) throw new ConfiguracionConceptoException("CLASE_HECHO_NO_SOPORTADA");
        if (datos.Monto is not null && (!c.PermiteMontoManual || c.TipoBase != "MONTO_AUTORIZADO"))
            throw new ConfiguracionConceptoException("MONTO_MANUAL_NO_PERMITIDO");
        if (c.PermiteMontoManual && c.TipoBase == "MONTO_AUTORIZADO" && datos.Monto is null)
            throw new ConfiguracionConceptoException("MONTO_DOCUMENTADO_FALTANTE");
    }

    public async Task CorregirHechoAsync(long id, DateTime? fecha, decimal? cantidad, decimal? monto,
        string referencia, string actor, CancellationToken ct = default)
    {
        // Consulta autorizada en servidor. El writer administrativo existente conserva los locks FIN-01;
        // F invalida la huella de la confirmación previa y exige reconfirmar. Nunca se escribe un snapshot.
        var h = (await consulta.IncidenciasAsync(null, actor, ct)).SingleOrDefault(h => h.Id == id)
            ?? throw new ConfiguracionConceptoException("INCIDENCIA_FALTANTE");
        if (h.EstadoPeriodo != "Borrador" || h.Estado != "Activo") throw new ConfiguracionConceptoException("PERIODO_BLOQUEADO");
        var c = (await consulta.CatalogosAsync(actor, ct)).Conceptos.SingleOrDefault(c => c.IdVersion == h.Captura.IdConceptoVersion)
            ?? throw new ConfiguracionConceptoException("CONCEPTO_VERSION_FALTANTE");
        ValidarCaptura(new() { IdPeriodo = h.Captura.IdPeriodo, IdColaborador = h.Captura.IdColaborador,
            IdConceptoVersion = c.IdVersion, Fecha = fecha, Unidad = h.Captura.Unidad, Cantidad = cantidad,
            Monto = monto, ReferenciaHecho = referencia, RelacionOrdinaria = "Corrección de captura; requiere reconfirmación" }, c);
        await correcciones.ActualizarIncidenciaAsync(id, new() { IdPeriodoPlanilla = h.Captura.IdPeriodo,
            IdColaborador = h.Captura.IdColaborador, IdTipoIncidenciaPlanilla = h.Captura.IdTipoIncidencia,
            FechaIncidencia = fecha, Cantidad = cantidad, Monto = monto ?? 0, Descripcion = referencia.Trim() }, actor, ct);
    }

    public async Task<long> ConfirmarAsync(ConfirmacionIncidenciaInterfaz datos, string actor, CancellationToken ct = default)
    {
        Validator.ValidateObject(datos, new(datos), true);
        var hecho = (await consulta.IncidenciasAsync(null, actor, ct)).SingleOrDefault(h => h.Id == datos.IdIncidencia)
            ?? throw new ConfiguracionConceptoException("INCIDENCIA_FALTANTE");
        ReferenciaSegmentoTemporal? referencia = null;
        if (datos.IdParametro is not null && hecho.Captura.Unidad == "Porcentaje")
        {
            if (!datos.ConfirmarBaseSalario) throw new ConfiguracionConceptoException("BASE_PORCENTUAL_NO_DEMOSTRADA");
            var periodo = await consulta.PeriodoAsync(hecho.Captura.IdPeriodo, actor, ct)
                ?? throw new ConfiguracionConceptoException("PERIODO_FALTANTE");
            var t = await temporal.ResolverPeriodoTemporalAsync(new(periodo.Periodo.FechaInicio,
                periodo.Periodo.FechaFin, periodo.Periodo.TipoPeriodo), actor, datos.RelacionOrdinaria.Trim(), ct);
            var persona = t.Poblacion.SingleOrDefault(p => p.IdColaborador == hecho.Captura.IdColaborador);
            if (persona is null || !persona.EsResoluble) throw new ConfiguracionConceptoException("TEMPORAL_BLOQUEADO");
            var segmentos = persona.Segmentos.Where(s => s.Inicio <= hecho.Captura.FechaHecho && s.Fin >= hecho.Captura.FechaHecho).ToArray();
            if (segmentos.Length != 1) throw new ConfiguracionConceptoException("BASE_PORCENTUAL_NO_DEMOSTRADA");
            referencia = ReferenciaSegmentoTemporal.De(segmentos[0]);
        }
        return await incidencias.ConfirmarIncidenciaAsync(new(datos.IdIncidencia, datos.VersionEsperada,
            datos.Soporte.Trim(), datos.TipoDia, datos.Autorizar, datos.Autorizar ? datos.Autorizacion?.Trim() : null,
            datos.IdParametro, referencia), datos.RelacionOrdinaria.Trim(), actor, ct);
    }

    public async Task<int> RegistrarParametroAsync(string codigo, string nombre, long idConceptoVersion, decimal valor,
        VigenciaFinanciera vigencia, string soporte, string actor, CancellationToken ct = default)
    {
        var c = (await consulta.CatalogosAsync(actor, ct)).Conceptos.SingleOrDefault(c => c.IdVersion == idConceptoVersion);
        if (c is null || !EsParametroInternoAdmitido(c)) throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
        if (!c.Vigencia.Contiene(vigencia)) throw new ConfiguracionConceptoException("VIGENCIA_INTERNA_INVALIDA");
        return await gobierno.RegistrarParametroInternoAsync(new(codigo.Trim(), nombre.Trim(), c.IdConcepto,
            c.UnidadPermitida, c.Clasificacion == "DeduccionInterna" ? "Deduccion" : "Beneficio", valor, null,
            vigencia, soporte.Trim()), actor, ct);
    }

    public Task<long> AsignarParametroAsync(SolicitudOverrideInterno solicitud, string actor, CancellationToken ct = default)
        => conceptos.RegistrarOverrideInternoAsync(solicitud, actor, ct);
    public Task<long> CalcularAsync(SolicitudSnapshotPlanilla solicitud, string actor, CancellationToken ct = default)
        => snapshot.CalcularSnapshotAsync(solicitud, actor, ct);
    public Task ReabrirAsync(long periodo, long ejecucion, string motivo, string actor, CancellationToken ct = default)
        => snapshot.ReabrirSnapshotAsync(periodo, ejecucion, motivo.Trim(), actor, ct);
    public Task AprobarAsync(long periodo, long ejecucion, string actor, CancellationToken ct = default)
        => snapshot.AprobarSnapshotAsync(periodo, ejecucion, actor, ct);
    public Task CerrarAsync(long periodo, long ejecucion, string actor, CancellationToken ct = default)
        => snapshot.CerrarSnapshotAsync(periodo, ejecucion, actor, ct);
}

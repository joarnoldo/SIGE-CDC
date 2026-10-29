namespace SIGECDC.Domain.Planillas;

// Valores de entrada al algoritmo puro, no entidades ni snapshots persistentes.
// El proveedor debe aportar evidencia efectiva; un texto legado no la sustituye.
public sealed record ContratoTemporal(long IdContrato, VigenciaFinanciera Vigencia, decimal SalarioMensual,
    string? CodigoJornada, string? ReferenciaRelacionOrdinaria, bool HistoriaSalarialVerificable,
    long? IdCondicionLaboralVersion = null, int? IdPuesto = null, int? IdDepartamento = null, long? IdJornadaDemostrada = null);
public sealed record OcupacionTemporal(long IdOcupacion, string Codigo, VigenciaFinanciera Vigencia, string ReferenciaSoporte,
    long? IdContrato = null);
public sealed record JornadaTemporal(long IdJornada, string Codigo, string Tipo, decimal HorasDia,
    decimal HorasSemana, VigenciaFinanciera Vigencia, bool Utilizable);
public sealed record MinimoTemporal(long IdMinimo, long IdOcupacion, string Periodicidad, string? TipoJornada,
    decimal Monto, VigenciaFinanciera Vigencia, long IdVersionNormativa, VigenciaFinanciera VigenciaNormativa,
    bool NormativaVerificada);
public sealed record BloqueoTemporal(string Codigo, DateTime Fecha, long? IdContrato = null);
public sealed record DatosTemporalesColaborador(long IdColaborador, VigenciaFinanciera Relacion,
    IReadOnlyList<ContratoTemporal> Contratos, IReadOnlyList<OcupacionTemporal> Ocupaciones,
    IReadOnlyList<BloqueoTemporal> Bloqueos);
public sealed record SegmentoSalarialTemporal(long IdColaborador, DateTime Inicio, DateTime Fin, int DiasBase30,
    long IdContrato, decimal SalarioMensual, JornadaTemporal Jornada, OcupacionTemporal Ocupacion,
    MinimoTemporal Minimo, string ReferenciaRelacionOrdinaria, long? IdCondicionLaboralVersion = null,
    int? IdPuesto = null, int? IdDepartamento = null);
public sealed record PoblacionSalarialTemporal(long IdColaborador, IReadOnlyList<SegmentoSalarialTemporal> Segmentos,
    IReadOnlyList<BloqueoTemporal> Bloqueos)
{
    public bool EsResoluble => Bloqueos.Count == 0 && Segmentos.Count > 0;
}
public sealed record ResultadoTemporalSalarial(IReadOnlyList<PoblacionSalarialTemporal> Poblacion)
{
    // Nunca equivale a autorización de cálculo, publicación o levantamiento de FIN-02.
    public bool EsResoluble => Poblacion.Count > 0 && Poblacion.All(p => p.EsResoluble);
}

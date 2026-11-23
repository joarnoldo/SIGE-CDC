namespace SIGECDC.Domain.Planillas;

public enum AmbitoParametroInterno { Global, Periodo, Colaborador }
public sealed record VersionParametroInterno(int IdVersion, string Codigo, long IdConcepto, string Tipo, string Naturaleza,
    decimal? ValorDecimal, string? ValorTexto, VigenciaFinanciera? Vigencia, bool Activo, bool Gobernado, bool EsNormativo);
public sealed record OverrideParametroInterno(long Id, int IdVersion, AmbitoParametroInterno Ambito, long IdDestino,
    decimal? ValorDecimal, string? ValorTexto, bool Activo, bool EvidenciaConfirmada);
public sealed record ParametroInternoResuelto(VersionParametroInterno Version, AmbitoParametroInterno Ambito,
    long? IdOverride, decimal? ValorDecimal, string? ValorTexto);

public static class ResolucionParametrosInternos
{
    public static ParametroInternoResuelto Resolver(string codigo, DateTime fecha, long idColaborador, long? idPeriodo,
        IEnumerable<VersionParametroInterno> versiones, IEnumerable<OverrideParametroInterno> overrides)
    {
        new VigenciaFinanciera(fecha, fecha).Validar();
        var candidatas = versiones.Where(v => v.Codigo == codigo).ToArray();
        if (candidatas.Any(v => v.EsNormativo)) throw new ConfiguracionConceptoException("NORMATIVA_NO_ADMITE_OVERRIDE");
        if (candidatas.Any(v => v.Vigencia is null)) throw new ConfiguracionConceptoException("PARAMETRO_SIN_VIGENCIA");
        foreach (var v in candidatas) v.Vigencia!.Validar();
        candidatas = candidatas.Where(v => v.Vigencia!.Contiene(fecha)).ToArray();
        if (candidatas.Length != 1) throw new ConfiguracionConceptoException(candidatas.Length == 0 ? "PARAMETRO_FALTANTE" : "PARAMETRO_AMBIGUO");
        var version = candidatas[0];
        if (!version.Activo || !version.Gobernado || version.IdConcepto <= 0) throw new ConfiguracionConceptoException("PARAMETRO_NO_GOBERNADO");
        ValidarValor(version.Tipo, version.ValorDecimal, version.ValorTexto);
        var delParametro = overrides.Where(o => o.IdVersion == version.IdVersion && o.Activo).ToArray();
        if (delParametro.Any(o => o.Ambito is not (AmbitoParametroInterno.Colaborador or AmbitoParametroInterno.Periodo)))
            throw new ConfiguracionConceptoException("AMBITO_OVERRIDE_INVALIDO");
        var filas = delParametro.Where(o =>
            (o.Ambito == AmbitoParametroInterno.Colaborador && o.IdDestino == idColaborador
                || o.Ambito == AmbitoParametroInterno.Periodo && o.IdDestino == idPeriodo)).ToArray();
        if (filas.GroupBy(o => o.Ambito).Any(g => g.Count() != 1)) throw new ConfiguracionConceptoException("OVERRIDE_AMBIGUO");
        foreach (var fila in filas)
        {
            if (!fila.EvidenciaConfirmada) throw new ConfiguracionConceptoException("OVERRIDE_SIN_EVIDENCIA");
            // Una fila activa sin valor es inválida; no equivale a ausencia del override.
            ValidarValor(version.Tipo, fila.ValorDecimal, fila.ValorTexto);
        }
        var elegida = filas.SingleOrDefault(o => o.Ambito == AmbitoParametroInterno.Colaborador)
            ?? filas.SingleOrDefault(o => o.Ambito == AmbitoParametroInterno.Periodo);
        return elegida is null ? new(version, AmbitoParametroInterno.Global, null, version.ValorDecimal, version.ValorTexto)
            : new(version, elegida.Ambito, elegida.Id, elegida.ValorDecimal, elegida.ValorTexto);
    }

    public static void ValidarValor(string tipo, decimal? valor, string? texto)
    {
        if (tipo is not ("Monto" or "Porcentaje" or "Cantidad" or "Texto")) throw new ConfiguracionConceptoException("TIPO_PARAMETRO_DESCONOCIDO");
        if (tipo == "Texto")
        {
            if (valor is not null || string.IsNullOrWhiteSpace(texto)) throw new ConfiguracionConceptoException("VALOR_PARAMETRO_INVALIDO");
        }
        else if (valor is null or < 0 || texto is not null) throw new ConfiguracionConceptoException("VALOR_PARAMETRO_INVALIDO");
    }
}

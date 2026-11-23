namespace SIGECDC.Application.Forecast;

public sealed class ParametroForecastConfigurado
{
    public long? IdForecastParametro { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string TipoParametro { get; set; } = string.Empty;

    public decimal? ValorDecimal { get; set; }

    public bool EstaHabilitado { get; set; }

    public string? EstadoRegistro { get; set; }

    public IReadOnlyList<string> TiposPermitidos { get; set; } = [];
}

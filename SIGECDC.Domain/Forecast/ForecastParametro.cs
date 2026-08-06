using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastParametro
{
    public long IdForecastParametro { get; set; }

    public long IdForecastEscenario { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string TipoParametro { get; set; } = TiposParametroPlanilla.Porcentaje;

    public decimal? ValorDecimal { get; set; }

    public string? ValorTexto { get; set; }

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ForecastEscenario? ForecastEscenario { get; set; }
}

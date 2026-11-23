namespace SIGECDC.Application.Forecast;

public sealed class ContratacionPrevistaForecastResumen
{
    public long IdForecastParticipante { get; set; }
    public string CodigoParticipante { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public int IdDepartamento { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int IdPuesto { get; set; }
    public string Puesto { get; set; } = string.Empty;
    public decimal SalarioBaseMensual { get; set; }
    public DateTime FechaInicioAplicacion { get; set; }
}

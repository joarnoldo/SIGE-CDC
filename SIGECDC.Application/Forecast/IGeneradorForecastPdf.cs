using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.Forecast;

public interface IGeneradorForecastPdf
{
    ArchivoDescarga Generar(ConsultaResultadosForecast consulta);
}

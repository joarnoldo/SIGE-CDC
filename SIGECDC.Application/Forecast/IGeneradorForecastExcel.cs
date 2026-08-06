using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.Forecast;

public interface IGeneradorForecastExcel
{
    ArchivoDescarga Generar(ConsultaResultadosForecast consulta);
}

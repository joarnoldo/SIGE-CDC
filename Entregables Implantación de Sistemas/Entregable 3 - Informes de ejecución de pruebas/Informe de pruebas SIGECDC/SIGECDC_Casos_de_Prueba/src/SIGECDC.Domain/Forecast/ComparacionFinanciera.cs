using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public sealed record DiferenciaComparativa(decimal? Firmada,decimal? Absoluta,decimal? Porcentual,string? Motivo);

public static class ComparacionFinanciera
{
    public static DiferenciaComparativa Comparar(decimal? forecast,decimal? real)
    {
        if(forecast is null || real is null)return new(null,null,null,"SinCorrespondencia");
        if(PrecisionMonetaria.R2(forecast.Value)!=forecast || PrecisionMonetaria.R2(real.Value)!=real)
            throw new ConfiguracionConceptoException("COMPARACION_IMPORTE_NO_MATERIALIZADO");
        var firmada=real.Value-forecast.Value;
        decimal? porcentaje=forecast==0?(real==0?0:null):Math.Round(firmada/Math.Abs(forecast.Value)*100m,4,MidpointRounding.AwayFromZero);
        if(Math.Abs(firmada)>9999999999999999.99m || porcentaje is {} p && Math.Abs(p)>99999999999999.9999m)
            throw new ConfiguracionConceptoException("COMPARACION_FUERA_DE_RANGO");
        return new(firmada,Math.Abs(firmada),porcentaje,forecast==0 && real!=0?"BaseForecastCero":null);
    }
}

namespace SIGECDC.Domain.RecursosHumanos;

public static class PeriodicidadesPago
{
    public const string Mensual = "Mensual";
    public const string Quincenal = "Quincenal";

    public static bool EsValida(string periodicidad)
    {
        return periodicidad is Mensual or Quincenal;
    }
}

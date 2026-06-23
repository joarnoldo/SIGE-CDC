namespace SIGECDC.Domain.RecursosHumanos;

public static class EstadosContrato
{
    public const string Activo = "Activo";
    public const string Finalizado = "Finalizado";
    public const string Suspendido = "Suspendido";
    public const string Anulado = "Anulado";

    public static bool EsValido(string estado)
    {
        return estado is Activo or Finalizado or Suspendido or Anulado;
    }
}

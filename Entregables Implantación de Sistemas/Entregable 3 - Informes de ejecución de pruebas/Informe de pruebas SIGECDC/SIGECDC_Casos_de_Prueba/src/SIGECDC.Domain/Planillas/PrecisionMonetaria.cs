namespace SIGECDC.Domain.Planillas;

public static class PrecisionMonetaria
{
    // Única materialización CRC. Nunca aplicar a bases, tasas ni pasos intermedios.
    public static decimal R2(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}

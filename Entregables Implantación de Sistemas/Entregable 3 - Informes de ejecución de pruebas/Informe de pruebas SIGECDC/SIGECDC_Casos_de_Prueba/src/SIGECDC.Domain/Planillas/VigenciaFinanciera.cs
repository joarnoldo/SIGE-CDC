namespace SIGECDC.Domain.Planillas;

// Fechas civiles inclusivas; NULL representa un extremo abierto, nunca una clave distinta.
public sealed record VigenciaFinanciera(DateTime Inicio, DateTime? Fin)
{
    public void Validar()
    {
        if (Inicio == default || Inicio.TimeOfDay != TimeSpan.Zero
            || (Fin.HasValue && (Fin.Value.TimeOfDay != TimeSpan.Zero || Fin < Inicio)))
            throw new ArgumentException("Vigencia inválida: indicar fechas civiles y fin >= inicio.");
    }

    public bool Contiene(DateTime fecha) => Inicio <= fecha && (!Fin.HasValue || fecha <= Fin);
    public bool Solapa(VigenciaFinanciera otra) => Inicio <= (otra.Fin ?? DateTime.MaxValue)
        && otra.Inicio <= (Fin ?? DateTime.MaxValue);
    public bool Contiene(VigenciaFinanciera otra) => Inicio <= otra.Inicio
        && (!Fin.HasValue || (otra.Fin.HasValue && otra.Fin <= Fin));
}

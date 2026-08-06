namespace SIGECDC.Domain.Activos;

public static class ReglasMantenimientoPreventivo
{
	public static void ValidarFechaProgramada(
		DateTime fechaProgramada,
		DateTime fechaReferencia)
	{
		if (fechaProgramada == default)
		{
			throw new ArgumentException(
				"La fecha programada es obligatoria.");
		}

		if (fechaProgramada.Date < fechaReferencia.Date)
		{
			throw new ArgumentException(
				"La fecha programada no puede ser anterior a la fecha actual.");
		}
	}
}

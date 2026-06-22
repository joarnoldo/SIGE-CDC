using SIGECDC.Application.Models;

namespace SIGECDC.Application
{
	public interface INoticiaService
	{
		Task<List<Noticia>> ObtenerTodasAsync();
		Task<Noticia?> ObtenerPorIdAsync(int id);
		Task CrearAsync(Noticia noticia);
		Task EditarAsync(Noticia noticia);
		Task EliminarAsync(int id);
	}
}

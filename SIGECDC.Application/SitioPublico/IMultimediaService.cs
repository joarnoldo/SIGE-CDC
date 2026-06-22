using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGECDC.Application
{
	public interface IMultimediaService
	{
		Task<IEnumerable<object>> ObtenerGaleriaAsync();
		Task CrearMultimediaAsync(object multimedia);
		Task EliminarMultimediaAsync(long id);
	}
}

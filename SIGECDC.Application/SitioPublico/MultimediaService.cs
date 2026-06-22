using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGECDC.Application
{
	public class MultimediaService : IMultimediaService
	{
		private readonly SigeCdcDbContext _context;

		public MultimediaService(SigeCdcDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<object>> ObtenerGaleriaAsync()
		{
			return await _context.Multimedia.AsNoTracking().ToListAsync();
		}

		public async Task CrearMultimediaAsync(object multimedia)
		{
			if (multimedia is SIGECDC.Application.Models.Multimedia modeloReal)
			{
				_context.Add(modeloReal);
				await _context.SaveChangesAsync();
			}
		}

		public async Task EliminarMultimediaAsync(long id)
		{
			var archivo = await _context.Multimedia.FindAsync(id);
			if (archivo != null)
			{
				_context.Multimedia.Remove(archivo);
				await _context.SaveChangesAsync();
			}
		}
	}
}

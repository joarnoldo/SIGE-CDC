using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Models;

namespace SIGECDC.Application
{
	public class NoticiaService : INoticiaService
	{
		private readonly SigeCdcDbContext _context;

		public NoticiaService(SigeCdcDbContext context)
		{
			_context = context;
		}

		// VER TODAS LAS NOTICIAS
		public async Task<List<Noticia>> ObtenerTodasAsync()
		{
			return await _context.Noticias.ToListAsync();
		}

		// VER DETALLE POR ID
		public async Task<Noticia?> ObtenerPorIdAsync(int id)
		{
			return await _context.Noticias.FindAsync(id);
		}

		// AGREGAR
		public async Task CrearAsync(Noticia noticia)
		{
			_context.Noticias.Add(noticia);
			await _context.SaveChangesAsync();
		}

		// EDITAR 
		public async Task EditarAsync(Noticia noticia)
		{
			// Buscamos el registro real guardado en MySQL de forma directa
			var noticiaExistente = await _context.Noticias.FindAsync(noticia.Id);

			if (noticiaExistente != null)
			{
				// Actualizamos los campos individuales mapeando los valores del formulario
				noticiaExistente.Titulo = noticia.Titulo;
				noticiaExistente.Contenido = noticia.Contenido;

				// Solo actualizamos la foto si el usuario subió una nueva
				if (!string.IsNullOrEmpty(noticia.ImagenUrl))
				{
					noticiaExistente.ImagenUrl = noticia.ImagenUrl;
				}

				await _context.SaveChangesAsync();
			}
		}

		// ELIMINAR
		public async Task EliminarAsync(int id)
		{
			var noticia = await _context.Noticias.FindAsync(id);
			if (noticia != null)
			{
				_context.Noticias.Remove(noticia);
				await _context.SaveChangesAsync();
			}
		}
	}
}


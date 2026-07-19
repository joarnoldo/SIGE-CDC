using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Activos;

public sealed class MantenimientoService(ApplicationDbContext contexto)
	: IMantenimientoService
{
	public async Task<IReadOnlyList<MantenimientoResumen>> ObtenerMantenimientosAsync(
		CancellationToken cancellationToken = default)
	{
		return await contexto.Mantenimientos
			.AsNoTracking()
			.Where(mantenimiento =>
				mantenimiento.EstadoRegistro == EstadosRegistro.Activo)
			.OrderByDescending(mantenimiento =>
				mantenimiento.FechaProgramada)
			.Select(mantenimiento => new MantenimientoResumen
			{
				IdMantenimiento = mantenimiento.IdMantenimiento,
				IdActivo = mantenimiento.IdActivo,

				Activo = mantenimiento.Activo == null
					? string.Empty
					: mantenimiento.Activo.NombreActivo,

				IdProyecto = mantenimiento.IdProyecto,

				Proyecto = mantenimiento.Proyecto == null
					? null
					: mantenimiento.Proyecto.NombreProyecto,

				TipoMantenimiento = mantenimiento.TipoMantenimiento,

				IdEstadoMantenimiento =
					mantenimiento.IdEstadoMantenimiento,

				EstadoMantenimiento =
					mantenimiento.EstadoMantenimiento == null
						? string.Empty
						: mantenimiento.EstadoMantenimiento.Nombre,

				FechaProgramada = mantenimiento.FechaProgramada,
				FechaInicio = mantenimiento.FechaInicio,
				FechaFin = mantenimiento.FechaFin,
				Descripcion = mantenimiento.Descripcion,
				CostoEstimado = mantenimiento.CostoEstimado,
				CostoReal = mantenimiento.CostoReal,

				TiempoFueraServicioHoras =
					mantenimiento.TiempoFueraServicioHoras,

				Resultado = mantenimiento.Resultado,
				Responsable = mantenimiento.Responsable
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<MantenimientoResumen?> ObtenerMantenimientoPorIdAsync(
		long idMantenimiento,
		CancellationToken cancellationToken = default)
	{
		if (idMantenimiento <= 0)
		{
			return null;
		}

		return await contexto.Mantenimientos
			.AsNoTracking()
			.Where(mantenimiento =>
				mantenimiento.IdMantenimiento == idMantenimiento
				&& mantenimiento.EstadoRegistro == EstadosRegistro.Activo)
			.Select(mantenimiento => new MantenimientoResumen
			{
				IdMantenimiento = mantenimiento.IdMantenimiento,
				IdActivo = mantenimiento.IdActivo,

				Activo = mantenimiento.Activo == null
					? string.Empty
					: mantenimiento.Activo.NombreActivo,

				IdProyecto = mantenimiento.IdProyecto,

				Proyecto = mantenimiento.Proyecto == null
					? null
					: mantenimiento.Proyecto.NombreProyecto,

				TipoMantenimiento = mantenimiento.TipoMantenimiento,

				IdEstadoMantenimiento =
					mantenimiento.IdEstadoMantenimiento,

				EstadoMantenimiento =
					mantenimiento.EstadoMantenimiento == null
						? string.Empty
						: mantenimiento.EstadoMantenimiento.Nombre,

				FechaProgramada = mantenimiento.FechaProgramada,
				FechaInicio = mantenimiento.FechaInicio,
				FechaFin = mantenimiento.FechaFin,
				Descripcion = mantenimiento.Descripcion,
				CostoEstimado = mantenimiento.CostoEstimado,
				CostoReal = mantenimiento.CostoReal,

				TiempoFueraServicioHoras =
					mantenimiento.TiempoFueraServicioHoras,

				Resultado = mantenimiento.Resultado,
				Responsable = mantenimiento.Responsable
			})
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<EstadoMantenimientoOpcion>>
		ObtenerEstadosMantenimientoAsync(
			CancellationToken cancellationToken = default)
	{
		return await contexto.EstadosMantenimiento
			.AsNoTracking()
			.Where(estado =>
				estado.EstadoRegistro == EstadosRegistro.Activo)
			.OrderBy(estado => estado.IdEstadoMantenimiento)
			.Select(estado => new EstadoMantenimientoOpcion
			{
				IdEstadoMantenimiento =
					estado.IdEstadoMantenimiento,

				Nombre = estado.Nombre
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<long> RegistrarMantenimientoAsync(
		SolicitudRegistroMantenimiento solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default)
	{
		var datos =
			DatosRegistroMantenimientoLimpios.DesdeSolicitud(solicitud);

		var idUsuario =
			LimpiarIdUsuarioObligatorio(idUsuarioActual);

		await ValidarUsuarioAsync(
			idUsuario,
			"No se encontró al usuario responsable del registro.",
			cancellationToken);

		var activoExiste = await contexto.Activos
			.AsNoTracking()
			.AnyAsync(
				activo =>
					activo.IdActivo == datos.IdActivo
					&& activo.EstadoRegistro == EstadosRegistro.Activo,
				cancellationToken);

		if (!activoExiste)
		{
			throw new InvalidOperationException(
				"No se encontró el activo seleccionado.");
		}

		if (datos.IdProyecto.HasValue)
		{
			var proyectoExiste = await contexto.Proyectos
				.AsNoTracking()
				.AnyAsync(
					proyecto =>
						proyecto.IdProyecto == datos.IdProyecto.Value
						&& proyecto.EstadoRegistro
							== EstadosRegistro.Activo,
					cancellationToken);

			if (!proyectoExiste)
			{
				throw new InvalidOperationException(
					"No se encontró el proyecto seleccionado.");
			}
		}

		var estadoExiste = await contexto.EstadosMantenimiento
			.AsNoTracking()
			.AnyAsync(
				estado =>
					estado.IdEstadoMantenimiento
						== datos.IdEstadoMantenimiento
					&& estado.EstadoRegistro == EstadosRegistro.Activo,
				cancellationToken);

		if (!estadoExiste)
		{
			throw new InvalidOperationException(
				"No se encontró el estado de mantenimiento seleccionado.");
		}

		ValidarTipoMantenimiento(datos.TipoMantenimiento);

		ValidarFechas(
			datos.FechaProgramada,
			datos.FechaInicio,
			datos.FechaFin);

		ValidarValoresMonetarios(
			datos.CostoEstimado,
			datos.CostoReal,
			datos.TiempoFueraServicioHoras);

		var fechaRegistro = DateTime.Now;

		var mantenimientoNuevo = new Mantenimiento
		{
			IdActivo = datos.IdActivo,
			IdProyecto = datos.IdProyecto,
			TipoMantenimiento = datos.TipoMantenimiento,

			IdEstadoMantenimiento =
				datos.IdEstadoMantenimiento,

			FechaProgramada = datos.FechaProgramada,
			FechaInicio = datos.FechaInicio,
			FechaFin = datos.FechaFin,
			Descripcion = datos.Descripcion,
			CostoEstimado = datos.CostoEstimado,
			CostoReal = datos.CostoReal,

			TiempoFueraServicioHoras =
				datos.TiempoFueraServicioHoras,

			Resultado = datos.Resultado,
			Responsable = datos.Responsable,
			FechaCreacion = fechaRegistro,
			CreadoPor = idUsuario,
			EstadoRegistro = EstadosRegistro.Activo
		};

		contexto.Mantenimientos.Add(mantenimientoNuevo);

		await contexto.SaveChangesAsync(cancellationToken);

		return mantenimientoNuevo.IdMantenimiento;
	}

	public async Task ActualizarMantenimientoAsync(
		SolicitudActualizarMantenimiento solicitud,
		string idUsuarioActual,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(solicitud);

		if (solicitud.IdMantenimiento <= 0)
		{
			throw new ArgumentException(
				"Seleccione un mantenimiento.");
		}

		if (solicitud.IdEstadoMantenimiento <= 0)
		{
			throw new ArgumentException(
				"Seleccione un estado de mantenimiento.");
		}

		var idUsuario =
			LimpiarIdUsuarioObligatorio(idUsuarioActual);

		await ValidarUsuarioAsync(
			idUsuario,
			"No se encontró al usuario responsable de la actualización.",
			cancellationToken);

		var descripcion = LimpiarTextoOpcional(
			solicitud.Descripcion,
			500,
			"La descripción");

		var resultado = LimpiarTextoOpcional(
			solicitud.Resultado,
			500,
			"El resultado");

		var responsable = LimpiarTextoOpcional(
			solicitud.Responsable,
			150,
			"El responsable");

		ValidarFechas(
			null,
			solicitud.FechaInicio,
			solicitud.FechaFin);

		ValidarValoresMonetarios(
			null,
			solicitud.CostoReal,
			solicitud.TiempoFueraServicioHoras);

		var mantenimiento = await contexto.Mantenimientos
			.Include(registro =>
				registro.EstadoMantenimiento)
			.FirstOrDefaultAsync(
				registro =>
					registro.IdMantenimiento
						== solicitud.IdMantenimiento
					&& registro.EstadoRegistro
						== EstadosRegistro.Activo,
				cancellationToken)
			?? throw new InvalidOperationException(
				"No se encontró el mantenimiento seleccionado.");

		var estadoNuevo = await contexto.EstadosMantenimiento
			.AsNoTracking()
			.FirstOrDefaultAsync(
				estado =>
					estado.IdEstadoMantenimiento
						== solicitud.IdEstadoMantenimiento
					&& estado.EstadoRegistro
						== EstadosRegistro.Activo,
				cancellationToken)
			?? throw new InvalidOperationException(
				"No se encontró el estado de mantenimiento seleccionado.");

		var valoresAnteriores = new
		{
			Estado = mantenimiento.EstadoMantenimiento?.Nombre,
			mantenimiento.FechaInicio,
			mantenimiento.FechaFin,
			mantenimiento.CostoReal,
			mantenimiento.TiempoFueraServicioHoras,
			mantenimiento.Resultado,
			mantenimiento.Descripcion,
			mantenimiento.Responsable
		};

		var noHayCambios =
			mantenimiento.IdEstadoMantenimiento
				== solicitud.IdEstadoMantenimiento
			&& mantenimiento.FechaInicio
				== solicitud.FechaInicio
			&& mantenimiento.FechaFin
				== solicitud.FechaFin
			&& mantenimiento.CostoReal
				== solicitud.CostoReal
			&& mantenimiento.TiempoFueraServicioHoras
				== solicitud.TiempoFueraServicioHoras
			&& string.Equals(
				mantenimiento.Resultado,
				resultado,
				StringComparison.Ordinal)
			&& string.Equals(
				mantenimiento.Descripcion,
				descripcion,
				StringComparison.Ordinal)
			&& string.Equals(
				mantenimiento.Responsable,
				responsable,
				StringComparison.Ordinal);

		if (noHayCambios)
		{
			throw new InvalidOperationException(
				"No se detectaron cambios en el mantenimiento.");
		}

		var fechaModificacion = DateTime.Now;

		mantenimiento.IdEstadoMantenimiento =
			estadoNuevo.IdEstadoMantenimiento;

		mantenimiento.FechaInicio =
			solicitud.FechaInicio;

		mantenimiento.FechaFin =
			solicitud.FechaFin;

		mantenimiento.CostoReal =
			solicitud.CostoReal;

		mantenimiento.TiempoFueraServicioHoras =
			solicitud.TiempoFueraServicioHoras;

		mantenimiento.Resultado =
			resultado;

		mantenimiento.Descripcion =
			descripcion;

		mantenimiento.Responsable =
			responsable;

		mantenimiento.FechaModificacion =
			fechaModificacion;

		mantenimiento.ModificadoPor =
			idUsuario;

		contexto.BitacoraAuditoria.Add(
			new BitacoraAuditoria
			{
				IdUsuario = idUsuario,
				FechaHora = fechaModificacion,

				Accion =
					"Actualización de mantenimiento",

				Entidad = "Mantenimiento",

				IdRegistro =
					mantenimiento.IdMantenimiento.ToString(),

				ValoresAnteriores =
					JsonSerializer.Serialize(valoresAnteriores),

				ValoresNuevos =
					JsonSerializer.Serialize(new
					{
						Estado = estadoNuevo.Nombre,
						mantenimiento.FechaInicio,
						mantenimiento.FechaFin,
						mantenimiento.CostoReal,
						mantenimiento.TiempoFueraServicioHoras,
						mantenimiento.Resultado,
						mantenimiento.Descripcion,
						mantenimiento.Responsable
					}),

				Observacion =
					$"Se actualizó el mantenimiento del activo con identificador {mantenimiento.IdActivo}."
			});

		await contexto.SaveChangesAsync(cancellationToken);
	}

	private async Task ValidarUsuarioAsync(
		string idUsuario,
		string mensajeError,
		CancellationToken cancellationToken)
	{
		var usuarioExiste = await contexto.Users
			.AsNoTracking()
			.AnyAsync(
				usuario => usuario.Id == idUsuario,
				cancellationToken);

		if (!usuarioExiste)
		{
			throw new InvalidOperationException(mensajeError);
		}
	}

	private static void ValidarTipoMantenimiento(
		string tipoMantenimiento)
	{
		var esPreventivo = string.Equals(
			tipoMantenimiento,
			TiposMantenimiento.Preventivo,
			StringComparison.OrdinalIgnoreCase);

		var esCorrectivo = string.Equals(
			tipoMantenimiento,
			TiposMantenimiento.Correctivo,
			StringComparison.OrdinalIgnoreCase);

		if (!esPreventivo && !esCorrectivo)
		{
			throw new ArgumentException(
				"El tipo de mantenimiento debe ser Preventivo o Correctivo.");
		}
	}

	private static void ValidarFechas(
		DateTime? fechaProgramada,
		DateTime? fechaInicio,
		DateTime? fechaFin)
	{
		if (fechaProgramada.HasValue
			&& fechaProgramada.Value == default)
		{
			throw new ArgumentException(
				"La fecha programada es obligatoria.");
		}

		if (fechaInicio.HasValue
			&& fechaFin.HasValue
			&& fechaFin.Value < fechaInicio.Value)
		{
			throw new ArgumentException(
				"La fecha de finalización no puede ser anterior a la fecha de inicio.");
		}
	}

	private static void ValidarValoresMonetarios(
		decimal? costoEstimado,
		decimal? costoReal,
		decimal? tiempoFueraServicioHoras)
	{
		if (costoEstimado.HasValue
			&& costoEstimado.Value < 0)
		{
			throw new ArgumentException(
				"El costo estimado no puede ser negativo.");
		}

		if (costoReal.HasValue
			&& costoReal.Value < 0)
		{
			throw new ArgumentException(
				"El costo real no puede ser negativo.");
		}

		if (tiempoFueraServicioHoras.HasValue
			&& tiempoFueraServicioHoras.Value < 0)
		{
			throw new ArgumentException(
				"El tiempo fuera de servicio no puede ser negativo.");
		}
	}

	private static string LimpiarIdUsuarioObligatorio(
		string idUsuario)
	{
		return string.IsNullOrWhiteSpace(idUsuario)
			? throw new ArgumentException(
				"No se pudo identificar al usuario responsable.")
			: idUsuario.Trim();
	}

	private static string? LimpiarTextoOpcional(
		string? valor,
		int longitudMaxima,
		string nombreCampo)
	{
		if (string.IsNullOrWhiteSpace(valor))
		{
			return null;
		}

		var limpio = valor.Trim();

		if (limpio.Length > longitudMaxima)
		{
			throw new ArgumentException(
				$"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
		}

		return limpio;
	}

	private sealed record DatosRegistroMantenimientoLimpios(
		long IdActivo,
		long? IdProyecto,
		string TipoMantenimiento,
		int IdEstadoMantenimiento,
		DateTime FechaProgramada,
		DateTime? FechaInicio,
		DateTime? FechaFin,
		string? Descripcion,
		decimal? CostoEstimado,
		decimal? CostoReal,
		decimal? TiempoFueraServicioHoras,
		string? Resultado,
		string? Responsable)
	{
		public static DatosRegistroMantenimientoLimpios DesdeSolicitud(
			SolicitudRegistroMantenimiento solicitud)
		{
			ArgumentNullException.ThrowIfNull(solicitud);

			if (solicitud.IdActivo <= 0)
			{
				throw new ArgumentException(
					"Seleccione un activo.");
			}

			if (solicitud.IdProyecto.HasValue
				&& solicitud.IdProyecto.Value <= 0)
			{
				throw new ArgumentException(
					"El proyecto seleccionado no es válido.");
			}

			if (string.IsNullOrWhiteSpace(
				solicitud.TipoMantenimiento))
			{
				throw new ArgumentException(
					"Seleccione el tipo de mantenimiento.");
			}

			if (solicitud.IdEstadoMantenimiento <= 0)
			{
				throw new ArgumentException(
					"Seleccione el estado del mantenimiento.");
			}

			if (solicitud.FechaProgramada == default)
			{
				throw new ArgumentException(
					"La fecha programada es obligatoria.");
			}

			return new DatosRegistroMantenimientoLimpios(
				solicitud.IdActivo,
				solicitud.IdProyecto,

				LimpiarObligatorio(
					solicitud.TipoMantenimiento,
					20,
					"El tipo de mantenimiento"),

				solicitud.IdEstadoMantenimiento,
				solicitud.FechaProgramada,
				solicitud.FechaInicio,
				solicitud.FechaFin,

				LimpiarOpcional(
					solicitud.Descripcion,
					500,
					"La descripción"),

				solicitud.CostoEstimado,
				solicitud.CostoReal,
				solicitud.TiempoFueraServicioHoras,

				LimpiarOpcional(
					solicitud.Resultado,
					500,
					"El resultado"),

				LimpiarOpcional(
					solicitud.Responsable,
					150,
					"El responsable"));
		}

		private static string LimpiarObligatorio(
			string valor,
			int longitudMaxima,
			string nombreCampo)
		{
			if (string.IsNullOrWhiteSpace(valor))
			{
				throw new ArgumentException(
					$"{nombreCampo} es obligatorio.");
			}

			var limpio = valor.Trim();

			if (limpio.Length > longitudMaxima)
			{
				throw new ArgumentException(
					$"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
			}

			return limpio;
		}

		private static string? LimpiarOpcional(
			string? valor,
			int longitudMaxima,
			string nombreCampo)
		{
			if (string.IsNullOrWhiteSpace(valor))
			{
				return null;
			}

			var limpio = valor.Trim();

			if (limpio.Length > longitudMaxima)
			{
				throw new ArgumentException(
					$"{nombreCampo} no puede superar {longitudMaxima} caracteres.");
			}

			return limpio;
		}
	}
}
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class IncidenciaPlanillaService(ApplicationDbContext contexto) : IIncidenciaPlanillaService
{
    private const string EstadoActivo = "Activo";
    private const string EstadoInactivo = "Inactivo";

    public async Task<IReadOnlyList<IncidenciaPlanillaResumen>> ObtenerIncidenciasAsync(
        long? idPeriodoPlanilla = null,
        long? idColaborador = null,
        int? idTipoIncidenciaPlanilla = null,
        string? estadoRegistro = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = contexto.IncidenciasPlanilla
            .AsNoTracking()
            .Include(incidencia => incidencia.PeriodoPlanilla)
            .Include(incidencia => incidencia.Colaborador)
            .Include(incidencia => incidencia.TipoIncidenciaPlanilla)
            .AsQueryable();

        if (idPeriodoPlanilla is > 0)
        {
            consulta = consulta.Where(incidencia => incidencia.IdPeriodoPlanilla == idPeriodoPlanilla.Value);
        }

        if (idColaborador is > 0)
        {
            consulta = consulta.Where(incidencia => incidencia.IdColaborador == idColaborador.Value);
        }

        if (idTipoIncidenciaPlanilla is > 0)
        {
            consulta = consulta.Where(incidencia => incidencia.IdTipoIncidenciaPlanilla == idTipoIncidenciaPlanilla.Value);
        }

        if (!string.IsNullOrWhiteSpace(estadoRegistro))
        {
            consulta = consulta.Where(incidencia => incidencia.EstadoRegistro == estadoRegistro);
        }

        var incidencias = await consulta
            .OrderByDescending(incidencia => incidencia.FechaIncidencia)
            .ThenBy(incidencia => incidencia.Colaborador!.PrimerApellido)
            .Select(incidencia => new
            {
                incidencia.IdIncidenciaPlanilla,
                incidencia.IdPeriodoPlanilla,
                CodigoPeriodo = incidencia.PeriodoPlanilla != null ? incidencia.PeriodoPlanilla.CodigoPeriodo : string.Empty,
                incidencia.IdColaborador,
                CodigoColaborador = incidencia.Colaborador != null ? incidencia.Colaborador.CodigoColaborador : string.Empty,
                Nombre = incidencia.Colaborador != null ? incidencia.Colaborador.Nombre : string.Empty,
                PrimerApellido = incidencia.Colaborador != null ? incidencia.Colaborador.PrimerApellido : string.Empty,
                SegundoApellido = incidencia.Colaborador != null ? incidencia.Colaborador.SegundoApellido : null,
                incidencia.IdTipoIncidenciaPlanilla,
                TipoIncidencia = incidencia.TipoIncidenciaPlanilla != null ? incidencia.TipoIncidenciaPlanilla.Nombre : string.Empty,
                Naturaleza = incidencia.TipoIncidenciaPlanilla != null ? incidencia.TipoIncidenciaPlanilla.Naturaleza : string.Empty,
                incidencia.FechaIncidencia,
                incidencia.Cantidad,
                incidencia.Monto,
                incidencia.Descripcion,
                incidencia.EstadoRegistro
            })
            .ToListAsync(cancellationToken);

        return incidencias
            .Select(incidencia => new IncidenciaPlanillaResumen
            {
                IdIncidenciaPlanilla = incidencia.IdIncidenciaPlanilla,
                IdPeriodoPlanilla = incidencia.IdPeriodoPlanilla,
                CodigoPeriodo = incidencia.CodigoPeriodo,
                IdColaborador = incidencia.IdColaborador,
                CodigoColaborador = incidencia.CodigoColaborador,
                NombreColaborador = ConstruirNombreCompleto(incidencia.Nombre, incidencia.PrimerApellido, incidencia.SegundoApellido),
                IdTipoIncidenciaPlanilla = incidencia.IdTipoIncidenciaPlanilla,
                TipoIncidencia = incidencia.TipoIncidencia,
                Naturaleza = incidencia.Naturaleza,
                FechaIncidencia = incidencia.FechaIncidencia,
                Cantidad = incidencia.Cantidad,
                Monto = incidencia.Monto,
                Descripcion = incidencia.Descripcion,
                EstadoRegistro = incidencia.EstadoRegistro
            })
            .ToList();
    }

    public async Task<IncidenciaPlanillaDetalle?> ObtenerIncidenciaPorIdAsync(
        long idIncidenciaPlanilla,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await contexto.IncidenciasPlanilla
            .AsNoTracking()
            .Include(registro => registro.PeriodoPlanilla)
            .Include(registro => registro.Colaborador)
            .Include(registro => registro.TipoIncidenciaPlanilla)
            .Where(registro => registro.IdIncidenciaPlanilla == idIncidenciaPlanilla)
            .Select(registro => new
            {
                registro.IdIncidenciaPlanilla,
                registro.IdPeriodoPlanilla,
                CodigoPeriodo = registro.PeriodoPlanilla != null ? registro.PeriodoPlanilla.CodigoPeriodo : string.Empty,
                FechaInicioPeriodo = registro.PeriodoPlanilla != null ? registro.PeriodoPlanilla.FechaInicio : default,
                FechaFinPeriodo = registro.PeriodoPlanilla != null ? registro.PeriodoPlanilla.FechaFin : default,
                registro.IdColaborador,
                CodigoColaborador = registro.Colaborador != null ? registro.Colaborador.CodigoColaborador : string.Empty,
                Nombre = registro.Colaborador != null ? registro.Colaborador.Nombre : string.Empty,
                PrimerApellido = registro.Colaborador != null ? registro.Colaborador.PrimerApellido : string.Empty,
                SegundoApellido = registro.Colaborador != null ? registro.Colaborador.SegundoApellido : null,
                registro.IdTipoIncidenciaPlanilla,
                TipoIncidencia = registro.TipoIncidenciaPlanilla != null ? registro.TipoIncidenciaPlanilla.Nombre : string.Empty,
                Naturaleza = registro.TipoIncidenciaPlanilla != null ? registro.TipoIncidenciaPlanilla.Naturaleza : string.Empty,
                registro.FechaIncidencia,
                registro.Cantidad,
                registro.Monto,
                registro.Descripcion,
                registro.FechaRegistro,
                registro.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (incidencia is null)
        {
            return null;
        }

        return new IncidenciaPlanillaDetalle
        {
            IdIncidenciaPlanilla = incidencia.IdIncidenciaPlanilla,
            IdPeriodoPlanilla = incidencia.IdPeriodoPlanilla,
            CodigoPeriodo = incidencia.CodigoPeriodo,
            FechaInicioPeriodo = incidencia.FechaInicioPeriodo,
            FechaFinPeriodo = incidencia.FechaFinPeriodo,
            IdColaborador = incidencia.IdColaborador,
            CodigoColaborador = incidencia.CodigoColaborador,
            NombreColaborador = ConstruirNombreCompleto(incidencia.Nombre, incidencia.PrimerApellido, incidencia.SegundoApellido),
            IdTipoIncidenciaPlanilla = incidencia.IdTipoIncidenciaPlanilla,
            TipoIncidencia = incidencia.TipoIncidencia,
            Naturaleza = incidencia.Naturaleza,
            FechaIncidencia = incidencia.FechaIncidencia,
            Cantidad = incidencia.Cantidad,
            Monto = incidencia.Monto,
            Descripcion = incidencia.Descripcion,
            FechaRegistro = incidencia.FechaRegistro,
            EstadoRegistro = incidencia.EstadoRegistro
        };
    }

    public async Task<IReadOnlyList<PeriodoPlanillaOpcion>> ObtenerPeriodosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Include(periodo => periodo.EstadoPlanilla)
            .Where(periodo => periodo.EstadoRegistro == EstadoActivo)
            .OrderByDescending(periodo => periodo.FechaInicio)
            .Select(periodo => new PeriodoPlanillaOpcion(
                periodo.IdPeriodoPlanilla,
                periodo.CodigoPeriodo,
                periodo.Nombre,
                periodo.FechaInicio,
                periodo.FechaFin,
                periodo.EstadoPlanilla != null ? periodo.EstadoPlanilla.Nombre : string.Empty))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ColaboradorIncidenciaOpcion>> ObtenerColaboradoresAsync(CancellationToken cancellationToken = default)
    {
        var colaboradores = await contexto.Colaboradores
            .AsNoTracking()
            .Where(colaborador => colaborador.EstadoRegistro == EstadoActivo)
            .OrderBy(colaborador => colaborador.PrimerApellido)
            .ThenBy(colaborador => colaborador.Nombre)
            .Select(colaborador => new
            {
                colaborador.IdColaborador,
                colaborador.CodigoColaborador,
                colaborador.Nombre,
                colaborador.PrimerApellido,
                colaborador.SegundoApellido
            })
            .ToListAsync(cancellationToken);

        return colaboradores
            .Select(colaborador => new ColaboradorIncidenciaOpcion(
                colaborador.IdColaborador,
                colaborador.CodigoColaborador,
                ConstruirNombreCompleto(colaborador.Nombre, colaborador.PrimerApellido, colaborador.SegundoApellido)))
            .ToList();
    }

    public async Task<IReadOnlyList<TipoIncidenciaPlanillaOpcion>> ObtenerTiposIncidenciaAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.TiposIncidenciaPlanilla
            .AsNoTracking()
            .Where(tipo => tipo.EstadoRegistro == EstadoActivo)
            .OrderBy(tipo => tipo.Nombre)
            .Select(tipo => new TipoIncidenciaPlanillaOpcion(
                tipo.IdTipoIncidenciaPlanilla,
                tipo.Nombre,
                tipo.Naturaleza,
                tipo.Descripcion))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> RegistrarIncidenciaAsync(
        SolicitudIncidenciaPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var datos = await ValidarSolicitudAsync(solicitud, cancellationToken);

        var incidencia = new IncidenciaPlanilla
        {
            IdPeriodoPlanilla = datos.IdPeriodoPlanilla,
            IdColaborador = datos.IdColaborador,
            IdTipoIncidenciaPlanilla = datos.IdTipoIncidenciaPlanilla,
            FechaIncidencia = datos.FechaIncidencia,
            Cantidad = datos.Cantidad,
            Monto = datos.Monto,
            Descripcion = datos.Descripcion,
            RegistradoPor = idUsuarioActual,
            FechaRegistro = DateTime.Now,
            EstadoRegistro = EstadoActivo
        };

        contexto.IncidenciasPlanilla.Add(incidencia);
        await contexto.SaveChangesAsync(cancellationToken);

        return incidencia.IdIncidenciaPlanilla;
    }

    public async Task ActualizarIncidenciaAsync(
        long idIncidenciaPlanilla,
        SolicitudIncidenciaPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await contexto.IncidenciasPlanilla
            .FirstOrDefaultAsync(
                registro => registro.IdIncidenciaPlanilla == idIncidenciaPlanilla
                    && registro.EstadoRegistro == EstadoActivo,
                cancellationToken);

        if (incidencia is null)
        {
            throw new InvalidOperationException("No se encontro la incidencia solicitada o ya esta inactiva.");
        }

        var datos = await ValidarSolicitudAsync(solicitud, cancellationToken);

        incidencia.IdPeriodoPlanilla = datos.IdPeriodoPlanilla;
        incidencia.IdColaborador = datos.IdColaborador;
        incidencia.IdTipoIncidenciaPlanilla = datos.IdTipoIncidenciaPlanilla;
        incidencia.FechaIncidencia = datos.FechaIncidencia;
        incidencia.Cantidad = datos.Cantidad;
        incidencia.Monto = datos.Monto;
        incidencia.Descripcion = datos.Descripcion;
        incidencia.RegistradoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarIncidenciaAsync(
        long idIncidenciaPlanilla,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await contexto.IncidenciasPlanilla
            .FirstOrDefaultAsync(
                registro => registro.IdIncidenciaPlanilla == idIncidenciaPlanilla
                    && registro.EstadoRegistro == EstadoActivo,
                cancellationToken);

        if (incidencia is null)
        {
            throw new InvalidOperationException("No se encontro la incidencia solicitada o ya esta inactiva.");
        }

        incidencia.EstadoRegistro = EstadoInactivo;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<DatosIncidenciaLimpios> ValidarSolicitudAsync(
        SolicitudIncidenciaPlanilla solicitud,
        CancellationToken cancellationToken)
    {
        var datos = DatosIncidenciaLimpios.DesdeSolicitud(solicitud);

        var periodo = await contexto.PeriodosPlanilla
            .AsNoTracking()
            .FirstOrDefaultAsync(
                periodo => periodo.IdPeriodoPlanilla == datos.IdPeriodoPlanilla
                    && periodo.EstadoRegistro == EstadoActivo,
                cancellationToken);

        if (periodo is null)
        {
            throw new ArgumentException("El periodo seleccionado no esta disponible.");
        }

        if (datos.FechaIncidencia < periodo.FechaInicio.Date || datos.FechaIncidencia > periodo.FechaFin.Date)
        {
            throw new ArgumentException("La fecha de incidencia debe estar dentro del rango del periodo seleccionado.");
        }

        var colaboradorExiste = await contexto.Colaboradores
            .AsNoTracking()
            .AnyAsync(
                colaborador => colaborador.IdColaborador == datos.IdColaborador
                    && colaborador.EstadoRegistro == EstadoActivo,
                cancellationToken);

        if (!colaboradorExiste)
        {
            throw new ArgumentException("El colaborador seleccionado no esta disponible.");
        }

        var tipoExiste = await contexto.TiposIncidenciaPlanilla
            .AsNoTracking()
            .AnyAsync(
                tipo => tipo.IdTipoIncidenciaPlanilla == datos.IdTipoIncidenciaPlanilla
                    && tipo.EstadoRegistro == EstadoActivo,
                cancellationToken);

        if (!tipoExiste)
        {
            throw new ArgumentException("El tipo de incidencia seleccionado no esta disponible.");
        }

        return datos;
    }

    private static string ConstruirNombreCompleto(string nombre, string primerApellido, string? segundoApellido)
    {
        return string.IsNullOrWhiteSpace(segundoApellido)
            ? $"{nombre} {primerApellido}"
            : $"{nombre} {primerApellido} {segundoApellido}";
    }

    private sealed record DatosIncidenciaLimpios(
        long IdPeriodoPlanilla,
        long IdColaborador,
        int IdTipoIncidenciaPlanilla,
        DateTime FechaIncidencia,
        decimal? Cantidad,
        decimal Monto,
        string? Descripcion)
    {
        public static DatosIncidenciaLimpios DesdeSolicitud(SolicitudIncidenciaPlanilla solicitud)
        {
            if (solicitud.IdPeriodoPlanilla <= 0)
            {
                throw new ArgumentException("Seleccione un periodo de planilla.");
            }

            if (solicitud.IdColaborador <= 0)
            {
                throw new ArgumentException("Seleccione un colaborador.");
            }

            if (solicitud.IdTipoIncidenciaPlanilla <= 0)
            {
                throw new ArgumentException("Seleccione un tipo de incidencia.");
            }

            if (solicitud.FechaIncidencia is null)
            {
                throw new ArgumentException("La fecha de incidencia es obligatoria.");
            }

            if (solicitud.Cantidad is < 0)
            {
                throw new ArgumentException("La cantidad no puede ser negativa.");
            }

            if (solicitud.Monto < 0)
            {
                throw new ArgumentException("El monto no puede ser negativo.");
            }

            return new DatosIncidenciaLimpios(
                solicitud.IdPeriodoPlanilla,
                solicitud.IdColaborador,
                solicitud.IdTipoIncidenciaPlanilla,
                solicitud.FechaIncidencia.Value.Date,
                solicitud.Cantidad,
                solicitud.Monto,
                LimpiarOpcional(solicitud.Descripcion));
        }

        private static string? LimpiarOpcional(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}

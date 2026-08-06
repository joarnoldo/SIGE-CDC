using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Activos;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Activos;

public sealed class ReporteUtilizacionCostosService(
    ApplicationDbContext contexto)
    : IReporteUtilizacionCostosService
{
    private const string ReportePorProyecto = "Proyecto";
    private const string ReportePorActivo = "Activo";

    public async Task<ReporteUtilizacionCostosResumen>
        GenerarPorProyectoAsync(
            long idProyecto,
            FiltroPeriodoReporte filtro,
            CancellationToken cancellationToken = default)
    {
        if (idProyecto <= 0)
        {
            throw new ArgumentException(
                "Seleccione un proyecto para generar el reporte.");
        }

        var periodo = ValidarPeriodo(filtro);
        var encabezado = await contexto.Proyectos
            .AsNoTracking()
            .Where(proyecto =>
                proyecto.IdProyecto == idProyecto
                && proyecto.EstadoRegistro == EstadosRegistro.Activo)
            .Select(proyecto =>
                new EncabezadoReporteUtilizacionCostos
                {
                    TipoReporte = ReportePorProyecto,
                    IdReferencia = proyecto.IdProyecto,
                    CodigoReferencia = proyecto.CodigoProyecto,
                    NombreReferencia = proyecto.NombreProyecto
                })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el proyecto seleccionado.");

        return await GenerarAsync(
            encabezado,
            idProyecto,
            idActivo: null,
            periodo,
            cancellationToken);
    }

    public async Task<ReporteUtilizacionCostosResumen>
        GenerarPorActivoAsync(
            long idActivo,
            FiltroPeriodoReporte filtro,
            CancellationToken cancellationToken = default)
    {
        if (idActivo <= 0)
        {
            throw new ArgumentException(
                "Seleccione un activo para generar el reporte.");
        }

        var periodo = ValidarPeriodo(filtro);
        var encabezado = await contexto.Activos
            .AsNoTracking()
            .Where(activo =>
                activo.IdActivo == idActivo
                && activo.EstadoRegistro == EstadosRegistro.Activo)
            .Select(activo =>
                new EncabezadoReporteUtilizacionCostos
                {
                    TipoReporte = ReportePorActivo,
                    IdReferencia = activo.IdActivo,
                    CodigoReferencia = activo.CodigoActivo,
                    NombreReferencia = activo.NombreActivo
                })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el activo seleccionado.");

        return await GenerarAsync(
            encabezado,
            idProyecto: null,
            idActivo,
            periodo,
            cancellationToken);
    }

    private async Task<ReporteUtilizacionCostosResumen> GenerarAsync(
        EncabezadoReporteUtilizacionCostos encabezado,
        long? idProyecto,
        long? idActivo,
        PeriodoNormalizado periodo,
        CancellationToken cancellationToken)
    {
        IQueryable<RegistroUsoActivo> consultaUso =
            contexto.RegistrosUsoActivo
                .AsNoTracking()
                .Where(registro =>
                    registro.EstadoRegistro == EstadosRegistro.Activo);

        consultaUso = idProyecto.HasValue
            ? consultaUso.Where(registro =>
                registro.IdProyecto == idProyecto.Value)
            : consultaUso.Where(registro =>
                registro.IdActivo == idActivo!.Value);

        if (periodo.FechaDesde.HasValue)
        {
            consultaUso = consultaUso.Where(registro =>
                registro.FechaRegistro >= periodo.FechaDesde.Value);
        }

        if (periodo.FechaHasta.HasValue)
        {
            consultaUso = consultaUso.Where(registro =>
                registro.FechaRegistro <= periodo.FechaHasta.Value);
        }

        var registrosUso = await consultaUso
            .OrderByDescending(registro => registro.FechaRegistro)
            .ThenByDescending(registro =>
                registro.IdRegistroUsoActivo)
            .Select(registro => new RegistroUsoReporteResumen
            {
                IdRegistroUsoActivo =
                    registro.IdRegistroUsoActivo,
                IdActivo = registro.IdActivo,
                CodigoActivo = registro.Activo == null
                    ? string.Empty
                    : registro.Activo.CodigoActivo,
                Activo = registro.Activo == null
                    ? string.Empty
                    : registro.Activo.NombreActivo,
                IdProyecto = registro.IdProyecto,
                CodigoProyecto = registro.Proyecto == null
                    ? null
                    : registro.Proyecto.CodigoProyecto,
                Proyecto = registro.Proyecto == null
                    ? null
                    : registro.Proyecto.NombreProyecto,
                FechaRegistro = registro.FechaRegistro,
                TipoMedicion = registro.Activo == null
                    || registro.Activo.TipoMedicionUso == null
                        ? null
                        : registro.Activo.TipoMedicionUso.Nombre,
                LecturaAnterior = registro.LecturaAnterior,
                LecturaNueva = registro.LecturaNueva,
                CantidadUso = registro.CantidadUso
            })
            .ToListAsync(cancellationToken);

        IQueryable<Mantenimiento> consultaMantenimientos =
            contexto.Mantenimientos
                .AsNoTracking()
                .Where(mantenimiento =>
                    mantenimiento.EstadoRegistro
                        == EstadosRegistro.Activo);

        consultaMantenimientos = idProyecto.HasValue
            ? consultaMantenimientos.Where(mantenimiento =>
                mantenimiento.IdProyecto == idProyecto.Value)
            : consultaMantenimientos.Where(mantenimiento =>
                mantenimiento.IdActivo == idActivo!.Value);

        var detalleMantenimientos = consultaMantenimientos
            .Select(mantenimiento =>
                new MantenimientoCostoReporteResumen
                {
                    IdMantenimiento =
                        mantenimiento.IdMantenimiento,
                    IdActivo = mantenimiento.IdActivo,
                    CodigoActivo = mantenimiento.Activo == null
                        ? string.Empty
                        : mantenimiento.Activo.CodigoActivo,
                    Activo = mantenimiento.Activo == null
                        ? string.Empty
                        : mantenimiento.Activo.NombreActivo,
                    IdProyecto = mantenimiento.IdProyecto,
                    CodigoProyecto =
                        mantenimiento.Proyecto == null
                            ? null
                            : mantenimiento.Proyecto.CodigoProyecto,
                    Proyecto = mantenimiento.Proyecto == null
                        ? null
                        : mantenimiento.Proyecto.NombreProyecto,
                    TipoMantenimiento =
                        mantenimiento.TipoMantenimiento,
                    EstadoMantenimiento =
                        mantenimiento.EstadoMantenimiento == null
                            ? string.Empty
                            : mantenimiento.EstadoMantenimiento.Nombre,
                    FechaEfectiva =
                        mantenimiento.EstadoMantenimiento != null
                        && mantenimiento.EstadoMantenimiento.Nombre
                            == EstadosMantenimiento.Finalizado
                            ? mantenimiento.FechaFin
                                ?? mantenimiento.FechaInicio
                                ?? mantenimiento.FechaProgramada
                            : mantenimiento.EstadoMantenimiento != null
                            && mantenimiento.EstadoMantenimiento.Nombre
                                == EstadosMantenimiento.EnProceso
                                ? mantenimiento.FechaInicio
                                    ?? mantenimiento.FechaFin
                                    ?? mantenimiento.FechaProgramada
                                : mantenimiento.FechaProgramada,
                    FechaProgramada =
                        mantenimiento.FechaProgramada,
                    FechaInicio = mantenimiento.FechaInicio,
                    FechaFin = mantenimiento.FechaFin,
                    Descripcion = mantenimiento.Descripcion,
                    CostoEstimado = mantenimiento.CostoEstimado,
                    CostoReal = mantenimiento.CostoReal
                });

        if (periodo.FechaDesde.HasValue)
        {
            detalleMantenimientos =
                detalleMantenimientos.Where(mantenimiento =>
                    mantenimiento.FechaEfectiva
                        >= periodo.FechaDesde.Value);
        }

        if (periodo.FechaHastaExclusiva.HasValue)
        {
            detalleMantenimientos =
                detalleMantenimientos.Where(mantenimiento =>
                    mantenimiento.FechaEfectiva
                        < periodo.FechaHastaExclusiva.Value);
        }

        var mantenimientos = await detalleMantenimientos
            .OrderByDescending(mantenimiento =>
                mantenimiento.FechaEfectiva)
            .ThenByDescending(mantenimiento =>
                mantenimiento.IdMantenimiento)
            .ToListAsync(cancellationToken);

        return CrearReporte(
            encabezado,
            periodo,
            registrosUso,
            mantenimientos);
    }

    private static ReporteUtilizacionCostosResumen CrearReporte(
        EncabezadoReporteUtilizacionCostos encabezado,
        PeriodoNormalizado periodo,
        IReadOnlyList<RegistroUsoReporteResumen> registrosUso,
        IReadOnlyList<MantenimientoCostoReporteResumen> mantenimientos)
    {
        var totalesUtilizacion = registrosUso
            .GroupBy(registro =>
                string.IsNullOrWhiteSpace(registro.TipoMedicion)
                    ? "Sin tipo de medición"
                    : registro.TipoMedicion)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new UtilizacionPorMedicionResumen
            {
                TipoMedicion = grupo.Key,
                CantidadUsoTotal = grupo.Sum(registro =>
                    registro.CantidadUso ?? 0m),
                CantidadRegistros = grupo.Count()
            })
            .ToList();

        return new ReporteUtilizacionCostosResumen
        {
            Encabezado = encabezado,
            FechaDesde = periodo.FechaDesde,
            FechaHasta = periodo.FechaHasta,
            CostoEstimadoTotal = mantenimientos.Sum(
                mantenimiento =>
                    mantenimiento.CostoEstimado ?? 0m),
            CostoRealTotal = mantenimientos.Sum(
                mantenimiento =>
                    mantenimiento.CostoReal ?? 0m),
            TotalesUtilizacion = totalesUtilizacion,
            RegistrosUso = registrosUso,
            Mantenimientos = mantenimientos
        };
    }

    private static PeriodoNormalizado ValidarPeriodo(
        FiltroPeriodoReporte filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var fechaDesde = filtro.FechaDesde?.Date;
        var fechaHasta = filtro.FechaHasta?.Date;

        if (fechaDesde.HasValue
            && fechaHasta.HasValue
            && fechaDesde.Value > fechaHasta.Value)
        {
            throw new ArgumentException(
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        DateTime? fechaHastaExclusiva = null;
        if (fechaHasta.HasValue
            && fechaHasta.Value < DateTime.MaxValue.Date)
        {
            fechaHastaExclusiva =
                fechaHasta.Value.AddDays(1);
        }

        return new PeriodoNormalizado(
            fechaDesde,
            fechaHasta,
            fechaHastaExclusiva);
    }

    private sealed record PeriodoNormalizado(
        DateTime? FechaDesde,
        DateTime? FechaHasta,
        DateTime? FechaHastaExclusiva);
}

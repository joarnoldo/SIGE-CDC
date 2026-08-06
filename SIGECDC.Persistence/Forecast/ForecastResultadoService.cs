using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastResultadoService(ApplicationDbContext contexto)
    : IForecastResultadoService
{
    public async Task<ConsultaResultadosForecast?> ObtenerResultadosAsync(
        long idForecastEscenario,
        FiltroResultadosForecast filtro,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario, nameof(idForecastEscenario));
        ArgumentNullException.ThrowIfNull(filtro);
        Validator.ValidateObject(filtro, new ValidationContext(filtro), true);

        var escenario = await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new EscenarioConsulta
            {
                IdForecastEscenario = item.IdForecastEscenario,
                Nombre = item.Nombre,
                EstadoEscenario = item.EstadoEscenario,
                FechaCalculo = item.FechaCalculo,
                MontoProyectadoTotal = item.MontoProyectadoTotal
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (escenario is null)
        {
            return null;
        }

        var periodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .OrderBy(item => item.NumeroOrden)
            .ThenBy(item => item.IdForecastPeriodo)
            .Select(item => new PeriodoResultadoForecastOpcion(
                item.IdForecastPeriodo,
                item.NumeroOrden,
                item.TipoPeriodo,
                item.FechaInicio,
                item.FechaFin))
            .ToListAsync(cancellationToken);

        if (filtro.IdForecastPeriodo.HasValue
            && periodos.All(item => item.IdForecastPeriodo != filtro.IdForecastPeriodo.Value))
        {
            throw new ValidationException(
                "El período seleccionado no pertenece al escenario forecast.");
        }

        var consultaDetalles = CrearConsultaDetalles(
            idForecastEscenario,
            filtro.IdForecastPeriodo);
        var detallesAgrupados = await AgruparAsync(
            consultaDetalles,
            filtro.Dimension,
            cancellationToken);

        if (filtro.Dimension == DimensionesResultadoForecast.Periodo
            && detallesAgrupados.Count > 0)
        {
            AgregarPeriodosSinDetalle(
                detallesAgrupados,
                periodos.Where(item => !filtro.IdForecastPeriodo.HasValue
                    || item.IdForecastPeriodo == filtro.IdForecastPeriodo.Value));
        }

        var filas = CrearFilas(filtro.Dimension, detallesAgrupados);
        var totalConsulta = filas.Sum(item => item.SalarioBrutoProyectado);
        var deducciones = filas.Sum(item => item.DeduccionesProyectadas);
        var neto = filas.Sum(item => item.SalarioNetoProyectado);

        return new ConsultaResultadosForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            NombreEscenario = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            FechaCalculo = escenario.FechaCalculo,
            Dimension = filtro.Dimension,
            IdForecastPeriodo = filtro.IdForecastPeriodo,
            MontoProyectadoTotalEscenario = escenario.MontoProyectadoTotal,
            MontoProyectadoTotalConsulta = totalConsulta,
            DeduccionesTotalConsulta = deducciones,
            SalarioNetoTotalConsulta = neto,
            EsConsistenteConEscenario = filtro.IdForecastPeriodo.HasValue
                ? null
                : totalConsulta == escenario.MontoProyectadoTotal,
            Periodos = periodos,
            Filas = filas
        };
    }

    private IQueryable<DetalleResultadoConsulta> CrearConsultaDetalles(
        long idForecastEscenario,
        long? idForecastPeriodo) => ForecastResultadoConsultaInterna.Crear(
            contexto,
            [idForecastEscenario],
            idForecastPeriodo);

    private static Task<List<DetalleAgrupadoConsulta>> AgruparAsync(
        IQueryable<DetalleResultadoConsulta> consulta,
        string dimension,
        CancellationToken cancellationToken) => dimension switch
    {
        DimensionesResultadoForecast.Colaborador => AgruparPorColaboradorAsync(
            consulta,
            cancellationToken),
        DimensionesResultadoForecast.Departamento => AgruparPorDepartamentoAsync(
            consulta,
            cancellationToken),
        DimensionesResultadoForecast.Proyecto => AgruparPorProyectoAsync(
            consulta,
            cancellationToken),
        DimensionesResultadoForecast.Periodo => AgruparPorPeriodoAsync(
            consulta,
            cancellationToken),
        _ => throw new ValidationException("La dimensión de resultados no es válida.")
    };

    private static Task<List<DetalleAgrupadoConsulta>> AgruparPorColaboradorAsync(
        IQueryable<DetalleResultadoConsulta> consulta,
        CancellationToken cancellationToken) => consulta
        .GroupBy(item => new
        {
            item.IdForecastParticipante,
            item.CodigoParticipante,
            item.EtiquetaParticipante,
            item.TipoParticipante,
            item.NombreDepartamento,
            item.NombrePuesto,
            item.Concepto
        })
        .Select(grupo => new DetalleAgrupadoConsulta
        {
            IdAgrupacion = grupo.Key.IdForecastParticipante,
            Codigo = grupo.Key.CodigoParticipante,
            Etiqueta = grupo.Key.EtiquetaParticipante,
            Detalle = grupo.Key.NombreDepartamento + " · " + grupo.Key.NombrePuesto,
            TipoParticipante = grupo.Key.TipoParticipante,
            CantidadParticipantes = 1,
            Concepto = grupo.Key.Concepto,
            MontoBase = grupo.Sum(item => item.MontoBase),
            MontoAjuste = grupo.Sum(item => item.MontoAjuste),
            MontoProyectado = grupo.Sum(item => item.MontoProyectado)
        })
        .ToListAsync(cancellationToken);

    private static Task<List<DetalleAgrupadoConsulta>> AgruparPorDepartamentoAsync(
        IQueryable<DetalleResultadoConsulta> consulta,
        CancellationToken cancellationToken) => consulta
        .GroupBy(item => new
        {
            item.IdDepartamento,
            item.NombreDepartamento,
            item.Concepto
        })
        .Select(grupo => new DetalleAgrupadoConsulta
        {
            IdAgrupacion = grupo.Key.IdDepartamento,
            Etiqueta = grupo.Key.NombreDepartamento,
            CantidadParticipantes = grupo
                .Select(item => item.IdForecastParticipante)
                .Distinct()
                .Count(),
            Concepto = grupo.Key.Concepto,
            MontoBase = grupo.Sum(item => item.MontoBase),
            MontoAjuste = grupo.Sum(item => item.MontoAjuste),
            MontoProyectado = grupo.Sum(item => item.MontoProyectado)
        })
        .ToListAsync(cancellationToken);

    private static Task<List<DetalleAgrupadoConsulta>> AgruparPorProyectoAsync(
        IQueryable<DetalleResultadoConsulta> consulta,
        CancellationToken cancellationToken) => consulta
        .GroupBy(item => new
        {
            item.IdProyecto,
            item.CodigoProyecto,
            item.NombreProyecto,
            item.Concepto
        })
        .Select(grupo => new DetalleAgrupadoConsulta
        {
            IdAgrupacion = grupo.Key.IdProyecto,
            Codigo = grupo.Key.CodigoProyecto,
            Etiqueta = grupo.Key.NombreProyecto,
            CantidadParticipantes = grupo
                .Select(item => item.IdForecastParticipante)
                .Distinct()
                .Count(),
            Concepto = grupo.Key.Concepto,
            MontoBase = grupo.Sum(item => item.MontoBase),
            MontoAjuste = grupo.Sum(item => item.MontoAjuste),
            MontoProyectado = grupo.Sum(item => item.MontoProyectado)
        })
        .ToListAsync(cancellationToken);

    private static Task<List<DetalleAgrupadoConsulta>> AgruparPorPeriodoAsync(
        IQueryable<DetalleResultadoConsulta> consulta,
        CancellationToken cancellationToken) => consulta
        .GroupBy(item => new
        {
            item.IdForecastPeriodo,
            item.NumeroOrdenPeriodo,
            item.TipoPeriodo,
            item.FechaInicioPeriodo,
            item.FechaFinPeriodo,
            item.Concepto
        })
        .Select(grupo => new DetalleAgrupadoConsulta
        {
            IdAgrupacion = grupo.Key.IdForecastPeriodo,
            Orden = grupo.Key.NumeroOrdenPeriodo,
            Etiqueta = grupo.Key.TipoPeriodo,
            FechaInicio = grupo.Key.FechaInicioPeriodo,
            FechaFin = grupo.Key.FechaFinPeriodo,
            CantidadParticipantes = grupo
                .Select(item => item.IdForecastParticipante)
                .Distinct()
                .Count(),
            Concepto = grupo.Key.Concepto,
            MontoBase = grupo.Sum(item => item.MontoBase),
            MontoAjuste = grupo.Sum(item => item.MontoAjuste),
            MontoProyectado = grupo.Sum(item => item.MontoProyectado)
        })
        .ToListAsync(cancellationToken);

    private static void AgregarPeriodosSinDetalle(
        ICollection<DetalleAgrupadoConsulta> detalles,
        IEnumerable<PeriodoResultadoForecastOpcion> periodos)
    {
        var idsExistentes = detalles.Select(item => item.IdAgrupacion).ToHashSet();
        foreach (var periodo in periodos.Where(item => !idsExistentes.Contains(item.IdForecastPeriodo)))
        {
            foreach (var concepto in CatalogoConceptosResultadoForecast.Todos)
            {
                detalles.Add(new DetalleAgrupadoConsulta
                {
                    IdAgrupacion = periodo.IdForecastPeriodo,
                    Orden = periodo.NumeroOrden,
                    Etiqueta = periodo.TipoPeriodo,
                    FechaInicio = periodo.FechaInicio,
                    FechaFin = periodo.FechaFin,
                    Concepto = concepto.Codigo
                });
            }
        }
    }

    private static IReadOnlyList<FilaResultadoForecast> CrearFilas(
        string dimension,
        IReadOnlyCollection<DetalleAgrupadoConsulta> detalles) => detalles
        .GroupBy(item => item.IdAgrupacion)
        .Select(grupo =>
        {
            var encabezado = grupo.First();
            var conceptosPersistidos = grupo.ToDictionary(
                item => item.Concepto,
                StringComparer.Ordinal);
            var conceptos = CatalogoConceptosResultadoForecast.Todos
                .Select(definicion => conceptosPersistidos.TryGetValue(
                    definicion.Codigo,
                    out var persistido)
                    ? new ConceptoResultadoForecast(
                        definicion.Codigo,
                        definicion.Nombre,
                        persistido.MontoBase,
                        persistido.MontoAjuste,
                        persistido.MontoProyectado)
                    : new ConceptoResultadoForecast(
                        definicion.Codigo,
                        definicion.Nombre,
                        0m,
                        0m,
                        0m))
                .ToList();
            var bruto = conceptos.Single(item => item.Codigo == ConceptosForecast.SalarioBruto);
            var deducciones = conceptos.Single(item => item.Codigo == ConceptosForecast.Deducciones);
            var neto = conceptos.Single(item => item.Codigo == ConceptosForecast.SalarioNeto);

            return new FilaResultadoForecast
            {
                Clave = $"{dimension}:{encabezado.IdAgrupacion}",
                Codigo = encabezado.Codigo,
                Etiqueta = encabezado.Etiqueta,
                Detalle = dimension == DimensionesResultadoForecast.Periodo
                    ? $"{encabezado.FechaInicio:dd/MM/yyyy} – {encabezado.FechaFin:dd/MM/yyyy}"
                    : encabezado.Detalle,
                TipoParticipante = encabezado.TipoParticipante,
                CantidadParticipantes = encabezado.CantidadParticipantes,
                SalarioBrutoProyectado = bruto.MontoProyectado,
                DeduccionesProyectadas = deducciones.MontoProyectado,
                SalarioNetoProyectado = neto.MontoProyectado,
                Conceptos = conceptos
            };
        })
        .OrderBy(item => dimension == DimensionesResultadoForecast.Periodo
            ? detalles.First(detalle => $"{dimension}:{detalle.IdAgrupacion}" == item.Clave).Orden
            : int.MaxValue)
        .ThenBy(item => item.Etiqueta)
        .ThenBy(item => item.Codigo)
        .ToList();

    private static void ValidarIdentificador(long identificador, string nombreParametro)
    {
        if (identificador <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                "El identificador solicitado no es válido.");
        }
    }

    private sealed class EscenarioConsulta
    {
        public long IdForecastEscenario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string EstadoEscenario { get; set; } = string.Empty;
        public DateTime? FechaCalculo { get; set; }
        public decimal MontoProyectadoTotal { get; set; }
    }

    private sealed class DetalleAgrupadoConsulta
    {
        public long IdAgrupacion { get; set; }
        public int Orden { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? TipoParticipante { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int CantidadParticipantes { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal MontoBase { get; set; }
        public decimal MontoAjuste { get; set; }
        public decimal MontoProyectado { get; set; }
    }
}

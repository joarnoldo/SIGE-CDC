using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class ParametroPlanillaService(ApplicationDbContext contexto) : IParametroPlanillaService
{
    private const string EstadoActivo = "Activo";
    private const string EstadoInactivo = "Inactivo";

    public async Task<IReadOnlyList<ParametroPlanillaResumen>> ObtenerParametrosAsync(
        string? busqueda = null,
        string? tipoParametro = null,
        string? estadoRegistro = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = contexto.ParametrosPlanilla
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var filtro = busqueda.Trim();
            consulta = consulta.Where(parametro =>
                parametro.Codigo.Contains(filtro)
                || parametro.Nombre.Contains(filtro)
                || (parametro.Descripcion != null && parametro.Descripcion.Contains(filtro)));
        }

        if (!string.IsNullOrWhiteSpace(tipoParametro))
        {
            consulta = consulta.Where(parametro => parametro.TipoParametro == tipoParametro);
        }

        if (!string.IsNullOrWhiteSpace(estadoRegistro))
        {
            consulta = consulta.Where(parametro => parametro.EstadoRegistro == estadoRegistro);
        }

        return await consulta
            .OrderBy(parametro => parametro.Nombre)
            .Select(parametro => new ParametroPlanillaResumen
            {
                IdParametroPlanilla = parametro.IdParametroPlanilla,
                Codigo = parametro.Codigo,
                Nombre = parametro.Nombre,
                Descripcion = parametro.Descripcion,
                TipoParametro = parametro.TipoParametro,
                Naturaleza = parametro.Naturaleza,
                ValorDecimal = parametro.ValorDecimal,
                ValorTexto = parametro.ValorTexto,
                EsEditable = parametro.EsEditable,
                FechaVigenciaInicio = parametro.FechaVigenciaInicio,
                FechaVigenciaFin = parametro.FechaVigenciaFin,
                EstadoRegistro = parametro.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ParametroPlanillaDetalle?> ObtenerParametroPorIdAsync(
        int idParametroPlanilla,
        CancellationToken cancellationToken = default)
    {
        return await contexto.ParametrosPlanilla
            .AsNoTracking()
            .Where(parametro => parametro.IdParametroPlanilla == idParametroPlanilla)
            .Select(parametro => new ParametroPlanillaDetalle
            {
                IdParametroPlanilla = parametro.IdParametroPlanilla,
                Codigo = parametro.Codigo,
                Nombre = parametro.Nombre,
                Descripcion = parametro.Descripcion,
                TipoParametro = parametro.TipoParametro,
                Naturaleza = parametro.Naturaleza,
                ValorDecimal = parametro.ValorDecimal,
                ValorTexto = parametro.ValorTexto,
                EsEditable = parametro.EsEditable,
                FechaVigenciaInicio = parametro.FechaVigenciaInicio,
                FechaVigenciaFin = parametro.FechaVigenciaFin,
                FechaCreacion = parametro.FechaCreacion,
                FechaModificacion = parametro.FechaModificacion,
                EstadoRegistro = parametro.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TipoParametroPlanillaOpcion>> ObtenerTiposParametroAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TipoParametroPlanillaOpcion> tipos =
        [
            new(TiposParametroPlanilla.Porcentaje, "Porcentaje utilizado en cálculos de planilla."),
            new(TiposParametroPlanilla.Monto, "Monto fijo aplicable a reglas de planilla."),
            new(TiposParametroPlanilla.Cantidad, "Cantidad de referencia para cálculos internos."),
            new(TiposParametroPlanilla.Texto, "Valor descriptivo o código de referencia.")
        ];

        return Task.FromResult(tipos);
    }

    public Task<IReadOnlyList<NaturalezaParametroPlanillaOpcion>> ObtenerNaturalezasParametroAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NaturalezaParametroPlanillaOpcion> naturalezas =
        [
            new(NaturalezasParametroPlanilla.Deduccion, "Concepto que reduce el salario neto."),
            new(NaturalezasParametroPlanilla.Beneficio, "Concepto que aumenta el salario bruto.")
        ];

        return Task.FromResult(naturalezas);
    }

    public async Task<IReadOnlyList<AsignacionParametroPlanillaResumen>> ObtenerAsignacionesAsync(
        int idParametroPlanilla,
        CancellationToken cancellationToken = default)
    {
        var asignacionesColaborador = await contexto.ParametrosPlanillaColaborador
            .AsNoTracking()
            .Include(asignacion => asignacion.Colaborador)
            .Where(asignacion => asignacion.IdParametroPlanilla == idParametroPlanilla)
            .Select(asignacion => new
            {
                asignacion.IdParametroPlanillaColaborador,
                asignacion.IdColaborador,
                Codigo = asignacion.Colaborador != null ? asignacion.Colaborador.CodigoColaborador : string.Empty,
                Nombre = asignacion.Colaborador != null ? asignacion.Colaborador.Nombre : string.Empty,
                PrimerApellido = asignacion.Colaborador != null ? asignacion.Colaborador.PrimerApellido : string.Empty,
                SegundoApellido = asignacion.Colaborador != null ? asignacion.Colaborador.SegundoApellido : null,
                asignacion.ValorDecimalOverride,
                asignacion.ValorTextoOverride,
                asignacion.EstadoRegistro
            })
            .ToListAsync(cancellationToken);

        var asignacionesPeriodo = await contexto.ParametrosPlanillaPeriodo
            .AsNoTracking()
            .Include(asignacion => asignacion.PeriodoPlanilla)
                .ThenInclude(periodo => periodo!.EstadoPlanilla)
            .Where(asignacion => asignacion.IdParametroPlanilla == idParametroPlanilla)
            .Select(asignacion => new AsignacionParametroPlanillaResumen
            {
                IdAsignacion = asignacion.IdParametroPlanillaPeriodo,
                Ambito = AmbitosAsignacionParametroPlanilla.Periodo,
                IdDestino = asignacion.IdPeriodoPlanilla,
                CodigoDestino = asignacion.PeriodoPlanilla != null ? asignacion.PeriodoPlanilla.CodigoPeriodo : string.Empty,
                NombreDestino = asignacion.PeriodoPlanilla != null ? asignacion.PeriodoPlanilla.Nombre : string.Empty,
                ValorDecimalOverride = asignacion.ValorDecimalOverride,
                ValorTextoOverride = asignacion.ValorTextoOverride,
                EstadoRegistro = asignacion.EstadoRegistro,
                EstaBloqueada = asignacion.PeriodoPlanilla != null
                    && asignacion.PeriodoPlanilla.EstadoPlanilla != null
                    && (asignacion.PeriodoPlanilla.EstadoPlanilla.Nombre == EstadosPlanilla.Aprobada
                        || asignacion.PeriodoPlanilla.EstadoPlanilla.Nombre == EstadosPlanilla.Cerrada)
            })
            .ToListAsync(cancellationToken);

        var resultado = asignacionesColaborador
            .Select(asignacion => new AsignacionParametroPlanillaResumen
            {
                IdAsignacion = asignacion.IdParametroPlanillaColaborador,
                Ambito = AmbitosAsignacionParametroPlanilla.Colaborador,
                IdDestino = asignacion.IdColaborador,
                CodigoDestino = asignacion.Codigo,
                NombreDestino = ConstruirNombreCompleto(asignacion.Nombre, asignacion.PrimerApellido, asignacion.SegundoApellido),
                ValorDecimalOverride = asignacion.ValorDecimalOverride,
                ValorTextoOverride = asignacion.ValorTextoOverride,
                EstadoRegistro = asignacion.EstadoRegistro
            })
            .Concat(asignacionesPeriodo)
            .OrderBy(asignacion => asignacion.Ambito)
            .ThenBy(asignacion => asignacion.NombreDestino)
            .ToList();

        return resultado;
    }

    public async Task<IReadOnlyList<DestinoAsignacionParametroPlanillaOpcion>> ObtenerColaboradoresAsignacionAsync(
        CancellationToken cancellationToken = default)
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
            .Select(colaborador => new DestinoAsignacionParametroPlanillaOpcion(
                colaborador.IdColaborador,
                colaborador.CodigoColaborador,
                ConstruirNombreCompleto(colaborador.Nombre, colaborador.PrimerApellido, colaborador.SegundoApellido)))
            .ToList();
    }

    public async Task<IReadOnlyList<DestinoAsignacionParametroPlanillaOpcion>> ObtenerPeriodosAsignacionAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Include(periodo => periodo.EstadoPlanilla)
            .Where(periodo => periodo.EstadoRegistro == EstadoActivo)
            .OrderByDescending(periodo => periodo.FechaInicio)
            .Select(periodo => new DestinoAsignacionParametroPlanillaOpcion(
                periodo.IdPeriodoPlanilla,
                periodo.CodigoPeriodo,
                periodo.Nombre,
                periodo.EstadoPlanilla != null
                    && (periodo.EstadoPlanilla.Nombre == EstadosPlanilla.Aprobada
                        || periodo.EstadoPlanilla.Nombre == EstadosPlanilla.Cerrada)))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RegistrarParametroAsync(
        SolicitudParametroPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var datos = await ValidarSolicitudAsync(solicitud, null, cancellationToken);

        var parametro = new ParametroPlanilla
        {
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            TipoParametro = datos.TipoParametro,
            Naturaleza = datos.Naturaleza,
            ValorDecimal = datos.ValorDecimal,
            ValorTexto = datos.ValorTexto,
            EsEditable = true,
            FechaVigenciaInicio = datos.FechaVigenciaInicio,
            FechaVigenciaFin = datos.FechaVigenciaFin,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuarioActual,
            EstadoRegistro = EstadoActivo
        };

        contexto.ParametrosPlanilla.Add(parametro);
        await contexto.SaveChangesAsync(cancellationToken);

        return parametro.IdParametroPlanilla;
    }

    public async Task ActualizarParametroAsync(
        int idParametroPlanilla,
        SolicitudParametroPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var parametro = await contexto.ParametrosPlanilla
            .FirstOrDefaultAsync(registro => registro.IdParametroPlanilla == idParametroPlanilla, cancellationToken)
            ?? throw new InvalidOperationException("No se encontró el parámetro solicitado.");

        ValidarParametroEditable(parametro);
        var datos = await ValidarSolicitudAsync(solicitud, idParametroPlanilla, cancellationToken);
        await ValidarCambioConAsignacionesAsync(parametro, datos, cancellationToken);

        parametro.Codigo = datos.Codigo;
        parametro.Nombre = datos.Nombre;
        parametro.Descripcion = datos.Descripcion;
        parametro.TipoParametro = datos.TipoParametro;
        parametro.Naturaleza = datos.Naturaleza;
        parametro.ValorDecimal = datos.ValorDecimal;
        parametro.ValorTexto = datos.ValorTexto;
        parametro.FechaVigenciaInicio = datos.FechaVigenciaInicio;
        parametro.FechaVigenciaFin = datos.FechaVigenciaFin;
        parametro.FechaModificacion = DateTime.Now;
        parametro.ModificadoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarParametroAsync(
        int idParametroPlanilla,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var parametro = await contexto.ParametrosPlanilla
            .FirstOrDefaultAsync(registro => registro.IdParametroPlanilla == idParametroPlanilla, cancellationToken)
            ?? throw new InvalidOperationException("No se encontró el parámetro solicitado.");

        ValidarParametroEditable(parametro);

        parametro.EstadoRegistro = EstadoInactivo;
        parametro.FechaModificacion = DateTime.Now;
        parametro.ModificadoPor = idUsuarioActual;

        var asignacionesColaborador = await contexto.ParametrosPlanillaColaborador
            .Where(asignacion => asignacion.IdParametroPlanilla == idParametroPlanilla
                && asignacion.EstadoRegistro == EstadoActivo)
            .ToListAsync(cancellationToken);
        var asignacionesPeriodo = await contexto.ParametrosPlanillaPeriodo
            .Where(asignacion => asignacion.IdParametroPlanilla == idParametroPlanilla
                && asignacion.EstadoRegistro == EstadoActivo)
            .ToListAsync(cancellationToken);

        foreach (var asignacion in asignacionesColaborador)
        {
            MarcarAsignacionInactiva(asignacion, idUsuarioActual);
        }

        foreach (var asignacion in asignacionesPeriodo)
        {
            MarcarAsignacionInactiva(asignacion, idUsuarioActual);
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GuardarAsignacionAsync(
        int idParametroPlanilla,
        SolicitudAsignacionParametroPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var parametro = await contexto.ParametrosPlanilla
            .AsNoTracking()
            .FirstOrDefaultAsync(registro => registro.IdParametroPlanilla == idParametroPlanilla
                && registro.EstadoRegistro == EstadoActivo, cancellationToken)
            ?? throw new InvalidOperationException("El parámetro seleccionado no está disponible.");

        if (string.IsNullOrWhiteSpace(parametro.Naturaleza))
        {
            throw new InvalidOperationException("Defina la naturaleza Deduccion o Beneficio antes de crear asignaciones.");
        }

        ValidarParametroEditable(parametro);

        var datos = DatosAsignacionLimpios.DesdeSolicitud(solicitud, parametro.TipoParametro);

        if (datos.Ambito == AmbitosAsignacionParametroPlanilla.Colaborador)
        {
            return await GuardarAsignacionColaboradorAsync(parametro.IdParametroPlanilla, datos, idUsuarioActual, cancellationToken);
        }

        return await GuardarAsignacionPeriodoAsync(parametro.IdParametroPlanilla, datos, idUsuarioActual, cancellationToken);
    }

    public async Task DesactivarAsignacionAsync(
        string ambito,
        long idAsignacion,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var ambitoNormalizado = NormalizarAmbito(ambito);

        if (ambitoNormalizado == AmbitosAsignacionParametroPlanilla.Colaborador)
        {
            var asignacion = await contexto.ParametrosPlanillaColaborador
                .FirstOrDefaultAsync(registro => registro.IdParametroPlanillaColaborador == idAsignacion
                    && registro.EstadoRegistro == EstadoActivo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró la asignación activa solicitada.");

            MarcarAsignacionInactiva(asignacion, idUsuarioActual);
        }
        else
        {
            var asignacion = await contexto.ParametrosPlanillaPeriodo
                .Include(registro => registro.PeriodoPlanilla)
                    .ThenInclude(periodo => periodo!.EstadoPlanilla)
                .FirstOrDefaultAsync(registro => registro.IdParametroPlanillaPeriodo == idAsignacion
                    && registro.EstadoRegistro == EstadoActivo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró la asignación activa solicitada.");

            ValidarPeriodoNoBloqueado(asignacion.PeriodoPlanilla);
            MarcarAsignacionInactiva(asignacion, idUsuarioActual);
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<long> GuardarAsignacionColaboradorAsync(
        int idParametroPlanilla,
        DatosAsignacionLimpios datos,
        string? idUsuarioActual,
        CancellationToken cancellationToken)
    {
        var colaboradorExiste = await contexto.Colaboradores
            .AsNoTracking()
            .AnyAsync(colaborador => colaborador.IdColaborador == datos.IdDestino
                && colaborador.EstadoRegistro == EstadoActivo, cancellationToken);

        if (!colaboradorExiste)
        {
            throw new ArgumentException("El colaborador seleccionado no está disponible.");
        }

        var asignacion = await contexto.ParametrosPlanillaColaborador
            .FirstOrDefaultAsync(registro => registro.IdParametroPlanilla == idParametroPlanilla
                && registro.IdColaborador == datos.IdDestino, cancellationToken);

        if (asignacion is null)
        {
            asignacion = new ParametroPlanillaColaborador
            {
                IdParametroPlanilla = idParametroPlanilla,
                IdColaborador = datos.IdDestino,
                FechaCreacion = DateTime.Now,
                CreadoPor = idUsuarioActual
            };
            contexto.ParametrosPlanillaColaborador.Add(asignacion);
        }
        else
        {
            asignacion.FechaModificacion = DateTime.Now;
            asignacion.ModificadoPor = idUsuarioActual;
        }

        asignacion.ValorDecimalOverride = datos.ValorDecimalOverride;
        asignacion.ValorTextoOverride = datos.ValorTextoOverride;
        asignacion.EstadoRegistro = EstadoActivo;

        await contexto.SaveChangesAsync(cancellationToken);
        return asignacion.IdParametroPlanillaColaborador;
    }

    private async Task<long> GuardarAsignacionPeriodoAsync(
        int idParametroPlanilla,
        DatosAsignacionLimpios datos,
        string? idUsuarioActual,
        CancellationToken cancellationToken)
    {
        var periodo = await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Include(registro => registro.EstadoPlanilla)
            .FirstOrDefaultAsync(registro => registro.IdPeriodoPlanilla == datos.IdDestino
                && registro.EstadoRegistro == EstadoActivo, cancellationToken);

        if (periodo is null)
        {
            throw new ArgumentException("El período seleccionado no está disponible.");
        }

        ValidarPeriodoNoBloqueado(periodo);

        var asignacion = await contexto.ParametrosPlanillaPeriodo
            .FirstOrDefaultAsync(registro => registro.IdParametroPlanilla == idParametroPlanilla
                && registro.IdPeriodoPlanilla == datos.IdDestino, cancellationToken);

        if (asignacion is null)
        {
            asignacion = new ParametroPlanillaPeriodo
            {
                IdParametroPlanilla = idParametroPlanilla,
                IdPeriodoPlanilla = datos.IdDestino,
                FechaCreacion = DateTime.Now,
                CreadoPor = idUsuarioActual
            };
            contexto.ParametrosPlanillaPeriodo.Add(asignacion);
        }
        else
        {
            asignacion.FechaModificacion = DateTime.Now;
            asignacion.ModificadoPor = idUsuarioActual;
        }

        asignacion.ValorDecimalOverride = datos.ValorDecimalOverride;
        asignacion.ValorTextoOverride = datos.ValorTextoOverride;
        asignacion.EstadoRegistro = EstadoActivo;

        await contexto.SaveChangesAsync(cancellationToken);
        return asignacion.IdParametroPlanillaPeriodo;
    }

    private static void ValidarPeriodoNoBloqueado(PeriodoPlanilla? periodo)
    {
        if (periodo is null)
        {
            throw new ArgumentException("El período seleccionado no está disponible.");
        }

        if (FlujoEstadosPlanilla.EstaBloqueada(periodo.EstadoPlanilla?.Nombre))
        {
            throw new InvalidOperationException(
                $"El período está en estado {periodo.EstadoPlanilla!.Nombre} y no permite modificar asignaciones.");
        }
    }

    private async Task<DatosParametroLimpios> ValidarSolicitudAsync(
        SolicitudParametroPlanilla solicitud,
        int? idParametroActual,
        CancellationToken cancellationToken)
    {
        var datos = DatosParametroLimpios.DesdeSolicitud(solicitud);

        if (datos.FechaVigenciaInicio.HasValue
            && datos.FechaVigenciaFin.HasValue
            && datos.FechaVigenciaFin.Value < datos.FechaVigenciaInicio.Value)
        {
            throw new ArgumentException("La fecha final de vigencia no puede ser anterior a la fecha inicial.");
        }

        var codigoExiste = await contexto.ParametrosPlanilla
            .AnyAsync(parametro => parametro.Codigo == datos.Codigo
                && parametro.IdParametroPlanilla != idParametroActual, cancellationToken);

        if (codigoExiste)
        {
            throw new InvalidOperationException("Ya existe un parámetro de planilla con el código indicado.");
        }

        return datos;
    }

    private async Task ValidarCambioConAsignacionesAsync(
        ParametroPlanilla parametro,
        DatosParametroLimpios datos,
        CancellationToken cancellationToken)
    {
        if (string.Equals(parametro.TipoParametro, datos.TipoParametro, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parametro.Naturaleza, datos.Naturaleza, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tieneAsignaciones = await contexto.ParametrosPlanillaColaborador
                .AnyAsync(asignacion => asignacion.IdParametroPlanilla == parametro.IdParametroPlanilla
                    && asignacion.EstadoRegistro == EstadoActivo, cancellationToken)
            || await contexto.ParametrosPlanillaPeriodo
                .AnyAsync(asignacion => asignacion.IdParametroPlanilla == parametro.IdParametroPlanilla
                    && asignacion.EstadoRegistro == EstadoActivo, cancellationToken);

        if (tieneAsignaciones)
        {
            throw new InvalidOperationException(
                "Desactive las asignaciones vigentes antes de cambiar el tipo o la naturaleza del parámetro.");
        }
    }

    private static void ValidarParametroEditable(ParametroPlanilla parametro)
    {
        if (!parametro.EsEditable)
        {
            throw new InvalidOperationException("El parámetro seleccionado no está habilitado para edición.");
        }
    }

    private static void MarcarAsignacionInactiva(ParametroPlanillaColaborador asignacion, string? idUsuarioActual)
    {
        asignacion.EstadoRegistro = EstadoInactivo;
        asignacion.FechaModificacion = DateTime.Now;
        asignacion.ModificadoPor = idUsuarioActual;
    }

    private static void MarcarAsignacionInactiva(ParametroPlanillaPeriodo asignacion, string? idUsuarioActual)
    {
        asignacion.EstadoRegistro = EstadoInactivo;
        asignacion.FechaModificacion = DateTime.Now;
        asignacion.ModificadoPor = idUsuarioActual;
    }

    private static string NormalizarAmbito(string ambito)
    {
        if (string.IsNullOrWhiteSpace(ambito))
        {
            throw new ArgumentException("Seleccione el ámbito de asignación.");
        }

        return AmbitosAsignacionParametroPlanilla.Permitidos.FirstOrDefault(
                valor => string.Equals(valor, ambito.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("El ámbito de asignación no es válido.");
    }

    private static string ConstruirNombreCompleto(string nombre, string primerApellido, string? segundoApellido)
    {
        return string.IsNullOrWhiteSpace(segundoApellido)
            ? $"{nombre} {primerApellido}"
            : $"{nombre} {primerApellido} {segundoApellido}";
    }

    private sealed record DatosParametroLimpios(
        string Codigo,
        string Nombre,
        string? Descripcion,
        string TipoParametro,
        string? Naturaleza,
        decimal? ValorDecimal,
        string? ValorTexto,
        DateTime? FechaVigenciaInicio,
        DateTime? FechaVigenciaFin)
    {
        public static DatosParametroLimpios DesdeSolicitud(SolicitudParametroPlanilla solicitud)
        {
            var tipoParametro = NormalizarTipoParametro(
                LimpiarObligatorio(solicitud.TipoParametro, "Seleccione el tipo de parámetro."));
            var naturaleza = NormalizarNaturaleza(solicitud.Naturaleza);
            var esTexto = string.Equals(tipoParametro, TiposParametroPlanilla.Texto, StringComparison.OrdinalIgnoreCase);

            if (naturaleza is not null
                && tipoParametro != TiposParametroPlanilla.Porcentaje
                && tipoParametro != TiposParametroPlanilla.Monto)
            {
                throw new ArgumentException(
                    "Solo los parámetros de tipo Porcentaje o Monto pueden definirse como deducción o beneficio.");
            }

            if (esTexto && string.IsNullOrWhiteSpace(solicitud.ValorTexto))
            {
                throw new ArgumentException("Ingrese el valor de texto del parámetro.");
            }

            if (!esTexto && solicitud.ValorDecimal is null)
            {
                throw new ArgumentException("Ingrese el valor numérico del parámetro.");
            }

            return new DatosParametroLimpios(
                LimpiarObligatorio(solicitud.Codigo, "El código es obligatorio.").ToUpperInvariant(),
                LimpiarObligatorio(solicitud.Nombre, "El nombre es obligatorio."),
                LimpiarOpcional(solicitud.Descripcion),
                tipoParametro,
                naturaleza,
                esTexto ? null : solicitud.ValorDecimal,
                esTexto ? LimpiarOpcional(solicitud.ValorTexto) : null,
                solicitud.FechaVigenciaInicio?.Date,
                solicitud.FechaVigenciaFin?.Date);
        }

        private static string NormalizarTipoParametro(string tipoParametro)
        {
            return TiposParametroPlanilla.Permitidos.FirstOrDefault(
                    tipo => string.Equals(tipo, tipoParametro, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("El tipo de parámetro seleccionado no está disponible.");
        }

        private static string? NormalizarNaturaleza(string? naturaleza)
        {
            if (string.IsNullOrWhiteSpace(naturaleza))
            {
                return null;
            }

            return NaturalezasParametroPlanilla.Permitidas.FirstOrDefault(
                    valor => string.Equals(valor, naturaleza.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("La naturaleza seleccionada no está disponible.");
        }

        private static string LimpiarObligatorio(string valor, string mensajeError)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException(mensajeError);
            }

            return valor.Trim();
        }

        private static string? LimpiarOpcional(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }

    private sealed record DatosAsignacionLimpios(
        string Ambito,
        long IdDestino,
        decimal? ValorDecimalOverride,
        string? ValorTextoOverride)
    {
        public static DatosAsignacionLimpios DesdeSolicitud(
            SolicitudAsignacionParametroPlanilla solicitud,
            string tipoParametro)
        {
            var ambito = NormalizarAmbito(solicitud.Ambito);

            if (solicitud.IdDestino <= 0)
            {
                throw new ArgumentException("Seleccione el colaborador o período correspondiente.");
            }

            if (solicitud.ValorDecimalOverride is < 0 or > 999999999999.9999m)
            {
                throw new ArgumentException("El valor numérico asignado no es válido.");
            }

            if (solicitud.ValorTextoOverride?.Length > 255)
            {
                throw new ArgumentException("El valor de texto asignado no debe superar los 255 caracteres.");
            }

            var esTexto = string.Equals(tipoParametro, TiposParametroPlanilla.Texto, StringComparison.OrdinalIgnoreCase);

            if (esTexto && solicitud.ValorDecimalOverride.HasValue)
            {
                throw new ArgumentException("Un parámetro de texto no admite un valor numérico asignado.");
            }

            if (!esTexto && !string.IsNullOrWhiteSpace(solicitud.ValorTextoOverride))
            {
                throw new ArgumentException("Un parámetro numérico no admite un valor de texto asignado.");
            }

            return new DatosAsignacionLimpios(
                ambito,
                solicitud.IdDestino,
                esTexto ? null : solicitud.ValorDecimalOverride,
                esTexto && !string.IsNullOrWhiteSpace(solicitud.ValorTextoOverride)
                    ? solicitud.ValorTextoOverride.Trim()
                    : null);
        }
    }
}

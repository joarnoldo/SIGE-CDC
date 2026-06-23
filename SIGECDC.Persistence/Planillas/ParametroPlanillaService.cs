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
            new(TiposParametroPlanilla.Porcentaje, "Porcentaje utilizado en calculos de planilla."),
            new(TiposParametroPlanilla.Monto, "Monto fijo aplicable a reglas de planilla."),
            new(TiposParametroPlanilla.Cantidad, "Cantidad de referencia para calculos internos."),
            new(TiposParametroPlanilla.Texto, "Valor descriptivo o codigo de referencia.")
        ];

        return Task.FromResult(tipos);
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
            .FirstOrDefaultAsync(
                parametro => parametro.IdParametroPlanilla == idParametroPlanilla,
                cancellationToken);

        if (parametro is null)
        {
            throw new InvalidOperationException("No se encontro el parametro solicitado.");
        }

        ValidarParametroEditable(parametro);
        var datos = await ValidarSolicitudAsync(solicitud, idParametroPlanilla, cancellationToken);

        parametro.Codigo = datos.Codigo;
        parametro.Nombre = datos.Nombre;
        parametro.Descripcion = datos.Descripcion;
        parametro.TipoParametro = datos.TipoParametro;
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
            .FirstOrDefaultAsync(
                parametro => parametro.IdParametroPlanilla == idParametroPlanilla,
                cancellationToken);

        if (parametro is null)
        {
            throw new InvalidOperationException("No se encontro el parametro solicitado.");
        }

        ValidarParametroEditable(parametro);

        parametro.EstadoRegistro = EstadoInactivo;
        parametro.FechaModificacion = DateTime.Now;
        parametro.ModificadoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<DatosParametroLimpios> ValidarSolicitudAsync(
        SolicitudParametroPlanilla solicitud,
        int? idParametroActual,
        CancellationToken cancellationToken)
    {
        var datos = DatosParametroLimpios.DesdeSolicitud(solicitud);

        if (!TiposParametroPlanilla.Permitidos.Contains(datos.TipoParametro))
        {
            throw new ArgumentException("El tipo de parametro seleccionado no esta disponible.");
        }

        if (datos.FechaVigenciaInicio.HasValue
            && datos.FechaVigenciaFin.HasValue
            && datos.FechaVigenciaFin.Value < datos.FechaVigenciaInicio.Value)
        {
            throw new ArgumentException("La fecha final de vigencia no puede ser anterior a la fecha inicial.");
        }

        var codigoExiste = await contexto.ParametrosPlanilla
            .AnyAsync(
                parametro => parametro.Codigo == datos.Codigo
                    && parametro.IdParametroPlanilla != idParametroActual,
                cancellationToken);

        if (codigoExiste)
        {
            throw new InvalidOperationException("Ya existe un parametro de planilla con el codigo indicado.");
        }

        return datos;
    }

    private static void ValidarParametroEditable(ParametroPlanilla parametro)
    {
        if (!parametro.EsEditable)
        {
            throw new InvalidOperationException("El parametro seleccionado no esta habilitado para edicion.");
        }
    }

    private sealed record DatosParametroLimpios(
        string Codigo,
        string Nombre,
        string? Descripcion,
        string TipoParametro,
        decimal? ValorDecimal,
        string? ValorTexto,
        DateTime? FechaVigenciaInicio,
        DateTime? FechaVigenciaFin)
    {
        public static DatosParametroLimpios DesdeSolicitud(SolicitudParametroPlanilla solicitud)
        {
            var tipoParametro = LimpiarObligatorio(solicitud.TipoParametro, "Seleccione el tipo de parametro.");
            var esTexto = string.Equals(tipoParametro, TiposParametroPlanilla.Texto, StringComparison.OrdinalIgnoreCase);

            if (esTexto && string.IsNullOrWhiteSpace(solicitud.ValorTexto))
            {
                throw new ArgumentException("Ingrese el valor de texto del parametro.");
            }

            if (!esTexto && solicitud.ValorDecimal is null)
            {
                throw new ArgumentException("Ingrese el valor numerico del parametro.");
            }

            return new DatosParametroLimpios(
                LimpiarObligatorio(solicitud.Codigo, "El codigo es obligatorio.").ToUpperInvariant(),
                LimpiarObligatorio(solicitud.Nombre, "El nombre es obligatorio."),
                LimpiarOpcional(solicitud.Descripcion),
                NormalizarTipoParametro(tipoParametro),
                esTexto ? null : solicitud.ValorDecimal,
                esTexto ? LimpiarOpcional(solicitud.ValorTexto) : null,
                solicitud.FechaVigenciaInicio?.Date,
                solicitud.FechaVigenciaFin?.Date);
        }

        private static string NormalizarTipoParametro(string tipoParametro)
        {
            return TiposParametroPlanilla.Permitidos.First(tipo => string.Equals(tipo, tipoParametro, StringComparison.OrdinalIgnoreCase));
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
}

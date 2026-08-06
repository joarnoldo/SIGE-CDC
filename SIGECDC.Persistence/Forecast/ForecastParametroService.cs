using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastParametroService(ApplicationDbContext contexto)
    : IForecastParametroService
{
    public async Task<ConfiguracionParametrosForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario);

        var escenario = await contexto.ForecastEscenarios
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idForecastEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => new
            {
                item.IdForecastEscenario,
                item.Nombre,
                item.EstadoEscenario
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (escenario is null)
        {
            return null;
        }

        var filas = await contexto.ForecastParametros
            .AsNoTracking()
            .Where(parametro => parametro.IdForecastEscenario == idForecastEscenario)
            .ToListAsync(cancellationToken);

        var parametros = CodigosParametroForecast.Definiciones
            .Select(definicion =>
            {
                var fila = filas.FirstOrDefault(parametro => string.Equals(
                    parametro.Codigo,
                    definicion.Codigo,
                    StringComparison.OrdinalIgnoreCase));

                return new ParametroForecastConfigurado
                {
                    IdForecastParametro = fila?.IdForecastParametro,
                    Codigo = definicion.Codigo,
                    Nombre = definicion.Nombre,
                    TipoParametro = fila?.TipoParametro ?? definicion.TipoInicial,
                    ValorDecimal = fila?.ValorDecimal,
                    EstaHabilitado = string.Equals(
                        fila?.EstadoRegistro,
                        EstadosRegistro.Activo,
                        StringComparison.OrdinalIgnoreCase),
                    EstadoRegistro = fila?.EstadoRegistro,
                    TiposPermitidos = [.. definicion.TiposPermitidos]
                };
            })
            .ToList();

        return new ConfiguracionParametrosForecast
        {
            IdForecastEscenario = escenario.IdForecastEscenario,
            NombreEscenario = escenario.Nombre,
            EstadoEscenario = escenario.EstadoEscenario,
            PuedeEditar = string.Equals(
                escenario.EstadoEscenario,
                EstadosEscenarioForecast.Borrador,
                StringComparison.OrdinalIgnoreCase),
            Parametros = parametros
        };
    }

    public async Task GuardarConfiguracionAsync(
        long idForecastEscenario,
        SolicitudGuardarParametrosForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idActor = await ForecastAutorizacion.ValidarRecursosHumanosAsync(
            contexto,
            idUsuarioActual,
            cancellationToken);
        ValidarIdentificador(idForecastEscenario);
        var datos = NormalizarSolicitud(solicitud);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var escenario = await contexto.ForecastEscenarios
                .Include(item => item.Parametros)
                .FirstOrDefaultAsync(item => item.IdForecastEscenario == idForecastEscenario
                    && item.EstadoRegistro == EstadosRegistro.Activo,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    "El escenario solicitado no existe o no está activo.");

            if (!string.Equals(
                    escenario.EstadoEscenario,
                    EstadosEscenarioForecast.Borrador,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Solo se pueden configurar parámetros en un escenario Borrador.");
            }

            var parametrosPorCodigo = escenario.Parametros
                .Where(parametro => CodigosParametroForecast.Obtener(parametro.Codigo) is not null)
                .ToDictionary(
                    parametro => parametro.Codigo,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var dato in datos)
            {
                parametrosPorCodigo.TryGetValue(dato.Definicion.Codigo, out var parametro);

                if (!dato.EstaHabilitado)
                {
                    if (parametro is not null)
                    {
                        parametro.Codigo = dato.Definicion.Codigo;
                        parametro.Nombre = dato.Definicion.Nombre;
                        parametro.ValorTexto = null;
                        parametro.Descripcion = null;
                        parametro.EstadoRegistro = EstadosRegistro.Inactivo;
                    }

                    continue;
                }

                if (parametro is null)
                {
                    parametro = new ForecastParametro
                    {
                        Codigo = dato.Definicion.Codigo,
                        Nombre = dato.Definicion.Nombre
                    };
                    escenario.Parametros.Add(parametro);
                }

                parametro.Codigo = dato.Definicion.Codigo;
                parametro.Nombre = dato.Definicion.Nombre;
                parametro.TipoParametro = dato.TipoParametro;
                parametro.ValorDecimal = dato.ValorDecimal;
                parametro.ValorTexto = null;
                parametro.Descripcion = null;
                parametro.EstadoRegistro = EstadosRegistro.Activo;
            }

            escenario.FechaModificacion = DateTime.Now;
            escenario.ModificadoPor = idActor;

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private static IReadOnlyList<DatoParametroLimpio> NormalizarSolicitud(
        SolicitudGuardarParametrosForecast solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        Validator.ValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            validateAllProperties: true);

        return CodigosParametroForecast.Definiciones
            .Select(definicion =>
            {
                var parametro = solicitud.Parametros.Single(item => string.Equals(
                    item.Codigo.Trim(),
                    definicion.Codigo,
                    StringComparison.OrdinalIgnoreCase));
                var tipo = definicion.TiposPermitidos.Single(tipoPermitido => string.Equals(
                    tipoPermitido,
                    parametro.TipoParametro.Trim(),
                    StringComparison.OrdinalIgnoreCase));

                return new DatoParametroLimpio(
                    definicion,
                    parametro.EstaHabilitado,
                    tipo,
                    parametro.ValorDecimal);
            })
            .ToList();
    }

    private static void ValidarIdentificador(long idForecastEscenario)
    {
        if (idForecastEscenario <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idForecastEscenario),
                "El identificador del escenario no es válido.");
        }
    }

    private sealed record DatoParametroLimpio(
        DefinicionParametroForecast Definicion,
        bool EstaHabilitado,
        string TipoParametro,
        decimal? ValorDecimal);
}

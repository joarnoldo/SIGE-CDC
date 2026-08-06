using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Auditoria;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Auditoria;

public sealed class HistorialSistemaService(ApplicationDbContext contexto) : IHistorialSistemaService
{
    private const string EntidadAutenticacion = "Autenticacion";
    private const string EntidadActivo = "Activo";
    private const string EntidadProyecto = "Proyecto";
    private const string EntidadAsignacion = "AsignacionActivoProyecto";
    private const string EntidadMantenimiento = "Mantenimiento";
    private const string EntidadRegistroUsoActivo = "RegistroUsoActivo";
    private const string EntidadColaborador = "Colaborador";
    private const string EntidadPeriodoPlanilla = "PeriodoPlanilla";
    private const string EntidadPlanilla = "Planilla";
    private const string AccionAsignacionHistorica =
        "Actualización de estado por asignación";
    private const string AccionMantenimientoHistorica =
        "Actualización de estado por mantenimiento";

    public async Task RegistrarAccesoAsync(
        SolicitudRegistroAcceso solicitud,
        CancellationToken cancellationToken = default)
    {
        var accion = LimpiarObligatorio(solicitud.Accion, "La accion de auditoria es obligatoria.");

        var registro = new BitacoraAuditoria
        {
            IdUsuario = string.IsNullOrWhiteSpace(solicitud.IdUsuario) ? null : solicitud.IdUsuario.Trim(),
            FechaHora = DateTime.Now,
            Accion = accion,
            Entidad = EntidadAutenticacion,
            DireccionIP = LimpiarOpcional(solicitud.DireccionIP),
            Observacion = LimpiarOpcional(solicitud.Observacion)
        };

        contexto.BitacoraAuditoria.Add(registro);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccesoHistorialResumen>> ObtenerAccesosAsync(
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? accion = null,
        string? busqueda = null,
        int cantidad = 100,
        CancellationToken cancellationToken = default)
    {
        var limite = NormalizarLimite(cantidad);
        var consulta = contexto.BitacoraAuditoria
            .AsNoTracking()
            .Where(registro => registro.Entidad == EntidadAutenticacion);

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(registro => registro.FechaHora >= fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            var fechaFinal = fechaHasta.Value.Date.AddDays(1);
            consulta = consulta.Where(registro => registro.FechaHora < fechaFinal);
        }

        if (!string.IsNullOrWhiteSpace(accion))
        {
            consulta = consulta.Where(registro => registro.Accion == accion);
        }

        var accesos = consulta
            .GroupJoin(
                contexto.Users.AsNoTracking(),
                registro => registro.IdUsuario,
                usuario => usuario.Id,
                (registro, usuarios) => new
                {
                    Registro = registro,
                    Usuario = usuarios.FirstOrDefault()
                })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var filtro = busqueda.Trim();
            accesos = accesos.Where(item =>
                item.Registro.Accion.Contains(filtro)
                || (item.Registro.DireccionIP != null && item.Registro.DireccionIP.Contains(filtro))
                || (item.Registro.Observacion != null && item.Registro.Observacion.Contains(filtro))
                || (item.Usuario != null && item.Usuario.UserName != null && item.Usuario.UserName.Contains(filtro))
                || (item.Usuario != null && item.Usuario.Email != null && item.Usuario.Email.Contains(filtro)));
        }

        return await accesos
            .OrderByDescending(item => item.Registro.FechaHora)
            .Select(item => new AccesoHistorialResumen
            {
                IdBitacoraAuditoria = item.Registro.IdBitacoraAuditoria,
                FechaHora = item.Registro.FechaHora,
                Accion = item.Registro.Accion,
                Entidad = item.Registro.Entidad,
                IdUsuario = item.Registro.IdUsuario,
                Usuario = item.Usuario != null ? item.Usuario.UserName : null,
                CorreoElectronico = item.Usuario != null ? item.Usuario.Email : null,
                DireccionIP = item.Registro.DireccionIP,
                Observacion = item.Registro.Observacion
            })
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormularioContactoHistorialResumen>> ObtenerFormulariosContactoAsync(
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estadoConsulta = null,
        string? busqueda = null,
        int cantidad = 100,
        CancellationToken cancellationToken = default)
    {
        var limite = NormalizarLimite(cantidad);
        var consulta = contexto.ConsultasContacto
            .AsNoTracking()
            .Where(consulta => consulta.EstadoRegistro == EstadosRegistro.Activo);

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(consulta => consulta.FechaEnvio >= fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            var fechaFinal = fechaHasta.Value.Date.AddDays(1);
            consulta = consulta.Where(consulta => consulta.FechaEnvio < fechaFinal);
        }

        if (!string.IsNullOrWhiteSpace(estadoConsulta))
        {
            consulta = consulta.Where(consulta => consulta.EstadoConsulta == estadoConsulta);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var filtro = busqueda.Trim();
            consulta = consulta.Where(consulta =>
                consulta.Nombre.Contains(filtro)
                || consulta.CorreoElectronico.Contains(filtro)
                || (consulta.Telefono != null && consulta.Telefono.Contains(filtro))
                || (consulta.Asunto != null && consulta.Asunto.Contains(filtro))
                || consulta.Mensaje.Contains(filtro));
        }

        return await consulta
            .OrderByDescending(consulta => consulta.FechaEnvio)
            .Take(limite)
            .Select(consulta => new FormularioContactoHistorialResumen
            {
                IdConsultaContacto = consulta.IdConsultaContacto,
                FechaEnvio = consulta.FechaEnvio,
                Nombre = consulta.Nombre,
                CorreoElectronico = consulta.CorreoElectronico,
                Telefono = consulta.Telefono,
                Asunto = consulta.Asunto,
                Mensaje = consulta.Mensaje,
                EstadoConsulta = consulta.EstadoConsulta
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperacionBitacoraResumen>>
        ObtenerOperacionesRecursosHumanosAsync(
            FiltroBitacoraRecursosHumanos filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        ValidarFiltroRecursosHumanos(filtro);

        var fechaDesde = filtro.FechaDesde?.Date;
        var fechaHasta = filtro.FechaHasta?.Date;
        var categoria = string.IsNullOrWhiteSpace(filtro.Categoria)
            ? null
            : filtro.Categoria.Trim();
        var busqueda = string.IsNullOrWhiteSpace(filtro.Busqueda)
            ? null
            : filtro.Busqueda.Trim();
        var limite = NormalizarLimite(filtro.Cantidad);

        var consulta = contexto.BitacoraAuditoria
            .AsNoTracking()
            .Where(registro =>
                registro.Entidad == EntidadColaborador
                || registro.Entidad == EntidadPeriodoPlanilla
                || registro.Entidad == EntidadPlanilla);

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(registro =>
                registro.FechaHora >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue
            && fechaHasta.Value < DateTime.MaxValue.Date)
        {
            var fechaHastaExclusiva = fechaHasta.Value.AddDays(1);
            consulta = consulta.Where(registro =>
                registro.FechaHora < fechaHastaExclusiva);
        }

        consulta = categoria switch
        {
            CategoriasBitacoraRecursosHumanos.Colaborador =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadColaborador),
            CategoriasBitacoraRecursosHumanos.PeriodoPlanilla =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadPeriodoPlanilla),
            CategoriasBitacoraRecursosHumanos.Planilla =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadPlanilla),
            _ => consulta
        };

        var operaciones = consulta
            .GroupJoin(
                contexto.Users.AsNoTracking(),
                registro => registro.IdUsuario,
                usuario => usuario.Id,
                (registro, usuarios) => new
                {
                    Registro = registro,
                    Usuario = usuarios.FirstOrDefault()
                })
            .AsQueryable();

        if (busqueda is not null)
        {
            operaciones = operaciones.Where(item =>
                item.Registro.Accion.Contains(busqueda)
                || item.Registro.Entidad.Contains(busqueda)
                || (item.Registro.IdRegistro != null
                    && item.Registro.IdRegistro.Contains(busqueda))
                || (item.Registro.Observacion != null
                    && item.Registro.Observacion.Contains(busqueda))
                || (item.Usuario != null
                    && item.Usuario.UserName != null
                    && item.Usuario.UserName.Contains(busqueda))
                || (item.Usuario != null
                    && item.Usuario.Email != null
                    && item.Usuario.Email.Contains(busqueda)));
        }

        return await operaciones
            .OrderByDescending(item => item.Registro.FechaHora)
            .ThenByDescending(item => item.Registro.IdBitacoraAuditoria)
            .Take(limite)
            .Select(item => new OperacionBitacoraResumen
            {
                IdBitacoraAuditoria = item.Registro.IdBitacoraAuditoria,
                FechaHora = item.Registro.FechaHora,
                Categoria = item.Registro.Entidad == EntidadColaborador
                    ? CategoriasBitacoraRecursosHumanos.Colaborador
                    : item.Registro.Entidad == EntidadPeriodoPlanilla
                        ? CategoriasBitacoraRecursosHumanos.PeriodoPlanilla
                        : CategoriasBitacoraRecursosHumanos.Planilla,
                Entidad = item.Registro.Entidad,
                Accion = item.Registro.Accion,
                IdRegistro = item.Registro.IdRegistro,
                IdUsuario = item.Registro.IdUsuario,
                Usuario = item.Usuario == null
                    ? null
                    : item.Usuario.UserName,
                CorreoElectronico = item.Usuario == null
                    ? null
                    : item.Usuario.Email,
                ValoresAnteriores = item.Registro.ValoresAnteriores,
                ValoresNuevos = item.Registro.ValoresNuevos,
                Observacion = item.Registro.Observacion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperacionBitacoraResumen>>
        ObtenerOperacionesAsync(
            FiltroBitacoraOperaciones filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        ValidarFiltroOperaciones(filtro);

        var fechaDesde = filtro.FechaDesde?.Date;
        var fechaHasta = filtro.FechaHasta?.Date;
        var categoria = string.IsNullOrWhiteSpace(filtro.Categoria)
            ? null
            : filtro.Categoria.Trim();
        var busqueda = string.IsNullOrWhiteSpace(filtro.Busqueda)
            ? null
            : filtro.Busqueda.Trim();
        var limite = NormalizarLimite(filtro.Cantidad);

        var consulta = contexto.BitacoraAuditoria
            .AsNoTracking()
            .Where(registro =>
                registro.Entidad == EntidadActivo
                || registro.Entidad == EntidadProyecto
                || registro.Entidad == EntidadAsignacion
                || registro.Entidad == EntidadMantenimiento
                || registro.Entidad == EntidadRegistroUsoActivo);

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(registro =>
                registro.FechaHora >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue
            && fechaHasta.Value < DateTime.MaxValue.Date)
        {
            var fechaHastaExclusiva =
                fechaHasta.Value.AddDays(1);
            consulta = consulta.Where(registro =>
                registro.FechaHora < fechaHastaExclusiva);
        }

        consulta = categoria switch
        {
            CategoriasBitacoraOperaciones.Activo =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadRegistroUsoActivo
                    || (registro.Entidad == EntidadActivo
                        && registro.Accion
                            != AccionAsignacionHistorica
                        && registro.Accion
                            != AccionMantenimientoHistorica)),
            CategoriasBitacoraOperaciones.Proyecto =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadProyecto),
            CategoriasBitacoraOperaciones.Asignacion =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadAsignacion
                    || (registro.Entidad == EntidadActivo
                        && registro.Accion
                            == AccionAsignacionHistorica)),
            CategoriasBitacoraOperaciones.Mantenimiento =>
                consulta.Where(registro =>
                    registro.Entidad == EntidadMantenimiento
                    || (registro.Entidad == EntidadActivo
                        && registro.Accion
                            == AccionMantenimientoHistorica)),
            _ => consulta
        };

        var operaciones = consulta
            .GroupJoin(
                contexto.Users.AsNoTracking(),
                registro => registro.IdUsuario,
                usuario => usuario.Id,
                (registro, usuarios) => new
                {
                    Registro = registro,
                    Usuario = usuarios.FirstOrDefault()
                })
            .AsQueryable();

        if (busqueda is not null)
        {
            operaciones = operaciones.Where(item =>
                item.Registro.Accion.Contains(busqueda)
                || item.Registro.Entidad.Contains(busqueda)
                || (item.Registro.IdRegistro != null
                    && item.Registro.IdRegistro.Contains(busqueda))
                || (item.Registro.Observacion != null
                    && item.Registro.Observacion.Contains(busqueda))
                || (item.Usuario != null
                    && item.Usuario.UserName != null
                    && item.Usuario.UserName.Contains(busqueda))
                || (item.Usuario != null
                    && item.Usuario.Email != null
                    && item.Usuario.Email.Contains(busqueda)));
        }

        return await operaciones
            .OrderByDescending(item =>
                item.Registro.FechaHora)
            .ThenByDescending(item =>
                item.Registro.IdBitacoraAuditoria)
            .Take(limite)
            .Select(item => new OperacionBitacoraResumen
            {
                IdBitacoraAuditoria =
                    item.Registro.IdBitacoraAuditoria,
                FechaHora = item.Registro.FechaHora,
                Categoria =
                    item.Registro.Entidad
                        == EntidadRegistroUsoActivo
                        ? CategoriasBitacoraOperaciones
                            .Activo
                        : item.Registro.Entidad
                            == EntidadAsignacion
                    || (item.Registro.Entidad
                            == EntidadActivo
                        && item.Registro.Accion
                            == AccionAsignacionHistorica)
                        ? CategoriasBitacoraOperaciones
                            .Asignacion
                        : item.Registro.Entidad
                                == EntidadMantenimiento
                            || (item.Registro.Entidad
                                    == EntidadActivo
                                && item.Registro.Accion
                                    == AccionMantenimientoHistorica)
                            ? CategoriasBitacoraOperaciones
                                .Mantenimiento
                            : item.Registro.Entidad
                                == EntidadProyecto
                                ? CategoriasBitacoraOperaciones
                                    .Proyecto
                                : CategoriasBitacoraOperaciones
                                    .Activo,
                Entidad = item.Registro.Entidad,
                Accion = item.Registro.Accion,
                IdRegistro = item.Registro.IdRegistro,
                IdUsuario = item.Registro.IdUsuario,
                Usuario = item.Usuario == null
                    ? null
                    : item.Usuario.UserName,
                CorreoElectronico = item.Usuario == null
                    ? null
                    : item.Usuario.Email,
                ValoresAnteriores =
                    item.Registro.ValoresAnteriores,
                ValoresNuevos =
                    item.Registro.ValoresNuevos,
                Observacion = item.Registro.Observacion
            })
            .ToListAsync(cancellationToken);
    }

    private static int NormalizarLimite(int cantidad)
    {
        return cantidad is <= 0 or > 250 ? 100 : cantidad;
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

    private static void ValidarFiltroOperaciones(
        FiltroBitacoraOperaciones filtro)
    {
        var resultados = new List<ValidationResult>();
        if (Validator.TryValidateObject(
            filtro,
            new ValidationContext(filtro),
            resultados,
            validateAllProperties: true))
        {
            return;
        }

        throw new ArgumentException(
            resultados[0].ErrorMessage
                ?? "Los filtros de la bitácora no son válidos.");
    }

    private static void ValidarFiltroRecursosHumanos(
        FiltroBitacoraRecursosHumanos filtro)
    {
        var resultados = new List<ValidationResult>();
        if (Validator.TryValidateObject(
            filtro,
            new ValidationContext(filtro),
            resultados,
            validateAllProperties: true))
        {
            return;
        }

        throw new ArgumentException(
            resultados[0].ErrorMessage
                ?? "Los filtros de la bitácora no son válidos.");
    }
}

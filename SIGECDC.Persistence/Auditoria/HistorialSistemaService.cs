using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Auditoria;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Auditoria;

public sealed class HistorialSistemaService(ApplicationDbContext contexto) : IHistorialSistemaService
{
    private const string EntidadAutenticacion = "Autenticacion";

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
}

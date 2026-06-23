using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.SitioPublico;

public sealed class ConsultaContactoService(ApplicationDbContext contexto) : IConsultaContactoService
{
    private static readonly IReadOnlyList<EstadoConsultaContactoOpcion> OpcionesEstadoConsulta =
    [
        new(EstadosConsultaContacto.Nueva, "Nueva"),
        new(EstadosConsultaContacto.EnRevision, "En revisión"),
        new(EstadosConsultaContacto.Atendida, "Atendida"),
        new(EstadosConsultaContacto.Descartada, "Descartada")
    ];

    public async Task RegistrarConsultaAsync(SolicitudConsultaContacto solicitud, CancellationToken cancellationToken = default)
    {
        var consulta = new ConsultaContacto
        {
            Nombre = solicitud.Nombre.Trim(),
            CorreoElectronico = solicitud.CorreoElectronico.Trim(),
            Telefono = string.IsNullOrWhiteSpace(solicitud.Telefono) ? null : solicitud.Telefono.Trim(),
            Asunto = string.IsNullOrWhiteSpace(solicitud.Asunto) ? null : solicitud.Asunto.Trim(),
            Mensaje = solicitud.Mensaje.Trim(),
            FechaEnvio = DateTime.Now,
            EstadoConsulta = EstadosConsultaContacto.Nueva,
            EstadoRegistro = EstadosRegistro.Activo
        };

        contexto.ConsultasContacto.Add(consulta);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConsultaContactoResumen>> ObtenerConsultasRecientesAsync(int cantidad = 50, CancellationToken cancellationToken = default)
    {
        var limite = cantidad <= 0 ? 50 : cantidad;

        return await contexto.ConsultasContacto
            .AsNoTracking()
            .Where(consulta => consulta.EstadoRegistro == EstadosRegistro.Activo)
            .OrderByDescending(consulta => consulta.FechaEnvio)
            .Take(limite)
            .Select(consulta => new ConsultaContactoResumen
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

    public IReadOnlyList<EstadoConsultaContactoOpcion> ObtenerEstadosConsulta()
    {
        return OpcionesEstadoConsulta;
    }

    public async Task ActualizarEstadoConsultaAsync(long idConsultaContacto, string estadoConsulta, CancellationToken cancellationToken = default)
    {
        var estadoNormalizado = estadoConsulta.Trim();

        if (!EstadosConsultaContacto.EsValido(estadoNormalizado))
        {
            throw new ArgumentException("El estado indicado no es válido para una consulta de contacto.", nameof(estadoConsulta));
        }

        var consulta = await contexto.ConsultasContacto
            .FirstOrDefaultAsync(
                consulta => consulta.IdConsultaContacto == idConsultaContacto
                    && consulta.EstadoRegistro == EstadosRegistro.Activo,
                cancellationToken);

        if (consulta is null)
        {
            throw new InvalidOperationException("No se encontró una consulta de contacto activa para actualizar.");
        }

        consulta.EstadoConsulta = estadoNormalizado;
        await contexto.SaveChangesAsync(cancellationToken);
    }
}

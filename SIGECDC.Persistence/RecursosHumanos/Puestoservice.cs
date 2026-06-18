using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class PuestoService(ApplicationDbContext contexto) : IPuestoService
{
    public async Task RegistrarAsync(SolicitudRegistrarPuesto solicitud, CancellationToken cancellationToken = default)
    {
        var puesto = new Puesto
        {
            Nombre = solicitud.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(solicitud.Descripcion) ? null : solicitud.Descripcion.Trim(),
            EstadoRegistro = EstadosRegistro.Activo
        };

        contexto.Puestos.Add(puesto);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(SolicitudActualizarPuesto solicitud, CancellationToken cancellationToken = default)
    {
        var puesto = await contexto.Puestos
            .FirstOrDefaultAsync(p => p.IdPuesto == solicitud.IdPuesto, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró el puesto con Id {solicitud.IdPuesto}.");

        puesto.Nombre = solicitud.Nombre.Trim();
        puesto.Descripcion = string.IsNullOrWhiteSpace(solicitud.Descripcion) ? null : solicitud.Descripcion.Trim();

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarAsync(long idPuesto, CancellationToken cancellationToken = default)
    {
        var puesto = await contexto.Puestos
            .FirstOrDefaultAsync(p => p.IdPuesto == idPuesto, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró el puesto con Id {idPuesto}.");

        puesto.EstadoRegistro = EstadosRegistro.Activo;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(long idPuesto, CancellationToken cancellationToken = default)
    {
        var puesto = await contexto.Puestos
            .FirstOrDefaultAsync(p => p.IdPuesto == idPuesto, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró el puesto con Id {idPuesto}.");

        puesto.EstadoRegistro = EstadosRegistro.Inactivo;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PuestoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Puestos
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new PuestoResumen
            {
                IdPuesto = p.IdPuesto,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                TotalColaboradores = p.Colaboradores.Count(c => c.EstadoRegistro == EstadosRegistro.Activo),
                EstadoRegistro = p.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PuestoResumen?> ObtenerPorIdAsync(long idPuesto, CancellationToken cancellationToken = default)
    {
        return await contexto.Puestos
            .AsNoTracking()
            .Where(p => p.IdPuesto == idPuesto)
            .Select(p => new PuestoResumen
            {
                IdPuesto = p.IdPuesto,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                TotalColaboradores = p.Colaboradores.Count(c => c.EstadoRegistro == EstadosRegistro.Activo),
                EstadoRegistro = p.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
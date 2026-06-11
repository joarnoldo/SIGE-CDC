using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class DepartamentoService(ApplicationDbContext contexto) : IDepartamentoService
{
    public async Task RegistrarAsync(SolicitudRegistrarDepartamento solicitud, CancellationToken cancellationToken = default)
    {
        var departamento = new Departamento
        {
            Nombre = solicitud.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(solicitud.Descripcion) ? null : solicitud.Descripcion.Trim(),
            EstadoRegistro = EstadosRegistro.Activo
        };

        contexto.Departamentos.Add(departamento);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(SolicitudActualizarDepartamento solicitud, CancellationToken cancellationToken = default)
    {
        var departamento = await contexto.Departamentos
            .FirstOrDefaultAsync(d => d.IdDepartamento == solicitud.IdDepartamento, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró el departamento con Id {solicitud.IdDepartamento}.");

        departamento.Nombre = solicitud.Nombre.Trim();
        departamento.Descripcion = string.IsNullOrWhiteSpace(solicitud.Descripcion) ? null : solicitud.Descripcion.Trim();

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(long idDepartamento, CancellationToken cancellationToken = default)
    {
        var departamento = await contexto.Departamentos
            .FirstOrDefaultAsync(d => d.IdDepartamento == idDepartamento, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró el departamento con Id {idDepartamento}.");

        departamento.EstadoRegistro = EstadosRegistro.Inactivo;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DepartamentoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Departamentos
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoResumen
            {
                IdDepartamento = d.IdDepartamento,
                Nombre = d.Nombre,
                Descripcion = d.Descripcion,
                TotalColaboradores = d.Colaboradores.Count(c => c.EstadoRegistro == EstadosRegistro.Activo),
                EstadoRegistro = d.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartamentoResumen?> ObtenerPorIdAsync(long idDepartamento, CancellationToken cancellationToken = default)
    {
        return await contexto.Departamentos
            .AsNoTracking()
            .Where(d => d.IdDepartamento == idDepartamento)
            .Select(d => new DepartamentoResumen
            {
                IdDepartamento = d.IdDepartamento,
                Nombre = d.Nombre,
                Descripcion = d.Descripcion,
                TotalColaboradores = d.Colaboradores.Count(c => c.EstadoRegistro == EstadosRegistro.Activo),
                EstadoRegistro = d.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
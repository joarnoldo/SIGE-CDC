using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class ColaboradorService(ApplicationDbContext contexto) : IColaboradorService
{
    public async Task AsignarPuestoYDepartamentoAsync(SolicitudAsignarColaborador solicitud, CancellationToken cancellationToken = default)
    {
        var colaborador = await contexto.Colaboradores
            .FirstOrDefaultAsync(c => c.IdColaborador == solicitud.IdColaborador, cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró el colaborador con Id {solicitud.IdColaborador}.");

        var departamentoExiste = await contexto.Departamentos
            .AnyAsync(d => d.IdDepartamento == solicitud.IdDepartamento && d.EstadoRegistro == EstadosRegistro.Activo, cancellationToken);

        if (!departamentoExiste)
            throw new InvalidOperationException($"El departamento con Id {solicitud.IdDepartamento} no existe o está inactivo.");

        var puestoExiste = await contexto.Puestos
            .AnyAsync(p => p.IdPuesto == solicitud.IdPuesto && p.EstadoRegistro == EstadosRegistro.Activo, cancellationToken);

        if (!puestoExiste)
            throw new InvalidOperationException($"El puesto con Id {solicitud.IdPuesto} no existe o está inactivo.");

        colaborador.IdDepartamento = solicitud.IdDepartamento;
        colaborador.IdPuesto = solicitud.IdPuesto;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ColaboradorResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Colaboradores
            .AsNoTracking()
            .Include(c => c.Departamento)
            .Include(c => c.Puesto)
            .OrderBy(c => c.PrimerApellido)
            .ThenBy(c => c.Nombre)
            .Select(c => new ColaboradorResumen
            {
                IdColaborador = c.IdColaborador,
                NombreCompleto = $"{c.Nombre} {c.PrimerApellido} {c.SegundoApellido}".Trim(),
                CorreoElectronico = c.CorreoElectronico ?? string.Empty,
                Telefono = c.Telefono,
                Departamento = c.Departamento != null ? c.Departamento.Nombre : null,
                Puesto = c.Puesto != null ? c.Puesto.Nombre : null,
                EstadoRegistro = c.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ColaboradorResumen?> ObtenerPorIdAsync(long idColaborador, CancellationToken cancellationToken = default)
    {
        return await contexto.Colaboradores
            .AsNoTracking()
            .Include(c => c.Departamento)
            .Include(c => c.Puesto)
            .Where(c => c.IdColaborador == idColaborador)
            .Select(c => new ColaboradorResumen
            {
                IdColaborador = c.IdColaborador,
                NombreCompleto = $"{c.Nombre} {c.PrimerApellido} {c.SegundoApellido}".Trim(),
                CorreoElectronico = c.CorreoElectronico ?? string.Empty,
                Telefono = c.Telefono,
                Departamento = c.Departamento != null ? c.Departamento.Nombre : null,
                Puesto = c.Puesto != null ? c.Puesto.Nombre : null,
                EstadoRegistro = c.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
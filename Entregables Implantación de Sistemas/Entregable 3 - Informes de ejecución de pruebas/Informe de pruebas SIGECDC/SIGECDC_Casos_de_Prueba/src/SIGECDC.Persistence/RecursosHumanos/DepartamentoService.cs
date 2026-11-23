using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class DepartamentoService(ApplicationDbContext contexto) : IDepartamentoService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";

    public async Task RegistrarAsync(SolicitudRegistrarDepartamento solicitud, CancellationToken cancellationToken = default)
    {
        var nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre del departamento es obligatorio.");
        await ValidarNombreUnicoAsync(nombre, null, cancellationToken);

        var departamento = new Departamento
        {
            Nombre = nombre,
            Descripcion = LimpiarOpcional(solicitud.Descripcion),
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.Departamentos.Add(departamento);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(SolicitudActualizarDepartamento solicitud, CancellationToken cancellationToken = default)
    {
        var departamento = await contexto.Departamentos
            .FirstOrDefaultAsync(departamento => departamento.IdDepartamento == solicitud.IdDepartamento, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el departamento solicitado.");

        var nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre del departamento es obligatorio.");
        await ValidarNombreUnicoAsync(nombre, solicitud.IdDepartamento, cancellationToken);

        departamento.Nombre = nombre;
        departamento.Descripcion = LimpiarOpcional(solicitud.Descripcion);

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarAsync(int idDepartamento, CancellationToken cancellationToken = default)
    {
        var departamento = await ObtenerDepartamentoEditableAsync(idDepartamento, cancellationToken);
        departamento.EstadoRegistro = EstadoRegistroActivo;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(int idDepartamento, CancellationToken cancellationToken = default)
    {
        var departamento = await ObtenerDepartamentoEditableAsync(idDepartamento, cancellationToken);
        departamento.EstadoRegistro = EstadoRegistroInactivo;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DepartamentoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Departamentos
            .AsNoTracking()
            .OrderBy(departamento => departamento.Nombre)
            .Select(departamento => new DepartamentoResumen
            {
                IdDepartamento = departamento.IdDepartamento,
                Nombre = departamento.Nombre,
                Descripcion = departamento.Descripcion,
                TotalColaboradores = contexto.Colaboradores.Count(colaborador =>
                    colaborador.IdDepartamento == departamento.IdDepartamento
                    && colaborador.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = departamento.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartamentoResumen?> ObtenerPorIdAsync(int idDepartamento, CancellationToken cancellationToken = default)
    {
        return await contexto.Departamentos
            .AsNoTracking()
            .Where(departamento => departamento.IdDepartamento == idDepartamento)
            .Select(departamento => new DepartamentoResumen
            {
                IdDepartamento = departamento.IdDepartamento,
                Nombre = departamento.Nombre,
                Descripcion = departamento.Descripcion,
                TotalColaboradores = contexto.Colaboradores.Count(colaborador =>
                    colaborador.IdDepartamento == departamento.IdDepartamento
                    && colaborador.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = departamento.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Departamento> ObtenerDepartamentoEditableAsync(int idDepartamento, CancellationToken cancellationToken)
    {
        return await contexto.Departamentos
            .FirstOrDefaultAsync(departamento => departamento.IdDepartamento == idDepartamento, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el departamento solicitado.");
    }

    private async Task ValidarNombreUnicoAsync(string nombre, int? idDepartamentoActual, CancellationToken cancellationToken)
    {
        var existe = await contexto.Departamentos
            .AnyAsync(departamento =>
                departamento.Nombre == nombre
                && departamento.IdDepartamento != idDepartamentoActual,
                cancellationToken);

        if (existe)
        {
            throw new InvalidOperationException("Ya existe un departamento con el nombre indicado.");
        }
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
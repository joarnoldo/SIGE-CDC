using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class PuestoService(ApplicationDbContext contexto) : IPuestoService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";

    public async Task RegistrarAsync(SolicitudRegistrarPuesto solicitud, CancellationToken cancellationToken = default)
    {
        var nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre del puesto es obligatorio.");
        await ValidarDepartamentoAsync(solicitud.IdDepartamento, cancellationToken);
        await ValidarNombreUnicoAsync(nombre, null, cancellationToken);

        var puesto = new Puesto
        {
            IdDepartamento = solicitud.IdDepartamento,
            Nombre = nombre,
            Descripcion = LimpiarOpcional(solicitud.Descripcion),
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.Puestos.Add(puesto);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(SolicitudActualizarPuesto solicitud, CancellationToken cancellationToken = default)
    {
        var puesto = await contexto.Puestos
            .FirstOrDefaultAsync(puesto => puesto.IdPuesto == solicitud.IdPuesto, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el puesto solicitado.");

        var nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre del puesto es obligatorio.");
        await ValidarDepartamentoAsync(solicitud.IdDepartamento, cancellationToken);
        await ValidarNombreUnicoAsync(nombre, solicitud.IdPuesto, cancellationToken);

        puesto.IdDepartamento = solicitud.IdDepartamento;
        puesto.Nombre = nombre;
        puesto.Descripcion = LimpiarOpcional(solicitud.Descripcion);

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarAsync(int idPuesto, CancellationToken cancellationToken = default)
    {
        var puesto = await ObtenerPuestoEditableAsync(idPuesto, cancellationToken);
        puesto.EstadoRegistro = EstadoRegistroActivo;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarAsync(int idPuesto, CancellationToken cancellationToken = default)
    {
        var puesto = await ObtenerPuestoEditableAsync(idPuesto, cancellationToken);
        puesto.EstadoRegistro = EstadoRegistroInactivo;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PuestoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Puestos
            .AsNoTracking()
            .Include(puesto => puesto.Departamento)
            .OrderBy(puesto => puesto.Nombre)
            .Select(puesto => new PuestoResumen
            {
                IdPuesto = puesto.IdPuesto,
                IdDepartamento = puesto.IdDepartamento,
                Departamento = puesto.Departamento != null ? puesto.Departamento.Nombre : null,
                Nombre = puesto.Nombre,
                Descripcion = puesto.Descripcion,
                TotalColaboradores = contexto.Colaboradores.Count(colaborador =>
                    colaborador.IdPuesto == puesto.IdPuesto
                    && colaborador.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = puesto.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PuestoResumen?> ObtenerPorIdAsync(int idPuesto, CancellationToken cancellationToken = default)
    {
        return await contexto.Puestos
            .AsNoTracking()
            .Include(puesto => puesto.Departamento)
            .Where(puesto => puesto.IdPuesto == idPuesto)
            .Select(puesto => new PuestoResumen
            {
                IdPuesto = puesto.IdPuesto,
                IdDepartamento = puesto.IdDepartamento,
                Departamento = puesto.Departamento != null ? puesto.Departamento.Nombre : null,
                Nombre = puesto.Nombre,
                Descripcion = puesto.Descripcion,
                TotalColaboradores = contexto.Colaboradores.Count(colaborador =>
                    colaborador.IdPuesto == puesto.IdPuesto
                    && colaborador.EstadoRegistro == EstadoRegistroActivo),
                EstadoRegistro = puesto.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Puesto> ObtenerPuestoEditableAsync(int idPuesto, CancellationToken cancellationToken)
    {
        return await contexto.Puestos
            .FirstOrDefaultAsync(puesto => puesto.IdPuesto == idPuesto, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el puesto solicitado.");
    }

    private async Task ValidarDepartamentoAsync(int? idDepartamento, CancellationToken cancellationToken)
    {
        if (idDepartamento is null)
        {
            return;
        }

        var existe = await contexto.Departamentos
            .AnyAsync(departamento =>
                departamento.IdDepartamento == idDepartamento.Value
                && departamento.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!existe)
        {
            throw new ArgumentException("El departamento seleccionado no esta disponible.");
        }
    }

    private async Task ValidarNombreUnicoAsync(string nombre, int? idPuestoActual, CancellationToken cancellationToken)
    {
        var existe = await contexto.Puestos
            .AnyAsync(puesto =>
                puesto.Nombre == nombre
                && puesto.IdPuesto != idPuestoActual,
                cancellationToken);

        if (existe)
        {
            throw new InvalidOperationException("Ya existe un puesto con el nombre indicado.");
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
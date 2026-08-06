using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class ColaboradorService(ApplicationDbContext contexto) : IColaboradorService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string RolEmpleado = "Empleado";
    private const string EntidadAuditoria = "Colaborador";

    public async Task<IReadOnlyList<ColaboradorResumen>> ObtenerColaboradoresAsync(
        string? busqueda = null,
        int? idEstadoLaboral = null,
        int? idDepartamento = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = contexto.Colaboradores
            .AsNoTracking()
            .Include(colaborador => colaborador.EstadoLaboral)
            .Include(colaborador => colaborador.Departamento)
            .Include(colaborador => colaborador.Puesto)
            .Where(colaborador => colaborador.EstadoRegistro == EstadoRegistroActivo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var filtro = busqueda.Trim();

            consulta = consulta.Where(colaborador =>
                colaborador.CodigoColaborador.Contains(filtro)
                || colaborador.Identificacion.Contains(filtro)
                || colaborador.Nombre.Contains(filtro)
                || colaborador.PrimerApellido.Contains(filtro)
                || (colaborador.SegundoApellido != null && colaborador.SegundoApellido.Contains(filtro))
                || (colaborador.CorreoElectronico != null && colaborador.CorreoElectronico.Contains(filtro))
                || (colaborador.Departamento != null && colaborador.Departamento.Nombre.Contains(filtro))
                || (colaborador.Puesto != null && colaborador.Puesto.Nombre.Contains(filtro)));
        }

        if (idEstadoLaboral is > 0)
        {
            consulta = consulta.Where(colaborador => colaborador.IdEstadoLaboral == idEstadoLaboral.Value);
        }

        if (idDepartamento is > 0)
        {
            consulta = consulta.Where(colaborador => colaborador.IdDepartamento == idDepartamento.Value);
        }

        var colaboradores = await consulta
            .OrderBy(colaborador => colaborador.PrimerApellido)
            .ThenBy(colaborador => colaborador.SegundoApellido)
            .ThenBy(colaborador => colaborador.Nombre)
            .Select(colaborador => new
            {
                colaborador.IdColaborador,
                colaborador.CodigoColaborador,
                colaborador.Identificacion,
                colaborador.Nombre,
                colaborador.PrimerApellido,
                colaborador.SegundoApellido,
                colaborador.CorreoElectronico,
                colaborador.Telefono,
                colaborador.FechaIngreso,
                colaborador.FechaSalida,
                colaborador.IdEstadoLaboral,
                EstadoLaboral = colaborador.EstadoLaboral != null ? colaborador.EstadoLaboral.Nombre : string.Empty,
                colaborador.IdDepartamento,
                Departamento = colaborador.Departamento != null ? colaborador.Departamento.Nombre : string.Empty,
                colaborador.IdPuesto,
                Puesto = colaborador.Puesto != null ? colaborador.Puesto.Nombre : string.Empty
            })
            .ToListAsync(cancellationToken);

        return colaboradores
            .Select(colaborador => new ColaboradorResumen
            {
                IdColaborador = colaborador.IdColaborador,
                CodigoColaborador = colaborador.CodigoColaborador,
                Identificacion = colaborador.Identificacion,
                NombreCompleto = ConstruirNombreCompleto(colaborador.Nombre, colaborador.PrimerApellido, colaborador.SegundoApellido),
                CorreoElectronico = colaborador.CorreoElectronico,
                Telefono = colaborador.Telefono,
                FechaIngreso = colaborador.FechaIngreso,
                FechaSalida = colaborador.FechaSalida,
                IdEstadoLaboral = colaborador.IdEstadoLaboral,
                EstadoLaboral = colaborador.EstadoLaboral,
                IdDepartamento = colaborador.IdDepartamento,
                Departamento = colaborador.Departamento,
                IdPuesto = colaborador.IdPuesto,
                Puesto = colaborador.Puesto
            })
            .ToList();
    }

    public async Task<ColaboradorDetalle?> ObtenerColaboradorPorIdAsync(long idColaborador, CancellationToken cancellationToken = default)
    {
        return await contexto.Colaboradores
            .AsNoTracking()
            .Include(colaborador => colaborador.EstadoLaboral)
            .Include(colaborador => colaborador.Departamento)
            .Include(colaborador => colaborador.Puesto)
            .Where(colaborador => colaborador.IdColaborador == idColaborador
                && colaborador.EstadoRegistro == EstadoRegistroActivo)
            .Select(colaborador => new ColaboradorDetalle
            {
                IdColaborador = colaborador.IdColaborador,
                CodigoColaborador = colaborador.CodigoColaborador,
                TipoIdentificacion = colaborador.TipoIdentificacion,
                Identificacion = colaborador.Identificacion,
                Nombre = colaborador.Nombre,
                PrimerApellido = colaborador.PrimerApellido,
                SegundoApellido = colaborador.SegundoApellido,
                FechaNacimiento = colaborador.FechaNacimiento,
                CorreoElectronico = colaborador.CorreoElectronico,
                Telefono = colaborador.Telefono,
                Direccion = colaborador.Direccion,
                FechaIngreso = colaborador.FechaIngreso,
                FechaSalida = colaborador.FechaSalida,
                IdEstadoLaboral = colaborador.IdEstadoLaboral,
                EstadoLaboral = colaborador.EstadoLaboral != null ? colaborador.EstadoLaboral.Nombre : string.Empty,
                IdDepartamento = colaborador.IdDepartamento,
                Departamento = colaborador.Departamento != null ? colaborador.Departamento.Nombre : string.Empty,
                IdPuesto = colaborador.IdPuesto,
                Puesto = colaborador.Puesto != null ? colaborador.Puesto.Nombre : string.Empty,
                IdUsuario = colaborador.IdUsuario,
                CuentaUsuario = contexto.Users
                    .Where(usuario => usuario.Id == colaborador.IdUsuario)
                    .Select(usuario => usuario.UserName ?? usuario.Email ?? usuario.Id)
                    .FirstOrDefault(),
                Observaciones = colaborador.Observaciones
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerEstadosLaboralesAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.EstadosLaborales
            .AsNoTracking()
            .Where(estado => estado.EstadoRegistro == EstadoRegistroActivo)
            .OrderBy(estado => estado.Nombre)
            .Select(estado => new OpcionCatalogo(estado.IdEstadoLaboral, estado.Nombre, estado.Descripcion))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerDepartamentosAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.Departamentos
            .AsNoTracking()
            .Where(departamento => departamento.EstadoRegistro == EstadoRegistroActivo)
            .OrderBy(departamento => departamento.Nombre)
            .Select(departamento => new OpcionCatalogo(departamento.IdDepartamento, departamento.Nombre, departamento.Descripcion))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerPuestosAsync(int? idDepartamento = null, CancellationToken cancellationToken = default)
    {
        var consulta = contexto.Puestos
            .AsNoTracking()
            .Where(puesto => puesto.EstadoRegistro == EstadoRegistroActivo);

        if (idDepartamento is > 0)
        {
            consulta = consulta.Where(puesto => puesto.IdDepartamento == idDepartamento.Value || puesto.IdDepartamento == null);
        }

        return await consulta
            .OrderBy(puesto => puesto.Nombre)
            .Select(puesto => new OpcionCatalogo(puesto.IdPuesto, puesto.Nombre, puesto.Descripcion))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CuentaEmpleadoOpcion>> ObtenerCuentasEmpleadoDisponiblesAsync(
        long idColaborador,
        CancellationToken cancellationToken = default)
    {
        var colaboradorExiste = await contexto.Colaboradores
            .AsNoTracking()
            .AnyAsync(colaborador => colaborador.IdColaborador == idColaborador
                && colaborador.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!colaboradorExiste)
        {
            throw new InvalidOperationException("No se encontró el colaborador solicitado.");
        }

        var idsUsuariosEmpleado = contexto.UserRoles
            .Join(
                contexto.Roles,
                usuarioRol => usuarioRol.RoleId,
                rol => rol.Id,
                (usuarioRol, rol) => new { usuarioRol.UserId, rol.Name })
            .Where(registro => registro.Name == RolEmpleado)
            .Select(registro => registro.UserId);

        return await contexto.Users
            .AsNoTracking()
            .Where(usuario => usuario.EstadoRegistro == EstadoRegistroActivo
                && idsUsuariosEmpleado.Contains(usuario.Id)
                && !contexto.Colaboradores.Any(colaborador =>
                    colaborador.IdUsuario == usuario.Id
                    && colaborador.IdColaborador != idColaborador))
            .OrderBy(usuario => usuario.UserName)
            .Select(usuario => new CuentaEmpleadoOpcion(
                usuario.Id,
                usuario.UserName ?? usuario.Email ?? usuario.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> RegistrarColaboradorAsync(
        SolicitudColaborador solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        var datos = await ValidarSolicitudAsync(solicitud, null, cancellationToken);

        var colaborador = new Colaborador
        {
            CodigoColaborador = datos.CodigoColaborador,
            TipoIdentificacion = datos.TipoIdentificacion,
            Identificacion = datos.Identificacion,
            Nombre = datos.Nombre,
            PrimerApellido = datos.PrimerApellido,
            SegundoApellido = datos.SegundoApellido,
            FechaNacimiento = datos.FechaNacimiento,
            CorreoElectronico = datos.CorreoElectronico,
            Telefono = datos.Telefono,
            Direccion = datos.Direccion,
            FechaIngreso = datos.FechaIngreso,
            FechaSalida = datos.FechaSalida,
            IdEstadoLaboral = solicitud.IdEstadoLaboral,
            IdDepartamento = solicitud.IdDepartamento,
            IdPuesto = solicitud.IdPuesto,
            Observaciones = datos.Observaciones,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuario,
            EstadoRegistro = EstadoRegistroActivo
        };

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            contexto.Colaboradores.Add(colaborador);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = colaborador.FechaCreacion,
                Accion = "CREAR_COLABORADOR",
                Entidad = EntidadAuditoria,
                IdRegistro = colaborador.IdColaborador.ToString(),
                ValoresNuevos = SerializarSnapshot(colaborador),
                Observacion = "Expediente de colaborador creado; los valores personales protegidos no se almacenan en la bitácora."
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return colaborador.IdColaborador;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task ActualizarColaboradorAsync(
        long idColaborador,
        SolicitudColaborador solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        var colaborador = await contexto.Colaboradores
            .FirstOrDefaultAsync(
                colaborador => colaborador.IdColaborador == idColaborador
                    && colaborador.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (colaborador is null)
        {
            throw new InvalidOperationException("No se encontro el colaborador solicitado.");
        }

        var datos = await ValidarSolicitudAsync(solicitud, idColaborador, cancellationToken);
        var valoresAnteriores = SerializarSnapshot(colaborador);
        var camposProtegidosModificados = ObtenerCamposProtegidosModificados(colaborador, datos);

        colaborador.CodigoColaborador = datos.CodigoColaborador;
        colaborador.TipoIdentificacion = datos.TipoIdentificacion;
        colaborador.Identificacion = datos.Identificacion;
        colaborador.Nombre = datos.Nombre;
        colaborador.PrimerApellido = datos.PrimerApellido;
        colaborador.SegundoApellido = datos.SegundoApellido;
        colaborador.FechaNacimiento = datos.FechaNacimiento;
        colaborador.CorreoElectronico = datos.CorreoElectronico;
        colaborador.Telefono = datos.Telefono;
        colaborador.Direccion = datos.Direccion;
        colaborador.FechaIngreso = datos.FechaIngreso;
        colaborador.FechaSalida = datos.FechaSalida;
        colaborador.IdEstadoLaboral = solicitud.IdEstadoLaboral;
        colaborador.IdDepartamento = solicitud.IdDepartamento;
        colaborador.IdPuesto = solicitud.IdPuesto;
        colaborador.Observaciones = datos.Observaciones;
        colaborador.FechaModificacion = DateTime.Now;
        colaborador.ModificadoPor = idUsuario;

        contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = idUsuario,
            FechaHora = colaborador.FechaModificacion.Value,
            Accion = "ACTUALIZAR_COLABORADOR",
            Entidad = EntidadAuditoria,
            IdRegistro = colaborador.IdColaborador.ToString(),
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = SerializarSnapshot(colaborador, camposProtegidosModificados),
            Observacion = "Expediente de colaborador actualizado; los valores personales protegidos no se almacenan en la bitácora."
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarColaboradorAsync(
        long idColaborador,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        var colaborador = await contexto.Colaboradores
            .FirstOrDefaultAsync(
                colaborador => colaborador.IdColaborador == idColaborador
                    && colaborador.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (colaborador is null)
        {
            throw new InvalidOperationException("No se encontro el colaborador solicitado.");
        }

        var estadoInactivo = await contexto.EstadosLaborales
            .AsNoTracking()
            .FirstOrDefaultAsync(
                estado => estado.Nombre == EstadosLaborales.Inactivo
                    && estado.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (estadoInactivo is null)
        {
            throw new InvalidOperationException("No existe un estado laboral Inactivo disponible.");
        }

        var valoresAnteriores = SerializarSnapshot(colaborador);
        colaborador.IdEstadoLaboral = estadoInactivo.IdEstadoLaboral;
        colaborador.FechaModificacion = DateTime.Now;
        colaborador.ModificadoPor = idUsuario;

        contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = idUsuario,
            FechaHora = colaborador.FechaModificacion.Value,
            Accion = "DESACTIVAR_COLABORADOR",
            Entidad = EntidadAuditoria,
            IdRegistro = colaborador.IdColaborador.ToString(),
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = SerializarSnapshot(colaborador),
            Observacion = "Colaborador marcado con el estado laboral Inactivo."
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task VincularCuentaEmpleadoAsync(
        long idColaborador,
        SolicitudVinculoCuentaColaborador solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var idUsuarioActor = LimpiarIdUsuarioObligatorio(idUsuarioActual);

        var colaborador = await contexto.Colaboradores
            .FirstOrDefaultAsync(registro => registro.IdColaborador == idColaborador
                && registro.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken)
            ?? throw new InvalidOperationException("No se encontró el colaborador solicitado.");

        var idUsuario = string.IsNullOrWhiteSpace(solicitud.IdUsuario)
            ? null
            : solicitud.IdUsuario.Trim();

        if (idUsuario is not null)
        {
            if (idUsuario.Length > 255)
            {
                throw new ArgumentException("La cuenta seleccionada no es válida.");
            }

            var cuentaValida = await contexto.Users
                .AsNoTracking()
                .AnyAsync(usuario => usuario.Id == idUsuario
                    && usuario.EstadoRegistro == EstadoRegistroActivo
                    && contexto.UserRoles.Any(usuarioRol => usuarioRol.UserId == usuario.Id
                        && contexto.Roles.Any(rol => rol.Id == usuarioRol.RoleId
                            && rol.Name == RolEmpleado)),
                    cancellationToken);

            if (!cuentaValida)
            {
                throw new InvalidOperationException(
                    "Solo se puede vincular una cuenta activa con el rol Empleado.");
            }

            var vinculadaAOtroColaborador = await contexto.Colaboradores
                .AsNoTracking()
                .AnyAsync(registro => registro.IdUsuario == idUsuario
                    && registro.IdColaborador != idColaborador,
                    cancellationToken);

            if (vinculadaAOtroColaborador)
            {
                throw new InvalidOperationException(
                    "La cuenta seleccionada ya está vinculada con otro colaborador.");
            }
        }

        var valoresAnteriores = SerializarSnapshot(colaborador);
        colaborador.IdUsuario = idUsuario;
        colaborador.FechaModificacion = DateTime.Now;
        colaborador.ModificadoPor = idUsuarioActor;

        contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = idUsuarioActor,
            FechaHora = colaborador.FechaModificacion.Value,
            Accion = idUsuario is null
                ? "DESVINCULAR_CUENTA_COLABORADOR"
                : "VINCULAR_CUENTA_COLABORADOR",
            Entidad = EntidadAuditoria,
            IdRegistro = colaborador.IdColaborador.ToString(),
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = SerializarSnapshot(colaborador),
            Observacion = idUsuario is null
                ? "Cuenta Empleado desvinculada del colaborador."
                : "Cuenta Empleado vinculada al colaborador."
        });

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excepcion) when (EsVinculoCuentaDuplicado(excepcion))
        {
            contexto.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "No fue posible vincular la cuenta porque ya está asignada a otro colaborador.",
                excepcion);
        }
    }

    public async Task AsignarPuestoYDepartamentoAsync(
        SolicitudAsignarColaborador solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);
        var colaborador = await contexto.Colaboradores
            .FirstOrDefaultAsync(
                colaborador => colaborador.IdColaborador == solicitud.IdColaborador
                    && colaborador.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el colaborador solicitado.");

        var departamentoExiste = await contexto.Departamentos
            .AnyAsync(
                departamento => departamento.IdDepartamento == solicitud.IdDepartamento
                    && departamento.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!departamentoExiste)
        {
            throw new ArgumentException("El departamento seleccionado no esta disponible.");
        }

        var puestoExiste = await contexto.Puestos
            .AnyAsync(
                puesto => puesto.IdPuesto == solicitud.IdPuesto
                    && puesto.EstadoRegistro == EstadoRegistroActivo
                    && (puesto.IdDepartamento == null || puesto.IdDepartamento == solicitud.IdDepartamento),
                cancellationToken);

        if (!puestoExiste)
        {
            throw new ArgumentException("El puesto seleccionado no esta disponible para el departamento indicado.");
        }

        var valoresAnteriores = SerializarSnapshot(colaborador);
        colaborador.IdDepartamento = solicitud.IdDepartamento;
        colaborador.IdPuesto = solicitud.IdPuesto;
        colaborador.FechaModificacion = DateTime.Now;
        colaborador.ModificadoPor = idUsuario;

        contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = idUsuario,
            FechaHora = colaborador.FechaModificacion.Value,
            Accion = "ASIGNAR_PUESTO_DEPARTAMENTO",
            Entidad = EntidadAuditoria,
            IdRegistro = colaborador.IdColaborador.ToString(),
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = SerializarSnapshot(colaborador),
            Observacion = "Puesto y departamento del colaborador actualizados."
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }
    private async Task<DatosColaboradorLimpios> ValidarSolicitudAsync(
        SolicitudColaborador solicitud,
        long? idColaboradorActual,
        CancellationToken cancellationToken)
    {
        var datos = DatosColaboradorLimpios.DesdeSolicitud(solicitud);

        if (datos.FechaSalida.HasValue && datos.FechaSalida.Value.Date < datos.FechaIngreso.Date)
        {
            throw new ArgumentException("La fecha de salida no puede ser anterior a la fecha de ingreso.");
        }

        if (datos.FechaNacimiento.HasValue && datos.FechaNacimiento.Value.Date > DateTime.Today)
        {
            throw new ArgumentException("La fecha de nacimiento no puede ser futura.");
        }

        await ValidarCatalogosAsync(solicitud, cancellationToken);
        await ValidarUnicosAsync(datos, idColaboradorActual, cancellationToken);

        return datos;
    }

    private async Task ValidarCatalogosAsync(SolicitudColaborador solicitud, CancellationToken cancellationToken)
    {
        var estadoExiste = await contexto.EstadosLaborales
            .AnyAsync(
                estado => estado.IdEstadoLaboral == solicitud.IdEstadoLaboral
                    && estado.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!estadoExiste)
        {
            throw new ArgumentException("El estado laboral seleccionado no esta disponible.");
        }

        var departamentoExiste = await contexto.Departamentos
            .AnyAsync(
                departamento => departamento.IdDepartamento == solicitud.IdDepartamento
                    && departamento.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!departamentoExiste)
        {
            throw new ArgumentException("El departamento seleccionado no esta disponible.");
        }

        var puestoExiste = await contexto.Puestos
            .AnyAsync(
                puesto => puesto.IdPuesto == solicitud.IdPuesto
                    && puesto.EstadoRegistro == EstadoRegistroActivo
                    && (puesto.IdDepartamento == null || puesto.IdDepartamento == solicitud.IdDepartamento),
                cancellationToken);

        if (!puestoExiste)
        {
            throw new ArgumentException("El puesto seleccionado no esta disponible para el departamento indicado.");
        }
    }

    private async Task ValidarUnicosAsync(
        DatosColaboradorLimpios datos,
        long? idColaboradorActual,
        CancellationToken cancellationToken)
    {
        var codigoExiste = await contexto.Colaboradores
            .AnyAsync(
                colaborador => colaborador.CodigoColaborador == datos.CodigoColaborador
                    && colaborador.IdColaborador != idColaboradorActual,
                cancellationToken);

        if (codigoExiste)
        {
            throw new InvalidOperationException("Ya existe un colaborador con el codigo indicado.");
        }

        var identificacionExiste = await contexto.Colaboradores
            .AnyAsync(
                colaborador => colaborador.Identificacion == datos.Identificacion
                    && colaborador.IdColaborador != idColaboradorActual,
                cancellationToken);

        if (identificacionExiste)
        {
            throw new InvalidOperationException("Ya existe un colaborador con la identificacion indicada.");
        }
    }

    private static bool EsVinculoCuentaDuplicado(DbUpdateException excepcion)
    {
        var detalle = excepcion.GetBaseException().Message;
        return detalle.Contains("UX_Colaborador_IdUsuario", StringComparison.OrdinalIgnoreCase)
            || detalle.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
    }

    private static string LimpiarIdUsuarioObligatorio(string idUsuarioActual)
    {
        if (string.IsNullOrWhiteSpace(idUsuarioActual))
        {
            throw new ArgumentException("No fue posible identificar al usuario responsable.");
        }

        var idUsuario = idUsuarioActual.Trim();
        if (idUsuario.Length > 255)
        {
            throw new ArgumentException("El identificador del usuario responsable no es válido.");
        }

        return idUsuario;
    }

    private static string SerializarSnapshot(
        Colaborador colaborador,
        IReadOnlyList<string>? camposProtegidosModificados = null)
    {
        return JsonSerializer.Serialize(new SnapshotAuditableColaborador(
            colaborador.CodigoColaborador,
            colaborador.FechaIngreso,
            colaborador.FechaSalida,
            colaborador.IdEstadoLaboral,
            colaborador.IdDepartamento,
            colaborador.IdPuesto,
            colaborador.IdUsuario,
            colaborador.EstadoRegistro,
            camposProtegidosModificados is { Count: > 0 }
                ? camposProtegidosModificados
                : null));
    }

    private static IReadOnlyList<string> ObtenerCamposProtegidosModificados(
        Colaborador colaborador,
        DatosColaboradorLimpios datos)
    {
        var campos = new List<string>();

        AgregarSiCambio(campos, "TipoIdentificacion", colaborador.TipoIdentificacion, datos.TipoIdentificacion);
        AgregarSiCambio(campos, "Identificacion", colaborador.Identificacion, datos.Identificacion);
        AgregarSiCambio(campos, "Nombre", colaborador.Nombre, datos.Nombre);
        AgregarSiCambio(campos, "PrimerApellido", colaborador.PrimerApellido, datos.PrimerApellido);
        AgregarSiCambio(campos, "SegundoApellido", colaborador.SegundoApellido, datos.SegundoApellido);
        AgregarSiCambio(campos, "FechaNacimiento", colaborador.FechaNacimiento, datos.FechaNacimiento);
        AgregarSiCambio(campos, "CorreoElectronico", colaborador.CorreoElectronico, datos.CorreoElectronico);
        AgregarSiCambio(campos, "Telefono", colaborador.Telefono, datos.Telefono);
        AgregarSiCambio(campos, "Direccion", colaborador.Direccion, datos.Direccion);
        AgregarSiCambio(campos, "Observaciones", colaborador.Observaciones, datos.Observaciones);

        return campos;
    }

    private static void AgregarSiCambio<T>(
        ICollection<string> campos,
        string nombreCampo,
        T valorAnterior,
        T valorNuevo)
    {
        if (!EqualityComparer<T>.Default.Equals(valorAnterior, valorNuevo))
        {
            campos.Add(nombreCampo);
        }
    }

    private static string ConstruirNombreCompleto(string nombre, string primerApellido, string? segundoApellido)
    {
        return string.IsNullOrWhiteSpace(segundoApellido)
            ? $"{nombre} {primerApellido}"
            : $"{nombre} {primerApellido} {segundoApellido}";
    }

    private sealed record SnapshotAuditableColaborador(
        string CodigoColaborador,
        DateTime FechaIngreso,
        DateTime? FechaSalida,
        int IdEstadoLaboral,
        int IdDepartamento,
        int IdPuesto,
        string? IdUsuario,
        string EstadoRegistro,
        IReadOnlyList<string>? CamposProtegidosModificados);

    private sealed record DatosColaboradorLimpios(
        string CodigoColaborador,
        string TipoIdentificacion,
        string Identificacion,
        string Nombre,
        string PrimerApellido,
        string? SegundoApellido,
        DateTime? FechaNacimiento,
        string? CorreoElectronico,
        string? Telefono,
        string? Direccion,
        DateTime FechaIngreso,
        DateTime? FechaSalida,
        string? Observaciones)
    {
        public static DatosColaboradorLimpios DesdeSolicitud(SolicitudColaborador solicitud)
        {
            if (solicitud.FechaIngreso is null)
            {
                throw new ArgumentException("La fecha de ingreso es obligatoria.");
            }

            return new DatosColaboradorLimpios(
                LimpiarObligatorio(solicitud.CodigoColaborador, "El codigo de colaborador es obligatorio."),
                LimpiarObligatorio(solicitud.TipoIdentificacion, "El tipo de identificacion es obligatorio."),
                LimpiarObligatorio(solicitud.Identificacion, "La identificacion es obligatoria."),
                LimpiarObligatorio(solicitud.Nombre, "El nombre es obligatorio."),
                LimpiarObligatorio(solicitud.PrimerApellido, "El primer apellido es obligatorio."),
                LimpiarOpcional(solicitud.SegundoApellido),
                solicitud.FechaNacimiento?.Date,
                LimpiarOpcional(solicitud.CorreoElectronico),
                LimpiarOpcional(solicitud.Telefono),
                LimpiarOpcional(solicitud.Direccion),
                solicitud.FechaIngreso.Value.Date,
                solicitud.FechaSalida?.Date,
                LimpiarOpcional(solicitud.Observaciones));
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

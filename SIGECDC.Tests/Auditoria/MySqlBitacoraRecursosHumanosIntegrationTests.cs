using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Auditoria;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Persistence.Auditoria;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.RecursosHumanos;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Auditoria;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlBitacoraRecursosHumanosIntegrationTests
{
    [MySqlQaFact]
    public async Task Colaboradores_RegistranSeisOperacionesSinExponerDatosProtegidos()
    {
        var datos = await PrepararUsuariosYCatalogosAsync();
        long? idColaborador = null;

        try
        {
            var solicitudInicial = CrearSolicitud(datos);

            await using (var contexto = CrearContexto())
            {
                var servicio = new ColaboradorService(contexto);
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    servicio.RegistrarColaboradorAsync(solicitudInicial, " "));

                var solicitudFallida = CrearSolicitud(datos);
                solicitudFallida.CodigoColaborador = $"QA-CF-{datos.Sufijo}";
                solicitudFallida.Identificacion = $"ID-FALLIDA-{datos.Sufijo}";
                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    servicio.RegistrarColaboradorAsync(
                        solicitudFallida,
                        $"usuario-inexistente-{datos.Sufijo}"));
                Assert.False(await contexto.Colaboradores
                    .AsNoTracking()
                    .AnyAsync(colaborador =>
                        colaborador.CodigoColaborador == solicitudFallida.CodigoColaborador));
                Assert.False(await contexto.BitacoraAuditoria
                    .AsNoTracking()
                    .AnyAsync(registro =>
                        registro.IdUsuario == $"usuario-inexistente-{datos.Sufijo}"));

                idColaborador = await servicio.RegistrarColaboradorAsync(
                    solicitudInicial,
                    datos.IdActor);

                var solicitudActualizada = CrearSolicitud(datos);
                solicitudActualizada.CodigoColaborador = $"QA-CU-{datos.Sufijo}";
                solicitudActualizada.Nombre = $"NombreNuevo{datos.Sufijo}";
                solicitudActualizada.CorreoElectronico = $"nuevo-{datos.Sufijo}@sensible.local";
                solicitudActualizada.FechaSalida = DateTime.Today.AddDays(1);

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    servicio.ActualizarColaboradorAsync(
                        idColaborador.Value,
                        solicitudActualizada,
                        " "));
                await servicio.ActualizarColaboradorAsync(
                    idColaborador.Value,
                    solicitudActualizada,
                    datos.IdActor);

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    servicio.AsignarPuestoYDepartamentoAsync(
                        new SolicitudAsignarColaborador
                        {
                            IdColaborador = idColaborador.Value,
                            IdDepartamento = datos.IdDepartamentoDestino,
                            IdPuesto = datos.IdPuestoDestino
                        },
                        " "));
                await servicio.AsignarPuestoYDepartamentoAsync(
                    new SolicitudAsignarColaborador
                    {
                        IdColaborador = idColaborador.Value,
                        IdDepartamento = datos.IdDepartamentoDestino,
                        IdPuesto = datos.IdPuestoDestino
                    },
                    datos.IdActor);

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    servicio.VincularCuentaEmpleadoAsync(
                        idColaborador.Value,
                        new SolicitudVinculoCuentaColaborador
                        {
                            IdUsuario = datos.IdEmpleado
                        },
                        " "));
                await servicio.VincularCuentaEmpleadoAsync(
                    idColaborador.Value,
                    new SolicitudVinculoCuentaColaborador
                    {
                        IdUsuario = datos.IdEmpleado
                    },
                    datos.IdActor);
                await servicio.VincularCuentaEmpleadoAsync(
                    idColaborador.Value,
                    new SolicitudVinculoCuentaColaborador(),
                    datos.IdActor);

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    servicio.DesactivarColaboradorAsync(
                        idColaborador.Value,
                        " "));
                await servicio.DesactivarColaboradorAsync(
                    idColaborador.Value,
                    datos.IdActor);
            }

            await using var contextoConsulta = CrearContexto();
            var historial = new HistorialSistemaService(contextoConsulta);
            var operaciones = await historial.ObtenerOperacionesRecursosHumanosAsync(
                new FiltroBitacoraRecursosHumanos
                {
                    Categoria = CategoriasBitacoraRecursosHumanos.Colaborador,
                    Busqueda = idColaborador.Value.ToString(),
                    Cantidad = 250
                });

            var accionesEsperadas = new[]
            {
                "CREAR_COLABORADOR",
                "ACTUALIZAR_COLABORADOR",
                "ASIGNAR_PUESTO_DEPARTAMENTO",
                "VINCULAR_CUENTA_COLABORADOR",
                "DESVINCULAR_CUENTA_COLABORADOR",
                "DESACTIVAR_COLABORADOR"
            };

            Assert.Equal(accionesEsperadas.Length, operaciones.Count);
            Assert.All(accionesEsperadas, accion =>
                Assert.Contains(operaciones, registro => registro.Accion == accion));
            Assert.All(operaciones, registro =>
            {
                Assert.Equal(CategoriasBitacoraRecursosHumanos.Colaborador, registro.Categoria);
                Assert.Equal("Colaborador", registro.Entidad);
                Assert.Equal(datos.IdActor, registro.IdUsuario);
                Assert.Equal(idColaborador.Value.ToString(), registro.IdRegistro);
            });
            Assert.True(operaciones.Zip(operaciones.Skip(1), (actual, siguiente) =>
                    actual.FechaHora > siguiente.FechaHora
                    || (actual.FechaHora == siguiente.FechaHora
                        && actual.IdBitacoraAuditoria > siguiente.IdBitacoraAuditoria))
                .All(ordenado => ordenado));
            Assert.Empty(contextoConsulta.ChangeTracker.Entries());

            var jsonAuditoria = string.Join(
                "|",
                operaciones.SelectMany(registro => new[]
                {
                    registro.ValoresAnteriores,
                    registro.ValoresNuevos
                }).Where(valor => valor is not null));

            foreach (var valorProtegido in new[]
            {
                solicitudInicial.Identificacion,
                solicitudInicial.Nombre,
                solicitudInicial.CorreoElectronico!,
                solicitudInicial.Telefono!,
                solicitudInicial.Direccion!,
                solicitudInicial.Observaciones!,
                $"NombreNuevo{datos.Sufijo}",
                $"nuevo-{datos.Sufijo}@sensible.local"
            })
            {
                Assert.DoesNotContain(valorProtegido, jsonAuditoria, StringComparison.Ordinal);
            }

            var actualizacion = Assert.Single(
                operaciones,
                registro => registro.Accion == "ACTUALIZAR_COLABORADOR");
            Assert.Contains("CamposProtegidosModificados", actualizacion.ValoresNuevos);
            Assert.Contains("Nombre", actualizacion.ValoresNuevos);
            Assert.Contains("CorreoElectronico", actualizacion.ValoresNuevos);
        }
        finally
        {
            await LimpiarColaboradorAsync(datos, idColaborador);
        }
    }

    [MySqlQaFact]
    public async Task Consulta_ClasificaPeriodosYPlanillasYRespetaFiltros()
    {
        var datos = await PrepararUsuariosYCatalogosAsync(crearEmpleado: false);
        var token = $"QA-RH-BIT-{datos.Sufijo}";

        try
        {
            await using (var contexto = CrearContexto())
            {
                contexto.BitacoraAuditoria.AddRange(
                    CrearRegistro(datos.IdActor, new DateTime(2026, 7, 1), "CREAR_PERIODO", "PeriodoPlanilla", token),
                    CrearRegistro(datos.IdActor, new DateTime(2026, 7, 15), "CALCULAR_PLANILLA", "Planilla", token),
                    CrearRegistro(null, new DateTime(2026, 7, 31, 23, 59, 59), "APROBAR_PLANILLA", "Planilla", token),
                    CrearRegistro(datos.IdActor, new DateTime(2026, 8, 1), "CERRAR_PLANILLA", "Planilla", token),
                    CrearRegistro(datos.IdActor, new DateTime(2026, 7, 20), "EVENTO_FUERA_RH", "Activo", token));
                await contexto.SaveChangesAsync();
            }

            await using var contextoConsulta = CrearContexto();
            var servicio = new HistorialSistemaService(contextoConsulta);
            var filtro = new FiltroBitacoraRecursosHumanos
            {
                FechaDesde = new DateTime(2026, 7, 1, 18, 0, 0),
                FechaHasta = new DateTime(2026, 7, 31, 8, 0, 0),
                Busqueda = token,
                Cantidad = 999
            };

            var julio = await servicio.ObtenerOperacionesRecursosHumanosAsync(filtro);
            Assert.Equal(3, julio.Count);
            Assert.DoesNotContain(julio, registro => registro.Entidad == "Activo");
            Assert.Contains(julio, registro =>
                registro.Categoria == CategoriasBitacoraRecursosHumanos.PeriodoPlanilla);
            Assert.Equal(2, julio.Count(registro =>
                registro.Categoria == CategoriasBitacoraRecursosHumanos.Planilla));
            Assert.Contains(julio, registro =>
                registro.Accion == "APROBAR_PLANILLA"
                && registro.IdUsuario is null
                && registro.Usuario is null);

            filtro.Categoria = CategoriasBitacoraRecursosHumanos.PeriodoPlanilla;
            Assert.Single(await servicio.ObtenerOperacionesRecursosHumanosAsync(filtro));

            filtro.Categoria = CategoriasBitacoraRecursosHumanos.Planilla;
            Assert.Equal(2, (await servicio.ObtenerOperacionesRecursosHumanosAsync(filtro)).Count);

            filtro.Categoria = null;
            filtro.Cantidad = 2;
            Assert.Equal(2, (await servicio.ObtenerOperacionesRecursosHumanosAsync(filtro)).Count);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                servicio.ObtenerOperacionesRecursosHumanosAsync(
                    new FiltroBitacoraRecursosHumanos
                    {
                        Categoria = "Auditor"
                    }));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                servicio.ObtenerOperacionesRecursosHumanosAsync(
                    new FiltroBitacoraRecursosHumanos
                    {
                        FechaDesde = new DateTime(2026, 8, 1),
                        FechaHasta = new DateTime(2026, 7, 31)
                    }));
            Assert.Empty(contextoConsulta.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarAuditoriaYUsuariosAsync(datos, token);
        }
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de integración solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySQL(cadena)
                .Options);
    }

    private static async Task<DatosRecursosHumanosQa> PrepararUsuariosYCatalogosAsync(
        bool crearEmpleado = true)
    {
        await using var contexto = CrearContexto();
        await LimpiarResiduosPreviosAsync(contexto);
        var sufijo = Guid.NewGuid().ToString("N")[..10];
        var actor = CrearUsuario($"qa-rh-actor-{sufijo}");
        var empleado = CrearUsuario($"qa-rh-empleado-{sufijo}");
        contexto.Users.Add(actor);

        if (crearEmpleado)
        {
            contexto.Users.Add(empleado);
            var rolEmpleado = await contexto.Roles
                .AsNoTracking()
                .SingleAsync(rol => rol.Name == "Empleado");
            contexto.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = empleado.Id,
                RoleId = rolEmpleado.Id
            });
        }

        var idEstadoActivo = await contexto.EstadosLaborales
            .AsNoTracking()
            .Where(estado => estado.Nombre == "Activo" && estado.EstadoRegistro == "Activo")
            .Select(estado => estado.IdEstadoLaboral)
            .SingleAsync();
        var departamentos = await contexto.Departamentos
            .AsNoTracking()
            .Where(departamento => departamento.EstadoRegistro == "Activo")
            .OrderBy(departamento => departamento.IdDepartamento)
            .Select(departamento => departamento.IdDepartamento)
            .Take(2)
            .ToListAsync();
        var puestos = await contexto.Puestos
            .AsNoTracking()
            .Where(puesto => puesto.EstadoRegistro == "Activo")
            .OrderBy(puesto => puesto.IdPuesto)
            .Select(puesto => new { puesto.IdPuesto, puesto.IdDepartamento })
            .ToListAsync();

        if (departamentos.Count == 0 || puestos.Count == 0)
        {
            throw new InvalidOperationException("QA no contiene catálogos activos de colaboradores.");
        }

        var puestoInicial = puestos.FirstOrDefault(puesto =>
                puesto.IdDepartamento == departamentos[0] || puesto.IdDepartamento is null)
            ?? throw new InvalidOperationException("QA no contiene un puesto compatible con el departamento inicial.");
        var idDepartamentoInicial = puestoInicial.IdDepartamento ?? departamentos[0];
        var puestoDestino = puestos.FirstOrDefault(puesto =>
                puesto.IdPuesto != puestoInicial.IdPuesto
                && (puesto.IdDepartamento is null || departamentos.Contains(puesto.IdDepartamento.Value)))
            ?? puestoInicial;
        var idDepartamentoDestino = puestoDestino.IdDepartamento
            ?? departamentos.FirstOrDefault(id => id != idDepartamentoInicial, idDepartamentoInicial);

        await contexto.SaveChangesAsync();

        return new DatosRecursosHumanosQa(
            sufijo,
            actor.Id,
            crearEmpleado ? empleado.Id : null,
            idEstadoActivo,
            idDepartamentoInicial,
            puestoInicial.IdPuesto,
            idDepartamentoDestino,
            puestoDestino.IdPuesto);
    }

    private static async Task LimpiarResiduosPreviosAsync(
        ApplicationDbContext contexto)
    {
        await contexto.BitacoraAuditoria
            .Where(registro => registro.IdUsuario != null
                && (registro.IdUsuario.StartsWith("qa-rh-actor-")
                    || registro.IdUsuario.StartsWith("qa-rh-empleado-")))
            .ExecuteDeleteAsync();
        await contexto.Colaboradores
            .Where(colaborador =>
                (colaborador.CreadoPor != null
                    && colaborador.CreadoPor.StartsWith("qa-rh-actor-"))
                || (colaborador.IdUsuario != null
                    && colaborador.IdUsuario.StartsWith("qa-rh-empleado-")))
            .ExecuteDeleteAsync();
        await contexto.UserRoles
            .Where(registro => registro.UserId.StartsWith("qa-rh-actor-")
                || registro.UserId.StartsWith("qa-rh-empleado-"))
            .ExecuteDeleteAsync();
        await contexto.Users
            .Where(usuario => usuario.Id.StartsWith("qa-rh-actor-")
                || usuario.Id.StartsWith("qa-rh-empleado-"))
            .ExecuteDeleteAsync();
    }

    private static ApplicationUser CrearUsuario(string prefijo)
    {
        return new ApplicationUser
        {
            Id = prefijo,
            UserName = $"{prefijo}@sige.local",
            NormalizedUserName = $"{prefijo}@sige.local".ToUpperInvariant(),
            Email = $"{prefijo}@sige.local",
            NormalizedEmail = $"{prefijo}@sige.local".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = "Activo",
            FechaCreacion = DateTime.Now
        };
    }

    private static SolicitudColaborador CrearSolicitud(DatosRecursosHumanosQa datos)
    {
        return new SolicitudColaborador
        {
            CodigoColaborador = $"QA-CR-{datos.Sufijo}",
            TipoIdentificacion = "Cedula fisica",
            Identificacion = $"ID-SENSIBLE-{datos.Sufijo}",
            Nombre = $"NombreSensible{datos.Sufijo}",
            PrimerApellido = "ApellidoProtegido",
            FechaNacimiento = new DateTime(1990, 1, 1),
            CorreoElectronico = $"original-{datos.Sufijo}@sensible.local",
            Telefono = $"TEL-{datos.Sufijo}",
            Direccion = $"Direccion sensible {datos.Sufijo}",
            FechaIngreso = DateTime.Today,
            IdEstadoLaboral = datos.IdEstadoLaboral,
            IdDepartamento = datos.IdDepartamentoInicial,
            IdPuesto = datos.IdPuestoInicial,
            Observaciones = $"Observacion sensible {datos.Sufijo}"
        };
    }

    private static BitacoraAuditoria CrearRegistro(
        string? idUsuario,
        DateTime fecha,
        string accion,
        string entidad,
        string token)
    {
        return new BitacoraAuditoria
        {
            IdUsuario = idUsuario,
            FechaHora = fecha,
            Accion = accion,
            Entidad = entidad,
            IdRegistro = Guid.NewGuid().ToString("N")[..12],
            Observacion = token
        };
    }

    private static async Task LimpiarColaboradorAsync(
        DatosRecursosHumanosQa datos,
        long? idColaborador)
    {
        await using var contexto = CrearContexto();
        await contexto.BitacoraAuditoria
            .Where(registro => registro.IdUsuario == datos.IdActor
                || (idColaborador.HasValue
                    && registro.Entidad == "Colaborador"
                    && registro.IdRegistro == idColaborador.Value.ToString()))
            .ExecuteDeleteAsync();

        if (idColaborador.HasValue)
        {
            await contexto.Colaboradores
                .Where(colaborador => colaborador.IdColaborador == idColaborador.Value)
                .ExecuteDeleteAsync();
        }

        await EliminarUsuariosAsync(contexto, datos);
    }

    private static async Task LimpiarAuditoriaYUsuariosAsync(
        DatosRecursosHumanosQa datos,
        string token)
    {
        await using var contexto = CrearContexto();
        await contexto.BitacoraAuditoria
            .Where(registro => registro.Observacion == token
                || registro.IdUsuario == datos.IdActor)
            .ExecuteDeleteAsync();
        await EliminarUsuariosAsync(contexto, datos);
    }

    private static async Task EliminarUsuariosAsync(
        ApplicationDbContext contexto,
        DatosRecursosHumanosQa datos)
    {
        await contexto.UserRoles
            .Where(registro => registro.UserId == datos.IdActor
                || (datos.IdEmpleado != null
                    && registro.UserId == datos.IdEmpleado))
            .ExecuteDeleteAsync();
        await contexto.Users
            .Where(usuario => usuario.Id == datos.IdActor
                || (datos.IdEmpleado != null
                    && usuario.Id == datos.IdEmpleado))
            .ExecuteDeleteAsync();
    }

    private sealed record DatosRecursosHumanosQa(
        string Sufijo,
        string IdActor,
        string? IdEmpleado,
        int IdEstadoLaboral,
        int IdDepartamentoInicial,
        int IdPuestoInicial,
        int IdDepartamentoDestino,
        int IdPuestoDestino);
}

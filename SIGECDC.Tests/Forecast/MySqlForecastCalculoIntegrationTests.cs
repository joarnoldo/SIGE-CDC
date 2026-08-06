using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Persistence.Identity;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastCalculoIntegrationTests
{
    [MySqlQaFact]
    public async Task GenerarForecast_UnoYVariosPeriodos_PersisteResultadosReproducibles()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            var escenarioUno = await CrearEscenarioAsync(
                datos,
                token,
                "UNO",
                cantidadPeriodos: 1,
                incluirContratacion: false,
                matrizCompleta: true,
                datos.IdPlanillaAprobada);
            var escenarioRepetido = await CrearEscenarioAsync(
                datos,
                token,
                "REP",
                cantidadPeriodos: 1,
                incluirContratacion: false,
                matrizCompleta: true,
                datos.IdPlanillaAprobada);
            var escenarioMultiple = await CrearEscenarioAsync(
                datos,
                token,
                "MUL",
                cantidadPeriodos: 2,
                incluirContratacion: true,
                matrizCompleta: true,
                datos.IdPlanillaAprobada);

            await using var contexto = CrearContexto();
            var servicio = new ForecastCalculoService(contexto);
            var resultadoUno = await servicio.GenerarForecastAsync(
                escenarioUno.IdForecastEscenario,
                datos.IdActorRecursosHumanos);
            var resultadoRepetido = await servicio.GenerarForecastAsync(
                escenarioRepetido.IdForecastEscenario,
                datos.IdActorRecursosHumanos);
            var resultadoMultiple = await servicio.GenerarForecastAsync(
                escenarioMultiple.IdForecastEscenario,
                datos.IdActorRecursosHumanos);

            Assert.Equal(1, resultadoUno.CantidadPeriodos);
            Assert.Equal(1, resultadoUno.CantidadParticipantes);
            Assert.Equal(8, resultadoUno.CantidadDetalles);
            Assert.Equal(1265m, resultadoUno.MontoProyectadoTotal);
            Assert.Equal(resultadoUno.MontoProyectadoTotal, resultadoRepetido.MontoProyectadoTotal);
            Assert.Equal(2, resultadoMultiple.CantidadPeriodos);
            Assert.Equal(2, resultadoMultiple.CantidadParticipantes);
            Assert.Equal(32, resultadoMultiple.CantidadDetalles);
            Assert.Equal(3950m, resultadoMultiple.MontoProyectadoTotal);

            var escenariosCalculados = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == escenarioUno.IdForecastEscenario
                    || item.IdForecastEscenario == escenarioMultiple.IdForecastEscenario)
                .ToListAsync();
            Assert.All(escenariosCalculados, item =>
            {
                Assert.Equal(EstadosEscenarioForecast.Calculado, item.EstadoEscenario);
                Assert.NotNull(item.FechaCalculo);
                Assert.Equal(datos.IdActorRecursosHumanos, item.ModificadoPor);
                Assert.Null(item.MontoRealTotal);
                Assert.Null(item.DiferenciaTotal);
            });

            var detallesMultiple = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == escenarioMultiple.IdForecastEscenario)
                .ToListAsync();
            Assert.Equal(32, detallesMultiple.Count);
            Assert.Equal(
                resultadoMultiple.MontoProyectadoTotal,
                detallesMultiple
                    .Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                    .Sum(item => item.MontoProyectado));
            Assert.DoesNotContain(detallesMultiple, item =>
                item.IdForecastParticipante == escenarioMultiple.IdParticipanteExcluido);

            var brutoContratacionPrimerPeriodo = detallesMultiple
                .Where(item => item.IdForecastParticipante == escenarioMultiple.IdContratacionPrevista
                    && item.IdForecastPeriodo == escenarioMultiple.IdsPeriodos[0]
                    && item.Concepto == ConceptosForecast.SalarioBruto)
                .Sum(item => item.MontoProyectado);
            var brutoContratacionSegundoPeriodo = detallesMultiple
                .Where(item => item.IdForecastParticipante == escenarioMultiple.IdContratacionPrevista
                    && item.IdForecastPeriodo == escenarioMultiple.IdsPeriodos[1]
                    && item.Concepto == ConceptosForecast.SalarioBruto)
                .Sum(item => item.MontoProyectado);
            Assert.Equal(0m, brutoContratacionPrimerPeriodo);
            Assert.Equal(1420m, brutoContratacionSegundoPeriodo);
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task GenerarForecast_RechazaActorFuenteOMatrizInvalida_SinDatosParciales()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            var escenarioValido = await CrearEscenarioAsync(
                datos,
                token,
                "SEG",
                1,
                false,
                true,
                datos.IdPlanillaAprobada);
            var escenarioIncompleto = await CrearEscenarioAsync(
                datos,
                token,
                "INC",
                1,
                false,
                false,
                datos.IdPlanillaAprobada);
            var escenarioFuenteInvalida = await CrearEscenarioAsync(
                datos,
                token,
                "FUE",
                1,
                false,
                true,
                datos.IdPlanillaBorrador);

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastCalculoService(contexto);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    servicio.GenerarForecastAsync(
                        escenarioValido.IdForecastEscenario,
                        datos.IdActorEmpleado));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    servicio.GenerarForecastAsync(
                        escenarioValido.IdForecastEscenario,
                        " "));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastCalculoService(contexto);
                await Assert.ThrowsAsync<ValidationException>(() =>
                    servicio.GenerarForecastAsync(
                        escenarioIncompleto.IdForecastEscenario,
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastCalculoService(contexto);
                await Assert.ThrowsAsync<ValidationException>(() =>
                    servicio.GenerarForecastAsync(
                        escenarioFuenteInvalida.IdForecastEscenario,
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using var verificacion = CrearContexto();
            var ids = new[]
            {
                escenarioValido.IdForecastEscenario,
                escenarioIncompleto.IdForecastEscenario,
                escenarioFuenteInvalida.IdForecastEscenario
            };
            Assert.Empty(await verificacion.ForecastDetalles
                .AsNoTracking()
                .Where(item => ids.Contains(item.IdForecastEscenario))
                .ToListAsync());
            Assert.All(await verificacion.ForecastEscenarios
                .AsNoTracking()
                .Where(item => ids.Contains(item.IdForecastEscenario))
                .ToListAsync(),
                item =>
                {
                    Assert.Equal(EstadosEscenarioForecast.Borrador, item.EstadoEscenario);
                    Assert.Null(item.FechaCalculo);
                    Assert.Equal(0m, item.MontoProyectadoTotal);
                });
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    private static async Task<DatosQa> PrepararDatosAsync(string token)
    {
        await using var contexto = CrearContexto();
        var roles = await contexto.Roles
            .AsNoTracking()
            .Where(rol => rol.Name == "Recursos Humanos" || rol.Name == "Empleado")
            .ToDictionaryAsync(rol => rol.Name!, rol => rol.Id);
        var estadosPlanilla = await contexto.EstadosPlanilla
            .AsNoTracking()
            .Where(estado => estado.Nombre == EstadosPlanilla.Aprobada
                || estado.Nombre == EstadosPlanilla.Borrador)
            .ToDictionaryAsync(estado => estado.Nombre, estado => estado.IdEstadoPlanilla);
        var idEstadoLaboral = await contexto.EstadosLaborales
            .AsNoTracking()
            .Where(estado => estado.Nombre == "Activo")
            .Select(estado => estado.IdEstadoLaboral)
            .SingleAsync();
        var idEstadoProyecto = await contexto.EstadosProyecto
            .AsNoTracking()
            .Where(estado => estado.Nombre == EstadosProyecto.Planificado)
            .Select(estado => estado.IdEstadoProyecto)
            .SingleAsync();

        var actorRecursosHumanos = CrearUsuario(token, "rrhh");
        var actorEmpleado = CrearUsuario(token, "empleado");
        contexto.Users.AddRange(actorRecursosHumanos, actorEmpleado);
        contexto.UserRoles.AddRange(
            new IdentityUserRole<string>
            {
                UserId = actorRecursosHumanos.Id,
                RoleId = roles["Recursos Humanos"]
            },
            new IdentityUserRole<string>
            {
                UserId = actorEmpleado.Id,
                RoleId = roles["Empleado"]
            });

        var departamento = new Departamento
        {
            Nombre = $"QA Forecast {token}",
            EstadoRegistro = EstadosRegistro.Activo
        };
        var puesto = new Puesto
        {
            Nombre = $"QA Forecast {token}",
            Departamento = departamento,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var colaborador = new Colaborador
        {
            CodigoColaborador = $"QAF-{token}",
            TipoIdentificacion = "QA",
            Identificacion = $"QAF-{token}",
            Nombre = "Colaborador",
            PrimerApellido = "Forecast",
            FechaIngreso = DateTime.Today.AddYears(-2),
            IdEstadoLaboral = idEstadoLaboral,
            Departamento = departamento,
            Puesto = puesto,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var proyecto = new Proyecto
        {
            CodigoProyecto = $"QAF-{token}",
            NombreProyecto = $"Proyecto forecast {token}",
            IdEstadoProyecto = idEstadoProyecto,
            FechaInicio = DateTime.Today.AddYears(-1),
            FechaFinEstimada = DateTime.Today.AddYears(2),
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.Colaboradores.Add(colaborador);
        contexto.Proyectos.Add(proyecto);

        var periodoAprobado = CrearPeriodoHistorico(
            token,
            "APR",
            estadosPlanilla[EstadosPlanilla.Aprobada],
            EstadosPlanilla.Aprobada,
            colaborador,
            conDetalle: true);
        var periodoBorrador = CrearPeriodoHistorico(
            token,
            "BOR",
            estadosPlanilla[EstadosPlanilla.Borrador],
            EstadosPlanilla.Borrador,
            colaborador,
            conDetalle: false);
        contexto.PeriodosPlanilla.AddRange(periodoAprobado, periodoBorrador);
        await contexto.SaveChangesAsync();

        return new DatosQa(
            actorRecursosHumanos.Id,
            actorEmpleado.Id,
            departamento.IdDepartamento,
            puesto.IdPuesto,
            colaborador.IdColaborador,
            proyecto.IdProyecto,
            periodoAprobado.Planilla!.IdPlanilla,
            periodoBorrador.Planilla!.IdPlanilla);
    }

    private static PeriodoPlanilla CrearPeriodoHistorico(
        string token,
        string codigo,
        int idEstado,
        string estado,
        Colaborador colaborador,
        bool conDetalle)
    {
        var fechaInicio = new DateTime(2026, codigo == "APR" ? 1 : 2, 1);
        var planilla = new Planilla
        {
            IdEstadoPlanilla = idEstado,
            FechaCalculo = estado == EstadosPlanilla.Aprobada ? fechaInicio.AddMonths(1) : null,
            FechaAprobacion = estado == EstadosPlanilla.Aprobada
                ? fechaInicio.AddMonths(1).AddHours(1)
                : null,
            SalarioBrutoTotal = conDetalle ? 1165m : 0m,
            DeduccionesTotal = conDetalle ? 20m : 0m,
            SalarioNetoTotal = conDetalle ? 1145m : 0m,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        if (conDetalle)
        {
            planilla.Detalles.Add(new DetallePlanilla
            {
                Colaborador = colaborador,
                SalarioBase = 1000m,
                SalarioProporcional = 1000m,
                TotalHorasExtra = 100m,
                TotalBonos = 50m,
                TotalBeneficiosConfigurables = 25m,
                TotalAusencias = 10m,
                SalarioBruto = 1165m,
                TotalDeducciones = 20m,
                SalarioNeto = 1145m,
                CostoPatronalEstimado = 0m,
                FechaCreacion = DateTime.Now
            });
        }

        return new PeriodoPlanilla
        {
            CodigoPeriodo = $"QAF-{codigo}-{token}",
            Nombre = $"Histórico forecast {codigo} {token}",
            TipoPeriodo = TiposPeriodoPlanilla.Mensual,
            FechaInicio = fechaInicio,
            FechaFin = fechaInicio.AddMonths(1).AddDays(-1),
            IdEstadoPlanilla = idEstado,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo,
            Planilla = planilla
        };
    }

    private static async Task<EscenarioQa> CrearEscenarioAsync(
        DatosQa datos,
        string token,
        string sufijo,
        int cantidadPeriodos,
        bool incluirContratacion,
        bool matrizCompleta,
        long idFuente)
    {
        await using var contexto = CrearContexto();
        var inicio = new DateTime(DateTime.Today.AddMonths(2).Year, DateTime.Today.AddMonths(2).Month, 1);
        var escenario = new ForecastEscenario
        {
            Nombre = $"QAF-{sufijo}-{token}",
            FechaInicioProyeccion = inicio,
            FechaFinProyeccion = inicio.AddMonths(cantidadPeriodos).AddDays(-1),
            PeriodosHistoricosConsiderados = 1,
            EstadoEscenario = EstadosEscenarioForecast.Borrador,
            MontoProyectadoTotal = 0m,
            FechaCreacion = DateTime.Now,
            CreadoPor = datos.IdActorRecursosHumanos,
            EstadoRegistro = EstadosRegistro.Activo
        };
        for (var indice = 0; indice < cantidadPeriodos; indice++)
        {
            var fechaInicio = inicio.AddMonths(indice);
            escenario.Periodos.Add(new ForecastPeriodo
            {
                NumeroOrden = indice + 1,
                TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                FechaInicio = fechaInicio,
                FechaFin = fechaInicio.AddMonths(1).AddDays(-1),
                EstadoRegistro = EstadosRegistro.Activo
            });
        }

        escenario.FuentesHistoricas.Add(new ForecastFuenteHistorica
        {
            IdPlanilla = idFuente,
            NumeroOrden = 1
        });
        escenario.Parametros.Add(new ForecastParametro
        {
            Codigo = CodigosParametroForecast.AjusteSalarial,
            Nombre = "Ajuste salarial",
            TipoParametro = TiposParametroPlanilla.Porcentaje,
            ValorDecimal = 10m,
            EstadoRegistro = EstadosRegistro.Activo
        });
        escenario.Parametros.Add(new ForecastParametro
        {
            Codigo = CodigosParametroForecast.HorasExtraEstimadas,
            Nombre = "Horas extra estimadas",
            TipoParametro = TiposParametroPlanilla.Monto,
            ValorDecimal = 100m,
            EstadoRegistro = EstadosRegistro.Activo
        });
        var participanteReal = new ForecastParticipante
        {
            CodigoParticipante = $"REAL-{sufijo}-{token}",
            Etiqueta = "Colaborador Forecast",
            TipoParticipante = TiposParticipanteForecast.Colaborador,
            IdColaborador = datos.IdColaborador,
            IdDepartamento = datos.IdDepartamento,
            IdPuesto = datos.IdPuesto,
            SalarioBaseMensual = 1000m,
            FechaInicioAplicacion = inicio,
            EstaIncluido = true,
            EstadoRegistro = EstadosRegistro.Activo
        };
        escenario.Participantes.Add(participanteReal);
        ForecastParticipante? contratacion = null;
        if (incluirContratacion)
        {
            contratacion = new ForecastParticipante
            {
                CodigoParticipante = $"PREV-{sufijo}-{token}",
                Etiqueta = "Plaza prevista QA",
                TipoParticipante = TiposParticipanteForecast.ContratacionPrevista,
                IdDepartamento = datos.IdDepartamento,
                IdPuesto = datos.IdPuesto,
                SalarioBaseMensual = 1200m,
                FechaInicioAplicacion = inicio.AddMonths(1),
                EstaIncluido = true,
                EstadoRegistro = EstadosRegistro.Activo
            };
            escenario.Participantes.Add(contratacion);
        }

        var excluido = new ForecastParticipante
        {
            CodigoParticipante = $"EXC-{sufijo}-{token}",
            Etiqueta = "Plaza excluida QA",
            TipoParticipante = TiposParticipanteForecast.ContratacionPrevista,
            IdDepartamento = datos.IdDepartamento,
            IdPuesto = datos.IdPuesto,
            SalarioBaseMensual = 900m,
            FechaInicioAplicacion = inicio,
            EstaIncluido = false,
            EstadoRegistro = EstadosRegistro.Activo
        };
        escenario.Participantes.Add(excluido);
        contexto.ForecastEscenarios.Add(escenario);
        await contexto.SaveChangesAsync();

        var incluidos = escenario.Participantes.Where(item => item.EstaIncluido).ToList();
        var celdas = escenario.Periodos
            .OrderBy(item => item.NumeroOrden)
            .SelectMany(periodo => incluidos.Select(participante => (periodo, participante)))
            .ToList();
        if (!matrizCompleta && celdas.Count > 0)
        {
            celdas.RemoveAt(celdas.Count - 1);
        }

        contexto.ForecastAsignacionesProyecto.AddRange(celdas.Select(celda =>
            new ForecastAsignacionProyecto
            {
                IdForecastEscenario = escenario.IdForecastEscenario,
                IdForecastPeriodo = celda.periodo.IdForecastPeriodo,
                IdForecastParticipante = celda.participante.IdForecastParticipante,
                IdProyecto = datos.IdProyecto,
                Porcentaje = 100m,
                EstadoRegistro = EstadosRegistro.Activo
            }));
        await contexto.SaveChangesAsync();

        return new EscenarioQa(
            escenario.IdForecastEscenario,
            escenario.Periodos.OrderBy(item => item.NumeroOrden).Select(item => item.IdForecastPeriodo).ToList(),
            participanteReal.IdForecastParticipante,
            contratacion?.IdForecastParticipante ?? 0,
            excluido.IdForecastParticipante);
    }

    private static ApplicationUser CrearUsuario(string token, string tipo)
    {
        var correo = $"forecast-calculo-{tipo}-{token}@qa.local";
        return new ApplicationUser
        {
            Id = $"forecast-calculo-{tipo}-{token}",
            UserName = correo,
            NormalizedUserName = correo.ToUpperInvariant(),
            Email = correo,
            NormalizedEmail = correo.ToUpperInvariant(),
            EmailConfirmed = true,
            EstadoRegistro = EstadosRegistro.Activo,
            FechaCreacion = DateTime.Now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de forecast solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(cadena)
            .Options;
        return new ApplicationDbContext(opciones);
    }

    private static async Task LimpiarDatosAsync(string token)
    {
        await using var contexto = CrearContexto();

        var escenarios = await contexto.ForecastEscenarios
            .Where(item => item.Nombre.Contains(token))
            .ToListAsync();
        contexto.ForecastEscenarios.RemoveRange(escenarios);
        await contexto.SaveChangesAsync();

        var periodos = await contexto.PeriodosPlanilla
            .Include(item => item.Planilla)
            .Where(item => item.CodigoPeriodo.Contains(token))
            .ToListAsync();
        contexto.Planillas.RemoveRange(periodos
            .Where(item => item.Planilla is not null)
            .Select(item => item.Planilla!));
        await contexto.SaveChangesAsync();
        contexto.PeriodosPlanilla.RemoveRange(periodos);
        await contexto.SaveChangesAsync();

        var colaboradores = await contexto.Colaboradores
            .Where(item => item.CodigoColaborador.Contains(token))
            .ToListAsync();
        contexto.Colaboradores.RemoveRange(colaboradores);
        var proyectos = await contexto.Proyectos
            .Where(item => item.CodigoProyecto.Contains(token))
            .ToListAsync();
        contexto.Proyectos.RemoveRange(proyectos);
        await contexto.SaveChangesAsync();

        var puestos = await contexto.Puestos
            .Where(item => item.Nombre.Contains(token))
            .ToListAsync();
        contexto.Puestos.RemoveRange(puestos);
        await contexto.SaveChangesAsync();
        var departamentos = await contexto.Departamentos
            .Where(item => item.Nombre.Contains(token))
            .ToListAsync();
        contexto.Departamentos.RemoveRange(departamentos);

        var rolesUsuarios = await contexto.UserRoles
            .Where(item => item.UserId.Contains(token))
            .ToListAsync();
        contexto.UserRoles.RemoveRange(rolesUsuarios);
        var usuarios = await contexto.Users
            .Where(item => item.Id.Contains(token))
            .ToListAsync();
        contexto.Users.RemoveRange(usuarios);
        await contexto.SaveChangesAsync();
    }

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    private sealed record DatosQa(
        string IdActorRecursosHumanos,
        string IdActorEmpleado,
        int IdDepartamento,
        int IdPuesto,
        long IdColaborador,
        long IdProyecto,
        long IdPlanillaAprobada,
        long IdPlanillaBorrador);

    private sealed record EscenarioQa(
        long IdForecastEscenario,
        IReadOnlyList<long> IdsPeriodos,
        long IdParticipanteReal,
        long IdContratacionPrevista,
        long IdParticipanteExcluido);
}

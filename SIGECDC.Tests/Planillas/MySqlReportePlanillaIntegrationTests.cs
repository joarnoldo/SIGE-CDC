using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Planillas;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlReportePlanillaIntegrationTests
{
    [MySqlQaFact]
    public async Task Consulta_FiltraEstadosOrdenaFilasYConservaMontosPersistidos()
    {
        await using var contexto = CrearContexto();
        var marca = Guid.NewGuid().ToString("N")[..8];

        try
        {
            var datos = await PrepararDatosAsync(contexto, marca);
            contexto.ChangeTracker.Clear();
            var servicio = new ReportePlanillaService(contexto);

            var periodos = await servicio.ObtenerPeriodosDisponiblesAsync();
            var periodosPrueba = periodos
                .Where(periodo => periodo.CodigoPeriodo.Contains(marca))
                .ToList();

            Assert.Equal(4, periodosPrueba.Count);
            Assert.Contains(periodosPrueba, periodo => periodo.EstadoPlanilla == EstadosPlanilla.Calculada);
            Assert.Contains(periodosPrueba, periodo => periodo.EstadoPlanilla == EstadosPlanilla.Aprobada);
            Assert.Contains(periodosPrueba, periodo => periodo.EstadoPlanilla == EstadosPlanilla.Cerrada);
            Assert.DoesNotContain(periodosPrueba, periodo => periodo.EstadoPlanilla == EstadosPlanilla.Borrador);
            Assert.DoesNotContain(periodosPrueba, periodo => periodo.EstadoPlanilla == EstadosPlanilla.EnRevision);
            Assert.DoesNotContain(periodosPrueba, periodo => periodo.EstadoPlanilla == "Anulada");
            Assert.DoesNotContain(periodosPrueba, periodo => periodo.IdPeriodoPlanilla == datos.IdPeriodoInactivo);

            var reporte = await servicio.ObtenerReporteAsync(datos.IdPeriodoCerrado);

            Assert.NotNull(reporte);
            Assert.Equal(1_200_000m, reporte.SalarioBrutoTotal);
            Assert.Equal(100_000m, reporte.DeduccionesTotal);
            Assert.Equal(1_100_000m, reporte.SalarioNetoTotal);
            Assert.Equal(2, reporte.Detalles.Count);
            Assert.Equal("Empleado Beta Alfa", reporte.Detalles[0].NombreColaborador);
            Assert.Equal("Empleado Alfa Zulu", reporte.Detalles[1].NombreColaborador);
            Assert.Equal(600_000m, reporte.Detalles[0].SalarioProporcional);
            Assert.Equal(40_000m, reporte.Detalles[0].TotalDeducciones);
            Assert.Equal(560_000m, reporte.Detalles[0].SalarioNeto);

            Assert.Null(await servicio.ObtenerReporteAsync(0));
            Assert.Null(await servicio.ObtenerReporteAsync(long.MaxValue));
            Assert.Null(await servicio.ObtenerReporteAsync(datos.IdPeriodoBorrador));
            Assert.Null(await servicio.ObtenerReporteAsync(datos.IdPeriodoEnRevision));
            Assert.Null(await servicio.ObtenerReporteAsync(datos.IdPeriodoAnulado));
            Assert.Null(await servicio.ObtenerReporteAsync(datos.IdPeriodoInactivo));

            var reporteSinFilas = await servicio.ObtenerReporteAsync(datos.IdPeriodoSinFilas);
            Assert.NotNull(reporteSinFilas);
            Assert.Empty(reporteSinFilas.Detalles);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarDatosAsync(contexto, marca);
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

    private static async Task<DatosReporte> PrepararDatosAsync(
        ApplicationDbContext contexto,
        string marca)
    {
        var estadoLaboral = await contexto.EstadosLaborales
            .AsNoTracking()
            .SingleAsync(estado => estado.Nombre == EstadosLaborales.Activo
                && estado.EstadoRegistro == "Activo");
        var puesto = await contexto.Puestos
            .AsNoTracking()
            .Where(registro => registro.EstadoRegistro == "Activo"
                && registro.Departamento != null
                && registro.Departamento.EstadoRegistro == "Activo")
            .OrderBy(registro => registro.IdPuesto)
            .Select(registro => new
            {
                registro.IdPuesto,
                IdDepartamento = registro.IdDepartamento!.Value
            })
            .FirstAsync();
        var estados = await contexto.EstadosPlanilla
            .AsNoTracking()
            .Where(estado => estado.Nombre == EstadosPlanilla.Borrador
                || estado.Nombre == EstadosPlanilla.Calculada
                || estado.Nombre == EstadosPlanilla.EnRevision
                || estado.Nombre == EstadosPlanilla.Aprobada
                || estado.Nombre == EstadosPlanilla.Cerrada
                || estado.Nombre == "Anulada")
            .ToDictionaryAsync(estado => estado.Nombre, estado => estado.IdEstadoPlanilla);

        var colaboradorAlfa = CrearColaborador(
            marca,
            "A",
            "Alfa",
            "Zulu",
            estadoLaboral.IdEstadoLaboral,
            puesto.IdDepartamento,
            puesto.IdPuesto);
        var colaboradorBeta = CrearColaborador(
            marca,
            "B",
            "Beta",
            "Alfa",
            estadoLaboral.IdEstadoLaboral,
            puesto.IdDepartamento,
            puesto.IdPuesto);
        contexto.Colaboradores.AddRange(colaboradorAlfa, colaboradorBeta);
        await contexto.SaveChangesAsync();

        var periodos = new List<PeriodoPlanilla>();
        var planillas = new List<Planilla>();
        var fechaBase = new DateTime(2026, 1, 1);
        var definiciones = new[]
        {
            new DefinicionPeriodo("BOR", EstadosPlanilla.Borrador, false, false, 0),
            new DefinicionPeriodo("CAL", EstadosPlanilla.Calculada, true, false, 1),
            new DefinicionPeriodo("REV", EstadosPlanilla.EnRevision, true, false, 2),
            new DefinicionPeriodo("APR", EstadosPlanilla.Aprobada, true, false, 3),
            new DefinicionPeriodo("CER", EstadosPlanilla.Cerrada, true, false, 4),
            new DefinicionPeriodo("ANU", "Anulada", true, false, 5),
            new DefinicionPeriodo("INA", EstadosPlanilla.Calculada, true, true, 6),
            new DefinicionPeriodo("VAC", EstadosPlanilla.Calculada, true, false, 7)
        };

        foreach (var definicion in definiciones)
        {
            var inicio = fechaBase.AddMonths(definicion.DesplazamientoMeses);
            var periodo = new PeriodoPlanilla
            {
                CodigoPeriodo = $"QA9-{definicion.Codigo}-{marca}",
                Nombre = $"Reporte QA {definicion.Codigo} {marca}",
                TipoPeriodo = "Quincenal",
                FechaInicio = inicio,
                FechaFin = inicio.AddDays(14),
                IdEstadoPlanilla = estados[definicion.Estado],
                FechaCreacion = DateTime.Now,
                EstadoRegistro = definicion.Inactivo ? "Inactivo" : "Activo"
            };
            contexto.PeriodosPlanilla.Add(periodo);
            await contexto.SaveChangesAsync();
            periodos.Add(periodo);

            if (!definicion.TienePlanilla)
            {
                continue;
            }

            var planilla = new Planilla
            {
                IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
                IdEstadoPlanilla = estados[definicion.Estado],
                FechaCalculo = inicio.AddDays(15).AddHours(8),
                FechaAprobacion = definicion.Estado is EstadosPlanilla.Aprobada or EstadosPlanilla.Cerrada
                    ? inicio.AddDays(15).AddHours(9)
                    : null,
                FechaCierre = definicion.Estado == EstadosPlanilla.Cerrada
                    ? inicio.AddDays(15).AddHours(10)
                    : null,
                SalarioBrutoTotal = definicion.Estado == EstadosPlanilla.Cerrada ? 1_200_000m : 600_000m,
                DeduccionesTotal = definicion.Estado == EstadosPlanilla.Cerrada ? 100_000m : 50_000m,
                SalarioNetoTotal = definicion.Estado == EstadosPlanilla.Cerrada ? 1_100_000m : 550_000m,
                FechaCreacion = DateTime.Now,
                EstadoRegistro = "Activo"
            };
            contexto.Planillas.Add(planilla);
            await contexto.SaveChangesAsync();
            planillas.Add(planilla);

            if (definicion.Codigo == "VAC")
            {
                continue;
            }

            if (definicion.Estado == EstadosPlanilla.Cerrada)
            {
                contexto.DetallesPlanilla.AddRange(
                    CrearDetalle(planilla.IdPlanilla, colaboradorAlfa.IdColaborador, 600_000m, 60_000m, 540_000m),
                    CrearDetalle(planilla.IdPlanilla, colaboradorBeta.IdColaborador, 600_000m, 40_000m, 560_000m));
            }
            else
            {
                contexto.DetallesPlanilla.Add(
                    CrearDetalle(planilla.IdPlanilla, colaboradorAlfa.IdColaborador, 600_000m, 50_000m, 550_000m));
            }

            await contexto.SaveChangesAsync();
        }

        long Id(string codigo) => periodos.Single(periodo =>
            periodo.CodigoPeriodo.StartsWith($"QA9-{codigo}-", StringComparison.Ordinal)).IdPeriodoPlanilla;

        return new DatosReporte(
            Id("BOR"),
            Id("REV"),
            Id("CER"),
            Id("ANU"),
            Id("INA"),
            Id("VAC"));
    }

    private static Colaborador CrearColaborador(
        string marca,
        string codigo,
        string nombre,
        string apellido,
        int idEstadoLaboral,
        int idDepartamento,
        int idPuesto)
    {
        return new Colaborador
        {
            CodigoColaborador = $"QA9-{codigo}-{marca}",
            TipoIdentificacion = "Cédula",
            Identificacion = $"QA9-ID-{codigo}-{marca}",
            Nombre = $"Empleado {nombre}",
            PrimerApellido = apellido,
            FechaIngreso = new DateTime(2025, 1, 1),
            IdEstadoLaboral = idEstadoLaboral,
            IdDepartamento = idDepartamento,
            IdPuesto = idPuesto,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = "Activo"
        };
    }

    private static DetallePlanilla CrearDetalle(
        long idPlanilla,
        long idColaborador,
        decimal salarioProporcional,
        decimal deducciones,
        decimal neto)
    {
        return new DetallePlanilla
        {
            IdPlanilla = idPlanilla,
            IdColaborador = idColaborador,
            SalarioBase = salarioProporcional * 2,
            SalarioProporcional = salarioProporcional,
            TotalHorasExtra = 20_000m,
            TotalBonos = 10_000m,
            TotalBeneficiosConfigurables = 5_000m,
            TotalAusencias = 5_000m,
            SalarioBruto = neto + deducciones,
            TotalDeducciones = deducciones,
            SalarioNeto = neto,
            CostoPatronalEstimado = 0m,
            FechaCreacion = DateTime.Now
        };
    }

    private static async Task LimpiarDatosAsync(ApplicationDbContext contexto, string marca)
    {
        contexto.ChangeTracker.Clear();

        var planillas = await contexto.Planillas
            .Where(planilla => planilla.PeriodoPlanilla != null
                && planilla.PeriodoPlanilla.CodigoPeriodo.Contains(marca))
            .ToListAsync();
        contexto.Planillas.RemoveRange(planillas);
        await contexto.SaveChangesAsync();

        var periodos = await contexto.PeriodosPlanilla
            .Where(periodo => periodo.CodigoPeriodo.Contains(marca))
            .ToListAsync();
        contexto.PeriodosPlanilla.RemoveRange(periodos);

        var colaboradores = await contexto.Colaboradores
            .Where(colaborador => colaborador.CodigoColaborador.Contains(marca))
            .ToListAsync();
        contexto.Colaboradores.RemoveRange(colaboradores);
        await contexto.SaveChangesAsync();
    }

    private sealed record DefinicionPeriodo(
        string Codigo,
        string Estado,
        bool TienePlanilla,
        bool Inactivo,
        int DesplazamientoMeses);

    private sealed record DatosReporte(
        long IdPeriodoBorrador,
        long IdPeriodoEnRevision,
        long IdPeriodoCerrado,
        long IdPeriodoAnulado,
        long IdPeriodoInactivo,
        long IdPeriodoSinFilas);
}

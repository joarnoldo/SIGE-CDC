using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Web.Configuracion;

public static class DatosDemoSprint3Seeder
{
    public const string ArgumentoEjecucion = "--seed-demo-sprint3";

    private const string MarcaDemo = "Datos para la demo del Sprint 3.";

    public static bool FueSolicitada(string[] argumentos) =>
        argumentos.Any(argumento =>
            string.Equals(argumento, ArgumentoEjecucion, StringComparison.OrdinalIgnoreCase));

    public static async Task SembrarDatosDemoSprint3Async(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "La semilla de demo solo se puede ejecutar en el entorno Development.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var servicios = scope.ServiceProvider;
        var contexto = servicios.GetRequiredService<ApplicationDbContext>();
        var userManager = servicios.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = servicios
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatosDemoSprint3Seeder");

        var usuarioOperaciones = (await userManager.GetUsersInRoleAsync("Operaciones")).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Se necesita al menos un usuario con el rol Operaciones para crear la semilla.");
        var usuarioRecursosHumanos = (await userManager.GetUsersInRoleAsync("Recursos Humanos")).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Se necesita al menos un usuario con el rol Recursos Humanos para crear la semilla.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);

        try
        {
            var fechaActual = DateTime.Today;
            var ahora = DateTime.Now;

            var catalogos = await CargarCatalogosAsync(contexto);

            var proyectos = await SembrarProyectosAsync(
                contexto,
                catalogos,
                usuarioOperaciones.Id,
                fechaActual,
                ahora);

            var activos = await SembrarActivosAsync(
                contexto,
                catalogos,
                usuarioOperaciones.Id,
                fechaActual,
                ahora);

            await contexto.SaveChangesAsync();

            await SembrarAsignacionesAsync(
                contexto,
                proyectos,
                activos,
                usuarioOperaciones.Id,
                fechaActual,
                ahora);
            await SembrarMantenimientosAsync(
                contexto,
                catalogos,
                proyectos,
                activos,
                usuarioOperaciones.Id,
                fechaActual,
                ahora);
            await SembrarRegistrosUsoAsync(
                contexto,
                proyectos,
                activos,
                usuarioOperaciones.Id,
                fechaActual,
                ahora);
            await SembrarPlanillaAsync(
                contexto,
                catalogos,
                usuarioRecursosHumanos.Id,
                fechaActual,
                ahora);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            logger.LogInformation(
                "Semilla Sprint 3 lista: 3 proyectos, 3 activos, 2 asignaciones, "
                + "2 mantenimientos, 3 registros de uso, 2 colaboradores y 1 período de planilla.");
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    private static async Task<CatalogosDemo> CargarCatalogosAsync(ApplicationDbContext contexto)
    {
        return new CatalogosDemo(
            EstadoProyectoPlanificado: await ObtenerRequeridoAsync(
                contexto.EstadosProyecto,
                estado => estado.Nombre == "Planificado",
                "el estado de proyecto Planificado"),
            EstadoProyectoEnEjecucion: await ObtenerRequeridoAsync(
                contexto.EstadosProyecto,
                estado => estado.Nombre == "En ejecución",
                "el estado de proyecto En ejecución"),
            EstadoProyectoFinalizado: await ObtenerRequeridoAsync(
                contexto.EstadosProyecto,
                estado => estado.Nombre == "Finalizado",
                "el estado de proyecto Finalizado"),
            EstadoActivoDisponible: await ObtenerRequeridoAsync(
                contexto.EstadosActivo,
                estado => estado.Nombre == EstadosActivo.Disponible,
                "el estado de activo Disponible"),
            EstadoActivoAsignado: await ObtenerRequeridoAsync(
                contexto.EstadosActivo,
                estado => estado.Nombre == EstadosActivo.Asignado,
                "el estado de activo Asignado"),
            EstadoMantenimientoProgramado: await ObtenerRequeridoAsync(
                contexto.EstadosMantenimiento,
                estado => estado.Nombre == EstadosMantenimiento.Programado,
                "el estado de mantenimiento Programado"),
            EstadoMantenimientoFinalizado: await ObtenerRequeridoAsync(
                contexto.EstadosMantenimiento,
                estado => estado.Nombre == EstadosMantenimiento.Finalizado,
                "el estado de mantenimiento Finalizado"),
            EstadoPlanillaBorrador: await ObtenerRequeridoAsync(
                contexto.EstadosPlanilla,
                estado => estado.Nombre == EstadosPlanilla.Borrador,
                "el estado de planilla Borrador"),
            EstadoLaboralActivo: await ObtenerRequeridoAsync(
                contexto.EstadosLaborales,
                estado => estado.Nombre == EstadosLaborales.Activo,
                "el estado laboral Activo"),
            DepartamentoOperaciones: await ObtenerRequeridoAsync(
                contexto.Departamentos,
                departamento => departamento.Nombre == "Operaciones",
                "el departamento Operaciones"),
            PuestoOperativo: await ObtenerRequeridoAsync(
                contexto.Puestos,
                puesto => puesto.Nombre == "Empleado operativo",
                "el puesto Empleado operativo"),
            TipoMaquinaria: await ObtenerRequeridoAsync(
                contexto.TiposActivo,
                tipo => tipo.Nombre == "Maquinaria",
                "el tipo de activo Maquinaria"),
            TipoVehiculo: await ObtenerRequeridoAsync(
                contexto.TiposActivo,
                tipo => tipo.Nombre == "Vehículo",
                "el tipo de activo Vehículo"),
            TipoEquipo: await ObtenerRequeridoAsync(
                contexto.TiposActivo,
                tipo => tipo.Nombre == "Equipo",
                "el tipo de activo Equipo"),
            MedicionHoras: await ObtenerRequeridoAsync(
                contexto.TiposMedicionUso,
                tipo => tipo.Nombre == "Horas",
                "el tipo de medición Horas"),
            MedicionKilometros: await ObtenerRequeridoAsync(
                contexto.TiposMedicionUso,
                tipo => tipo.Nombre == "Kilómetros",
                "el tipo de medición Kilómetros"),
            TipoIncidenciaBono: await ObtenerRequeridoAsync(
                contexto.TiposIncidenciaPlanilla,
                tipo => tipo.Nombre == TiposIncidenciaPlanilla.Bono,
                "el tipo de incidencia Bono"),
            TipoIncidenciaHoraExtra: await ObtenerRequeridoAsync(
                contexto.TiposIncidenciaPlanilla,
                tipo => tipo.Nombre == TiposIncidenciaPlanilla.HoraExtra,
                "el tipo de incidencia Hora extra"),
            TipoIncidenciaAusencia: await ObtenerRequeridoAsync(
                contexto.TiposIncidenciaPlanilla,
                tipo => tipo.Nombre == TiposIncidenciaPlanilla.AusenciaSinGoce,
                "el tipo de incidencia Ausencia sin goce"));
    }

    private static async Task<ProyectosDemo> SembrarProyectosAsync(
        ApplicationDbContext contexto,
        CatalogosDemo catalogos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var proyectoActivo = await ObtenerOCrearProyectoAsync(
            contexto,
            "DEMO-PRY-01",
            "Mejora de drenaje Los Pinos",
            "Mejora del drenaje pluvial y limpieza de pasos de agua.",
            "María Pérez",
            "Los Pinos",
            catalogos.EstadoProyectoEnEjecucion.IdEstadoProyecto,
            fechaActual.AddDays(-30),
            fechaActual.AddDays(60),
            null,
            idUsuario,
            ahora);

        var proyectoFinalizado = await ObtenerOCrearProyectoAsync(
            contexto,
            "DEMO-PRY-02",
            "Limpieza de canal San José",
            "Limpieza y retiro de sedimentos en el canal principal.",
            "José Ramírez",
            "San José",
            catalogos.EstadoProyectoFinalizado.IdEstadoProyecto,
            fechaActual.AddDays(-120),
            fechaActual.AddDays(-20),
            fechaActual.AddDays(-18),
            idUsuario,
            ahora);

        var proyectoOculto = await ObtenerOCrearProyectoAsync(
            contexto,
            "DEMO-PRY-03",
            "Reparación de puente La Palma",
            "Trabajo interno pendiente de autorización para publicación.",
            "Carlos Mora",
            "La Palma",
            catalogos.EstadoProyectoPlanificado.IdEstadoProyecto,
            fechaActual.AddDays(30),
            fechaActual.AddDays(90),
            null,
            idUsuario,
            ahora);

        await contexto.SaveChangesAsync();

        await ObtenerOCrearPublicacionAsync(
            contexto,
            proyectoActivo,
            "Mejora de drenaje Los Pinos",
            "Obra para mejorar el paso del agua durante la época lluviosa.",
            "En ejecución",
            true,
            idUsuario,
            ahora);
        await ObtenerOCrearPublicacionAsync(
            contexto,
            proyectoFinalizado,
            "Limpieza de canal San José",
            "Canal limpio y habilitado para mejorar el flujo del agua.",
            "Finalizado",
            true,
            idUsuario,
            ahora);
        await ObtenerOCrearPublicacionAsync(
            contexto,
            proyectoOculto,
            "Reparación de puente La Palma",
            "Proyecto pendiente de autorización para publicación.",
            "Planificado",
            false,
            idUsuario,
            ahora);

        await RegistrarEventoSiNoExisteAsync(
            contexto,
            idUsuario,
            "Creación de proyecto",
            "Proyecto",
            proyectoActivo.IdProyecto.ToString(),
            new { proyectoActivo.CodigoProyecto, proyectoActivo.NombreProyecto },
            "Proyecto de demostración creado para el Sprint 3.",
            ahora);

        return new ProyectosDemo(proyectoActivo, proyectoFinalizado, proyectoOculto);
    }

    private static async Task<ActivosDemo> SembrarActivosAsync(
        ApplicationDbContext contexto,
        CatalogosDemo catalogos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var categoriaMaquinaria = await ObtenerCategoriaGeneralAsync(
            contexto,
            catalogos.TipoMaquinaria.IdTipoActivo);
        var categoriaVehiculo = await ObtenerCategoriaGeneralAsync(
            contexto,
            catalogos.TipoVehiculo.IdTipoActivo);
        var categoriaEquipo = await ObtenerCategoriaGeneralAsync(
            contexto,
            catalogos.TipoEquipo.IdTipoActivo);

        var excavadora = await ObtenerOCrearActivoAsync(
            contexto,
            "DEMO-ACT-01",
            "Excavadora",
            catalogos.TipoMaquinaria.IdTipoActivo,
            categoriaMaquinaria.IdCategoriaActivo,
            catalogos.EstadoActivoAsignado.IdEstadoActivo,
            catalogos.MedicionHoras.IdTipoMedicionUso,
            "CAT",
            "320",
            "Patio de Los Pinos",
            620m,
            185_000_000m,
            fechaActual.AddYears(-3),
            idUsuario,
            ahora);

        var camion = await ObtenerOCrearActivoAsync(
            contexto,
            "DEMO-ACT-02",
            "Camión",
            catalogos.TipoVehiculo.IdTipoActivo,
            categoriaVehiculo.IdCategoriaActivo,
            catalogos.EstadoActivoDisponible.IdEstadoActivo,
            catalogos.MedicionKilometros.IdTipoMedicionUso,
            "Hino",
            "500",
            "Plantel principal",
            18_540m,
            72_000_000m,
            fechaActual.AddYears(-2),
            idUsuario,
            ahora);

        var bomba = await ObtenerOCrearActivoAsync(
            contexto,
            "DEMO-ACT-03",
            "Bomba de agua",
            catalogos.TipoEquipo.IdTipoActivo,
            categoriaEquipo.IdCategoriaActivo,
            catalogos.EstadoActivoDisponible.IdEstadoActivo,
            catalogos.MedicionHoras.IdTipoMedicionUso,
            "Honda",
            "WT40",
            "Bodega de equipos",
            140m,
            2_800_000m,
            fechaActual.AddYears(-1),
            idUsuario,
            ahora);

        await contexto.SaveChangesAsync();

        foreach (var activo in new[] { excavadora, camion, bomba })
        {
            await RegistrarEventoSiNoExisteAsync(
                contexto,
                idUsuario,
                "Creación de activo",
                "Activo",
                activo.IdActivo.ToString(),
                new { activo.CodigoActivo, activo.NombreActivo },
                "Activo de demostración creado para el Sprint 3.",
                ahora);
        }

        return new ActivosDemo(excavadora, camion, bomba);
    }

    private static async Task SembrarAsignacionesAsync(
        ApplicationDbContext contexto,
        ProyectosDemo proyectos,
        ActivosDemo activos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var asignacionActual = await ObtenerOCrearAsignacionAsync(
            contexto,
            activos.Excavadora.IdActivo,
            proyectos.ProyectoActivo.IdProyecto,
            fechaActual.AddDays(-7),
            fechaActual.AddDays(30),
            "Trabajo de drenaje en Los Pinos. " + MarcaDemo,
            idUsuario,
            ahora);

        await ObtenerOCrearAsignacionAsync(
            contexto,
            activos.Camion.IdActivo,
            proyectos.ProyectoActivo.IdProyecto,
            fechaActual.AddDays(15),
            fechaActual.AddDays(25),
            "Reserva futura para traslado de materiales. " + MarcaDemo,
            idUsuario,
            ahora);

        await contexto.SaveChangesAsync();

        await RegistrarEventoSiNoExisteAsync(
            contexto,
            idUsuario,
            "Creación de asignación de activo",
            "AsignacionActivoProyecto",
            asignacionActual.IdAsignacionActivoProyecto.ToString(),
            new
            {
                asignacionActual.IdActivo,
                asignacionActual.IdProyecto,
                asignacionActual.FechaInicio,
                asignacionActual.FechaFin
            },
            "Asignación vigente preparada para la demo del Sprint 3.",
            ahora);
    }

    private static async Task SembrarMantenimientosAsync(
        ApplicationDbContext contexto,
        CatalogosDemo catalogos,
        ProyectosDemo proyectos,
        ActivosDemo activos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var mantenimientoFinalizado = await ObtenerOCrearMantenimientoAsync(
            contexto,
            activos.Camion.IdActivo,
            proyectos.ProyectoActivo.IdProyecto,
            TiposMantenimiento.Correctivo,
            catalogos.EstadoMantenimientoFinalizado.IdEstadoMantenimiento,
            fechaActual.AddDays(-10),
            fechaActual.AddDays(-10).AddHours(8),
            fechaActual.AddDays(-10).AddHours(14),
            "Cambio de manguera del sistema de frenos. " + MarcaDemo,
            90_000m,
            85_000m,
            6m,
            "Camión revisado y disponible.",
            "Taller Central",
            idUsuario,
            ahora);

        var mantenimientoProgramado = await ObtenerOCrearMantenimientoAsync(
            contexto,
            activos.Bomba.IdActivo,
            null,
            TiposMantenimiento.Preventivo,
            catalogos.EstadoMantenimientoProgramado.IdEstadoMantenimiento,
            fechaActual.AddDays(7),
            null,
            null,
            "Revisión de aceite, filtros y mangueras. " + MarcaDemo,
            45_000m,
            null,
            null,
            null,
            "Taller Central",
            idUsuario,
            ahora);

        await contexto.SaveChangesAsync();

        await RegistrarEventoSiNoExisteAsync(
            contexto,
            idUsuario,
            "Finalización de orden de mantenimiento",
            "Mantenimiento",
            mantenimientoFinalizado.IdMantenimiento.ToString(),
            new
            {
                mantenimientoFinalizado.TipoMantenimiento,
                mantenimientoFinalizado.CostoReal,
                mantenimientoFinalizado.TiempoFueraServicioHoras,
                mantenimientoFinalizado.Resultado
            },
            "Mantenimiento finalizado preparado para el reporte de costos.",
            ahora);
        await RegistrarEventoSiNoExisteAsync(
            contexto,
            idUsuario,
            "Definición de mantenimiento preventivo",
            "Mantenimiento",
            mantenimientoProgramado.IdMantenimiento.ToString(),
            new
            {
                mantenimientoProgramado.TipoMantenimiento,
                mantenimientoProgramado.FechaProgramada,
                mantenimientoProgramado.CostoEstimado
            },
            "Mantenimiento preventivo preparado para la demo del Sprint 3.",
            ahora);
    }

    private static async Task SembrarRegistrosUsoAsync(
        ApplicationDbContext contexto,
        ProyectosDemo proyectos,
        ActivosDemo activos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var registros = new[]
        {
            await ObtenerOCrearRegistroUsoAsync(
                contexto,
                activos.Excavadora.IdActivo,
                proyectos.ProyectoActivo.IdProyecto,
                fechaActual.AddDays(-5),
                600m,
                612m,
                "Jornada de excavación. " + MarcaDemo,
                idUsuario,
                ahora.AddMinutes(-3)),
            await ObtenerOCrearRegistroUsoAsync(
                contexto,
                activos.Excavadora.IdActivo,
                proyectos.ProyectoActivo.IdProyecto,
                fechaActual.AddDays(-1),
                612m,
                620m,
                "Limpieza del canal. " + MarcaDemo,
                idUsuario,
                ahora.AddMinutes(-2)),
            await ObtenerOCrearRegistroUsoAsync(
                contexto,
                activos.Camion.IdActivo,
                proyectos.ProyectoActivo.IdProyecto,
                fechaActual.AddDays(-3),
                18_420m,
                18_540m,
                "Traslado de materiales. " + MarcaDemo,
                idUsuario,
                ahora.AddMinutes(-1))
        };

        await contexto.SaveChangesAsync();

        foreach (var registro in registros)
        {
            await RegistrarEventoSiNoExisteAsync(
                contexto,
                idUsuario,
                "Registro de uso de activo",
                "RegistroUsoActivo",
                registro.IdRegistroUsoActivo.ToString(),
                new
                {
                    registro.IdActivo,
                    registro.IdProyecto,
                    registro.FechaRegistro,
                    registro.LecturaAnterior,
                    registro.LecturaNueva,
                    registro.CantidadUso
                },
                "Registro de uso preparado para la demo del Sprint 3.",
                registro.FechaCreacion);
        }
    }

    private static async Task SembrarPlanillaAsync(
        ApplicationDbContext contexto,
        CatalogosDemo catalogos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var ana = await ObtenerOCrearColaboradorAsync(
            contexto,
            "DEMO-001",
            "DEMO1001",
            "Ana",
            "Mora",
            "ana.mora.demo@correo.local",
            catalogos,
            idUsuario,
            fechaActual,
            ahora);
        var luis = await ObtenerOCrearColaboradorAsync(
            contexto,
            "DEMO-002",
            "DEMO1002",
            "Luis",
            "Vargas",
            "luis.vargas.demo@correo.local",
            catalogos,
            idUsuario,
            fechaActual,
            ahora);

        await contexto.SaveChangesAsync();

        await ObtenerOCrearContratoAsync(
            contexto,
            ana.IdColaborador,
            650_000m,
            idUsuario,
            fechaActual,
            ahora);
        await ObtenerOCrearContratoAsync(
            contexto,
            luis.IdColaborador,
            575_000m,
            idUsuario,
            fechaActual,
            ahora);

        var inicioPeriodo = fechaActual.Day <= 15
            ? new DateTime(fechaActual.Year, fechaActual.Month, 1)
            : new DateTime(fechaActual.Year, fechaActual.Month, 16);
        var finPeriodo = fechaActual.Day <= 15
            ? new DateTime(fechaActual.Year, fechaActual.Month, 15)
            : new DateTime(
                fechaActual.Year,
                fechaActual.Month,
                DateTime.DaysInMonth(fechaActual.Year, fechaActual.Month));
        var numeroQuincena = fechaActual.Day <= 15 ? 1 : 2;
        var codigoPeriodo = $"DEMO-{fechaActual:yyyyMM}-Q{numeroQuincena}";

        var periodo = await contexto.PeriodosPlanilla.FirstOrDefaultAsync(
            registro => registro.CodigoPeriodo == codigoPeriodo);

        if (periodo is null)
        {
            periodo = new PeriodoPlanilla
            {
                CodigoPeriodo = codigoPeriodo,
                Nombre = $"Planilla demo {fechaActual:MMMM yyyy}",
                TipoPeriodo = "Quincenal",
                FechaInicio = inicioPeriodo,
                FechaFin = finPeriodo,
                IdEstadoPlanilla = catalogos.EstadoPlanillaBorrador.IdEstadoPlanilla,
                Observaciones = MarcaDemo,
                FechaCreacion = ahora,
                CreadoPor = idUsuario,
                EstadoRegistro = EstadosRegistro.Activo
            };
            contexto.PeriodosPlanilla.Add(periodo);
            await contexto.SaveChangesAsync();

            await RegistrarEventoSiNoExisteAsync(
                contexto,
                idUsuario,
                "CREAR_PERIODO",
                "PeriodoPlanilla",
                periodo.IdPeriodoPlanilla.ToString(),
                new
                {
                    periodo.CodigoPeriodo,
                    periodo.Nombre,
                    periodo.TipoPeriodo,
                    periodo.FechaInicio,
                    periodo.FechaFin,
                    Estado = EstadosPlanilla.Borrador
                },
                "Período de demostración creado en estado Borrador.",
                ahora);
        }

        if (await contexto.Planillas.AnyAsync(planilla =>
                planilla.IdPeriodoPlanilla == periodo.IdPeriodoPlanilla
                && planilla.FechaCalculo.HasValue))
        {
            return;
        }

        await ObtenerOCrearIncidenciaAsync(
            contexto,
            periodo,
            ana.IdColaborador,
            catalogos.TipoIncidenciaBono.IdTipoIncidenciaPlanilla,
            fechaActual,
            1m,
            40_000m,
            "Bono por cumplimiento. " + MarcaDemo,
            idUsuario,
            ahora);
        await ObtenerOCrearIncidenciaAsync(
            contexto,
            periodo,
            luis.IdColaborador,
            catalogos.TipoIncidenciaHoraExtra.IdTipoIncidenciaPlanilla,
            fechaActual,
            4m,
            25_000m,
            "Horas extra de la quincena. " + MarcaDemo,
            idUsuario,
            ahora);
        await ObtenerOCrearIncidenciaAsync(
            contexto,
            periodo,
            luis.IdColaborador,
            catalogos.TipoIncidenciaAusencia.IdTipoIncidenciaPlanilla,
            fechaActual,
            1m,
            15_000m,
            "Ausencia sin goce. " + MarcaDemo,
            idUsuario,
            ahora);
    }

    private static async Task<Proyecto> ObtenerOCrearProyectoAsync(
        ApplicationDbContext contexto,
        string codigo,
        string nombre,
        string descripcion,
        string responsable,
        string ubicacion,
        int idEstado,
        DateTime fechaInicio,
        DateTime fechaFinEstimada,
        DateTime? fechaFinReal,
        string idUsuario,
        DateTime ahora)
    {
        var proyecto = await contexto.Proyectos.FirstOrDefaultAsync(
            registro => registro.CodigoProyecto == codigo);
        if (proyecto is null)
        {
            proyecto = new Proyecto
            {
                CodigoProyecto = codigo,
                FechaCreacion = ahora,
                CreadoPor = idUsuario
            };
            contexto.Proyectos.Add(proyecto);
        }

        proyecto.NombreProyecto = nombre;
        proyecto.Descripcion = descripcion;
        proyecto.Responsable = responsable;
        proyecto.Ubicacion = ubicacion;
        proyecto.IdEstadoProyecto = idEstado;
        proyecto.FechaInicio = fechaInicio;
        proyecto.FechaFinEstimada = fechaFinEstimada;
        proyecto.FechaFinReal = fechaFinReal;
        proyecto.Observaciones = MarcaDemo;
        proyecto.EstadoRegistro = EstadosRegistro.Activo;

        if (proyecto.IdProyecto != 0)
        {
            proyecto.FechaModificacion = ahora;
            proyecto.ModificadoPor = idUsuario;
        }

        return proyecto;
    }

    private static async Task ObtenerOCrearPublicacionAsync(
        ApplicationDbContext contexto,
        Proyecto proyecto,
        string titulo,
        string descripcion,
        string estadoVisual,
        bool estaPublicado,
        string idUsuario,
        DateTime ahora)
    {
        var publicacion = await contexto.ProyectosPublicados.FirstOrDefaultAsync(
            registro => registro.IdProyecto == proyecto.IdProyecto);
        if (publicacion is null)
        {
            publicacion = new ProyectoPublicado
            {
                IdProyecto = proyecto.IdProyecto,
                FechaCreacion = ahora,
                CreadoPor = idUsuario
            };
            contexto.ProyectosPublicados.Add(publicacion);
        }

        publicacion.Titulo = titulo;
        publicacion.Descripcion = descripcion;
        publicacion.EstadoVisual = estadoVisual;
        publicacion.EstaPublicado = estaPublicado;
        publicacion.FechaPublicacion = estaPublicado ? ahora : null;
        publicacion.EstadoRegistro = EstadosRegistro.Activo;

        if (publicacion.IdProyectoPublicado != 0)
        {
            publicacion.FechaModificacion = ahora;
            publicacion.ModificadoPor = idUsuario;
        }
    }

    private static async Task<Activo> ObtenerOCrearActivoAsync(
        ApplicationDbContext contexto,
        string codigo,
        string nombre,
        int idTipo,
        int idCategoria,
        int idEstado,
        int idTipoMedicion,
        string marca,
        string modelo,
        string ubicacion,
        decimal lectura,
        decimal valor,
        DateTime fechaAdquisicion,
        string idUsuario,
        DateTime ahora)
    {
        var activo = await contexto.Activos.FirstOrDefaultAsync(
            registro => registro.CodigoActivo == codigo);
        if (activo is null)
        {
            activo = new Activo
            {
                CodigoActivo = codigo,
                FechaCreacion = ahora,
                CreadoPor = idUsuario
            };
            contexto.Activos.Add(activo);
        }

        activo.NombreActivo = nombre;
        activo.IdTipoActivo = idTipo;
        activo.IdCategoriaActivo = idCategoria;
        activo.IdEstadoActivo = idEstado;
        activo.IdTipoMedicionUso = idTipoMedicion;
        activo.Marca = marca;
        activo.Modelo = modelo;
        activo.Descripcion = $"{nombre} para operaciones de campo.";
        activo.FechaAdquisicion = fechaAdquisicion;
        activo.ValorAdquisicion = valor;
        activo.UbicacionActual = ubicacion;
        activo.LecturaUsoActual = lectura;
        activo.Observaciones = MarcaDemo;
        activo.EstadoRegistro = EstadosRegistro.Activo;

        if (activo.IdActivo != 0)
        {
            activo.FechaModificacion = ahora;
            activo.ModificadoPor = idUsuario;
        }

        return activo;
    }

    private static async Task<AsignacionActivoProyecto> ObtenerOCrearAsignacionAsync(
        ApplicationDbContext contexto,
        long idActivo,
        long idProyecto,
        DateTime fechaInicio,
        DateTime fechaFin,
        string observaciones,
        string idUsuario,
        DateTime ahora)
    {
        var asignacion = await contexto.AsignacionesActivoProyecto.FirstOrDefaultAsync(
            registro => registro.IdActivo == idActivo
                && registro.IdProyecto == idProyecto
                && registro.Observaciones == observaciones);
        if (asignacion is null)
        {
            asignacion = new AsignacionActivoProyecto
            {
                IdActivo = idActivo,
                IdProyecto = idProyecto,
                Observaciones = observaciones,
                AsignadoPor = idUsuario,
                FechaAsignacion = ahora
            };
            contexto.AsignacionesActivoProyecto.Add(asignacion);
        }

        asignacion.FechaInicio = fechaInicio;
        asignacion.FechaFin = fechaFin;
        asignacion.EstadoRegistro = EstadosRegistro.Activo;
        return asignacion;
    }

    private static async Task<Mantenimiento> ObtenerOCrearMantenimientoAsync(
        ApplicationDbContext contexto,
        long idActivo,
        long? idProyecto,
        string tipo,
        int idEstado,
        DateTime fechaProgramada,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string descripcion,
        decimal? costoEstimado,
        decimal? costoReal,
        decimal? tiempoFueraServicio,
        string? resultado,
        string responsable,
        string idUsuario,
        DateTime ahora)
    {
        var mantenimiento = await contexto.Mantenimientos.FirstOrDefaultAsync(
            registro => registro.IdActivo == idActivo
                && registro.Descripcion == descripcion);
        if (mantenimiento is null)
        {
            mantenimiento = new Mantenimiento
            {
                IdActivo = idActivo,
                Descripcion = descripcion,
                FechaCreacion = ahora,
                CreadoPor = idUsuario
            };
            contexto.Mantenimientos.Add(mantenimiento);
        }

        mantenimiento.IdProyecto = idProyecto;
        mantenimiento.TipoMantenimiento = tipo;
        mantenimiento.IdEstadoMantenimiento = idEstado;
        mantenimiento.FechaProgramada = fechaProgramada;
        mantenimiento.FechaInicio = fechaInicio;
        mantenimiento.FechaFin = fechaFin;
        mantenimiento.CostoEstimado = costoEstimado;
        mantenimiento.CostoReal = costoReal;
        mantenimiento.TiempoFueraServicioHoras = tiempoFueraServicio;
        mantenimiento.Resultado = resultado;
        mantenimiento.Responsable = responsable;
        mantenimiento.EstadoRegistro = EstadosRegistro.Activo;

        if (mantenimiento.IdMantenimiento != 0)
        {
            mantenimiento.FechaModificacion = ahora;
            mantenimiento.ModificadoPor = idUsuario;
        }

        return mantenimiento;
    }

    private static async Task<RegistroUsoActivo> ObtenerOCrearRegistroUsoAsync(
        ApplicationDbContext contexto,
        long idActivo,
        long idProyecto,
        DateTime fechaRegistro,
        decimal lecturaAnterior,
        decimal lecturaNueva,
        string observaciones,
        string idUsuario,
        DateTime fechaCreacion)
    {
        var registro = await contexto.RegistrosUsoActivo.FirstOrDefaultAsync(
            uso => uso.IdActivo == idActivo && uso.Observaciones == observaciones);
        if (registro is null)
        {
            registro = new RegistroUsoActivo
            {
                IdActivo = idActivo,
                Observaciones = observaciones,
                RegistradoPor = idUsuario,
                FechaCreacion = fechaCreacion
            };
            contexto.RegistrosUsoActivo.Add(registro);
        }

        registro.IdProyecto = idProyecto;
        registro.FechaRegistro = fechaRegistro;
        registro.LecturaAnterior = lecturaAnterior;
        registro.LecturaNueva = lecturaNueva;
        registro.CantidadUso = lecturaNueva - lecturaAnterior;
        registro.EstadoRegistro = EstadosRegistro.Activo;
        return registro;
    }

    private static async Task<Colaborador> ObtenerOCrearColaboradorAsync(
        ApplicationDbContext contexto,
        string codigo,
        string identificacion,
        string nombre,
        string apellido,
        string correo,
        CatalogosDemo catalogos,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var colaborador = await contexto.Colaboradores.FirstOrDefaultAsync(
            registro => registro.CodigoColaborador == codigo);
        if (colaborador is null)
        {
            colaborador = new Colaborador
            {
                CodigoColaborador = codigo,
                Identificacion = identificacion,
                FechaCreacion = ahora,
                CreadoPor = idUsuario
            };
            contexto.Colaboradores.Add(colaborador);
        }

        colaborador.TipoIdentificacion = "Cedula fisica";
        colaborador.Nombre = nombre;
        colaborador.PrimerApellido = apellido;
        colaborador.CorreoElectronico = correo;
        colaborador.Telefono = "8000-0000";
        colaborador.FechaIngreso = fechaActual.AddYears(-1);
        colaborador.IdEstadoLaboral = catalogos.EstadoLaboralActivo.IdEstadoLaboral;
        colaborador.IdDepartamento = catalogos.DepartamentoOperaciones.IdDepartamento;
        colaborador.IdPuesto = catalogos.PuestoOperativo.IdPuesto;
        colaborador.Observaciones = MarcaDemo;
        colaborador.EstadoRegistro = EstadosRegistro.Activo;

        if (colaborador.IdColaborador != 0)
        {
            colaborador.FechaModificacion = ahora;
            colaborador.ModificadoPor = idUsuario;
        }

        return colaborador;
    }

    private static async Task ObtenerOCrearContratoAsync(
        ApplicationDbContext contexto,
        long idColaborador,
        decimal salario,
        string idUsuario,
        DateTime fechaActual,
        DateTime ahora)
    {
        var contrato = await contexto.Contratos.FirstOrDefaultAsync(
            registro => registro.IdColaborador == idColaborador
                && registro.Observaciones == MarcaDemo);
        if (contrato is null)
        {
            contrato = new Contrato
            {
                IdColaborador = idColaborador,
                Observaciones = MarcaDemo,
                FechaCreacion = ahora,
                CreadoPor = idUsuario
            };
            contexto.Contratos.Add(contrato);
        }

        contrato.TipoContrato = "Tiempo indefinido";
        contrato.FechaInicio = fechaActual.AddYears(-1);
        contrato.FechaFin = null;
        contrato.SalarioBase = salario;
        contrato.Jornada = "Tiempo completo";
        contrato.PeriodicidadPago = PeriodicidadesPago.Quincenal;
        contrato.EstadoContrato = EstadosContrato.Activo;
        contrato.EstadoRegistro = EstadosRegistro.Activo;

        if (contrato.IdContrato != 0)
        {
            contrato.FechaModificacion = ahora;
            contrato.ModificadoPor = idUsuario;
        }
    }

    private static async Task ObtenerOCrearIncidenciaAsync(
        ApplicationDbContext contexto,
        PeriodoPlanilla periodo,
        long idColaborador,
        int idTipo,
        DateTime fecha,
        decimal cantidad,
        decimal monto,
        string descripcion,
        string idUsuario,
        DateTime ahora)
    {
        var incidencia = await contexto.IncidenciasPlanilla.FirstOrDefaultAsync(
            registro => registro.IdPeriodoPlanilla == periodo.IdPeriodoPlanilla
                && registro.IdColaborador == idColaborador
                && registro.IdTipoIncidenciaPlanilla == idTipo
                && registro.Descripcion == descripcion);
        if (incidencia is null)
        {
            incidencia = new IncidenciaPlanilla
            {
                IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
                IdColaborador = idColaborador,
                IdTipoIncidenciaPlanilla = idTipo,
                Descripcion = descripcion,
                RegistradoPor = idUsuario,
                FechaRegistro = ahora
            };
            contexto.IncidenciasPlanilla.Add(incidencia);
        }

        incidencia.FechaIncidencia = fecha;
        incidencia.Cantidad = cantidad;
        incidencia.Monto = monto;
        incidencia.EstadoRegistro = EstadosRegistro.Activo;
    }

    private static async Task RegistrarEventoSiNoExisteAsync(
        ApplicationDbContext contexto,
        string idUsuario,
        string accion,
        string entidad,
        string idRegistro,
        object valoresNuevos,
        string observacion,
        DateTime fechaHora)
    {
        var existe = await contexto.BitacoraAuditoria.AnyAsync(registro =>
            registro.Entidad == entidad
            && registro.IdRegistro == idRegistro
            && registro.Observacion == observacion);
        if (existe)
        {
            return;
        }

        contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = idUsuario,
            FechaHora = fechaHora,
            Accion = accion,
            Entidad = entidad,
            IdRegistro = idRegistro,
            ValoresNuevos = JsonSerializer.Serialize(valoresNuevos),
            Observacion = observacion
        });
    }

    private static async Task<CategoriaActivo> ObtenerCategoriaGeneralAsync(
        ApplicationDbContext contexto,
        int idTipoActivo) =>
        await ObtenerRequeridoAsync(
            contexto.CategoriasActivo,
            categoria => categoria.IdTipoActivo == idTipoActivo && categoria.Nombre == "General",
            "la categoría General del tipo de activo requerido");

    private static async Task<T> ObtenerRequeridoAsync<T>(
        DbSet<T> registros,
        System.Linq.Expressions.Expression<Func<T, bool>> filtro,
        string descripcion)
        where T : class
    {
        return await registros.FirstOrDefaultAsync(filtro)
            ?? throw new InvalidOperationException(
                $"No se encontró {descripcion} en el esquema oficial.");
    }

    private sealed record CatalogosDemo(
        EstadoProyecto EstadoProyectoPlanificado,
        EstadoProyecto EstadoProyectoEnEjecucion,
        EstadoProyecto EstadoProyectoFinalizado,
        EstadoActivo EstadoActivoDisponible,
        EstadoActivo EstadoActivoAsignado,
        EstadoMantenimiento EstadoMantenimientoProgramado,
        EstadoMantenimiento EstadoMantenimientoFinalizado,
        EstadoPlanilla EstadoPlanillaBorrador,
        EstadoLaboral EstadoLaboralActivo,
        Departamento DepartamentoOperaciones,
        Puesto PuestoOperativo,
        TipoActivo TipoMaquinaria,
        TipoActivo TipoVehiculo,
        TipoActivo TipoEquipo,
        TipoMedicionUso MedicionHoras,
        TipoMedicionUso MedicionKilometros,
        TipoIncidenciaPlanilla TipoIncidenciaBono,
        TipoIncidenciaPlanilla TipoIncidenciaHoraExtra,
        TipoIncidenciaPlanilla TipoIncidenciaAusencia);

    private sealed record ProyectosDemo(
        Proyecto ProyectoActivo,
        Proyecto ProyectoFinalizado,
        Proyecto ProyectoOculto);

    private sealed record ActivosDemo(Activo Excavadora, Activo Camion, Activo Bomba);
}

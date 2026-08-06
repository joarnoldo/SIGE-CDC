using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Web.Configuracion;

public static class ValidacionConexionMySql
{
    private static readonly string[] TablasRequeridas =
    [
        "AspNetUsers",
        "AspNetRoles",
        "AspNetUserRoles",
        "EstadoProyecto",
        "Proyecto",
        "EstadoActivo",
        "TipoActivo",
        "CategoriaActivo",
        "TipoMedicionUso",
        "Activo",
        "AsignacionActivoProyecto",
        "RegistroUsoActivo",
        "EstadoMantenimiento",
        "Mantenimiento",
        "Galeria",
        "ImagenGaleria",
        "DocumentoArchivo",
        "TipoDocumento",
        "EstadoPlanilla",
        "TipoIncidenciaPlanilla",
        "PeriodoPlanilla",
        "Planilla",
        "DetallePlanilla",
        "ColillaPago",
        "ForecastEscenario",
        "ForecastPeriodo",
        "ForecastFuenteHistorica",
        "ForecastParticipante",
        "ForecastAsignacionProyecto",
        "ForecastParametro",
        "ForecastDetalle",
        "IncidenciaPlanilla",
        "ParametroPlanilla",
        "ParametroPlanillaColaborador",
        "ParametroPlanillaPeriodo",
        "PaginaContenido",
        "Noticia",
        "FAQ",
        "ProyectoPublicado",
        "ConsultaContacto",
        "BitacoraAuditoria",
        "Colaborador",
        "Departamento",
        "Puesto",
        "Contrato"
    ];

    public static async Task ValidarConexionMySqlDesarrolloAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var contexto = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ValidacionConexionMySql");
        var conexion = contexto.Database.GetDbConnection();

        try
        {
            await conexion.OpenAsync();
            await ValidarTablasRequeridasAsync(conexion);
            await ValidarCatalogosRequeridosAsync(conexion);
            await ValidarEstructuraParametrosPlanillaAsync(conexion);
            await ValidarEstructuraPlanillaAsync(conexion);
            await ValidarEstructuraColillaPagoAsync(conexion);
            await ValidarEstructuraForecastAsync(conexion);
            await ValidarEstructuraAsignacionesActivoAsync(conexion);
            await ValidarEstructuraRegistroUsoActivoAsync(conexion);

            var periodosActivos = await EjecutarConteoAsync(
                conexion,
                "SELECT COUNT(*) FROM PeriodoPlanilla WHERE EstadoRegistro = 'Activo';");

            if (periodosActivos == 0)
            {
                logger.LogWarning(
                    "No hay períodos de planilla activos. El registro de incidencias permanecerá bloqueado hasta preparar datos válidos para la demo.");
            }

            logger.LogInformation("Conexión, tablas y catálogos de desarrollo validados correctamente.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "No se pudo abrir o validar la conexión local a MySQL con ConnectionStrings:DefaultConnection. " +
                "Verifique que MySQL esté iniciado, que localhost:3306 acepte el usuario configurado, que la contraseña guardada en User Secrets sea correcta, " +
                "que la base SIGE_CDC_DB exista, que se haya ejecutado el script SQL oficial completo y que la cadena incluya AllowPublicKeyRetrieval=True;SslMode=Disabled. " +
                "La contraseña no se muestra por seguridad.",
                ex);
        }
        finally
        {
            if (conexion.State != ConnectionState.Closed)
            {
                await conexion.CloseAsync();
            }
        }
    }

    private static async Task ValidarTablasRequeridasAsync(System.Data.Common.DbConnection conexion)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name IN (
                  'AspNetUsers', 'AspNetRoles', 'AspNetUserRoles', 'EstadoProyecto',
                  'Proyecto', 'EstadoActivo', 'TipoActivo', 'CategoriaActivo',
                  'TipoMedicionUso', 'Activo', 'AsignacionActivoProyecto',
                  'RegistroUsoActivo',
                  'EstadoMantenimiento', 'Mantenimiento',
                  'Galeria', 'ImagenGaleria', 'DocumentoArchivo',
                  'TipoDocumento', 'EstadoPlanilla', 'TipoIncidenciaPlanilla',
                  'PeriodoPlanilla', 'Planilla', 'DetallePlanilla', 'ColillaPago',
                  'ForecastEscenario', 'ForecastPeriodo', 'ForecastFuenteHistorica',
                  'ForecastParticipante', 'ForecastAsignacionProyecto',
                  'ForecastParametro', 'ForecastDetalle',
                  'IncidenciaPlanilla',
                  'ParametroPlanilla', 'ParametroPlanillaColaborador',
                  'ParametroPlanillaPeriodo', 'PaginaContenido', 'Noticia', 'FAQ',
                  'ProyectoPublicado', 'ConsultaContacto', 'BitacoraAuditoria',
                  'Colaborador', 'Departamento', 'Puesto', 'Contrato');
            """;

        var resultado = await comando.ExecuteScalarAsync();
        var totalTablas = Convert.ToInt32(resultado);

        if (totalTablas != TablasRequeridas.Length)
        {
            throw new InvalidOperationException(
                "La base SIGE_CDC_DB no contiene todas las tablas requeridas por los módulos integrados. " +
                "Ejecute el script SQL oficial completo antes de iniciar la aplicación.");
        }
    }

    private static async Task ValidarEstructuraParametrosPlanillaAsync(System.Data.Common.DbConnection conexion)
    {
        var columnaNaturaleza = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'ParametroPlanilla'
              AND column_name = 'Naturaleza';
            """);

        if (columnaNaturaleza == 0)
        {
            throw new InvalidOperationException(
                "Falta la columna ParametroPlanilla.Naturaleza requerida por la estabilización HU-RH-006.");
        }
    }

    private static async Task ValidarEstructuraPlanillaAsync(System.Data.Common.DbConnection conexion)
    {
        var columnaBeneficios = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'DetallePlanilla'
              AND column_name = 'TotalBeneficiosConfigurables'
              AND column_type = 'decimal(18,2)';
            """);

        if (columnaBeneficios == 0)
        {
            throw new InvalidOperationException(
                "Falta DetallePlanilla.TotalBeneficiosConfigurables requerida para calcular HU-RH-005.");
        }

        var estadosCalculo = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM EstadoPlanilla
            WHERE Nombre IN ('Borrador', 'Calculada')
              AND EstadoRegistro = 'Activo';
            """);

        if (estadosCalculo != 2)
        {
            throw new InvalidOperationException(
                "Los estados Borrador y Calculada deben existir y estar activos para HU-RH-005.");
        }
    }

    private static async Task ValidarEstructuraForecastAsync(
        System.Data.Common.DbConnection conexion)
    {
        var columnasBase = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND (
                  (table_name = 'ForecastEscenario'
                   AND column_name IN (
                       'IdForecastEscenario', 'Nombre', 'Descripcion',
                       'FechaInicioProyeccion', 'FechaFinProyeccion',
                       'PeriodosHistoricosConsiderados', 'EstadoEscenario',
                       'MontoProyectadoTotal', 'MontoRealTotal', 'DiferenciaTotal',
                       'FechaCalculo', 'FechaCreacion', 'CreadoPor',
                       'FechaModificacion', 'ModificadoPor', 'EstadoRegistro'))
                  OR
                  (table_name = 'ForecastPeriodo'
                   AND column_name IN (
                       'IdForecastPeriodo', 'IdForecastEscenario', 'NumeroOrden',
                       'TipoPeriodo', 'FechaInicio', 'FechaFin', 'EstadoRegistro'))
                  OR
                  (table_name = 'ForecastFuenteHistorica'
                   AND column_name IN (
                       'IdForecastFuenteHistorica', 'IdForecastEscenario',
                       'IdPlanilla', 'NumeroOrden'))
                  OR
                  (table_name = 'ForecastParametro'
                   AND column_name IN (
                       'IdForecastParametro', 'IdForecastEscenario', 'Codigo',
                       'Nombre', 'TipoParametro', 'ValorDecimal', 'ValorTexto',
                       'Descripcion', 'EstadoRegistro'))
                  OR
                  (table_name = 'ForecastParticipante'
                   AND column_name IN (
                       'IdForecastParticipante', 'IdForecastEscenario',
                       'CodigoParticipante', 'Etiqueta', 'TipoParticipante',
                       'IdColaborador', 'IdDepartamento', 'IdPuesto',
                       'SalarioBaseMensual', 'FechaInicioAplicacion',
                       'FechaSalidaPrevista', 'EstaIncluido', 'EstadoRegistro'))
                  OR
                  (table_name = 'ForecastAsignacionProyecto'
                   AND column_name IN (
                       'IdForecastAsignacionProyecto', 'IdForecastEscenario',
                       'IdForecastPeriodo', 'IdForecastParticipante',
                       'IdProyecto', 'Porcentaje', 'EstadoRegistro'))
              );
            """);

        if (columnasBase != 56)
        {
            throw new InvalidOperationException(
                "El esquema no contiene la estructura oficial de escenarios, períodos, fuentes históricas, parámetros, participantes y asignaciones por proyecto de forecast.");
        }

        var restriccionesBase = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = DATABASE()
              AND constraint_name IN (
                  'CK_ForecastEscenario_Fechas',
                  'CK_ForecastEscenario_Historicos',
                  'UX_ForecastPeriodo_Escenario_Orden',
                  'UX_ForecastPeriodo_Escenario_Fechas',
                  'UX_ForecastPeriodo_Escenario_Id',
                  'CK_ForecastPeriodo_Orden',
                  'CK_ForecastPeriodo_Fechas',
                  'FK_ForecastPeriodo_ForecastEscenario',
                  'UX_ForecastFuente_Escenario_Planilla',
                  'UX_ForecastFuente_Escenario_Orden',
                  'CK_ForecastFuente_Orden',
                  'FK_ForecastFuente_ForecastEscenario',
                  'FK_ForecastFuente_Planilla',
                  'UX_ForecastParametro_Escenario_Codigo',
                  'FK_ForecastParametro_ForecastEscenario',
                  'UX_ForecastParticipante_Escenario_Codigo',
                  'UX_ForecastParticipante_Escenario_Colaborador',
                  'UX_ForecastParticipante_Escenario_Id',
                  'CK_ForecastParticipante_Tipo',
                  'CK_ForecastParticipante_Salario',
                  'CK_ForecastParticipante_Fechas',
                  'CK_ForecastParticipante_Inclusion',
                   'FK_ForecastParticipante_ForecastEscenario',
                   'FK_ForecastParticipante_Colaborador',
                   'FK_ForecastParticipante_Departamento',
                   'FK_ForecastParticipante_Puesto',
                   'UX_FcstAsign_Esc_Per_Part_Proy',
                   'CK_ForecastAsignacionProyecto_Porcentaje',
                   'FK_FcstAsign_Participante',
                   'FK_FcstAsign_Periodo',
                   'FK_FcstAsign_Proyecto');
            """);

        if (restriccionesBase != 31)
        {
            throw new InvalidOperationException(
                "Faltan índices, controles o relaciones oficiales en la base estructural de forecast.");
        }

        var indicesSecundariosForecast = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(DISTINCT CONCAT(table_name, '.', index_name))
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND (
                  (table_name = 'ForecastParticipante'
                   AND index_name IN (
                       'IX_ForecastParticipante_IdDepartamento',
                       'IX_ForecastParticipante_IdPuesto'))
                  OR
                  (table_name = 'ForecastAsignacionProyecto'
                   AND index_name IN (
                       'IX_FcstAsign_Esc_Part',
                       'IX_FcstAsign_Esc_Per',
                       'IX_ForecastAsignacionProyecto_IdProyecto')));
            """);

        if (indicesSecundariosForecast != 5)
        {
            throw new InvalidOperationException(
                "Faltan índices secundarios oficiales de participantes o asignaciones por proyecto de forecast.");
        }

        var columnasDetalleNuevas = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'ForecastDetalle'
              AND column_name IN (
                  'IdForecastDetalle', 'IdForecastEscenario',
                  'IdForecastPeriodo', 'IdForecastParticipante',
                  'IdProyecto', 'Concepto', 'MontoBase', 'MontoAjuste',
                  'MontoProyectado', 'MontoReal', 'Diferencia',
                  'Observaciones', 'EstadoRegistro');
            """);

        var columnaDetalleAnterior = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'ForecastDetalle'
              AND column_name = 'IdColaborador';
            """);

        var restriccionesDetalle = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = DATABASE()
              AND table_name = 'ForecastDetalle'
              AND constraint_name IN (
                  'UX_FcstDetalle_Esc_Per_Part_Proy_Concepto',
                  'FK_FcstDetalle_AsignacionProyecto');
            """);

        if (columnasDetalleNuevas != 13
            || columnaDetalleAnterior != 0
            || restriccionesDetalle != 2)
        {
            throw new InvalidOperationException(
                "ForecastDetalle no corresponde al contrato estructural aprobado en F-SQL-01.");
        }
    }

    private static async Task ValidarEstructuraAsignacionesActivoAsync(System.Data.Common.DbConnection conexion)
    {
        var columnasAsignacion = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'AsignacionActivoProyecto'
              AND column_name IN (
                  'IdAsignacionActivoProyecto', 'IdActivo', 'IdProyecto',
                  'FechaInicio', 'FechaFin', 'Observaciones', 'AsignadoPor',
                  'FechaAsignacion', 'EstadoRegistro');
            """);

        if (columnasAsignacion != 9)
        {
            throw new InvalidOperationException(
                "La tabla AsignacionActivoProyecto no contiene la estructura oficial requerida para HU-ACT-005.");
        }

        var relacionesAsignacion = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = DATABASE()
              AND table_name = 'AsignacionActivoProyecto'
              AND constraint_type = 'FOREIGN KEY'
              AND constraint_name IN (
                  'FK_AsignacionActivoProyecto_Activo',
                  'FK_AsignacionActivoProyecto_Proyecto',
                  'FK_AsignacionActivoProyecto_AsignadoPor');
            """);

        if (relacionesAsignacion != 3)
        {
            throw new InvalidOperationException(
                "Faltan relaciones oficiales en AsignacionActivoProyecto para activo, proyecto o usuario responsable.");
        }

        var estadosActivo = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM EstadoActivo
            WHERE Nombre IN (
                'Disponible', 'Asignado', 'En mantenimiento',
                'Fuera de servicio', 'Dado de baja')
              AND EstadoRegistro = 'Activo';
            """);

        if (estadosActivo != 5)
        {
            throw new InvalidOperationException(
                "Los cinco estados oficiales de activo deben existir y estar activos para HU-ACT-005.");
        }
    }

    private static async Task ValidarCatalogosRequeridosAsync(System.Data.Common.DbConnection conexion)
    {
        var estadoPlanificado = await EjecutarConteoAsync(
            conexion,
            "SELECT COUNT(*) FROM EstadoProyecto WHERE Nombre = 'Planificado' AND EstadoRegistro = 'Activo';");

        if (estadoPlanificado == 0)
        {
            throw new InvalidOperationException("Falta el estado de proyecto Planificado activo.");
        }

        var tipoImagen = await EjecutarConteoAsync(
            conexion,
            "SELECT COUNT(*) FROM TipoDocumento WHERE Nombre IN ('Imagen galería', 'Imagen galeria') AND EstadoRegistro = 'Activo';");

        if (tipoImagen == 0)
        {
            throw new InvalidOperationException("Falta el tipo de documento Imagen galería activo.");
        }

        var estadosMantenimiento = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM EstadoMantenimiento
            WHERE Nombre IN ('Programado', 'En proceso', 'Finalizado', 'Cancelado')
              AND EstadoRegistro = 'Activo';
            """);

        if (estadosMantenimiento != 4)
        {
            throw new InvalidOperationException(
                "Los cuatro estados oficiales de mantenimiento deben existir y estar activos.");
        }

        var tipoEvidenciaMantenimiento = await EjecutarConteoAsync(
            conexion,
            "SELECT COUNT(*) FROM TipoDocumento WHERE Nombre = 'Evidencia mantenimiento' AND EstadoRegistro = 'Activo';");

        if (tipoEvidenciaMantenimiento != 1)
        {
            throw new InvalidOperationException(
                "El tipo de documento Evidencia mantenimiento debe existir y estar activo.");
        }

        var tiposMedicionUso = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM TipoMedicionUso
            WHERE Nombre IN ('Horas', 'Kilómetros', 'Unidades', 'No aplica')
              AND EstadoRegistro = 'Activo';
            """);

        if (tiposMedicionUso != 4)
        {
            throw new InvalidOperationException(
                "Los cuatro tipos oficiales de medición de uso deben existir y estar activos.");
        }
    }

    private static async Task ValidarEstructuraColillaPagoAsync(
        System.Data.Common.DbConnection conexion)
    {
        var columnas = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'ColillaPago'
              AND column_name IN (
                  'IdColillaPago', 'IdDetallePlanilla', 'CodigoColilla',
                  'RutaArchivo', 'FechaGeneracion', 'GeneradoPor', 'EstadoRegistro');
            """);

        if (columnas != 7)
        {
            throw new InvalidOperationException(
                "La tabla ColillaPago no contiene la estructura oficial requerida para HU-RH-007.");
        }

        var relaciones = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = DATABASE()
              AND table_name = 'ColillaPago'
              AND constraint_name IN (
                  'UX_ColillaPago_IdDetallePlanilla',
                  'UX_ColillaPago_CodigoColilla',
                  'FK_ColillaPago_DetallePlanilla',
                  'FK_ColillaPago_GeneradoPor');
            """);

        if (relaciones != 4)
        {
            throw new InvalidOperationException(
                "Faltan índices únicos o relaciones oficiales en ColillaPago para HU-RH-007.");
        }
    }

    private static async Task ValidarEstructuraRegistroUsoActivoAsync(
        System.Data.Common.DbConnection conexion)
    {
        var columnasRegistro = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'RegistroUsoActivo'
              AND column_name IN (
                  'IdRegistroUsoActivo', 'IdActivo', 'IdProyecto',
                  'FechaRegistro', 'LecturaAnterior', 'LecturaNueva',
                  'CantidadUso', 'Observaciones', 'RegistradoPor',
                  'FechaCreacion', 'EstadoRegistro');
            """);

        if (columnasRegistro != 11)
        {
            throw new InvalidOperationException(
                "La tabla RegistroUsoActivo no contiene la estructura oficial requerida para HU-ACT-008.");
        }

        var columnasActivo = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'Activo'
              AND column_name IN ('IdTipoMedicionUso', 'LecturaUsoActual');
            """);

        if (columnasActivo != 2)
        {
            throw new InvalidOperationException(
                "La tabla Activo no contiene los campos oficiales de medición requeridos para HU-ACT-008.");
        }

        var decimalesUso = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND column_type = 'decimal(18,2)'
              AND (
                  (table_name = 'Activo' AND column_name = 'LecturaUsoActual')
                  OR
                  (table_name = 'RegistroUsoActivo'
                   AND column_name IN ('LecturaAnterior', 'LecturaNueva', 'CantidadUso')));
            """);

        if (decimalesUso != 4)
        {
            throw new InvalidOperationException(
                "Las lecturas de uso no utilizan la precisión DECIMAL(18,2) definida por el SQL oficial.");
        }

        var relacionesRegistro = await EjecutarConteoAsync(
            conexion,
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = DATABASE()
              AND constraint_type = 'FOREIGN KEY'
              AND (
                  (table_name = 'Activo'
                   AND constraint_name = 'FK_Activo_TipoMedicionUso')
                  OR
                  (table_name = 'RegistroUsoActivo'
                   AND constraint_name IN (
                       'FK_RegistroUsoActivo_Activo',
                       'FK_RegistroUsoActivo_Proyecto',
                       'FK_RegistroUsoActivo_RegistradoPor')));
            """);

        if (relacionesRegistro != 4)
        {
            throw new InvalidOperationException(
                "Faltan relaciones oficiales de medición o registro de uso para HU-ACT-008.");
        }
    }

    private static async Task<int> EjecutarConteoAsync(
        System.Data.Common.DbConnection conexion,
        string consultaSql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = consultaSql;
        var resultado = await comando.ExecuteScalarAsync();
        return Convert.ToInt32(resultado);
    }
}

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
        "Galeria",
        "ImagenGaleria",
        "DocumentoArchivo",
        "TipoDocumento",
        "EstadoPlanilla",
        "TipoIncidenciaPlanilla",
        "PeriodoPlanilla",
        "Planilla",
        "DetallePlanilla",
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
                  'Proyecto', 'Galeria', 'ImagenGaleria', 'DocumentoArchivo',
                  'TipoDocumento', 'EstadoPlanilla', 'TipoIncidenciaPlanilla',
                  'PeriodoPlanilla', 'Planilla', 'DetallePlanilla', 'IncidenciaPlanilla',
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
                "La base SIGE_CDC_DB no contiene todas las tablas requeridas para Sprint 1, Sprint 2 y la estabilización HU-RH-006. " +
                "Ejecute el script SQL oficial y el DDL aprobado de HU-RH-006 antes de iniciar la aplicación.");
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

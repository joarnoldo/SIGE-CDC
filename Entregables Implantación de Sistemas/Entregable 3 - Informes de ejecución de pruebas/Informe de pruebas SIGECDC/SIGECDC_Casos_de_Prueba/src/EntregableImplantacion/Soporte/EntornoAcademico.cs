using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Operaciones;
using SIGECDC.Application.Planillas;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Infrastructure.Archivos;
using SIGECDC.Infrastructure.Reportes;
using SIGECDC.Persistence;
using SIGECDC.Persistence.Identity;
using SIGECDC.Tests.Finanzas;
using SIGECDC.Web.Configuracion;
using SIGECDC.Web.Components.Account;

namespace SIGECDC.Tests.EntregableImplantacion.Soporte;

[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionAcademica : ICollectionFixture<ServidorAcademico>
{
    public const string Nombre = "Entregable Implantación — MySQL aislado";
}

// Única reutilización de la suite anterior: infraestructura genérica de MySQL,
// sin casos, datasets, oráculos ni resultados anteriores.
public sealed class ServidorAcademico : IAsyncLifetime
{
    internal Fin02BMySqlSandbox Sandbox { get; } = new();
    public Task InitializeAsync() => ConexionRevisionAcademica.EsRemoto ? Task.CompletedTask : Sandbox.InitializeAsync();
    public Task DisposeAsync() => ConexionRevisionAcademica.EsRemoto ? Task.CompletedTask : Sandbox.DisposeAsync();
    public async Task<EntornoAcademico> NuevoAsync()
    {
        if (ConexionRevisionAcademica.EsRemoto) throw new InvalidOperationException("BLOCKED: caso con escritura local; no se admite contra MonsterASP. Use modo LocalAislado.");
        var entorno = new EntornoAcademico(Sandbox, await Sandbox.CrearAsync(actual: true));
        try { await entorno.InicializarAsync(); return entorno; }
        catch { await entorno.DisposeAsync(); throw; }
    }
}

public sealed class EntornoAcademico : IAsyncDisposable
{
    public const string ClaveSintetica = "Universidad-QA!42";
    public static readonly string[] Roles = ["Administrador", "Recursos Humanos", "Operaciones", "Empleado"];
    public const string Admin = "academico-admin", Rrhh = "academico-rrhh", Ops = "academico-ops", Empleado = "academico-empleado";
    public static readonly DateTime Inicio = new(2032, 1, 1);
    private readonly IServiceScope scope;
    public string Archivos { get; } = Path.Combine(Path.GetTempPath(), "SIGECDC-Academico-" + Guid.NewGuid().ToString("N"));
    public ServiceProvider Servicios { get; }
    public ApplicationDbContext Db => Obtener<ApplicationDbContext>();
    public T Obtener<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    public ApplicationDbContext LecturaNueva() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(ConexionLocal).Options);
    public string ConexionLocal { get; }

    internal EntornoAcademico(Fin02BMySqlSandbox servidor, string nombre)
    {
        using var contexto = servidor.CrearContexto(nombre);
        // Pool exclusivo de la base sintética de este TC: evita abrir miles de sockets
        // cuando EF cierra y vuelve a abrir conexiones entre operaciones cortas.
        ConexionLocal = new MySqlConnectionStringBuilder(contexto.Database.GetConnectionString()!)
        { Pooling = true, MinimumPoolSize = 0, MaximumPoolSize = 10 }.ConnectionString;
        var servicios = new ServiceCollection();
        Configurar(servicios);
        servicios.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager, NavegacionAcademica>();
        Servicios = servicios.BuildServiceProvider();
        scope = Servicios.CreateScope();
    }

    public void Configurar(IServiceCollection servicios)
    {
        servicios.AddLogging(b => b.ClearProviders());
        servicios.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        servicios.AgregarPersistencia(ConexionLocal);
        servicios.AddSingleton<IAlmacenamientoArchivosService>(new AlmacenamientoArchivosLocal(Archivos));
        servicios.AddSingleton<IGeneradorColillaPdf, GeneradorColillaPdf>();
        servicios.AddSingleton<IGeneradorReportePlanillaPdf, GeneradorReportePlanillaPdf>();
        servicios.AddSingleton<IGeneradorReportePlanillaExcel, GeneradorReportePlanillaExcel>();
        servicios.AddSingleton(new DisponibilidadFuncionesFinancieras(true, true));
        servicios.AddHttpContextAccessor();
        servicios.AddAuthentication(o => { o.DefaultScheme = IdentityConstants.ApplicationScheme; o.DefaultSignInScheme = IdentityConstants.ExternalScheme; }).AddIdentityCookies();
        servicios.AddAuthorization();
        servicios.AddIdentityCore<ApplicationUser>(o =>
        {
            o.SignIn.RequireConfirmedAccount = true; o.User.RequireUniqueEmail = true;
            o.Lockout.MaxFailedAccessAttempts = 5; o.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>()
          .AddSignInManager<UsuarioActivoSignInManager>().AddDefaultTokenProviders();
    }

    internal async Task InicializarAsync()
    {
        var roles = Obtener<RoleManager<IdentityRole>>();
        var usuarios = Obtener<UserManager<ApplicationUser>>();
        var ids = new[] { Admin, Rrhh, Ops, Empleado };
        for (var i = 0; i < Roles.Length; i++)
        {
            if (!await roles.RoleExistsAsync(Roles[i])) Verificar(await roles.CreateAsync(new(Roles[i])));
            var usuario = new ApplicationUser { Id = ids[i], UserName = ids[i] + "@example.invalid", Email = ids[i] + "@example.invalid", EmailConfirmed = true };
            Verificar(await usuarios.CreateAsync(usuario, ClaveSintetica));
            Verificar(await usuarios.AddToRoleAsync(usuario, Roles[i]));
        }
    }

    private static void Verificar(IdentityResult resultado)
    {
        if (!resultado.Succeeded) throw new InvalidOperationException("Preparación de identidad sintética: " + string.Join(",", resultado.Errors.Select(e => e.Code)));
    }

    public async Task<SolicitudColaborador> DatosColaboradorAsync(string codigo = "ACA-01")
    {
        var departamentos = Obtener<IDepartamentoService>();
        if (!(await departamentos.ObtenerTodosAsync()).Any(d => d.Nombre == "Departamento académico"))
            await departamentos.RegistrarAsync(new() { Nombre = "Departamento académico" });
        var departamento = (await departamentos.ObtenerTodosAsync()).Single(d => d.Nombre == "Departamento académico");
        var puestos = Obtener<IPuestoService>();
        if (!(await puestos.ObtenerTodosAsync()).Any(p => p.Nombre == "Técnico académico"))
            await puestos.RegistrarAsync(new() { Nombre = "Técnico académico", IdDepartamento = departamento.IdDepartamento });
        var puesto = (await puestos.ObtenerTodosAsync()).Single(p => p.Nombre == "Técnico académico");
        return new() { CodigoColaborador = codigo, Identificacion = "SINTETICO-" + codigo, Nombre = "Persona " + codigo,
            PrimerApellido = "Académica", CorreoElectronico = codigo.ToLowerInvariant() + "@example.invalid", Telefono = "00000000",
            FechaIngreso = new DateTime(2024, 1, 1), IdDepartamento = departamento.IdDepartamento, IdPuesto = puesto.IdPuesto,
            IdEstadoLaboral = (await Obtener<IColaboradorService>().ObtenerEstadosLaboralesAsync()).Single(e => e.Nombre == "Activo").Id };
    }

    public async Task<long> ColaboradorAsync(string codigo = "ACA-01") =>
        await Obtener<IColaboradorService>().RegistrarColaboradorAsync(await DatosColaboradorAsync(codigo), Rrhh);

    public async Task<long> ActivoAsync(string codigo = "ACT-ACA-01")
    {
        var servicio = Obtener<IActivoService>();
        var tipo = (await servicio.ObtenerTiposActivoAsync()).First();
        var categoria = (await servicio.ObtenerCategoriasPorTipoAsync(tipo.IdTipoActivo)).First();
        return await servicio.RegistrarActivoAsync(new() { CodigoActivo = codigo, NombreActivo = "Equipo " + codigo,
            IdTipoActivo = tipo.IdTipoActivo, IdCategoriaActivo = categoria.IdCategoriaActivo, UbicacionActual = "Bodega académica" }, Ops);
    }

    public async Task<long> ProyectoAsync(string codigo = "PRO-ACA-01")
    {
        var servicio = Obtener<IProyectoService>();
        await servicio.GuardarProyectoAsync(new() { CodigoProyecto = codigo, NombreProyecto = "Proyecto " + codigo,
            FechaInicio = Inicio, FechaFinEstimada = Inicio.AddMonths(2).AddDays(-1),
            IdEstadoProyecto = (await servicio.ObtenerEstadosAsync()).Single(e => e.Nombre == "Planificado").IdEstadoProyecto }, Ops);
        return (await servicio.ObtenerProyectosAsync()).Single(p => p.CodigoProyecto == codigo).IdProyecto;
    }

    public static byte[] PdfSintetico()
    {
        using var pdf = new PdfSharp.Pdf.PdfDocument(); pdf.Info.Title = "Evidencia académica sintética"; pdf.AddPage();
        using var salida = new MemoryStream(); pdf.Save(salida, false); return salida.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        scope.Dispose(); await Servicios.DisposeAsync();
        using (var conexion = new MySqlConnection(ConexionLocal)) MySqlConnection.ClearPool(conexion);
        // Solo este directorio, creado con GUID por esta instancia; nunca rutas del producto.
        var ruta = Path.GetFullPath(Archivos);
        var temporal = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!ruta.StartsWith(temporal, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(ruta).StartsWith("SIGECDC-Academico-", StringComparison.Ordinal))
            throw new InvalidOperationException("Ruta temporal fuera del ámbito académico.");
        if (Directory.Exists(ruta)) Directory.Delete(ruta, recursive: true);
    }
}

// Contexto de navegación del renderizador de componentes; el host HTTP usa el suyo real.
internal sealed class NavegacionAcademica : Microsoft.AspNetCore.Components.NavigationManager
{
    public NavegacionAcademica() => Initialize("http://localhost/", "http://localhost/");
    protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).AbsoluteUri;
}

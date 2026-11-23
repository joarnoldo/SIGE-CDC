using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Tests.Finanzas;

[CollectionDefinition("F1-FIN-02B MySQL desechable", DisableParallelization = true)]
public sealed class Fin02BCollection : ICollectionFixture<Fin02BMySqlSandbox>;

// Instancia propia, nunca reutiliza el endpoint/credenciales de SIGECDC_QA_CONNECTION.
// Ausencia de mysqld/mysqldump es un fallo de infraestructura, no un test omitido.
public sealed class Fin02BMySqlSandbox : IAsyncLifetime
{
    private Process? servidor;
    private string clave = "";
    private string uuid = "";
    private uint puerto;
    private readonly HashSet<string> bases = [];
    public string Evidencia { get; } = Path.Combine(Path.GetTempPath(), "SIGECDC-FIN02BR-" + Guid.NewGuid().ToString("N"));
    public static string Raiz { get; } = BuscarRaiz();
    public static string Patch(string nombre) => File.ReadAllText(Path.Combine(Raiz, "database", "patches", $"20260831_F1_FIN_02B_{nombre}.sql"));
    public static string Oficial => File.ReadAllText(Path.Combine(Raiz, "database", "ScriptDB_SIGE_CDC_Corregido.sql")).Replace("\r\n", "\n");
    public static string Anterior => Oficial[..Oficial.IndexOf("/* ============================================================\n   12. MODELO", StringComparison.Ordinal)];
    public static string AnteriorCondiciones => Oficial[..Oficial.IndexOf("/* F1-FIN-02D: condiciones laborales efectivas.", StringComparison.Ordinal)];
    public static string AnteriorConfirmaciones => Oficial[..Oficial.IndexOf("/* F1-FIN-02F: confirmaciones inmutables", StringComparison.Ordinal)];
    public static string AnteriorSnapshotJ => Oficial[..Oficial.IndexOf("/* F1-FIN-02J: evidencia de snapshot", StringComparison.Ordinal)];
    public static string AnteriorForecastL => Oficial[..Oficial.IndexOf("/* F1-FIN-02L: evidencia Forecast", StringComparison.Ordinal)];
    public static string AnteriorComparacionM => Oficial[..Oficial.IndexOf("/* F1-FIN-02M: comparacion persistida", StringComparison.Ordinal)];

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Evidencia);
        var datos = Path.Combine(Evidencia, "mysql-data");
        Directory.CreateDirectory(datos);
        var exe = Binario("mysqld");
        var init = await ProcesoAsync(exe, ["--no-defaults", "--initialize-insecure", "--datadir=" + datos], 120);
        if (init.Codigo != 0) throw new InvalidOperationException("No se pudo inicializar MySQL desechable. Revisar log en " + Evidencia);
        var escucha = new TcpListener(IPAddress.Loopback, 0);
        escucha.Start();
        puerto = (uint)((IPEndPoint)escucha.LocalEndpoint).Port;
        escucha.Stop();
        var info = Inicio(exe, ["--no-defaults", "--datadir=" + datos, "--port=" + puerto,
            "--bind-address=127.0.0.1", "--mysqlx=OFF", "--skip-log-bin",
            "--log-error=" + Path.Combine(Evidencia, "mysql.log")]);
        servidor = Process.Start(info) ?? throw new InvalidOperationException("MySQL temporal no inició.");
        // auto.cnf procede exclusivamente del datadir recién inicializado.
        uuid = File.ReadAllLines(Path.Combine(datos, "auto.cnf"))
            .Single(linea => linea.StartsWith("server-uuid=", StringComparison.Ordinal)).Split('=')[1].Trim();
        for (var intento = 0; ; intento++)
        {
            if (servidor.HasExited) throw new InvalidOperationException("MySQL temporal terminó. Revisar " + Evidencia);
            try
            {
                await using var conexion = await AbrirAsync();
                clave = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                await new MySqlCommand("ALTER USER 'root'@'localhost' IDENTIFIED BY '" + clave + "'", conexion).ExecuteNonQueryAsync();
                break;
            }
            catch (MySqlException) when (intento < 90) { await Task.Delay(500); }
        }
        await using var comprobacion = await AbrirAsync();
        var version = Convert.ToString(await new MySqlCommand("SELECT VERSION()", comprobacion).ExecuteScalarAsync());
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "entorno.txt"),
            $"MySQL {version}; localhost:{puerto}; UUID {uuid}; instancia temporal nueva; solo fixtures sintéticos.\n");
    }

    public async Task DisposeAsync()
    {
        if (servidor is { HasExited: false })
        {
            try
            {
                await using var conexion = await AbrirAsync();
                await new MySqlCommand("SHUTDOWN", conexion).ExecuteNonQueryAsync();
            }
            catch (MySqlException) { /* Solo este proceso se termina abajo si no respondió. */ }
            if (!servidor.WaitForExit(10000)) servidor.Kill(entireProcessTree: true);
        }
        servidor?.Dispose();
        // Backups/logs sintéticos permanecen para evidencia; no borrar rutas amplias.
    }

    public async Task<MySqlConnection> AbrirAsync(string? baseDatos = null)
    {
        if (baseDatos is not null) ValidarBase(baseDatos);
        var cadena = new MySqlConnectionStringBuilder
        {
            Server = "127.0.0.1", Port = puerto, UserID = "root", Password = clave,
            Database = baseDatos ?? "", SslMode = MySqlSslMode.Disabled,
            AllowPublicKeyRetrieval = true, Pooling = false, ConnectionTimeout = 3,
            AllowUserVariables = true,
            DefaultCommandTimeout = 60
        };
        var conexion = new MySqlConnection(cadena.ConnectionString);
        try
        {
            await conexion.OpenAsync();
            var actual = Convert.ToString(await new MySqlCommand("SELECT @@server_uuid", conexion).ExecuteScalarAsync());
            if (actual != uuid) throw new InvalidOperationException("El endpoint no pertenece al sandbox creado por esta suite.");
            return conexion;
        }
        catch { await conexion.DisposeAsync(); throw; }
    }

    // Para pruebas EF de servicios: ConnectionString de una conexión abierta oculta la contraseña.
    // Las credenciales efímeras permanecen privadas en este fixture y nunca se registran.
    public ApplicationDbContext CrearContexto(string baseDatos, Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor? interceptor = null)
    {
        ValidarBase(baseDatos);
        var cadena = new MySqlConnectionStringBuilder
        {
            Server = "127.0.0.1", Port = puerto, UserID = "root", Password = clave, Database = baseDatos,
            SslMode = MySqlSslMode.Disabled, AllowPublicKeyRetrieval = true, Pooling = false,
            ConnectionTimeout = 3, AllowUserVariables = true, DefaultCommandTimeout = 60
        };
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(cadena.ConnectionString);
        if (interceptor is not null) opciones.AddInterceptors(interceptor);
        return new ApplicationDbContext(opciones.Options);
    }

    public async Task<string> CrearAsync(bool actual = false, bool condiciones = true, bool confirmaciones = true, bool snapshotJ = true, bool forecastL = true, bool comparacionM = true)
    {
        var nombre = "sige_cdc_db_qa_fin02br_" + Guid.NewGuid().ToString("N");
        bases.Add(nombre);
        await using var conexion = await AbrirAsync();
        await EjecutarAsync(conexion, "CREATE DATABASE `" + nombre + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
        await using var destino = await AbrirAsync(nombre);
        await EjecutarAsync(destino, Redirigir(actual ? (condiciones ? (confirmaciones ? (snapshotJ ? (forecastL ? (comparacionM ? Oficial : AnteriorComparacionM) : AnteriorForecastL) : AnteriorSnapshotJ) : AnteriorConfirmaciones) : AnteriorCondiciones) : Anterior, nombre));
        return nombre;
    }

    public static string Redirigir(string sql, string nombre) => sql.Replace("SIGE_CDC_DB", nombre, StringComparison.Ordinal);

    public async Task<string> BackupAsync(string nombre)
    {
        ValidarBase(nombre);
        await using var conexion = await AbrirAsync(nombre); // confirma instancia propia antes del dump
        var huella = await HuellaAsync(conexion);
        var ruta = Path.Combine(Evidencia, nombre + "-pre.sql");
        var resultado = await ProcesoAsync(Binario("mysqldump"), ["--no-defaults", "--host=127.0.0.1", "--port=" + puerto,
            "--user=root", "--single-transaction", "--skip-lock-tables", "--no-tablespaces", "--set-gtid-purged=OFF",
            "--skip-comments", nombre], 90, clave);
        if (resultado.Codigo != 0) throw new InvalidOperationException("Falló mysqldump del sandbox: " + resultado.Error);
        await File.WriteAllTextAsync(ruta, resultado.Salida);
        await File.WriteAllTextAsync(ruta + ".huella.json", huella);
        await File.WriteAllTextAsync(ruta + ".sha256", Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(ruta))));
        return ruta;
    }

    public async Task RestaurarAsync(string nombre, string backup)
    {
        ValidarBase(nombre);
        if (Path.GetFullPath(backup) != Path.Combine(Evidencia, nombre + "-pre.sql"))
            throw new InvalidOperationException("Backup no pertenece a esta base sintética.");
        var hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(backup)));
        if (hash != await File.ReadAllTextAsync(backup + ".sha256")) throw new InvalidOperationException("Backup alterado.");
        await using var conexion = await AbrirAsync(nombre);
        await FijarAsync(conexion, "fin02b_post_upgrade", await File.ReadAllTextAsync(backup + ".post.json"));
        var control = await EjecutarAsync(conexion, Patch("Rollback_PreData"));
        if (!control.Any(fila => fila.GetValueOrDefault("Resultado") == "PERMITIDO_PRE_DATA"))
            throw new InvalidOperationException("Rollback físico rechazado por la guardia.");
        await using var admin = await AbrirAsync();
        await EjecutarAsync(admin, "DROP DATABASE `" + nombre + "`; CREATE DATABASE `" + nombre + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
        // mysql (sin --force) interpreta correctamente las directivas de mysqldump.
        var resultado = await ProcesoAsync(Binario("mysql"), ["--no-defaults", "--host=127.0.0.1", "--port=" + puerto,
            "--user=root", "--database=" + nombre, "--binary-mode"], 90, clave, await File.ReadAllTextAsync(backup));
        if (resultado.Codigo != 0) throw new InvalidOperationException("Falló restore sintético: " + resultado.Error);
        await using var restaurada = await AbrirAsync(nombre);
        Assert.Equal(NormalizarHuella(await File.ReadAllTextAsync(backup + ".huella.json")), NormalizarHuella(await HuellaAsync(restaurada)));
        await File.WriteAllTextAsync(backup + ".restore.txt", "Restauración física ensayada; todas las filas/tablas coinciden con el backup PRE.\n");
    }

    // F conserva cualquier confirmación: restaura en otra base vacía, sin eliminar la base de origen.
    public async Task<string> RestaurarCopiaAsync(string origen, string backup)
    {
        ValidarBase(origen);
        if (Path.GetFullPath(backup) != Path.Combine(Evidencia, origen + "-pre.sql")
            || Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(backup))) != await File.ReadAllTextAsync(backup + ".sha256"))
            throw new InvalidOperationException("Backup ajeno o alterado.");
        var nombre = "sige_cdc_db_qa_fin02br_" + Guid.NewGuid().ToString("N"); bases.Add(nombre);
        await using var admin = await AbrirAsync(); // UUID confirma que no es QA compartida/Staging/Production.
        await EjecutarAsync(admin, "CREATE DATABASE `" + nombre + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
        var resultado = await ProcesoAsync(Binario("mysql"), ["--no-defaults", "--host=127.0.0.1", "--port=" + puerto,
            "--user=root", "--database=" + nombre, "--binary-mode"], 90, clave, await File.ReadAllTextAsync(backup));
        if (resultado.Codigo != 0) throw new InvalidOperationException("Falló restore sintético F: " + resultado.Error);
        await using var restaurada = await AbrirAsync(nombre);
        Assert.Equal(NormalizarHuella(await File.ReadAllTextAsync(backup + ".huella.json")), NormalizarHuella(await HuellaAsync(restaurada)));
        await File.WriteAllTextAsync(backup + ".restore-F.txt", $"Restaurado en {nombre}; filas y conteos idénticos al backup. Origen conservado.\n");
        return nombre;
    }

    public static async Task<string> HuellaAsync(MySqlConnection conexion)
    {
        await EjecutarAsync(conexion, Patch("Huella"));
        return Convert.ToString(await new MySqlCommand("SELECT @fin02b_huella_actual", conexion).ExecuteScalarAsync())!;
    }

    public static string NormalizarHuella(string json) => string.Join("\n", JsonDocument.Parse(json).RootElement.EnumerateArray()
        .OrderBy(t => t.GetProperty("Tabla").GetString(), StringComparer.Ordinal)
        .Select(t => t.GetProperty("Tabla").GetString() + ":" + t.GetProperty("Cantidad").GetRawText() + ":" +
            string.Join(",", t.GetProperty("Filas").EnumerateArray().Select(f => f.GetString()).Order(StringComparer.Ordinal))));

    public static async Task<List<Dictionary<string, string?>>> EjecutarAsync(MySqlConnection conexion, string sql)
    {
        // DELIMITER pertenece al cliente mysql. Separar solo sus bloques explícitos;
        // no dividir sentencias de los scripts existentes por sus puntos y coma internos.
        var bloques = Regex.Split(sql, @"(?m)^DELIMITER ([^\r\n]+)\r?$");
        if (bloques.Length > 1)
        {
            var resultados = await EjecutarAsync(conexion, bloques[0]);
            for (var i = 1; i < bloques.Length; i += 2)
            {
                var delimitador = bloques[i].Trim();
                var comandos = delimitador == ";" ? new[] { bloques[i + 1] } : bloques[i + 1].Split(delimitador, StringSplitOptions.RemoveEmptyEntries);
                foreach (var fragmento in comandos.Where(c => !string.IsNullOrWhiteSpace(c))) resultados.AddRange(await EjecutarAsync(conexion, fragmento.Trim()));
            }
            return resultados;
        }
        var filas = new List<Dictionary<string, string?>>();
        await using var comando = new MySqlCommand(sql, conexion) { CommandTimeout = 120 };
        await using var lector = await comando.ExecuteReaderAsync();
        do
        {
            while (await lector.ReadAsync())
            {
                var fila = new Dictionary<string, string?>();
                for (var i = 0; i < lector.FieldCount; i++)
                    fila[lector.GetName(i)] = lector.IsDBNull(i) ? null : Convert.ToString(lector.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                filas.Add(fila);
            }
        } while (await lector.NextResultAsync());
        return filas;
    }

    public static async Task FijarAsync(MySqlConnection conexion, string variable, string valor)
    {
        if (!Regex.IsMatch(variable, "^fin02b_[a-z_]+$")) throw new ArgumentException("Variable inesperada.");
        await using var comando = new MySqlCommand("SET @" + variable + " = @valor", conexion);
        comando.Parameters.AddWithValue("@valor", valor);
        await comando.ExecuteNonQueryAsync();
    }

    private void ValidarBase(string nombre)
    {
        if (!bases.Contains(nombre) || !Regex.IsMatch(nombre, "^sige_cdc_db_qa_fin02br_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Base ajena al sandbox; operación rechazada.");
    }
    private static string BuscarRaiz()
    {
        var ruta = new DirectoryInfo(AppContext.BaseDirectory);
        while (ruta is not null && !File.Exists(Path.Combine(ruta.FullName, "database", "ScriptDB_SIGE_CDC_Corregido.sql"))) ruta = ruta.Parent;
        return ruta?.FullName ?? throw new InvalidOperationException("No se localizó el SQL oficial local.");
    }
    private static string Binario(string nombre)
    {
        var explicito = Environment.GetEnvironmentVariable("SIGECDC_QA_" + nombre.ToUpperInvariant() + "_PATH");
        var candidatos = new[] { explicito, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MySQL", "MySQL Server 8.0", nombre + ".exe") }
            .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).Select(p => Path.Combine(p, nombre + (OperatingSystem.IsWindows() ? ".exe" : ""))));
        return candidatos.FirstOrDefault(p => p is not null && File.Exists(p)) ?? throw new InvalidOperationException(nombre + " requerido para F1-FIN-02B; no se omite esta puerta.");
    }
    private static ProcessStartInfo Inicio(string exe, string[] argumentos)
    {
        var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argumento in argumentos) info.ArgumentList.Add(argumento);
        return info;
    }
    private static async Task<(int Codigo, string Salida, string Error)> ProcesoAsync(string exe, string[] argumentos, int segundos, string? password = null, string? entrada = null)
    {
        var info = Inicio(exe, argumentos);
        if (password is not null) info.Environment["MYSQL_PWD"] = password;
        info.RedirectStandardInput = entrada is not null;
        using var proceso = Process.Start(info) ?? throw new InvalidOperationException("No inició herramienta MySQL.");
        var salida = proceso.StandardOutput.ReadToEndAsync();
        var error = proceso.StandardError.ReadToEndAsync();
        IOException? errorEntrada = null;
        if (entrada is not null)
        {
            try { await proceso.StandardInput.WriteAsync(entrada); proceso.StandardInput.Close(); }
            catch (IOException e) { errorEntrada = e; } // Recoger stderr del cliente, no ocultarlo con "pipe cerrada".
        }
        using var tiempo = new CancellationTokenSource(TimeSpan.FromSeconds(segundos));
        try { await proceso.WaitForExitAsync(tiempo.Token); }
        catch { proceso.Kill(entireProcessTree: true); throw; }
        if (proceso.ExitCode == 0 && errorEntrada is not null) throw errorEntrada;
        return (proceso.ExitCode, await salida, await error);
    }
}

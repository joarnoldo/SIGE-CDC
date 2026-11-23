namespace SIGECDC.Tests.EntregableImplantacion.Soporte;

// Solo dos consultas públicas admiten ejecución remota; ningún secreto o conexión DB.
public static class ConexionRevisionAcademica
{
    public static bool EsRemoto => Environment.GetEnvironmentVariable("SIGECDC_QA51_MODO") == "RemotoLectura";
    public static HttpClient ClienteRemoto()
    {
        var uri = new Uri(Environment.GetEnvironmentVariable("SIGECDC_QA51_URL") ?? "https://sigecdc.runasp.net/");
        if (uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query))
            throw new InvalidOperationException("BLOCKED: URL de revisión debe ser HTTPS sin credenciales ni query.");
        return new(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(30) };
    }
}

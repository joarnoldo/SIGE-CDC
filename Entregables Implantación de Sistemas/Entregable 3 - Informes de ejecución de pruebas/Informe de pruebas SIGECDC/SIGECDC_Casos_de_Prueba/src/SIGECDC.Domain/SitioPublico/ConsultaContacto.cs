namespace SIGECDC.Domain.SitioPublico;

public class ConsultaContacto
{
    public long IdConsultaContacto { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string? Asunto { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public DateTime FechaEnvio { get; set; }

    public string EstadoConsulta { get; set; } = EstadosConsultaContacto.Nueva;

    public string? ObservacionesInternas { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

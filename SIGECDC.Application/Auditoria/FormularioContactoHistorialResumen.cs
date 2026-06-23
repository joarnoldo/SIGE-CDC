namespace SIGECDC.Application.Auditoria;

public sealed class FormularioContactoHistorialResumen
{
    public long IdConsultaContacto { get; set; }

    public DateTime FechaEnvio { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string? Asunto { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public string EstadoConsulta { get; set; } = string.Empty;
}

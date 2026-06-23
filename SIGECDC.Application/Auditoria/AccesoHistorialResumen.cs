namespace SIGECDC.Application.Auditoria;

public sealed class AccesoHistorialResumen
{
    public long IdBitacoraAuditoria { get; set; }

    public DateTime FechaHora { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string? IdUsuario { get; set; }

    public string? Usuario { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? DireccionIP { get; set; }

    public string? Observacion { get; set; }
}

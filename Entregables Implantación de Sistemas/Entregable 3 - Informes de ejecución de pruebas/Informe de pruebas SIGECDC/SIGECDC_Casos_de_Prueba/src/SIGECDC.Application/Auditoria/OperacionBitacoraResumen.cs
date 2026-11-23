namespace SIGECDC.Application.Auditoria;

public sealed class OperacionBitacoraResumen
{
    public long IdBitacoraAuditoria { get; set; }

    public DateTime FechaHora { get; set; }

    public string Categoria { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string? IdRegistro { get; set; }

    public string? IdUsuario { get; set; }

    public string? Usuario { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    public string? Observacion { get; set; }
}

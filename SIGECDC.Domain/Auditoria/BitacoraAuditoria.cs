namespace SIGECDC.Domain.Auditoria;

public sealed class BitacoraAuditoria
{
    public long IdBitacoraAuditoria { get; set; }

    public string? IdUsuario { get; set; }

    public DateTime FechaHora { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string? IdRegistro { get; set; }

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    public string? DireccionIP { get; set; }

    public string? Observacion { get; set; }
}

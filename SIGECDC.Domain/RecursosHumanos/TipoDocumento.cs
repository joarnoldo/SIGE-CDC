namespace SIGECDC.Domain.RecursosHumanos;

public class TipoDocumento
{
    public int IdTipoDocumento { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";
}

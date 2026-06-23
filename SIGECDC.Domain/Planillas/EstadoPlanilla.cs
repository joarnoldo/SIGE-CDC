namespace SIGECDC.Domain.Planillas;

public sealed class EstadoPlanilla
{
    public int IdEstadoPlanilla { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";
}

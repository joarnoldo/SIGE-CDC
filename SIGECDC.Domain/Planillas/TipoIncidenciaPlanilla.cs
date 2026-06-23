namespace SIGECDC.Domain.Planillas;

public sealed class TipoIncidenciaPlanilla
{
    public int IdTipoIncidenciaPlanilla { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Naturaleza { get; set; } = "Ingreso";

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";
}

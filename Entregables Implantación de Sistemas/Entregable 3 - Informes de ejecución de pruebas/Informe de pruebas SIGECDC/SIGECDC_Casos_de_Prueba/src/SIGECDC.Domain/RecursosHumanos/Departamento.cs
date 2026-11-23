namespace SIGECDC.Domain.RecursosHumanos;

public class Departamento
{
    public int IdDepartamento { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";
}

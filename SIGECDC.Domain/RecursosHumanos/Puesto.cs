namespace SIGECDC.Domain.RecursosHumanos;

public class Puesto
{
    public int IdPuesto { get; set; }

    public int? IdDepartamento { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public Departamento? Departamento { get; set; }
}

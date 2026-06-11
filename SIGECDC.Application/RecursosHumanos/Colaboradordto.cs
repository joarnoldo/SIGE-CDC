namespace SIGECDC.Application.RecursosHumanos;

public class SolicitudAsignarColaborador
{
    public long IdColaborador { get; set; }

    public int IdDepartamento { get; set; }  // ← int porque en BD es INT

    public int IdPuesto { get; set; }         // ← int porque en BD es INT
}

public class ColaboradorResumen
{
    public long IdColaborador { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string? Departamento { get; set; }

    public string? Puesto { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
namespace SIGECDC.Application.RecursosHumanos;

public sealed class ColaboradorResumen
{
    public long IdColaborador { get; set; }

    public string CodigoColaborador { get; set; } = string.Empty;

    public string Identificacion { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string? CorreoElectronico { get; set; }

    public string? Telefono { get; set; }

    public DateTime FechaIngreso { get; set; }

    public DateTime? FechaSalida { get; set; }

    public int IdEstadoLaboral { get; set; }

    public string EstadoLaboral { get; set; } = string.Empty;

    public int IdDepartamento { get; set; }

    public string Departamento { get; set; } = string.Empty;

    public int IdPuesto { get; set; }

    public string Puesto { get; set; } = string.Empty;
}

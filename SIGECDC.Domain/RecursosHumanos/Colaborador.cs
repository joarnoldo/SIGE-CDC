namespace SIGECDC.Domain.RecursosHumanos;

public class Colaborador
{
    public long IdColaborador { get; set; }

    public string CodigoColaborador { get; set; } = string.Empty;

    public string TipoIdentificacion { get; set; } = string.Empty;

    public string Identificacion { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string PrimerApellido { get; set; } = string.Empty;

    public string? SegundoApellido { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public DateTime FechaIngreso { get; set; }

    public DateTime? FechaSalida { get; set; }

    public int IdEstadoLaboral { get; set; }

    public int IdDepartamento { get; set; }

    public int IdPuesto { get; set; }

    public string? IdUsuario { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public EstadoLaboral? EstadoLaboral { get; set; }

    public Departamento? Departamento { get; set; }

    public Puesto? Puesto { get; set; }
}

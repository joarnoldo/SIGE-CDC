using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.RecursosHumanos;

public sealed class SolicitudColaborador
{
    [Required(ErrorMessage = "El codigo de colaborador es obligatorio.")]
    [StringLength(20, ErrorMessage = "El codigo de colaborador no debe superar los 20 caracteres.")]
    public string CodigoColaborador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de identificacion es obligatorio.")]
    [StringLength(30, ErrorMessage = "El tipo de identificacion no debe superar los 30 caracteres.")]
    public string TipoIdentificacion { get; set; } = "Cedula fisica";

    [Required(ErrorMessage = "La identificacion es obligatoria.")]
    [StringLength(30, ErrorMessage = "La identificacion no debe superar los 30 caracteres.")]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no debe superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer apellido no debe superar los 100 caracteres.")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no debe superar los 100 caracteres.")]
    public string? SegundoApellido { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido.")]
    [StringLength(150, ErrorMessage = "El correo electronico no debe superar los 150 caracteres.")]
    public string? CorreoElectronico { get; set; }

    [StringLength(30, ErrorMessage = "El telefono no debe superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [StringLength(300, ErrorMessage = "La direccion no debe superar los 300 caracteres.")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    public DateTime? FechaIngreso { get; set; } = DateTime.Today;

    public DateTime? FechaSalida { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estado laboral.")]
    public int IdEstadoLaboral { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento.")]
    public int IdDepartamento { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto.")]
    public int IdPuesto { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no deben superar los 500 caracteres.")]
    public string? Observaciones { get; set; }
}

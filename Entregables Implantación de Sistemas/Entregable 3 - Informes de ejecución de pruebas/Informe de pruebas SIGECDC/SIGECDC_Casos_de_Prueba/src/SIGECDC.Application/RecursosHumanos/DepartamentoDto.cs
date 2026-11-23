using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.RecursosHumanos;

public sealed class SolicitudRegistrarDepartamento
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no debe superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripcion no debe superar los 255 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class SolicitudActualizarDepartamento
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento.")]
    public int IdDepartamento { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no debe superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripcion no debe superar los 255 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class DepartamentoResumen
{
    public int IdDepartamento { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int TotalColaboradores { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
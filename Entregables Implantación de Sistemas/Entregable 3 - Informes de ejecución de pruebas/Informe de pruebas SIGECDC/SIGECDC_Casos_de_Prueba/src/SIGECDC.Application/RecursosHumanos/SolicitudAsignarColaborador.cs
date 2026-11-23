using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.RecursosHumanos;

public sealed class SolicitudAsignarColaborador
{
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un colaborador.")]
    public long IdColaborador { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento.")]
    public int IdDepartamento { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto.")]
    public int IdPuesto { get; set; }
}
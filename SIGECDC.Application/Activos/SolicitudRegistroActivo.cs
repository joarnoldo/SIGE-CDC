using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.Activos;

public sealed class SolicitudRegistroActivo
{
    [Required(ErrorMessage = "El código del activo es obligatorio.")]
    [StringLength(30, ErrorMessage = "El código no puede superar 30 caracteres.")]
    public string CodigoActivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del activo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string NombreActivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un tipo de activo.")]
    public int? IdTipoActivo { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una categoría.")]
    public int? IdCategoriaActivo { get; set; }

    [StringLength(100, ErrorMessage = "La marca no puede superar 100 caracteres.")]
    public string? Marca { get; set; }

    [StringLength(100, ErrorMessage = "El modelo no puede superar 100 caracteres.")]
    public string? Modelo { get; set; }

    [StringLength(100, ErrorMessage = "El número de serie no puede superar 100 caracteres.")]
    public string? NumeroSerie { get; set; }

    [StringLength(30, ErrorMessage = "La placa no puede superar 30 caracteres.")]
    public string? Placa { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }

    public DateTime? FechaAdquisicion { get; set; }

    public decimal? ValorAdquisicion { get; set; }

    [StringLength(150, ErrorMessage = "La ubicación no puede superar 150 caracteres.")]
    public string? UbicacionActual { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar 500 caracteres.")]
    public string? Observaciones { get; set; }
}

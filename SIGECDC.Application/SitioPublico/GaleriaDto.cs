using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.SitioPublico;

public sealed class SolicitudGaleria
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no debe superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripcion no debe superar los 500 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class GaleriaResumen
{
    public long IdGaleria { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EstaPublicado { get; set; }

    public int TotalImagenes { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
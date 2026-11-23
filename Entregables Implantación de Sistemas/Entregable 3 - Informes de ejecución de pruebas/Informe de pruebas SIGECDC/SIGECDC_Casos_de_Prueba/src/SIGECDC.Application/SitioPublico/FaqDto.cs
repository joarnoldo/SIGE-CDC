using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Application.SitioPublico;

public sealed class SolicitudRegistrarFaq
{
    [Required(ErrorMessage = "La pregunta es obligatoria.")]
    [StringLength(300, ErrorMessage = "La pregunta no debe superar los 300 caracteres.")]
    public string Pregunta { get; set; } = string.Empty;

    [Required(ErrorMessage = "La respuesta es obligatoria.")]
    public string Respuesta { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo.")]
    public int Orden { get; set; }
}

public sealed class SolicitudActualizarFaq
{
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione una pregunta frecuente.")]
    public long IdFAQ { get; set; }

    [Required(ErrorMessage = "La pregunta es obligatoria.")]
    [StringLength(300, ErrorMessage = "La pregunta no debe superar los 300 caracteres.")]
    public string Pregunta { get; set; } = string.Empty;

    [Required(ErrorMessage = "La respuesta es obligatoria.")]
    public string Respuesta { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo.")]
    public int Orden { get; set; }
}

public sealed class FaqResumen
{
    public long IdFAQ { get; set; }

    public string Pregunta { get; set; } = string.Empty;

    public string Respuesta { get; set; } = string.Empty;

    public int Orden { get; set; }

    public bool EstaPublicado { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
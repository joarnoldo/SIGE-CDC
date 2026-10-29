using System.ComponentModel.DataAnnotations;

namespace SIGECDC.Web.Components.Pages.Planillas;

public sealed class ParametroInternoInterfaz
{
    [Required, StringLength(100)] public string Codigo { get; set; } = "";
    [Required, StringLength(150)] public string Nombre { get; set; } = "";
    [Range(1, long.MaxValue)] public long IdConceptoVersion { get; set; }
    [Required] public decimal? Valor { get; set; }
    [Required] public DateTime? Inicio { get; set; }
    public DateTime? Fin { get; set; }
    [Required, StringLength(300)] public string Soporte { get; set; } = "";
}

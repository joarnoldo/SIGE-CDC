using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Multimedia
{
    public long IdMultimedia { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string UrlMedia { get; set; } = null!;

    public string TipoMedia { get; set; } = null!;

    public DateTime? FechaCreacion { get; set; }

    public string EstadoRegistro { get; set; } = null!;
}

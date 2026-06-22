using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Multimedium
{
    public int Id { get; set; }

    public string Titulo { get; set; } = null!;

    public string RutaArchivo { get; set; } = null!;

    public string TipoArchivo { get; set; } = null!;

    public DateTime? FechaSubida { get; set; }

    public int? NoticiaId { get; set; }

    public virtual Noticia? Noticia { get; set; }
}

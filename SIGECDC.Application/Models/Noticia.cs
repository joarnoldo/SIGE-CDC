using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Noticia
{
    public int Id { get; set; }

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime? FechaPublicacion { get; set; }

    public string? ImagenUrl { get; set; }

    public virtual ICollection<Multimedium> Multimedia { get; set; } = new List<Multimedium>();
}

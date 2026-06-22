using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Estadoproyecto
{
    public int IdEstadoProyecto { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string EstadoRegistro { get; set; } = null!;

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}

using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Proyectopublicado
{
    public long IdProyectoPublicado { get; set; }

    public long? IdProyecto { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? EstadoVisual { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = null!;

    public virtual Proyecto? IdProyectoNavigation { get; set; }
}

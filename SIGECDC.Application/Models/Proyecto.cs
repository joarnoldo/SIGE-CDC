using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Proyecto
{
    public long IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string NombreProyecto { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFinEstimada { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public string? Responsable { get; set; }

    public string? Ubicacion { get; set; }

    public int IdEstadoProyecto { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = null!;

    public virtual Estadoproyecto IdEstadoProyectoNavigation { get; set; } = null!;

    public virtual ICollection<Proyectopublicado> Proyectopublicados { get; set; } = new List<Proyectopublicado>();
}

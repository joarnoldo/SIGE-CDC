using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Paginacontenido
{
    public long IdPaginaContenido { get; set; }

    public string CodigoPagina { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string? Contenido { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace SIGECDC.Application.Models;

public partial class Consultacontacto
{
    public long IdConsultaContacto { get; set; }

    public string Nombre { get; set; } = null!;

    public string CorreoElectronico { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Asunto { get; set; }

    public string Mensaje { get; set; } = null!;

    public DateTime? FechaEnvio { get; set; }

    public string? EstadoConsulta { get; set; }

    public string? ObservacionesInternas { get; set; }

    public string EstadoRegistro { get; set; } = null!;
}

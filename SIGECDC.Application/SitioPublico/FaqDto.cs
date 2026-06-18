using System;
using System.Collections.Generic;
using System.Text;

namespace SIGECDC.Application.SitioPublico;

public class SolicitudRegistrarFaq
{
    public string Pregunta { get; set; } = string.Empty;

    public string Respuesta { get; set; } = string.Empty;

    public int Orden { get; set; }
}

public class SolicitudActualizarFaq
{
    public long IdFAQ { get; set; }

    public string Pregunta { get; set; } = string.Empty;

    public string Respuesta { get; set; } = string.Empty;

    public int Orden { get; set; }
}

public class FaqResumen
{
    public long IdFAQ { get; set; }

    public string Pregunta { get; set; } = string.Empty;

    public string Respuesta { get; set; } = string.Empty;

    public int Orden { get; set; }

    public bool EstaPublicado { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
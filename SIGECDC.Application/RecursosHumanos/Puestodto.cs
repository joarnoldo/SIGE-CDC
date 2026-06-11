namespace SIGECDC.Application.RecursosHumanos;

public class SolicitudRegistrarPuesto
{
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}

public class SolicitudActualizarPuesto
{
    public long IdPuesto { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}

public class PuestoResumen
{
    public long IdPuesto { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int TotalColaboradores { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
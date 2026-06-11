namespace SIGECDC.Application.RecursosHumanos;

public class SolicitudRegistrarDepartamento
{
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}

public class SolicitudActualizarDepartamento
{
    public long IdDepartamento { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}

public class DepartamentoResumen
{
    public long IdDepartamento { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int TotalColaboradores { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
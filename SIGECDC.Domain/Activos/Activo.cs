using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Activos;

public sealed class Activo
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public int IdEstadoActivo { get; set; }

    public EstadoActivo? EstadoActivo { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

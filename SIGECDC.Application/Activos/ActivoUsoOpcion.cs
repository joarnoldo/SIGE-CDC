namespace SIGECDC.Application.Activos;

public sealed class ActivoUsoOpcion
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public string EstadoActivo { get; set; } = string.Empty;

    public int? IdTipoMedicionUso { get; set; }

    public string? TipoMedicionUso { get; set; }

    public decimal? LecturaUsoActual { get; set; }

    public bool PuedeRegistrarUso { get; set; }

    public string? MotivoNoDisponible { get; set; }
}

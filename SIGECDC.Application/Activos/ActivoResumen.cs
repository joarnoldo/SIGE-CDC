namespace SIGECDC.Application.Activos;

public sealed class ActivoResumen
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public int IdTipoActivo { get; set; }

    public string TipoActivo { get; set; } = string.Empty;

    public int IdCategoriaActivo { get; set; }

    public string CategoriaActivo { get; set; } = string.Empty;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? NumeroSerie { get; set; }

    public string? Placa { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaAdquisicion { get; set; }

    public decimal? ValorAdquisicion { get; set; }

    public string? UbicacionActual { get; set; }

    public int IdEstadoActivo { get; set; }

    public string EstadoActivo { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}

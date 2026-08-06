using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Activos;

public sealed class Activo
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public int IdTipoActivo { get; set; }

    public TipoActivo? TipoActivo { get; set; }

    public int IdCategoriaActivo { get; set; }

    public CategoriaActivo? CategoriaActivo { get; set; }

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? NumeroSerie { get; set; }

    public string? Placa { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaAdquisicion { get; set; }

    public decimal? ValorAdquisicion { get; set; }

    public string? UbicacionActual { get; set; }

    public int IdEstadoActivo { get; set; }

    public EstadoActivo? EstadoActivo { get; set; }

    public int? IdTipoMedicionUso { get; set; }

    public TipoMedicionUso? TipoMedicionUso { get; set; }

    public decimal? LecturaUsoActual { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}

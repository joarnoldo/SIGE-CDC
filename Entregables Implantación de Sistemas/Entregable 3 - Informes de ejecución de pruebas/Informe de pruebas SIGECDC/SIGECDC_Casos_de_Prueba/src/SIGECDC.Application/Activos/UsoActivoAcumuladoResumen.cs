namespace SIGECDC.Application.Activos;

public sealed class UsoActivoAcumuladoResumen
{
    public long IdActivo { get; set; }

    public string CodigoActivo { get; set; } = string.Empty;

    public string NombreActivo { get; set; } = string.Empty;

    public string? TipoMedicionUso { get; set; }

    public decimal? LecturaUsoActual { get; set; }

    public decimal TotalUsoRegistrado { get; set; }

    public IReadOnlyList<RegistroUsoActivoResumen> Registros { get; set; } = [];

    public IReadOnlyList<PeriodoFueraServicioResumen> PeriodosFueraServicio { get; set; } = [];
}

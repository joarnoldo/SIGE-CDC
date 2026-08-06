namespace SIGECDC.Application.Activos;

public sealed class UtilizacionPorMedicionResumen
{
    public string TipoMedicion { get; set; } = string.Empty;

    public decimal CantidadUsoTotal { get; set; }

    public int CantidadRegistros { get; set; }
}

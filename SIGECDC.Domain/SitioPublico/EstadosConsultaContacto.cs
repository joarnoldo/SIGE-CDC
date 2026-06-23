namespace SIGECDC.Domain.SitioPublico;

public static class EstadosConsultaContacto
{
    public const string Nueva = "Nueva";
    public const string EnRevision = "EnRevision";
    public const string Atendida = "Atendida";
    public const string Descartada = "Descartada";

    private static readonly string[] ValoresPermitidos =
    [
        Nueva,
        EnRevision,
        Atendida,
        Descartada
    ];

    public static IReadOnlyList<string> Valores => ValoresPermitidos;

    public static bool EsValido(string? estadoConsulta)
    {
        return estadoConsulta is not null && ValoresPermitidos.Contains(estadoConsulta);
    }
}

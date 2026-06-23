namespace SIGECDC.Application.SitioPublico;

public sealed class EstadoConsultaContactoOpcion
{
    public EstadoConsultaContactoOpcion(string valor, string etiqueta)
    {
        Valor = valor;
        Etiqueta = etiqueta;
    }

    public string Valor { get; }

    public string Etiqueta { get; }
}

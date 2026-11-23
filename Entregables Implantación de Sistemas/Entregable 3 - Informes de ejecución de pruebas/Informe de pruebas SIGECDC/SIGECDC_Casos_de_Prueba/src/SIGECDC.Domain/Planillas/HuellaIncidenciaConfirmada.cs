using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SIGECDC.Domain.Planillas;

public static class HuellaIncidenciaConfirmada
{
    public static string Calcular(IncidenciaPlanilla h, string soporte, string? tipoDia,
        bool usaMonto, int? parametro, long? condicionBase, DateTime? inicioBase, DateTime? finBase)
    {
        static string? Texto(string? s) => s?.Normalize(NormalizationForm.FormC);
        static string? Numero(decimal? n) => n?.ToString("0.############################", CultureInfo.InvariantCulture);
        static string? Fecha(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string?[] campos = ["F1-INC-1", Numero(h.IdIncidenciaPlanilla), Numero(h.IdColaborador), Numero(h.IdPeriodoPlanilla),
            Numero(h.IdTipoIncidenciaPlanilla), Fecha(h.FechaIncidencia), Numero(h.Cantidad), Texto(h.Unidad),
            Numero(h.IdConceptoPlanillaVersion), usaMonto ? Numero(h.Monto) : null, Texto(h.Descripcion), Texto(h.EstadoRegistro),
            Texto(soporte), Texto(tipoDia), Numero(parametro), Numero(condicionBase), Fecha(inicioBase), Fecha(finBase)];
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(campos)));
    }

    public static string Calcular(IncidenciaPlanilla h, IncidenciaPlanillaConfirmacion c)
        => Calcular(h, c.ReferenciaSoporte, c.TipoDiaHE, c.MontoManual is not null, c.IdParametroPlanilla,
            c.IdCondicionBase, c.FechaBaseInicio, c.FechaBaseFin);
}

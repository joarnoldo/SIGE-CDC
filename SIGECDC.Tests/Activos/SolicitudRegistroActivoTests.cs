using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class SolicitudRegistroActivoTests
{
    [Fact]
    public void Validar_DatosObligatoriosCompletos_NoReportaErrores()
    {
        var solicitud = new SolicitudRegistroActivo
        {
            CodigoActivo = "ACT-001",
            NombreActivo = "Excavadora",
            IdTipoActivo = 1,
            IdCategoriaActivo = 1
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Validar_SinCamposObligatorios_ReportaTodosLosErrores()
    {
        var solicitud = new SolicitudRegistroActivo();

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.CodigoActivo)));
        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.NombreActivo)));
        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.IdTipoActivo)));
        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.IdCategoriaActivo)));
    }

    [Fact]
    public void Validar_CodigoMayorAlLimite_ReportaError()
    {
        var solicitud = new SolicitudRegistroActivo
        {
            CodigoActivo = new string('A', 31),
            NombreActivo = "Excavadora",
            IdTipoActivo = 1,
            IdCategoriaActivo = 1
        };

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(solicitud.CodigoActivo)));
    }

    private static IReadOnlyCollection<ValidationResult> Validar(SolicitudRegistroActivo solicitud)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}

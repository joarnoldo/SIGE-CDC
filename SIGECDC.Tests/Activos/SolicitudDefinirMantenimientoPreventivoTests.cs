using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class SolicitudDefinirMantenimientoPreventivoTests
{
    [Fact]
    public void Validar_ActivoValidoYDescripcionEnLimite_NoReportaErrores()
    {
        var solicitud = new SolicitudDefinirMantenimientoPreventivo
        {
            IdActivo = 1,
            FechaProgramada = new DateTime(2026, 8, 1),
            Descripcion = new string('A', 500)
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Validar_SinActivo_ReportaError()
    {
        var solicitud = new SolicitudDefinirMantenimientoPreventivo
        {
            FechaProgramada = new DateTime(2026, 8, 1)
        };

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(solicitud.IdActivo)));
    }

    [Fact]
    public void Validar_DescripcionMayorAlLimite_ReportaError()
    {
        var solicitud = new SolicitudDefinirMantenimientoPreventivo
        {
            IdActivo = 1,
            FechaProgramada = new DateTime(2026, 8, 1),
            Descripcion = new string('A', 501)
        };

        var resultados = Validar(solicitud);

        Assert.Contains(resultados, resultado =>
            resultado.MemberNames.Contains(nameof(solicitud.Descripcion)));
    }

    private static IReadOnlyCollection<ValidationResult> Validar(
        SolicitudDefinirMantenimientoPreventivo solicitud)
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

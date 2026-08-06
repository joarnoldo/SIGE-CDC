using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class SolicitudRegistrarUsoActivoTests
{
    [Fact]
    public void Validar_DatosValidos_NoGeneraErrores()
    {
        var solicitud = new SolicitudRegistrarUsoActivo
        {
            IdActivo = 1,
            IdTipoMedicionUso = 1,
            FechaRegistro = DateTime.Today,
            LecturaInicial = 100,
            LecturaNueva = 125.50m,
            Observaciones = "Lectura del turno."
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Validar_DatosObligatoriosAusentes_GeneraErrores()
    {
        var resultados = Validar(new SolicitudRegistrarUsoActivo());

        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(
                nameof(SolicitudRegistrarUsoActivo.IdActivo)));
        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(
                nameof(SolicitudRegistrarUsoActivo.FechaRegistro)));
        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(
                nameof(SolicitudRegistrarUsoActivo.LecturaNueva)));
    }

    [Fact]
    public void Validar_LecturasNegativas_GeneraErrores()
    {
        var solicitud = new SolicitudRegistrarUsoActivo
        {
            IdActivo = 1,
            FechaRegistro = DateTime.Today,
            LecturaInicial = -1,
            LecturaNueva = -0.01m
        };

        var resultados = Validar(solicitud);

        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(
                nameof(SolicitudRegistrarUsoActivo.LecturaInicial)));
        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(
                nameof(SolicitudRegistrarUsoActivo.LecturaNueva)));
    }

    [Fact]
    public void Validar_ObservacionExtensa_GeneraError()
    {
        var solicitud = new SolicitudRegistrarUsoActivo
        {
            IdActivo = 1,
            FechaRegistro = DateTime.Today,
            LecturaNueva = 1,
            Observaciones = new string('x', 501)
        };

        var resultados = Validar(solicitud);

        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(
                nameof(SolicitudRegistrarUsoActivo.Observaciones)));
    }

    private static IReadOnlyList<ValidationResult> Validar(
        SolicitudRegistrarUsoActivo solicitud)
    {
        List<ValidationResult> resultados = [];
        Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}

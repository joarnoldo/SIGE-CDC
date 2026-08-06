using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class SolicitudOrdenMantenimientoTests
{
    [Fact]
    public void Crear_ValoresValidos_NoReportaErrores()
    {
        var solicitud = new SolicitudCrearOrdenMantenimiento
        {
            IdActivo = 1,
            IdProyecto = 2,
            FechaProgramada = new DateTime(2026, 8, 1),
            Descripcion = new string('A', 500),
            CostoEstimado = 9999999999999999.99m,
            Responsable = new string('R', 150)
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Crear_ValoresFueraDeRango_ReportaCampos()
    {
        var solicitud = new SolicitudCrearOrdenMantenimiento
        {
            IdActivo = 0,
            IdProyecto = 0,
            FechaProgramada = new DateTime(2026, 8, 1),
            Descripcion = new string('A', 501),
            CostoEstimado = -0.01m,
            Responsable = new string('R', 151)
        };

        var resultados = Validar(solicitud);

        AssertCampo(resultados, nameof(solicitud.IdActivo));
        AssertCampo(resultados, nameof(solicitud.IdProyecto));
        AssertCampo(resultados, nameof(solicitud.Descripcion));
        AssertCampo(resultados, nameof(solicitud.Responsable));
    }

    [Fact]
    public void Crear_CostoInvalido_ReportaCampo()
    {
        var solicitud = new SolicitudCrearOrdenMantenimiento
        {
            IdActivo = 1,
            FechaProgramada = new DateTime(2026, 8, 1),
            CostoEstimado = -0.01m
        };

        AssertCampo(
            Validar(solicitud),
            nameof(solicitud.CostoEstimado));
    }

    [Fact]
    public void Cerrar_CostoTiempoYEvidenciaValidos_NoReportaErrores()
    {
        using var solicitud = new SolicitudCerrarOrdenMantenimiento
        {
            IdMantenimiento = 1,
            CostoReal = 0,
            TiempoFueraServicioHoras = 0,
            Resultado = new string('R', 500),
            Evidencias =
            [
                new SolicitudEvidenciaMantenimiento
                {
                    NombreOriginal = "evidencia.pdf",
                    MimeType = "application/pdf",
                    TamanoBytes = 8,
                    Contenido = new MemoryStream(
                        "%PDF-1.4"u8.ToArray())
                }
            ]
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Cerrar_SinDetalleObligatorio_ReportaCampos()
    {
        using var solicitud = new SolicitudCerrarOrdenMantenimiento
        {
            IdMantenimiento = 0,
            Resultado = new string('R', 501),
            Evidencias = []
        };

        var resultados = Validar(solicitud);

        AssertCampo(resultados, nameof(solicitud.IdMantenimiento));
        AssertCampo(resultados, nameof(solicitud.CostoReal));
        AssertCampo(
            resultados,
            nameof(solicitud.TiempoFueraServicioHoras));
        AssertCampo(resultados, nameof(solicitud.Resultado));
        AssertCampo(resultados, nameof(solicitud.Evidencias));
    }

    [Fact]
    public void Evidencia_MetadatosInvalidos_ReportaCampos()
    {
        using var evidencia = new SolicitudEvidenciaMantenimiento
        {
            NombreOriginal = string.Empty,
            MimeType = new string('M', 151),
            TamanoBytes = 0,
            Contenido = Stream.Null
        };

        var resultados = Validar(evidencia);

        AssertCampo(resultados, nameof(evidencia.NombreOriginal));
        AssertCampo(resultados, nameof(evidencia.MimeType));
        AssertCampo(resultados, nameof(evidencia.TamanoBytes));
    }

    private static IReadOnlyCollection<ValidationResult> Validar(
        object solicitud)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);
        return resultados;
    }

    private static void AssertCampo(
        IEnumerable<ValidationResult> resultados,
        string campo)
    {
        Assert.Contains(
            resultados,
            resultado => resultado.MemberNames.Contains(campo));
    }
}

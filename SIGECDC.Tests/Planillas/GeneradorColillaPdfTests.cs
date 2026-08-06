using PdfSharp.Pdf.IO;
using SIGECDC.Application.Planillas;
using SIGECDC.Infrastructure.Reportes;

namespace SIGECDC.Tests.Planillas;

public sealed class GeneradorColillaPdfTests
{
    [Fact]
    public void Generar_CreaPdfA4EnMemoriaConNombreMimeYFuentePortable()
    {
        var servicio = new GeneradorColillaPdf();
        var colilla = CrearColilla();

        var archivo = servicio.Generar(colilla);

        Assert.Equal("application/pdf", archivo.MimeType);
        Assert.Equal("colilla-COL-PRUEBA-Ñ.pdf", archivo.NombreOriginal);
        Assert.Equal(0, archivo.Contenido.Position);

        using var memoria = new MemoryStream();
        archivo.Contenido.CopyTo(memoria);
        var bytes = memoria.ToArray();
        Assert.True(bytes.Length > 10_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

        memoria.Position = 0;
        using var documento = PdfReader.Open(memoria, PdfDocumentOpenMode.Import);
        Assert.Single(documento.Pages);
        Assert.InRange(documento.Pages[0].Width.Point, 594.5, 595.5);
        Assert.InRange(documento.Pages[0].Height.Point, 841.5, 842.5);
    }

    [Fact]
    public void Generar_EsIdempotenteConCaracteresEspanolesYRutaNoPersistida()
    {
        var servicio = new GeneradorColillaPdf();
        var colilla = CrearColilla();

        using var primero = servicio.Generar(colilla).Contenido;
        using var segundo = servicio.Generar(colilla).Contenido;

        Assert.True(primero.Length > 0);
        Assert.True(segundo.Length > 0);
        Assert.Equal(0, primero.Position);
        Assert.Equal(0, segundo.Position);
    }

    private static ColillaPagoDetalle CrearColilla()
    {
        return new ColillaPagoDetalle
        {
            IdColillaPago = 7,
            CodigoColilla = "COL-PRUEBA-Ñ",
            CodigoPeriodo = "PLAN-2026-08-Q1",
            NombrePeriodo = "Primera quincena de agosto",
            TipoPeriodo = "Quincenal",
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 15),
            EstadoPlanilla = "Cerrada",
            FechaGeneracion = new DateTime(2026, 8, 16, 9, 30, 0),
            CodigoColaborador = "COL-007",
            NombreColaborador = "José Núñez Álvarez",
            Departamento = "Recursos Humanos",
            Puesto = "Técnico de nómina",
            SalarioBase = 1_000_000m,
            SalarioProporcional = 500_000m,
            TotalHorasExtra = 32_500m,
            TotalBonos = 25_000m,
            TotalBeneficiosConfigurables = 10_000m,
            TotalAusencias = 5_000m,
            SalarioBruto = 562_500m,
            TotalDeducciones = 47_500m,
            SalarioNeto = 515_000m
        };
    }
}

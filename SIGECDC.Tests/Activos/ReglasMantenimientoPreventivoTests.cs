using SIGECDC.Domain.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class ReglasMantenimientoPreventivoTests
{
    private static readonly DateTime FechaActual = new(2026, 7, 19);

    [Fact]
    public void ValidarFechaProgramada_FechaPasada_Rechaza()
    {
        var fechaPasada = FechaActual.AddDays(-1);

        Assert.Throws<ArgumentException>(() =>
            ReglasMantenimientoPreventivo.ValidarFechaProgramada(
                fechaPasada,
                FechaActual));
    }

    [Fact]
    public void ValidarFechaProgramada_FechaActual_Acepta()
    {
        ReglasMantenimientoPreventivo.ValidarFechaProgramada(
            FechaActual,
            FechaActual);
    }

    [Fact]
    public void ValidarFechaProgramada_FechaFutura_Acepta()
    {
        ReglasMantenimientoPreventivo.ValidarFechaProgramada(
            FechaActual.AddDays(1),
            FechaActual);
    }

    [Fact]
    public void ValidarFechaProgramada_FechaDefault_Rechaza()
    {
        Assert.Throws<ArgumentException>(() =>
            ReglasMantenimientoPreventivo.ValidarFechaProgramada(
                default,
                FechaActual));
    }
}

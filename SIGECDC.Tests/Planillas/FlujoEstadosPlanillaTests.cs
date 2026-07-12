using SIGECDC.Domain.Planillas;

namespace SIGECDC.Tests.Planillas;

public sealed class FlujoEstadosPlanillaTests
{
    [Fact]
    public void ValidarAprobacion_DesdeCalculada_PermiteTransicion()
    {
        FlujoEstadosPlanilla.ValidarAprobacion(
            EstadosPlanilla.Calculada,
            EstadosPlanilla.Calculada);
    }

    [Theory]
    [InlineData(EstadosPlanilla.Borrador)]
    [InlineData(EstadosPlanilla.Aprobada)]
    [InlineData(EstadosPlanilla.Cerrada)]
    public void ValidarAprobacion_DesdeOtroEstado_RechazaTransicion(string estado)
    {
        Assert.Throws<InvalidOperationException>(() =>
            FlujoEstadosPlanilla.ValidarAprobacion(estado, estado));
    }

    [Fact]
    public void ValidarCierre_DesdeAprobada_PermiteTransicion()
    {
        FlujoEstadosPlanilla.ValidarCierre(
            EstadosPlanilla.Aprobada,
            EstadosPlanilla.Aprobada);
    }

    [Theory]
    [InlineData(EstadosPlanilla.Borrador)]
    [InlineData(EstadosPlanilla.Calculada)]
    [InlineData(EstadosPlanilla.Cerrada)]
    public void ValidarCierre_DesdeOtroEstado_RechazaTransicion(string estado)
    {
        Assert.Throws<InvalidOperationException>(() =>
            FlujoEstadosPlanilla.ValidarCierre(estado, estado));
    }

    [Fact]
    public void ValidarTransicion_EstadosIncoherentes_RechazaOperacion()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FlujoEstadosPlanilla.ValidarAprobacion(
                EstadosPlanilla.Calculada,
                EstadosPlanilla.Aprobada));
    }

    [Theory]
    [InlineData(EstadosPlanilla.Aprobada, true)]
    [InlineData(EstadosPlanilla.Cerrada, true)]
    [InlineData(EstadosPlanilla.Calculada, false)]
    [InlineData(null, false)]
    public void EstaBloqueada_IdentificaEstadosFinales(string? estado, bool esperado)
    {
        Assert.Equal(esperado, FlujoEstadosPlanilla.EstaBloqueada(estado));
    }
}

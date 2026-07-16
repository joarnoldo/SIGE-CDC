using SIGECDC.Domain.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class ReglasEstadoActivoTests
{
    [Theory]
    [InlineData(EstadosActivo.Disponible)]
    [InlineData(EstadosActivo.Asignado)]
    [InlineData(EstadosActivo.EnMantenimiento)]
    [InlineData(EstadosActivo.FueraDeServicio)]
    [InlineData(EstadosActivo.DadoDeBaja)]
    public void EsEstadoOficial_ReconoceCatalogoAutorizado(string estado)
    {
        Assert.True(ReglasEstadoActivo.EsEstadoOficial(estado));
    }

    [Fact]
    public void ValidarTransicion_DesdeDadoDeBaja_RechazaRegresoOperativo()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() =>
            ReglasEstadoActivo.ValidarTransicion(
                EstadosActivo.DadoDeBaja,
                EstadosActivo.Disponible,
                tieneAsignacionNoFinalizada: false));

        Assert.Contains("dado de baja", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(EstadosActivo.EnMantenimiento)]
    [InlineData(EstadosActivo.FueraDeServicio)]
    [InlineData(EstadosActivo.DadoDeBaja)]
    public void ValidarTransicion_ConAsignacionNoFinalizada_RechazaEstadoIncompatible(string estadoNuevo)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReglasEstadoActivo.ValidarTransicion(
                EstadosActivo.Asignado,
                estadoNuevo,
                tieneAsignacionNoFinalizada: true));
    }

    [Fact]
    public void ValidarTransicion_ConAsignacionNoFinalizada_PermiteSoloAsignado()
    {
        ReglasEstadoActivo.ValidarTransicion(
            EstadosActivo.Asignado,
            EstadosActivo.Asignado,
            tieneAsignacionNoFinalizada: true);
    }

    [Fact]
    public void ValidarTransicion_ConAsignacionNoFinalizada_RechazaDisponible()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReglasEstadoActivo.ValidarTransicion(
                EstadosActivo.Asignado,
                EstadosActivo.Disponible,
                tieneAsignacionNoFinalizada: true));
    }
}

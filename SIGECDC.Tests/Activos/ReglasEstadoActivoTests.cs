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

    [Theory]
    [InlineData(EstadosActivo.Disponible)]
    [InlineData(EstadosActivo.Asignado)]
    public void ValidarTransicion_ConAsignacionNoFinalizada_PermiteEstadosAsignables(string estadoNuevo)
    {
        ReglasEstadoActivo.ValidarTransicion(
            EstadosActivo.Asignado,
            estadoNuevo,
            tieneAsignacionNoFinalizada: true);
    }
}

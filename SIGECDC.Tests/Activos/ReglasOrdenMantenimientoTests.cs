using SIGECDC.Domain.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class ReglasOrdenMantenimientoTests
{
    [Fact]
    public void ValidarInicio_Programado_PermiteTransicion()
    {
        ReglasOrdenMantenimiento.ValidarInicio(
            EstadosMantenimiento.Programado);
    }

    [Theory]
    [InlineData(EstadosMantenimiento.EnProceso)]
    [InlineData(EstadosMantenimiento.Finalizado)]
    [InlineData(EstadosMantenimiento.Cancelado)]
    [InlineData(null)]
    public void ValidarInicio_EstadoDistintoDeProgramado_Rechaza(
        string? estado)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReglasOrdenMantenimiento.ValidarInicio(estado));
    }

    [Fact]
    public void ValidarCierre_EnProceso_PermiteTransicion()
    {
        ReglasOrdenMantenimiento.ValidarCierre(
            EstadosMantenimiento.EnProceso);
    }

    [Theory]
    [InlineData(EstadosMantenimiento.Programado)]
    [InlineData(EstadosMantenimiento.Finalizado)]
    [InlineData(EstadosMantenimiento.Cancelado)]
    [InlineData(null)]
    public void ValidarCierre_EstadoDistintoDeEnProceso_Rechaza(
        string? estado)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReglasOrdenMantenimiento.ValidarCierre(estado));
    }

    [Theory]
    [InlineData(
        EstadosActivo.Disponible,
        EstadosActivo.EnMantenimiento)]
    [InlineData(
        EstadosActivo.Asignado,
        EstadosActivo.EnMantenimiento)]
    [InlineData(
        EstadosActivo.EnMantenimiento,
        EstadosActivo.EnMantenimiento)]
    [InlineData(
        EstadosActivo.FueraDeServicio,
        EstadosActivo.FueraDeServicio)]
    public void DeterminarEstadoAlIniciar_RespetaDisponibilidad(
        string estadoActual,
        string estadoEsperado)
    {
        Assert.Equal(
            estadoEsperado,
            ReglasOrdenMantenimiento.DeterminarEstadoActivoAlIniciar(
                estadoActual));
    }

    [Fact]
    public void DeterminarEstadoAlIniciar_DadoDeBaja_Rechaza()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() =>
            ReglasOrdenMantenimiento.DeterminarEstadoActivoAlIniciar(
                EstadosActivo.DadoDeBaja));

        Assert.Contains(
            "dado de baja",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(
        EstadosActivo.FueraDeServicio,
        false,
        false,
        EstadosActivo.FueraDeServicio)]
    [InlineData(
        EstadosActivo.DadoDeBaja,
        false,
        false,
        EstadosActivo.DadoDeBaja)]
    [InlineData(
        EstadosActivo.EnMantenimiento,
        true,
        false,
        EstadosActivo.EnMantenimiento)]
    [InlineData(
        EstadosActivo.EnMantenimiento,
        false,
        true,
        EstadosActivo.Asignado)]
    [InlineData(
        EstadosActivo.EnMantenimiento,
        false,
        false,
        EstadosActivo.Disponible)]
    public void DeterminarEstadoAlFinalizar_RecalculaRestricciones(
        string estadoActual,
        bool otroMantenimiento,
        bool asignacionNoFinalizada,
        string estadoEsperado)
    {
        Assert.Equal(
            estadoEsperado,
            ReglasOrdenMantenimiento.DeterminarEstadoActivoAlFinalizar(
                estadoActual,
                otroMantenimiento,
                asignacionNoFinalizada));
    }
}

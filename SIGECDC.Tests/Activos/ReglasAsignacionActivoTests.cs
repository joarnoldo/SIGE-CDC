using SIGECDC.Domain.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class ReglasAsignacionActivoTests
{
    [Fact]
    public void HayTraslape_CuandoLosRangosCompartenElDiaLimite_RetornaVerdadero()
    {
        var hayTraslape = ReglasAsignacionActivo.HayTraslape(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 5),
            new DateTime(2026, 8, 5),
            new DateTime(2026, 8, 10));

        Assert.True(hayTraslape);
    }

    [Fact]
    public void HayTraslape_CuandoElNuevoRangoIniciaAlDiaSiguiente_RetornaFalso()
    {
        var hayTraslape = ReglasAsignacionActivo.HayTraslape(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 5),
            new DateTime(2026, 8, 6),
            new DateTime(2026, 8, 10));

        Assert.False(hayTraslape);
    }

    [Theory]
    [InlineData(EstadosActivo.Disponible, true)]
    [InlineData(EstadosActivo.Asignado, true)]
    [InlineData(EstadosActivo.EnMantenimiento, false)]
    [InlineData(EstadosActivo.FueraDeServicio, false)]
    [InlineData(EstadosActivo.DadoDeBaja, false)]
    [InlineData(null, false)]
    public void EsEstadoAsignable_RespetaLosEstadosOperativos(string? estado, bool esperado)
    {
        Assert.Equal(esperado, ReglasAsignacionActivo.EsEstadoAsignable(estado));
    }

    [Fact]
    public void ValidarRango_FechaFinalAnterior_RechazaLaAsignacion()
    {
        Assert.Throws<ArgumentException>(() => ReglasAsignacionActivo.ValidarRango(
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 9)));
    }

    [Fact]
    public void ValidarEstadoAsignable_Mantenimiento_ReportaFaltaDeDisponibilidad()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() =>
            ReglasAsignacionActivo.ValidarEstadoAsignable(EstadosActivo.EnMantenimiento));

        Assert.Contains("no se encuentra disponible", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(EstadosActivo.Disponible, false, true)]
    [InlineData(EstadosActivo.Asignado, false, true)]
    [InlineData(EstadosActivo.Disponible, true, false)]
    [InlineData(EstadosActivo.Asignado, true, false)]
    [InlineData(EstadosActivo.EnMantenimiento, false, false)]
    [InlineData(EstadosActivo.FueraDeServicio, false, false)]
    [InlineData(EstadosActivo.DadoDeBaja, false, false)]
    public void EstaDisponible_CombinaEstadoYTraslape(
        string estado,
        bool tieneConflicto,
        bool esperado)
    {
        Assert.Equal(esperado, ReglasAsignacionActivo.EstaDisponible(estado, tieneConflicto));
    }

    [Fact]
    public void DescribirDisponibilidad_ConTraslape_ExplicaLaAsignacionVigente()
    {
        var motivo = ReglasAsignacionActivo.DescribirDisponibilidad(
            EstadosActivo.Disponible,
            tieneConflictoDeAsignacion: true);

        Assert.Contains("asignación vigente", motivo, StringComparison.OrdinalIgnoreCase);
    }
}

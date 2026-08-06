using System.Globalization;
using SIGECDC.Domain.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class ReglasRegistroUsoActivoTests
{
    [Theory]
    [InlineData(EstadosActivo.Disponible)]
    [InlineData(EstadosActivo.Asignado)]
    public void ValidarEstadoUtilizable_EstadoOperativo_Permite(
        string estado)
    {
        ReglasRegistroUsoActivo.ValidarEstadoUtilizable(
            estado,
            tieneMantenimientoEnLaFecha: false);
    }

    [Theory]
    [InlineData(EstadosActivo.EnMantenimiento)]
    [InlineData(EstadosActivo.FueraDeServicio)]
    [InlineData(EstadosActivo.DadoDeBaja)]
    [InlineData(null)]
    public void ValidarEstadoUtilizable_EstadoNoOperativo_Rechaza(
        string? estado)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReglasRegistroUsoActivo.ValidarEstadoUtilizable(
                estado,
                tieneMantenimientoEnLaFecha: false));
    }

    [Fact]
    public void ValidarEstadoUtilizable_MantenimientoEnProceso_Rechaza()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() =>
            ReglasRegistroUsoActivo.ValidarEstadoUtilizable(
                EstadosActivo.Disponible,
                tieneMantenimientoEnLaFecha: true));

        Assert.Contains(
            "mantenimiento",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(
        EstadosMantenimiento.EnProceso,
        "2026-07-19T08:00:00",
        null,
        "2026-07-19",
        true)]
    [InlineData(
        EstadosMantenimiento.EnProceso,
        "2026-07-19T08:00:00",
        null,
        "2026-07-18",
        false)]
    [InlineData(
        EstadosMantenimiento.Finalizado,
        "2026-07-17T15:00:00",
        "2026-07-19T09:00:00",
        "2026-07-17",
        true)]
    [InlineData(
        EstadosMantenimiento.Finalizado,
        "2026-07-17T15:00:00",
        "2026-07-19T09:00:00",
        "2026-07-19",
        true)]
    [InlineData(
        EstadosMantenimiento.Finalizado,
        "2026-07-17T15:00:00",
        "2026-07-19T09:00:00",
        "2026-07-20",
        false)]
    [InlineData(
        EstadosMantenimiento.Programado,
        "2026-07-19T08:00:00",
        null,
        "2026-07-19",
        false)]
    [InlineData(
        EstadosMantenimiento.Cancelado,
        "2026-07-19T08:00:00",
        "2026-07-19T09:00:00",
        "2026-07-19",
        false)]
    public void EsFechaAfectadaPorMantenimiento_AplicaIntervaloPorDia(
        string estado,
        string inicio,
        string? fin,
        string fechaRegistro,
        bool esperado)
    {
        var resultado =
            ReglasRegistroUsoActivo.EsFechaAfectadaPorMantenimiento(
                DateTime.Parse(
                    fechaRegistro,
                    CultureInfo.InvariantCulture),
                estado,
                DateTime.Parse(
                    inicio,
                    CultureInfo.InvariantCulture),
                fin is null
                    ? null
                    : DateTime.Parse(
                        fin,
                        CultureInfo.InvariantCulture));

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(TiposMedicionUso.Horas)]
    [InlineData(TiposMedicionUso.Kilometros)]
    [InlineData(TiposMedicionUso.Unidades)]
    public void ValidarTipoMedicion_TipoOperativo_Permite(string tipo)
    {
        ReglasRegistroUsoActivo.ValidarTipoMedicionRegistrable(tipo);
    }

    [Theory]
    [InlineData(TiposMedicionUso.NoAplica)]
    [InlineData("Otro")]
    [InlineData(null)]
    public void ValidarTipoMedicion_TipoNoRegistrable_Rechaza(
        string? tipo)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReglasRegistroUsoActivo.ValidarTipoMedicionRegistrable(tipo));
    }

    [Fact]
    public void CalcularCantidadUso_LecturasValidas_CalculaDiferencia()
    {
        var cantidad = ReglasRegistroUsoActivo.CalcularCantidadUso(
            125.25m,
            140.75m);

        Assert.Equal(15.50m, cantidad);
    }

    [Theory]
    [InlineData("10.00", "10.00")]
    [InlineData("10.00", "9.99")]
    public void CalcularCantidadUso_LecturaNoAumenta_Rechaza(
        string anterior,
        string nueva)
    {
        Assert.Throws<ArgumentException>(() =>
            ReglasRegistroUsoActivo.CalcularCantidadUso(
                decimal.Parse(anterior, CultureInfo.InvariantCulture),
                decimal.Parse(nueva, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    [InlineData("10000000000000000.00")]
    public void ValidarLectura_ValorFueraDeDecimalOficial_Rechaza(
        string valor)
    {
        Assert.Throws<ArgumentException>(() =>
            ReglasRegistroUsoActivo.ValidarLectura(
                decimal.Parse(valor, CultureInfo.InvariantCulture),
                "La lectura"));
    }
}

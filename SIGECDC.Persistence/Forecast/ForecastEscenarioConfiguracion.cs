using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastEscenarioConfiguracion : IEntityTypeConfiguration<ForecastEscenario>
{
    public void Configure(EntityTypeBuilder<ForecastEscenario> entidad)
    {
        entidad.ToTable("ForecastEscenario", tabla =>
        {
            tabla.HasCheckConstraint(
                "CK_ForecastEscenario_Fechas",
                "`FechaInicioProyeccion` <= `FechaFinProyeccion`");
            tabla.HasCheckConstraint(
                "CK_ForecastEscenario_Historicos",
                "`PeriodosHistoricosConsiderados` > 0");
        });

        entidad.HasKey(escenario => escenario.IdForecastEscenario);

        entidad.Property(escenario => escenario.IdForecastEscenario)
            .ValueGeneratedOnAdd();

        entidad.Property(escenario => escenario.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        entidad.Property(escenario => escenario.Descripcion)
            .HasMaxLength(500);

        entidad.Property(escenario => escenario.FechaInicioProyeccion)
            .HasColumnType("date")
            .IsRequired();

        entidad.Property(escenario => escenario.FechaFinProyeccion)
            .HasColumnType("date")
            .IsRequired();

        entidad.Property(escenario => escenario.PeriodosHistoricosConsiderados)
            .HasDefaultValue(3)
            .IsRequired();

        entidad.Property(escenario => escenario.EstadoEscenario)
            .HasColumnType("enum('Borrador','Calculado','Guardado','Comparado','Anulado')")
            .HasDefaultValue(EstadosEscenarioForecast.Borrador)
            .IsRequired();

        entidad.Property(escenario => escenario.MontoProyectadoTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        entidad.Property(escenario => escenario.MontoRealTotal)
            .HasPrecision(18, 2);

        entidad.Property(escenario => escenario.DiferenciaTotal)
            .HasPrecision(18, 2);

        entidad.Property(escenario => escenario.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        entidad.Property(escenario => escenario.CreadoPor)
            .HasMaxLength(255);

        entidad.Property(escenario => escenario.ModificadoPor)
            .HasMaxLength(255);

        entidad.Property(escenario => escenario.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasIndex(escenario => new
            {
                escenario.FechaInicioProyeccion,
                escenario.FechaFinProyeccion
            })
            .HasDatabaseName("IX_ForecastEscenario_Fechas");

        entidad.HasIndex(escenario => escenario.EstadoEscenario)
            .HasDatabaseName("IX_ForecastEscenario_EstadoEscenario");

        entidad.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(escenario => escenario.CreadoPor)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_ForecastEscenario_CreadoPor");

        entidad.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(escenario => escenario.ModificadoPor)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_ForecastEscenario_ModificadoPor");
    }
}

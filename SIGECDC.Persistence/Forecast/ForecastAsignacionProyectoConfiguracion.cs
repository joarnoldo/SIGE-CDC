using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastAsignacionProyectoConfiguracion
    : IEntityTypeConfiguration<ForecastAsignacionProyecto>
{
    public void Configure(EntityTypeBuilder<ForecastAsignacionProyecto> entidad)
    {
        entidad.ToTable("ForecastAsignacionProyecto", tabla =>
            tabla.HasCheckConstraint(
                "CK_ForecastAsignacionProyecto_Porcentaje",
                "`Porcentaje` > 0.0000 AND `Porcentaje` <= 100.0000"));

        entidad.HasKey(item => item.IdForecastAsignacionProyecto);
        entidad.Property(item => item.IdForecastAsignacionProyecto).ValueGeneratedOnAdd();
        entidad.Property(item => item.Porcentaje).HasPrecision(7, 4).IsRequired();
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasAlternateKey(item => new
            {
                item.IdForecastEscenario,
                item.IdForecastPeriodo,
                item.IdForecastParticipante,
                item.IdProyecto
            })
            .HasName("UX_FcstAsign_Esc_Per_Part_Proy");
        entidad.HasIndex(item => new { item.IdForecastEscenario, item.IdForecastParticipante })
            .HasDatabaseName("IX_FcstAsign_Esc_Part");
        entidad.HasIndex(item => new { item.IdForecastEscenario, item.IdForecastPeriodo })
            .HasDatabaseName("IX_FcstAsign_Esc_Per");
        entidad.HasIndex(item => item.IdProyecto)
            .HasDatabaseName("IX_ForecastAsignacionProyecto_IdProyecto");

        entidad.HasOne(item => item.ForecastParticipante)
            .WithMany(item => item.AsignacionesProyecto)
            .HasForeignKey(item => new { item.IdForecastEscenario, item.IdForecastParticipante })
            .HasPrincipalKey(item => new { item.IdForecastEscenario, item.IdForecastParticipante })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FcstAsign_Participante");
        entidad.HasOne(item => item.ForecastPeriodo)
            .WithMany(item => item.AsignacionesProyecto)
            .HasForeignKey(item => new { item.IdForecastEscenario, item.IdForecastPeriodo })
            .HasPrincipalKey(item => new { item.IdForecastEscenario, item.IdForecastPeriodo })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FcstAsign_Periodo");
        entidad.HasOne(item => item.Proyecto)
            .WithMany()
            .HasForeignKey(item => item.IdProyecto)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FcstAsign_Proyecto");
    }
}

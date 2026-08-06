using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastDetalleConfiguracion
    : IEntityTypeConfiguration<ForecastDetalle>
{
    public void Configure(EntityTypeBuilder<ForecastDetalle> entidad)
    {
        entidad.ToTable("ForecastDetalle");

        entidad.HasKey(detalle => detalle.IdForecastDetalle);

        entidad.Property(detalle => detalle.IdForecastDetalle)
            .ValueGeneratedOnAdd();

        entidad.Property(detalle => detalle.Concepto)
            .HasMaxLength(150)
            .IsRequired();

        entidad.Property(detalle => detalle.MontoBase)
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        entidad.Property(detalle => detalle.MontoAjuste)
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        entidad.Property(detalle => detalle.MontoProyectado)
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        entidad.Property(detalle => detalle.MontoReal)
            .HasPrecision(18, 2);

        entidad.Property(detalle => detalle.Diferencia)
            .HasPrecision(18, 2);

        entidad.Property(detalle => detalle.Observaciones)
            .HasMaxLength(500);

        entidad.Property(detalle => detalle.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasIndex(detalle => new
            {
                detalle.IdForecastEscenario,
                detalle.IdForecastPeriodo,
                detalle.IdForecastParticipante,
                detalle.IdProyecto,
                detalle.Concepto
            })
            .IsUnique()
            .HasDatabaseName("UX_FcstDetalle_Esc_Per_Part_Proy_Concepto");

        entidad.HasOne(detalle => detalle.ForecastAsignacionProyecto)
            .WithMany(asignacion => asignacion.Detalles)
            .HasForeignKey(detalle => new
            {
                detalle.IdForecastEscenario,
                detalle.IdForecastPeriodo,
                detalle.IdForecastParticipante,
                detalle.IdProyecto
            })
            .HasPrincipalKey(asignacion => new
            {
                asignacion.IdForecastEscenario,
                asignacion.IdForecastPeriodo,
                asignacion.IdForecastParticipante,
                asignacion.IdProyecto
            })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FcstDetalle_AsignacionProyecto");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastDetalleConfiguracion
    : IEntityTypeConfiguration<ForecastDetalle>
{
    public void Configure(EntityTypeBuilder<ForecastDetalle> entidad)
    {
        entidad.ToTable("ForecastDetalle", tabla =>
        {
            tabla.HasCheckConstraint("CK_ForecastDetalle_Observaciones",
                "`CantidadObservaciones` >= 0 AND `HistoricoLimitado` IN (0,1)");
            tabla.HasCheckConstraint("CK_ForecastDetalle_DiferenciaAbsoluta",
                "`DiferenciaAbsoluta` IS NULL OR `DiferenciaAbsoluta` >= 0");
        });

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

        entidad.Property(detalle => detalle.MontoBaseSinRedondear)
            .HasPrecision(65, 28);

        entidad.Property(detalle => detalle.MontoProyectadoSinRedondear)
            .HasPrecision(65, 28);

        entidad.Property(detalle => detalle.CantidadObservaciones)
            .HasDefaultValue(0)
            .IsRequired();

        entidad.Property(detalle => detalle.HistoricoLimitado)
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        entidad.Property(detalle => detalle.VersionMotor)
            .HasMaxLength(100);

        entidad.Property(detalle => detalle.DiferenciaFirmada)
            .HasPrecision(18, 2);

        entidad.Property(detalle => detalle.DiferenciaAbsoluta)
            .HasPrecision(18, 2);

        entidad.Property(detalle => detalle.DiferenciaPorcentual)
            .HasPrecision(18, 4);

        entidad.Property(detalle => detalle.Observaciones)
            .HasMaxLength(500);

        entidad.Property(detalle => detalle.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasIndex(detalle => new
            {
                detalle.IdForecastAsignacionProyecto,
                detalle.IdConceptoPlanillaVersion
            })
            .IsUnique()
            .HasDatabaseName("UX_FL_Detalle_Asignacion_Version");

        entidad.HasIndex(detalle => detalle.IdConceptoPlanillaVersion)
            .HasDatabaseName("IX_FcstDetalle_ConceptoVersion");

        entidad.HasOne(detalle => detalle.ForecastAsignacionProyecto)
            .WithMany(asignacion => asignacion.Detalles)
            .HasForeignKey(detalle => detalle.IdForecastAsignacionProyecto)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FcstDetalle_AsignacionProyecto");

        entidad.HasOne(detalle => detalle.ConceptoPlanillaVersion)
            .WithMany()
            .HasForeignKey(detalle => detalle.IdConceptoPlanillaVersion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FcstDetalle_ConceptoVersion");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastObservacionHistoricaConfiguracion
    : IEntityTypeConfiguration<ForecastObservacionHistorica>
{
    public void Configure(EntityTypeBuilder<ForecastObservacionHistorica> entidad)
    {
        entidad.ToTable("ForecastObservacionHistorica", tabla =>
            tabla.HasCheckConstraint("CK_ForecastObservacion_Comparabilidad",
                "(`EsComparable` = 1 AND `IdDetallePlanilla` IS NOT NULL AND `MontoObservado` IS NOT NULL AND `MotivoNoComparable` IS NULL) OR (`EsComparable` = 0 AND `MotivoNoComparable` IS NOT NULL)"));
        entidad.HasKey(item => item.IdForecastObservacionHistorica);
        entidad.Property(item => item.IdForecastObservacionHistorica).ValueGeneratedOnAdd();
        entidad.Property(item => item.MontoObservado).HasPrecision(18, 2);
        entidad.Property(item => item.EsComparable).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.MotivoNoComparable).HasMaxLength(100);
        entidad.HasIndex(item => new
            {
                item.IdForecastEscenario,
                item.IdForecastParticipante,
                item.IdForecastFuenteHistorica,
                item.IdConceptoPlanillaVersion
            }).IsUnique().HasDatabaseName("UX_ForecastObservacion_Origen_Concepto");
        entidad.HasIndex(item => item.IdForecastFuenteHistorica)
            .HasDatabaseName("IX_ForecastObservacion_Fuente");
        entidad.HasIndex(item => item.IdForecastParticipante)
            .HasDatabaseName("IX_ForecastObservacion_Participante");
        entidad.HasOne(item => item.ForecastEscenario).WithMany(item => item.ObservacionesHistoricas)
            .HasForeignKey(item => item.IdForecastEscenario).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ForecastObservacion_Escenario");
        entidad.HasOne(item => item.ForecastParticipante).WithMany()
            .HasForeignKey(item => item.IdForecastParticipante).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ForecastObservacion_Participante");
        entidad.HasOne(item => item.ForecastFuenteHistorica).WithMany()
            .HasForeignKey(item => item.IdForecastFuenteHistorica).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastObservacion_Fuente");
        entidad.HasOne(item => item.DetallePlanilla).WithMany()
            .HasForeignKey(item => item.IdDetallePlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastObservacion_DetallePlanilla");
        entidad.HasOne(item => item.ConceptoPlanillaVersion).WithMany()
            .HasForeignKey(item => item.IdConceptoPlanillaVersion).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastObservacion_ConceptoVersion");
    }
}

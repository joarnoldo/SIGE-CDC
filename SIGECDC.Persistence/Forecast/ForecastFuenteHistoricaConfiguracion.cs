using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastFuenteHistoricaConfiguracion
    : IEntityTypeConfiguration<ForecastFuenteHistorica>
{
    public void Configure(EntityTypeBuilder<ForecastFuenteHistorica> entidad)
    {
        entidad.ToTable("ForecastFuenteHistorica", tabla =>
            tabla.HasCheckConstraint("CK_ForecastFuente_Orden", "`NumeroOrden` > 0"));

        entidad.HasKey(fuente => fuente.IdForecastFuenteHistorica);

        entidad.Property(fuente => fuente.IdForecastFuenteHistorica)
            .ValueGeneratedOnAdd();

        entidad.Property(fuente => fuente.NumeroOrden)
            .IsRequired();

        entidad.HasIndex(fuente => new
            {
                fuente.IdForecastEscenario,
                fuente.IdPlanilla
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastFuente_Escenario_Planilla");

        entidad.HasIndex(fuente => new
            {
                fuente.IdForecastEscenario,
                fuente.NumeroOrden
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastFuente_Escenario_Orden");

        entidad.HasOne(fuente => fuente.ForecastEscenario)
            .WithMany(escenario => escenario.FuentesHistoricas)
            .HasForeignKey(fuente => fuente.IdForecastEscenario)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ForecastFuente_ForecastEscenario");

        entidad.HasOne(fuente => fuente.Planilla)
            .WithMany()
            .HasForeignKey(fuente => fuente.IdPlanilla)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastFuente_Planilla");
    }
}

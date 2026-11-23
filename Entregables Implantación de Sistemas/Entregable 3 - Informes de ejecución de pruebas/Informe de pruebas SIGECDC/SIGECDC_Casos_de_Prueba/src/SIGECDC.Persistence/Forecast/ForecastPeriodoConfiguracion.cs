using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastPeriodoConfiguracion : IEntityTypeConfiguration<ForecastPeriodo>
{
    public void Configure(EntityTypeBuilder<ForecastPeriodo> entidad)
    {
        entidad.ToTable("ForecastPeriodo", tabla =>
        {
            tabla.HasCheckConstraint("CK_ForecastPeriodo_Orden", "`NumeroOrden` > 0");
            tabla.HasCheckConstraint(
                "CK_ForecastPeriodo_Fechas",
                "`FechaInicio` <= `FechaFin`");
        });

        entidad.HasKey(periodo => periodo.IdForecastPeriodo);

        entidad.Property(periodo => periodo.IdForecastPeriodo)
            .ValueGeneratedOnAdd();

        entidad.Property(periodo => periodo.NumeroOrden)
            .IsRequired();

        entidad.Property(periodo => periodo.TipoPeriodo)
            .HasColumnType("enum('Mensual','Quincenal')")
            .IsRequired();

        entidad.Property(periodo => periodo.FechaInicio)
            .HasColumnType("date")
            .IsRequired();

        entidad.Property(periodo => periodo.FechaFin)
            .HasColumnType("date")
            .IsRequired();

        entidad.Property(periodo => periodo.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasIndex(periodo => new
            {
                periodo.IdForecastEscenario,
                periodo.NumeroOrden
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastPeriodo_Escenario_Orden");

        entidad.HasIndex(periodo => new
            {
                periodo.IdForecastEscenario,
                periodo.FechaInicio,
                periodo.FechaFin
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastPeriodo_Escenario_Fechas");

        entidad.HasAlternateKey(periodo => new
            {
                periodo.IdForecastEscenario,
                periodo.IdForecastPeriodo
            })
            .HasName("UX_ForecastPeriodo_Escenario_Id");

        entidad.HasOne(periodo => periodo.ForecastEscenario)
            .WithMany(escenario => escenario.Periodos)
            .HasForeignKey(periodo => periodo.IdForecastEscenario)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ForecastPeriodo_ForecastEscenario");
    }
}

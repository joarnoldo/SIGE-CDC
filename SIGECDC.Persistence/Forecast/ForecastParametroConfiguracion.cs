using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastParametroConfiguracion : IEntityTypeConfiguration<ForecastParametro>
{
    public void Configure(EntityTypeBuilder<ForecastParametro> entidad)
    {
        entidad.ToTable("ForecastParametro");

        entidad.HasKey(parametro => parametro.IdForecastParametro);

        entidad.Property(parametro => parametro.IdForecastParametro)
            .ValueGeneratedOnAdd();

        entidad.Property(parametro => parametro.Codigo)
            .HasMaxLength(100)
            .IsRequired();

        entidad.Property(parametro => parametro.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        entidad.Property(parametro => parametro.TipoParametro)
            .HasColumnType("enum('Porcentaje','Monto','Cantidad','Texto')")
            .HasDefaultValue(TiposParametroPlanilla.Porcentaje)
            .IsRequired();

        entidad.Property(parametro => parametro.ValorDecimal)
            .HasPrecision(18, 4);

        entidad.Property(parametro => parametro.ValorTexto)
            .HasMaxLength(255);

        entidad.Property(parametro => parametro.Descripcion)
            .HasMaxLength(300);

        entidad.Property(parametro => parametro.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasIndex(parametro => new
            {
                parametro.IdForecastEscenario,
                parametro.Codigo
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastParametro_Escenario_Codigo");

        entidad.HasOne(parametro => parametro.ForecastEscenario)
            .WithMany(escenario => escenario.Parametros)
            .HasForeignKey(parametro => parametro.IdForecastEscenario)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ForecastParametro_ForecastEscenario");
    }
}

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
        entidad.ToTable("ForecastParametro", tabla =>
        {
            tabla.HasCheckConstraint("CK_FL_Parametro_Evidencia", "(IdConceptoPlanillaVersion IS NULL AND Unidad IS NULL AND ReferenciaSoporte IS NULL AND RegistradoPor IS NULL AND FechaRegistro IS NULL AND AutorizacionRequerida IS NULL AND AutorizadoPor IS NULL AND FechaAutorizacion IS NULL AND ReferenciaAutorizacion IS NULL) OR (IdConceptoPlanillaVersion IS NOT NULL AND Unidad IS NOT NULL AND CHAR_LENGTH(TRIM(Unidad)) > 0 AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND RegistradoPor IS NOT NULL AND CHAR_LENGTH(TRIM(RegistradoPor)) > 0 AND FechaRegistro IS NOT NULL AND AutorizacionRequerida IS NOT NULL AND AutorizacionRequerida IN (0,1) AND TipoParametro IN ('Monto','Porcentaje','Cantidad') AND ValorDecimal IS NOT NULL AND ValorTexto IS NULL AND ValorDecimal >= 0)");
            tabla.HasCheckConstraint("CK_FL_Parametro_Autorizacion", "(AutorizadoPor IS NULL AND FechaAutorizacion IS NULL AND ReferenciaAutorizacion IS NULL AND (AutorizacionRequerida IS NULL OR AutorizacionRequerida=0)) OR (AutorizacionRequerida IS NOT NULL AND AutorizadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AutorizadoPor)) > 0 AND FechaAutorizacion IS NOT NULL AND ReferenciaAutorizacion IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAutorizacion)) > 0)");
        });
        entidad.Property(x => x.IdConceptoPlanillaVersion).HasColumnType("bigint");
        entidad.Property(x => x.Unidad).HasMaxLength(30);
        entidad.Property(x => x.ReferenciaSoporte).HasMaxLength(500);
        entidad.Property(x => x.RegistradoPor).HasMaxLength(255);
        entidad.Property(x => x.FechaRegistro).HasColumnType("datetime");
        entidad.Property(x => x.AutorizacionRequerida).HasColumnType("tinyint(1)");
        entidad.Property(x => x.AutorizadoPor).HasMaxLength(255);
        entidad.Property(x => x.FechaAutorizacion).HasColumnType("datetime");
        entidad.Property(x => x.ReferenciaAutorizacion).HasMaxLength(500);
        entidad.HasIndex(x => x.IdConceptoPlanillaVersion).HasDatabaseName("IX_FL_Parametro_Concepto");
        entidad.HasOne<ConceptoPlanillaVersion>().WithMany().HasForeignKey(x => x.IdConceptoPlanillaVersion)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_Parametro_Concepto");

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

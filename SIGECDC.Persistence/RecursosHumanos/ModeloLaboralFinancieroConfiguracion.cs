using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class JornadaLaboralConfiguracion : IEntityTypeConfiguration<JornadaLaboral>
{
    public void Configure(EntityTypeBuilder<JornadaLaboral> entidad)
    {
        entidad.ToTable("JornadaLaboral", tabla =>
        {
            tabla.HasCheckConstraint("CK_JornadaLaboral_Horas",
                "`HorasOrdinariasDia` > 0 AND `HorasOrdinariasSemana` > 0");
            tabla.HasCheckConstraint("CK_JornadaLaboral_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
        });
        entidad.HasKey(item => item.IdJornadaLaboral);
        entidad.Property(item => item.IdJornadaLaboral).ValueGeneratedOnAdd();
        entidad.Property(item => item.Codigo).HasMaxLength(50).IsRequired();
        entidad.Property(item => item.Nombre).HasMaxLength(150).IsRequired();
        entidad.Property(item => item.TipoJornada).HasMaxLength(30).IsRequired();
        entidad.Property(item => item.HorasOrdinariasDia).HasPrecision(8, 4).IsRequired();
        entidad.Property(item => item.HorasOrdinariasSemana).HasPrecision(8, 4).IsRequired();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.FechaCreacion).HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").IsRequired();
        entidad.Property(item => item.CreadoPor).HasMaxLength(255);
        entidad.Property(item => item.FechaModificacion).HasColumnType("datetime(6)");
        entidad.Property(item => item.ModificadoPor).HasMaxLength(255);
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => new { item.Codigo, item.FechaVigenciaInicio })
            .IsUnique().HasDatabaseName("UX_JornadaLaboral_Codigo_Inicio");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.CreadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_JornadaLaboral_CreadoPor");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.ModificadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_JornadaLaboral_ModificadoPor");
    }
}

public sealed class OcupacionSalarioMinimoConfiguracion
    : IEntityTypeConfiguration<OcupacionSalarioMinimo>
{
    public void Configure(EntityTypeBuilder<OcupacionSalarioMinimo> entidad)
    {
        entidad.ToTable("OcupacionSalarioMinimo");
        entidad.HasKey(item => item.IdOcupacionSalarioMinimo);
        entidad.Property(item => item.IdOcupacionSalarioMinimo).ValueGeneratedOnAdd();
        entidad.Property(item => item.CodigoOficial).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.Nombre).HasMaxLength(200).IsRequired();
        entidad.Property(item => item.Descripcion).HasMaxLength(500);
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => item.CodigoOficial).IsUnique()
            .HasDatabaseName("UX_OcupacionSalarioMinimo_Codigo");
    }
}

public sealed class SalarioMinimoVersionConfiguracion
    : IEntityTypeConfiguration<SalarioMinimoVersion>
{
    public void Configure(EntityTypeBuilder<SalarioMinimoVersion> entidad)
    {
        entidad.ToTable("SalarioMinimoVersion", tabla =>
        {
            tabla.HasCheckConstraint("CK_SalarioMinimo_Monto", "`Monto` > 0");
            tabla.HasCheckConstraint("CK_SalarioMinimo_Periodicidad",
                "`PeriodicidadReferencia` IN ('Diario','Mensual')");
            tabla.HasCheckConstraint("CK_SalarioMinimo_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
        });
        entidad.HasKey(item => item.IdSalarioMinimoVersion);
        entidad.Property(item => item.IdSalarioMinimoVersion).ValueGeneratedOnAdd();
        entidad.Property(item => item.PeriodicidadReferencia).HasMaxLength(20).IsRequired();
        entidad.Property(item => item.TipoJornadaAplicable).HasMaxLength(30);
        entidad.Property(item => item.Monto).HasPrecision(18, 2).IsRequired();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.HasIndex(item => new
            {
                item.IdOcupacionSalarioMinimo,
                item.PeriodicidadReferencia,
                item.TipoJornadaAplicable,
                item.FechaVigenciaInicio
            })
            .IsUnique().HasDatabaseName("UX_SalarioMinimo_Ocupacion_Periodo_Jornada_Inicio");
        entidad.HasIndex(item => new
            {
                item.IdOcupacionSalarioMinimo,
                item.FechaVigenciaInicio,
                item.FechaVigenciaFin
            }).HasDatabaseName("IX_SalarioMinimo_Vigencia");
        entidad.HasOne(item => item.OcupacionSalarioMinimo).WithMany()
            .HasForeignKey(item => item.IdOcupacionSalarioMinimo).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalarioMinimo_Ocupacion");
        entidad.HasOne(item => item.VersionNormativa).WithMany()
            .HasForeignKey(item => item.IdVersionNormativa).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalarioMinimo_VersionNormativa");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class CondicionLaboralVersionConfiguracion : IEntityTypeConfiguration<CondicionLaboralVersion>
{
    public void Configure(EntityTypeBuilder<CondicionLaboralVersion> e)
    {
        e.ToTable("CondicionLaboralVersion", t =>
        {
            t.HasCheckConstraint("CK_CondicionLaboral_Fechas", "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
            t.HasCheckConstraint("CK_CondicionLaboral_Salario", "`SalarioBase` > 0");
            t.HasCheckConstraint("CK_CondicionLaboral_Soporte", "CHAR_LENGTH(TRIM(`ReferenciaSoporte`)) > 0");
        });
        e.HasKey(x => x.IdCondicionLaboralVersion);
        e.Property(x => x.IdCondicionLaboralVersion).ValueGeneratedOnAdd();
        e.Property(x => x.FechaVigenciaInicio).HasColumnType("date");
        e.Property(x => x.FechaVigenciaFin).HasColumnType("date");
        e.Property(x => x.SalarioBase).HasPrecision(18, 2);
        e.Property(x => x.ReferenciaSoporte).HasMaxLength(500).IsRequired();
        e.Property(x => x.FechaCreacion).HasColumnType("datetime(6)").HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        e.Property(x => x.CreadoPor).HasMaxLength(255).IsRequired();
        e.Property(x => x.EstadoRegistro).HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        e.HasIndex(x => new { x.IdContrato, x.FechaVigenciaInicio }).IsUnique().HasDatabaseName("UX_CondicionLaboral_Contrato_Inicio");
        e.HasIndex(x => new { x.IdContrato, x.FechaVigenciaInicio, x.FechaVigenciaFin }).HasDatabaseName("IX_CondicionLaboral_Contrato_Vigencia");
        e.HasIndex(x => x.IdPuesto).HasDatabaseName("IX_CondicionLaboral_Puesto");
        e.HasIndex(x => x.IdDepartamento).HasDatabaseName("IX_CondicionLaboral_Departamento");
        e.HasIndex(x => x.IdOcupacionSalarioMinimo).HasDatabaseName("IX_CondicionLaboral_Ocupacion");
        e.HasIndex(x => x.IdJornadaLaboral).HasDatabaseName("IX_CondicionLaboral_Jornada");
        e.HasIndex(x => x.CreadoPor).HasDatabaseName("IX_CondicionLaboral_CreadoPor");
        e.HasOne(x => x.Contrato).WithMany(x => x.CondicionesLaborales).HasForeignKey(x => x.IdContrato)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CondicionLaboral_Contrato");
        e.HasOne(x => x.Puesto).WithMany().HasForeignKey(x => x.IdPuesto)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CondicionLaboral_Puesto");
        e.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.IdDepartamento)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CondicionLaboral_Departamento");
        e.HasOne(x => x.OcupacionSalarioMinimo).WithMany().HasForeignKey(x => x.IdOcupacionSalarioMinimo)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CondicionLaboral_Ocupacion");
        e.HasOne(x => x.JornadaLaboral).WithMany().HasForeignKey(x => x.IdJornadaLaboral)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CondicionLaboral_Jornada");
        e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreadoPor)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CondicionLaboral_CreadoPor");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class IncidenciaPlanillaConfirmacionConfiguracion : IEntityTypeConfiguration<IncidenciaPlanillaConfirmacion>
{
    public void Configure(EntityTypeBuilder<IncidenciaPlanillaConfirmacion> e)
    {
        e.ToTable("IncidenciaPlanillaConfirmacion", t =>
        {
            t.HasCheckConstraint("CK_IPC_Version", "NumeroVersion > 0");
            t.HasCheckConstraint("CK_IPC_UnidadCantidad", "(Unidad IN ('Hora','Día') AND Cantidad IS NOT NULL AND Cantidad > 0) OR (Unidad IN ('Monto','Porcentaje') AND Cantidad IS NULL)");
            t.HasCheckConstraint("CK_IPC_Monto", "MontoManual IS NULL OR (Unidad='Monto' AND MontoManual >= 0)");
            t.HasCheckConstraint("CK_IPC_TipoDia", "TipoDiaHE IS NULL OR TipoDiaHE IN ('Ordinario','Feriado','Descanso','Especial')");
            t.HasCheckConstraint("CK_IPC_Soporte", "CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0");
            t.HasCheckConstraint("CK_IPC_Huella", "CHAR_LENGTH(HuellaIncidencia)=64 AND REGEXP_LIKE(HuellaIncidencia,'^[A-F0-9]{64}$','c')");
            t.HasCheckConstraint("CK_IPC_Autorizacion", "AutorizacionRequerida IN (0,1) AND ((AutorizacionRequerida=0 AND AutorizadoPor IS NULL AND FechaAutorizacion IS NULL AND ReferenciaAutorizacion IS NULL) OR (AutorizadoPor IS NOT NULL AND FechaAutorizacion IS NOT NULL AND ReferenciaAutorizacion IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAutorizacion)) > 0))");
            t.HasCheckConstraint("CK_IPC_Base", "(IdCondicionBase IS NULL AND FechaBaseInicio IS NULL AND FechaBaseFin IS NULL) OR (IdParametroPlanilla IS NOT NULL AND IdCondicionBase IS NOT NULL AND FechaBaseInicio IS NOT NULL AND FechaBaseFin IS NOT NULL AND FechaBaseFin >= FechaBaseInicio AND FechaHecho BETWEEN FechaBaseInicio AND FechaBaseFin)");
        });
        e.HasKey(c => c.IdIncidenciaPlanillaConfirmacion);
        e.Property(c => c.IdIncidenciaPlanillaConfirmacion).HasColumnType("bigint").ValueGeneratedOnAdd();
        e.Property(c => c.IdIncidenciaPlanilla).HasColumnType("bigint");
        e.Property(c => c.NumeroVersion).HasColumnType("int");
        e.Property(c => c.IdConceptoPlanillaVersion).HasColumnType("bigint");
        e.Property(c => c.FechaHecho).HasColumnType("date");
        e.Property(c => c.Unidad).HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        e.Property(c => c.Cantidad).HasColumnType("decimal(18,2)");
        e.Property(c => c.MontoManual).HasColumnType("decimal(18,2)");
        e.Property(c => c.TipoDiaHE).HasColumnType("varchar(20)").HasMaxLength(20);
        e.Property(c => c.ReferenciaSoporte).HasColumnType("varchar(500)").HasMaxLength(500).IsRequired();
        e.Property(c => c.HuellaIncidencia).HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        e.Property(c => c.ConfirmadoPor).HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        e.Property(c => c.FechaConfirmacion).HasColumnType("datetime(6)");
        e.Property(c => c.AutorizacionRequerida).HasColumnType("tinyint(1)");
        e.Property(c => c.AutorizadoPor).HasColumnType("varchar(255)").HasMaxLength(255);
        e.Property(c => c.FechaAutorizacion).HasColumnType("datetime(6)");
        e.Property(c => c.ReferenciaAutorizacion).HasColumnType("varchar(500)").HasMaxLength(500);
        e.Property(c => c.IdParametroPlanilla).HasColumnType("int");
        e.Property(c => c.IdCondicionBase).HasColumnType("bigint");
        e.Property(c => c.FechaBaseInicio).HasColumnType("date");
        e.Property(c => c.FechaBaseFin).HasColumnType("date");
        e.HasIndex(c => new { c.IdIncidenciaPlanilla, c.NumeroVersion }).IsUnique().HasDatabaseName("UX_IPC_Incidencia_Version");
        e.HasIndex(c => c.IdConceptoPlanillaVersion).HasDatabaseName("IX_IPC_ConceptoVersion");
        e.HasIndex(c => c.ConfirmadoPor).HasDatabaseName("IX_IPC_ConfirmadoPor");
        e.HasIndex(c => c.AutorizadoPor).HasDatabaseName("IX_IPC_AutorizadoPor");
        e.HasIndex(c => c.IdParametroPlanilla).HasDatabaseName("IX_IPC_Parametro");
        e.HasIndex(c => c.IdCondicionBase).HasDatabaseName("IX_IPC_CondicionBase");
        e.HasOne(c => c.IncidenciaPlanilla).WithMany().HasForeignKey(c => c.IdIncidenciaPlanilla).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_IPC_Incidencia");
        e.HasOne(c => c.ConceptoPlanillaVersion).WithMany().HasForeignKey(c => c.IdConceptoPlanillaVersion).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_IPC_ConceptoVersion");
        e.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.ConfirmadoPor).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_IPC_ConfirmadoPor");
        e.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.AutorizadoPor).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_IPC_AutorizadoPor");
        e.HasOne(c => c.ParametroPlanilla).WithMany().HasForeignKey(c => c.IdParametroPlanilla).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_IPC_Parametro");
        e.HasOne<CondicionLaboralVersion>().WithMany().HasForeignKey(c => c.IdCondicionBase).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_IPC_CondicionBase");
    }
}

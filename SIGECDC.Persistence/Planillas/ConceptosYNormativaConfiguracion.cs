using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class ConceptoPlanillaConfiguracion : IEntityTypeConfiguration<ConceptoPlanilla>
{
    public void Configure(EntityTypeBuilder<ConceptoPlanilla> entidad)
    {
        entidad.ToTable("ConceptoPlanilla");
        entidad.HasKey(item => item.IdConceptoPlanilla);
        entidad.Property(item => item.IdConceptoPlanilla).ValueGeneratedOnAdd();
        entidad.Property(item => item.Codigo).HasMaxLength(80).IsRequired();
        entidad.Property(item => item.Nombre).HasMaxLength(150).IsRequired();
        entidad.Property(item => item.Descripcion).HasMaxLength(500);
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue("Activo")
            .IsRequired();
        entidad.Property(item => item.FechaCreacion)
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .IsRequired();
        entidad.Property(item => item.CreadoPor).HasMaxLength(255);
        entidad.HasIndex(item => item.Codigo).IsUnique().HasDatabaseName("UX_ConceptoPlanilla_Codigo");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.CreadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_ConceptoPlanilla_CreadoPor");
    }
}

public sealed class ConceptoPlanillaVersionConfiguracion
    : IEntityTypeConfiguration<ConceptoPlanillaVersion>
{
    public void Configure(EntityTypeBuilder<ConceptoPlanillaVersion> entidad)
    {
        entidad.ToTable("ConceptoPlanillaVersion", tabla =>
        {
            tabla.HasCheckConstraint("CK_ConceptoVersion_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
            tabla.HasCheckConstraint("CK_ConceptoVersion_Clasificacion",
                "`Clasificacion` IN ('IngresoSalarial','IngresoNoSalarial','DeduccionLegal','DeduccionInterna','CargaPatronal')");
            tabla.HasCheckConstraint("CK_ConceptoVersion_Banderas",
                "`EsSalarial` IN (0,1) AND `EsContributivoCCSS` IN (0,1) AND `EsTributario` IN (0,1) AND `EsPatronal` IN (0,1) AND `PermiteMontoManual` IN (0,1) AND `RequiereAutorizacion` IN (0,1)");
        });
        entidad.HasKey(item => item.IdConceptoPlanillaVersion);
        entidad.Property(item => item.IdConceptoPlanillaVersion).ValueGeneratedOnAdd();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.Clasificacion).HasMaxLength(40).IsRequired();
        entidad.Property(item => item.EsSalarial).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EsContributivoCCSS).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EsTributario).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EsPatronal).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.TipoBase).HasMaxLength(50).IsRequired();
        entidad.Property(item => item.UnidadPermitida).HasMaxLength(30).IsRequired();
        entidad.Property(item => item.PermiteMontoManual).HasColumnType("tinyint(1)").HasDefaultValue(false).IsRequired();
        entidad.Property(item => item.RequiereAutorizacion).HasColumnType("tinyint(1)").HasDefaultValue(false).IsRequired();
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => new { item.IdConceptoPlanilla, item.FechaVigenciaInicio })
            .IsUnique().HasDatabaseName("UX_ConceptoVersion_Concepto_Inicio");
        entidad.HasIndex(item => new { item.IdConceptoPlanilla, item.FechaVigenciaInicio, item.FechaVigenciaFin })
            .HasDatabaseName("IX_ConceptoVersion_Vigencia");
        entidad.HasOne(item => item.ConceptoPlanilla).WithMany(item => item.Versiones)
            .HasForeignKey(item => item.IdConceptoPlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConceptoVersion_Concepto");
    }
}

public sealed class VersionNormativaConfiguracion : IEntityTypeConfiguration<VersionNormativa>
{
    public void Configure(EntityTypeBuilder<VersionNormativa> entidad)
    {
        entidad.ToTable("VersionNormativa", tabla =>
            tabla.HasCheckConstraint("CK_VersionNormativa_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`"));
        entidad.HasKey(item => item.IdVersionNormativa);
        entidad.Property(item => item.IdVersionNormativa).ValueGeneratedOnAdd();
        entidad.Property(item => item.Codigo).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.Institucion).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.Nombre).HasMaxLength(200).IsRequired();
        entidad.Property(item => item.FuenteOficialUrl).HasMaxLength(1000).IsRequired();
        entidad.Property(item => item.IdentificadorPublicacion).HasMaxLength(150);
        entidad.Property(item => item.FechaPublicacion).HasColumnType("date");
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.HuellaContenido).HasColumnType("char(64)");
        entidad.Property(item => item.FechaCreacion).HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").IsRequired();
        entidad.Property(item => item.CreadoPor).HasMaxLength(255);
        entidad.Property(item => item.FechaActivacion).HasColumnType("datetime(6)");
        entidad.Property(item => item.ActivadoPor).HasMaxLength(255);
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => item.Codigo).IsUnique().HasDatabaseName("UX_VersionNormativa_Codigo");
        entidad.HasIndex(item => new { item.Institucion, item.FechaVigenciaInicio, item.FechaVigenciaFin })
            .HasDatabaseName("IX_VersionNormativa_Vigencia");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.CreadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_VersionNormativa_CreadoPor");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.ActivadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_VersionNormativa_ActivadoPor");
    }
}

public sealed class ParametroNormativoConfiguracion : IEntityTypeConfiguration<ParametroNormativo>
{
    public void Configure(EntityTypeBuilder<ParametroNormativo> entidad)
    {
        entidad.ToTable("ParametroNormativo", tabla =>
        {
            tabla.HasCheckConstraint("CK_ParametroNormativo_Sujeto",
                "`Sujeto` IN ('Trabajador','Patrono','Estado','Empresa')");
            tabla.HasCheckConstraint("CK_ParametroNormativo_Obligatorio", "`EsObligatorio` IN (0,1)");
        });
        entidad.HasKey(item => item.IdParametroNormativo);
        entidad.Property(item => item.IdParametroNormativo).ValueGeneratedOnAdd();
        entidad.Property(item => item.Codigo).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.Nombre).HasMaxLength(200).IsRequired();
        entidad.Property(item => item.Institucion).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.Sujeto).HasMaxLength(30).IsRequired();
        entidad.Property(item => item.Unidad).HasMaxLength(30).IsRequired();
        entidad.Property(item => item.CodigoBase).HasMaxLength(80).IsRequired();
        entidad.Property(item => item.EsObligatorio).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => item.Codigo).IsUnique().HasDatabaseName("UX_ParametroNormativo_Codigo");
    }
}

public sealed class ValorParametroNormativoConfiguracion
    : IEntityTypeConfiguration<ValorParametroNormativo>
{
    public void Configure(EntityTypeBuilder<ValorParametroNormativo> entidad)
    {
        entidad.ToTable("ValorParametroNormativo", tabla =>
            tabla.HasCheckConstraint("CK_ValorNormativo_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`"));
        entidad.HasKey(item => item.IdValorParametroNormativo);
        entidad.Property(item => item.IdValorParametroNormativo).ValueGeneratedOnAdd();
        entidad.Property(item => item.ClaveAplicacion).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.ValorDecimal).HasPrecision(28, 10).IsRequired();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.Observaciones).HasMaxLength(500);
        entidad.HasIndex(item => new { item.IdParametroNormativo, item.ClaveAplicacion, item.FechaVigenciaInicio })
            .IsUnique().HasDatabaseName("UX_ValorNormativo_Parametro_Clave_Inicio");
        entidad.HasIndex(item => new { item.IdParametroNormativo, item.FechaVigenciaInicio, item.FechaVigenciaFin })
            .HasDatabaseName("IX_ValorNormativo_Vigencia");
        entidad.HasIndex(item => item.IdVersionNormativa).HasDatabaseName("IX_ValorNormativo_Version");
        entidad.HasOne(item => item.ParametroNormativo).WithMany()
            .HasForeignKey(item => item.IdParametroNormativo).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ValorNormativo_Parametro");
        entidad.HasOne(item => item.VersionNormativa).WithMany()
            .HasForeignKey(item => item.IdVersionNormativa).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ValorNormativo_Version");
    }
}

public sealed class TramoImpuestoSalarioConfiguracion
    : IEntityTypeConfiguration<TramoImpuestoSalario>
{
    public void Configure(EntityTypeBuilder<TramoImpuestoSalario> entidad)
    {
        entidad.ToTable("TramoImpuestoSalario", tabla =>
        {
            tabla.HasCheckConstraint("CK_TramoImpuesto_Orden", "`NumeroOrden` > 0");
            tabla.HasCheckConstraint("CK_TramoImpuesto_Limites",
                "`LimiteInferior` >= 0 AND (`LimiteSuperior` IS NULL OR `LimiteSuperior` > `LimiteInferior`)");
            tabla.HasCheckConstraint("CK_TramoImpuesto_Tasa", "`Tasa` >= 0 AND `Tasa` <= 1");
        });
        entidad.HasKey(item => item.IdTramoImpuestoSalario);
        entidad.Property(item => item.IdTramoImpuestoSalario).ValueGeneratedOnAdd();
        entidad.Property(item => item.LimiteInferior).HasPrecision(18, 2).IsRequired();
        entidad.Property(item => item.LimiteSuperior).HasPrecision(18, 2);
        entidad.Property(item => item.Tasa).HasPrecision(18, 10).IsRequired();
        entidad.HasIndex(item => new { item.IdVersionNormativa, item.NumeroOrden })
            .IsUnique().HasDatabaseName("UX_TramoImpuesto_Version_Orden");
        entidad.HasIndex(item => new { item.IdVersionNormativa, item.LimiteInferior })
            .IsUnique().HasDatabaseName("UX_TramoImpuesto_Version_Inferior");
        entidad.HasOne(item => item.VersionNormativa).WithMany()
            .HasForeignKey(item => item.IdVersionNormativa).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TramoImpuesto_Version");
    }
}

public sealed class CreditoFiscalNormativoConfiguracion
    : IEntityTypeConfiguration<CreditoFiscalNormativo>
{
    public void Configure(EntityTypeBuilder<CreditoFiscalNormativo> entidad)
    {
        entidad.ToTable("CreditoFiscalNormativo", tabla =>
        {
            tabla.HasCheckConstraint("CK_CreditoNormativo_Monto", "`MontoUnitario` >= 0");
            tabla.HasCheckConstraint("CK_CreditoNormativo_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
        });
        entidad.HasKey(item => item.IdCreditoFiscalNormativo);
        entidad.Property(item => item.IdCreditoFiscalNormativo).ValueGeneratedOnAdd();
        entidad.Property(item => item.CodigoTipo).HasMaxLength(80).IsRequired();
        entidad.Property(item => item.Nombre).HasMaxLength(200).IsRequired();
        entidad.Property(item => item.MontoUnitario).HasPrecision(18, 2).IsRequired();
        entidad.Property(item => item.Requisitos).HasMaxLength(1000).IsRequired();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => new { item.IdVersionNormativa, item.CodigoTipo })
            .IsUnique().HasDatabaseName("UX_CreditoNormativo_Version_Tipo");
        entidad.HasOne(item => item.VersionNormativa).WithMany()
            .HasForeignKey(item => item.IdVersionNormativa).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CreditoNormativo_Version");
    }
}

public sealed class CreditoFiscalColaboradorConfiguracion
    : IEntityTypeConfiguration<CreditoFiscalColaborador>
{
    public void Configure(EntityTypeBuilder<CreditoFiscalColaborador> entidad)
    {
        entidad.ToTable("CreditoFiscalColaborador", tabla =>
        {
            tabla.HasCheckConstraint("CK_CreditoColaborador_Cantidad", "`CantidadAcreditada` > 0");
            tabla.HasCheckConstraint("CK_CreditoColaborador_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
        });
        entidad.HasKey(item => item.IdCreditoFiscalColaborador);
        entidad.Property(item => item.IdCreditoFiscalColaborador).ValueGeneratedOnAdd();
        entidad.Property(item => item.CantidadAcreditada).HasPrecision(8, 2).IsRequired();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.ReferenciaSoporte).HasMaxLength(300).IsRequired();
        entidad.Property(item => item.AprobadoPor).HasMaxLength(255).IsRequired();
        entidad.Property(item => item.FechaAprobacion).HasColumnType("datetime(6)").IsRequired();
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => new { item.IdColaborador, item.IdCreditoFiscalNormativo, item.FechaVigenciaInicio })
            .IsUnique().HasDatabaseName("UX_CreditoColab_Concepto_Inicio");
        entidad.HasIndex(item => new { item.IdColaborador, item.FechaVigenciaInicio, item.FechaVigenciaFin })
            .HasDatabaseName("IX_CreditoColaborador_Vigencia");
        entidad.HasOne(item => item.Colaborador).WithMany()
            .HasForeignKey(item => item.IdColaborador).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CreditoColaborador_Colaborador");
        entidad.HasOne(item => item.CreditoFiscalNormativo).WithMany()
            .HasForeignKey(item => item.IdCreditoFiscalNormativo).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CreditoColaborador_Normativo");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.AprobadoPor)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CreditoColaborador_AprobadoPor");
    }
}

public sealed class ConfiguracionRiesgoTrabajoConfiguracion
    : IEntityTypeConfiguration<ConfiguracionRiesgoTrabajo>
{
    public void Configure(EntityTypeBuilder<ConfiguracionRiesgoTrabajo> entidad)
    {
        entidad.ToTable("ConfiguracionRiesgoTrabajo", tabla =>
        {
            tabla.HasCheckConstraint("CK_RiesgoTrabajo_Fechas",
                "`FechaVigenciaFin` IS NULL OR `FechaVigenciaFin` >= `FechaVigenciaInicio`");
            tabla.HasCheckConstraint("CK_RiesgoTrabajo_Tasas",
                "`Tarifa` >= 0 AND `Descuento` >= 0 AND `Descuento` <= 1 AND `Recargo` >= 0 AND `Recargo` <= 1 AND (1 - `Descuento` + `Recargo`) >= 0");
        });
        entidad.HasKey(item => item.IdConfiguracionRiesgoTrabajo);
        entidad.Property(item => item.IdConfiguracionRiesgoTrabajo).ValueGeneratedOnAdd();
        entidad.Property(item => item.NumeroPoliza).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.CodigoActividad).HasMaxLength(100).IsRequired();
        entidad.Property(item => item.DescripcionActividad).HasMaxLength(300);
        entidad.Property(item => item.Tarifa).HasPrecision(18, 10).IsRequired();
        entidad.Property(item => item.Descuento).HasPrecision(18, 10).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.Recargo).HasPrecision(18, 10).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.FechaVigenciaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaVigenciaFin).HasColumnType("date");
        entidad.Property(item => item.ReferenciaDocumento).HasMaxLength(500).IsRequired();
        entidad.Property(item => item.FechaCreacion).HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").IsRequired();
        entidad.Property(item => item.CreadoPor).HasMaxLength(255);
        entidad.Property(item => item.FechaModificacion).HasColumnType("datetime(6)");
        entidad.Property(item => item.ModificadoPor).HasMaxLength(255);
        entidad.Property(item => item.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')").HasDefaultValue("Activo").IsRequired();
        entidad.HasIndex(item => new { item.NumeroPoliza, item.CodigoActividad, item.FechaVigenciaInicio })
            .IsUnique().HasDatabaseName("UX_RiesgoTrabajo_Poliza_Actividad_Inicio");
        entidad.HasIndex(item => new { item.FechaVigenciaInicio, item.FechaVigenciaFin })
            .HasDatabaseName("IX_RiesgoTrabajo_Vigencia");
        entidad.HasOne(item => item.VersionNormativa).WithMany()
            .HasForeignKey(item => item.IdVersionNormativa).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RiesgoTrabajo_VersionNormativa");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.CreadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_RiesgoTrabajo_CreadoPor");
        entidad.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.ModificadoPor)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_RiesgoTrabajo_ModificadoPor");
    }
}

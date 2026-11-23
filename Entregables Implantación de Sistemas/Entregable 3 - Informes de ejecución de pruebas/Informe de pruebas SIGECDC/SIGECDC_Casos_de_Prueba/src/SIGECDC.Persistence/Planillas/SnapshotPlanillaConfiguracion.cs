using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

public sealed class EjecucionPlanillaConfiguracion : IEntityTypeConfiguration<EjecucionPlanilla>
{
    public void Configure(EntityTypeBuilder<EjecucionPlanilla> entidad)
    {
        entidad.ToTable("EjecucionPlanilla", tabla =>
        {
            tabla.HasCheckConstraint("CK_EjecucionPlanilla_Version", "`NumeroVersion` > 0");
            tabla.HasCheckConstraint("CK_EjecucionPlanilla_Estado",
                "`EstadoEjecucion` IN ('Calculada','InvalidadaPorReapertura','Autorizada','LegadoNoReproducible')");
            tabla.HasCheckConstraint("CK_EjecucionPlanilla_Reproducibilidad",
                "`EstadoReproducibilidad` IN ('REPRODUCIBLE','LEGADO_NO_REPRODUCIBLE')");
            tabla.HasCheckConstraint("CK_EjecucionPlanilla_Snapshot",
                "`SnapshotCompleto` IN (0,1) AND ((`EstadoReproducibilidad` = 'REPRODUCIBLE' AND `SnapshotCompleto` = 1) OR (`EstadoReproducibilidad` = 'LEGADO_NO_REPRODUCIBLE' AND `SnapshotCompleto` = 0))");
            tabla.HasCheckConstraint("CK_EjecucionPlanilla_Fechas",
                "`FechaInicioSnapshot` <= `FechaFinSnapshot`");
            tabla.HasCheckConstraint("CK_EjecucionPlanilla_Invalidacion",
                "(`EstadoEjecucion` <> 'InvalidadaPorReapertura') OR (`MotivoInvalidacion` IS NOT NULL AND `FechaInvalidacion` IS NOT NULL AND `InvalidadoPor` IS NOT NULL)");
        });
        entidad.HasKey(item => item.IdEjecucionPlanilla);
        entidad.Property(item => item.IdEjecucionPlanilla).ValueGeneratedOnAdd();
        entidad.Property(item => item.EstadoEjecucion).HasMaxLength(40).IsRequired();
        entidad.Property(item => item.EstadoReproducibilidad).HasMaxLength(40).IsRequired();
        entidad.Property(item => item.SnapshotCompleto).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.Moneda).HasColumnType("char(3)").HasDefaultValue("CRC").IsRequired();
        entidad.Property(item => item.VersionMotor).HasMaxLength(100);
        entidad.Property(item => item.FechaInicioSnapshot).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaFinSnapshot).HasColumnType("date").IsRequired();
        entidad.Property(item => item.TipoPeriodoSnapshot).HasMaxLength(20).IsRequired();
        entidad.Property(item => item.FechaCalculo).HasColumnType("datetime(6)").IsRequired();
        entidad.Property(item => item.CalculadoPor).HasMaxLength(255);
        entidad.Property(item => item.SalarioBrutoTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.IngresosNoSalarialesTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.DeduccionesTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.SalarioNetoTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.CargasPatronalesTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.RiesgosTrabajoTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.OtrosCostosPatronalesTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.CostoLaboralTotal).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entidad.Property(item => item.MotivoInvalidacion).HasMaxLength(500);
        entidad.Property(item => item.FechaInvalidacion).HasColumnType("datetime(6)");
        entidad.Property(item => item.InvalidadoPor).HasMaxLength(255);
        entidad.HasIndex(item => new { item.IdPlanilla, item.NumeroVersion })
            .IsUnique().HasDatabaseName("UX_EjecucionPlanilla_Planilla_Version");
        entidad.HasIndex(item => new { item.EstadoEjecucion, item.FechaCalculo })
            .HasDatabaseName("IX_EjecucionPlanilla_Estado_Fecha");
        entidad.HasOne(item => item.Planilla).WithMany(item => item.Ejecuciones)
            .HasForeignKey(item => item.IdPlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EjecucionPlanilla_Planilla");
    }
}

public sealed class SegmentoCalculoPlanillaConfiguracion
    : IEntityTypeConfiguration<SegmentoCalculoPlanilla>
{
    public void Configure(EntityTypeBuilder<SegmentoCalculoPlanilla> entidad)
    {
        entidad.ToTable("SegmentoCalculoPlanilla", tabla =>
        {
            tabla.HasCheckConstraint("CK_SegmentoPlanilla_Orden", "`NumeroOrden` > 0");
            tabla.HasCheckConstraint("CK_SegmentoPlanilla_Fechas", "`FechaInicio` <= `FechaFin`");
            tabla.HasCheckConstraint("CK_SegmentoPlanilla_Valores",
                "`SalarioMensual` >= 0 AND `HorasOrdinariasDia` > 0 AND `DiasBase30` >= 0 AND `MontoOrdinario` >= 0");
        });
        entidad.HasKey(item => item.IdSegmentoCalculoPlanilla);
        entidad.Property(item => item.IdSegmentoCalculoPlanilla).ValueGeneratedOnAdd();
        entidad.Property(item => item.FechaInicio).HasColumnType("date").IsRequired();
        entidad.Property(item => item.FechaFin).HasColumnType("date").IsRequired();
        entidad.Property(item => item.SalarioMensual).HasPrecision(18, 2).IsRequired();
        entidad.Property(item => item.CodigoJornada).HasMaxLength(50).IsRequired();
        entidad.Property(item => item.HorasOrdinariasDia).HasPrecision(8, 4).IsRequired();
        entidad.Property(item => item.DiasBase30).HasPrecision(8, 4).IsRequired();
        entidad.Property(item => item.MontoOrdinarioSinRedondear).HasPrecision(28, 10).IsRequired();
        entidad.Property(item => item.MontoOrdinario).HasPrecision(18, 2).IsRequired();
        entidad.HasIndex(item => new { item.IdDetallePlanilla, item.NumeroOrden })
            .IsUnique().HasDatabaseName("UX_SegmentoPlanilla_Detalle_Orden");
        entidad.HasIndex(item => new { item.IdDetallePlanilla, item.FechaInicio, item.FechaFin })
            .HasDatabaseName("IX_SegmentoPlanilla_Fechas");
        entidad.HasIndex(item => item.IdContratoOrigen).HasDatabaseName("IX_SegmentoPlanilla_Contrato");
        entidad.HasOne(item => item.DetallePlanilla).WithMany(item => item.Segmentos)
            .HasForeignKey(item => item.IdDetallePlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SegmentoPlanilla_Detalle");
        entidad.HasOne<Contrato>().WithMany().HasForeignKey(item => item.IdContratoOrigen)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SegmentoPlanilla_Contrato");
        SnapshotEvidenciaConfiguracion.Configurar(entidad);
    }
}

public sealed class ConceptoDetallePlanillaConfiguracion
    : IEntityTypeConfiguration<ConceptoDetallePlanilla>
{
    public void Configure(EntityTypeBuilder<ConceptoDetallePlanilla> entidad)
    {
        entidad.ToTable("ConceptoDetallePlanilla", tabla =>
        {
            tabla.HasCheckConstraint("CK_ConceptoDetalle_Orden", "`NumeroOrden` > 0");
            tabla.HasCheckConstraint("CK_ConceptoDetalle_Monto", "`Monto` >= 0");
            tabla.HasCheckConstraint("CK_ConceptoDetalle_Banderas",
                "`EsSalarialSnapshot` IN (0,1) AND `EsContributivoCCSSSnapshot` IN (0,1) AND `EsTributarioSnapshot` IN (0,1) AND `EsPatronalSnapshot` IN (0,1)");
        });
        entidad.HasKey(item => item.IdConceptoDetallePlanilla);
        entidad.Property(item => item.IdConceptoDetallePlanilla).ValueGeneratedOnAdd();
        entidad.Property(item => item.TipoFuente).HasMaxLength(30).IsRequired();
        entidad.Property(item => item.FechaHecho).HasColumnType("date");
        entidad.Property(item => item.Cantidad).HasPrecision(28, 10);
        entidad.Property(item => item.Unidad).HasMaxLength(30);
        entidad.Property(item => item.BaseCalculo).HasPrecision(28, 10);
        entidad.Property(item => item.Tasa).HasPrecision(28, 10);
        entidad.Property(item => item.Multiplicador).HasPrecision(28, 10);
        entidad.Property(item => item.MontoSinRedondear).HasPrecision(28, 10).IsRequired();
        entidad.Property(item => item.Monto).HasPrecision(18, 2).IsRequired();
        entidad.Property(item => item.EsSalarialSnapshot).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EsContributivoCCSSSnapshot).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EsTributarioSnapshot).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.EsPatronalSnapshot).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.DescripcionSnapshot).HasMaxLength(300).IsRequired();
        entidad.HasIndex(item => new { item.IdDetallePlanilla, item.NumeroOrden })
            .IsUnique().HasDatabaseName("UX_ConceptoDetalle_Detalle_Orden");
        entidad.HasIndex(item => item.IdConceptoPlanillaVersion)
            .HasDatabaseName("IX_ConceptoDetalle_ConceptoVersion");
        entidad.HasIndex(item => new { item.TipoFuente, item.IdFuente })
            .HasDatabaseName("IX_ConceptoDetalle_Fuente");
        entidad.HasIndex(item => item.IdSegmentoCalculoPlanilla)
            .HasDatabaseName("IX_ConceptoDetalle_Segmento");
        entidad.HasOne(item => item.DetallePlanilla).WithMany(item => item.Conceptos)
            .HasForeignKey(item => item.IdDetallePlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConceptoDetalle_Detalle");
        entidad.HasOne(item => item.SegmentoCalculoPlanilla).WithMany()
            .HasForeignKey(item => item.IdSegmentoCalculoPlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConceptoDetalle_Segmento");
        entidad.HasOne(item => item.ConceptoPlanillaVersion).WithMany()
            .HasForeignKey(item => item.IdConceptoPlanillaVersion).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConceptoDetalle_ConceptoVersion");
        SnapshotEvidenciaConfiguracion.Configurar(entidad);
    }
}

public sealed class AplicacionNormativaDetalleConfiguracion
    : IEntityTypeConfiguration<AplicacionNormativaDetallePlanilla>
{
    public void Configure(EntityTypeBuilder<AplicacionNormativaDetallePlanilla> entidad)
    {
        entidad.ToTable("AplicacionNormativaDetallePlanilla", tabla =>
            tabla.HasCheckConstraint("CK_AplicacionNormativa_Referencia",
                "((`IdValorParametroNormativo` IS NOT NULL) + (`IdTramoImpuestoSalario` IS NOT NULL) + (`IdCreditoFiscalNormativo` IS NOT NULL) + (`IdConfiguracionRiesgoTrabajo` IS NOT NULL)) = 1"));
        entidad.HasKey(item => item.IdAplicacionNormativaDetalle);
        entidad.Property(item => item.IdAplicacionNormativaDetalle).ValueGeneratedOnAdd();
        entidad.Property(item => item.BaseAplicada).HasPrecision(28, 10);
        entidad.Property(item => item.ValorAplicado).HasPrecision(28, 10);
        entidad.Property(item => item.MontoAplicado).HasPrecision(18, 2).IsRequired();
        entidad.HasIndex(item => item.IdConceptoDetallePlanilla)
            .HasDatabaseName("IX_AplicacionNormativa_ConceptoDetalle");
        entidad.HasIndex(item => item.IdValorParametroNormativo)
            .HasDatabaseName("IX_AplicacionNormativa_Valor");
        entidad.HasIndex(item => item.IdTramoImpuestoSalario)
            .HasDatabaseName("IX_AplicacionNormativa_Tramo");
        entidad.HasIndex(item => item.IdCreditoFiscalNormativo)
            .HasDatabaseName("IX_AplicacionNormativa_Credito");
        entidad.HasIndex(item => item.IdConfiguracionRiesgoTrabajo)
            .HasDatabaseName("IX_AplicacionNormativa_Riesgo");
        entidad.HasOne(item => item.ConceptoDetallePlanilla).WithMany(item => item.AplicacionesNormativas)
            .HasForeignKey(item => item.IdConceptoDetallePlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AplicacionNormativa_ConceptoDetalle");
        entidad.HasOne(item => item.ValorParametroNormativo).WithMany()
            .HasForeignKey(item => item.IdValorParametroNormativo).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AplicacionNormativa_Valor");
        entidad.HasOne(item => item.TramoImpuestoSalario).WithMany()
            .HasForeignKey(item => item.IdTramoImpuestoSalario).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AplicacionNormativa_Tramo");
        entidad.HasOne(item => item.CreditoFiscalNormativo).WithMany()
            .HasForeignKey(item => item.IdCreditoFiscalNormativo).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AplicacionNormativa_Credito");
        entidad.HasOne(item => item.ConfiguracionRiesgoTrabajo).WithMany()
            .HasForeignKey(item => item.IdConfiguracionRiesgoTrabajo).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AplicacionNormativa_Riesgo");
        SnapshotEvidenciaConfiguracion.Configurar(entidad);
    }
}

public sealed class DetallePlanillaDistribucionProyectoConfiguracion
    : IEntityTypeConfiguration<DetallePlanillaDistribucionProyecto>
{
    public void Configure(EntityTypeBuilder<DetallePlanillaDistribucionProyecto> entidad)
    {
        entidad.ToTable("DetallePlanillaDistribucionProyecto", tabla =>
        {
            tabla.HasCheckConstraint("CK_DetalleDistribucion_Destino",
                "(`EsSinAsignar` = 1 AND `IdProyecto` IS NULL) OR (`EsSinAsignar` = 0 AND `IdProyecto` IS NOT NULL)");
            tabla.HasCheckConstraint("CK_DetalleDistribucion_Porcentaje",
                "`Porcentaje` > 0 AND `Porcentaje` <= 100");
            tabla.HasCheckConstraint("CK_DetalleDistribucion_Valores",
                "`MontoReal` >= 0 AND `NumeroOrden` > 0");
        });
        entidad.HasKey(item => item.IdDetallePlanillaDistribucionProyecto);
        entidad.Property(item => item.CodigoProyectoSnapshot).HasMaxLength(50);
        entidad.Property(item => item.NombreProyectoSnapshot).HasMaxLength(200);
        entidad.Property(item => item.IdDetallePlanillaDistribucionProyecto).ValueGeneratedOnAdd();
        entidad.Property(item => item.EsSinAsignar).HasColumnType("tinyint(1)").IsRequired();
        entidad.Property(item => item.IdProyectoNormalizado)
            .HasComputedColumnSql("COALESCE(`IdProyecto`, 0)", stored: true);
        entidad.Property(item => item.Porcentaje).HasPrecision(7, 4).IsRequired();
        entidad.Property(item => item.MontoReal).HasPrecision(18, 2).IsRequired();
        entidad.Property(item => item.MontoSinRedondear).HasPrecision(28, 10).IsRequired();
        entidad.HasIndex(item => new { item.IdDetallePlanilla, item.IdProyectoNormalizado })
            .IsUnique().HasDatabaseName("UX_DetalleDistribucion_Detalle_Proyecto");
        entidad.HasIndex(item => item.IdProyecto).HasDatabaseName("IX_DetalleDistribucion_Proyecto");
        entidad.HasOne(item => item.DetallePlanilla).WithMany(item => item.DistribucionesProyecto)
            .HasForeignKey(item => item.IdDetallePlanilla).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DetalleDistribucion_Detalle");
        entidad.HasOne<Proyecto>().WithMany().HasForeignKey(item => item.IdProyecto)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DetalleDistribucion_Proyecto");
    }
}

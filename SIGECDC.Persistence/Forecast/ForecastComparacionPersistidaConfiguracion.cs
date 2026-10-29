using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.Operaciones;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastComparacionRealConfiguracion : IEntityTypeConfiguration<ForecastComparacionReal>
{
    public void Configure(EntityTypeBuilder<ForecastComparacionReal> e)
    {
        e.ToTable("ForecastComparacionReal", t =>
        {
            t.HasCheckConstraint("CK_FM_Cabecera", "CHAR_LENGTH(TRIM(EvaluadoPor)) > 0 AND ((Disponibilidad='SinRealComparable' AND MotivoNoComparable='SinRealComparable' AND MotivoNoComparable IS NOT NULL AND IdEjecucionPlanillaReal IS NULL AND EstadoPlanillaObservado IS NULL AND EstadoPeriodoObservado IS NULL AND EstadoEjecucionObservado IS NULL AND SnapshotCompletoObservado IS NULL) OR (Disponibilidad='Disponible' AND MotivoNoComparable IS NULL AND IdEjecucionPlanillaReal IS NOT NULL AND EstadoPlanillaObservado IS NOT NULL AND EstadoPlanillaObservado IN ('Aprobada','Cerrada') AND EstadoPeriodoObservado IS NOT NULL AND EstadoPeriodoObservado=EstadoPlanillaObservado AND EstadoEjecucionObservado IS NOT NULL AND EstadoEjecucionObservado='Autorizada' AND SnapshotCompletoObservado IS NOT NULL AND SnapshotCompletoObservado=1))");
        });
        e.HasKey(x => x.IdForecastComparacionReal);
        e.Property(x => x.IdForecastComparacionReal).ValueGeneratedOnAdd();
        e.Property(x => x.IdForecastPeriodo).HasColumnType("bigint").IsRequired();
        e.Property(x => x.IdEjecucionPlanillaReal).HasColumnType("bigint");
        e.Property(x => x.FechaEvaluacion).HasColumnType("datetime").IsRequired();
        e.Property(x => x.EvaluadoPor).HasMaxLength(255).IsRequired();
        e.Property(x => x.Disponibilidad).HasMaxLength(30).IsRequired();
        e.Property(x => x.MotivoNoComparable).HasMaxLength(100);
        e.Property(x => x.EstadoPlanillaObservado).HasMaxLength(24);
        e.Property(x => x.EstadoPeriodoObservado).HasMaxLength(24);
        e.Property(x => x.EstadoEjecucionObservado).HasMaxLength(30);
        e.Property(x => x.SnapshotCompletoObservado).HasColumnType("tinyint(1)");
        e.HasIndex(x => x.IdForecastPeriodo).IsUnique().HasDatabaseName("UX_FM_Comparacion_Periodo");
        e.HasIndex(x => x.IdEjecucionPlanillaReal).HasDatabaseName("IX_FM_0_0");
        e.HasOne<ForecastPeriodo>().WithMany().HasForeignKey(x => x.IdForecastPeriodo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_0_0");
        e.HasOne<EjecucionPlanilla>().WithMany().HasForeignKey(x => x.IdEjecucionPlanillaReal).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_0_1");
    }
}

public sealed class ForecastComparacionResultadoConfiguracion : IEntityTypeConfiguration<ForecastComparacionResultado>
{
    public void Configure(EntityTypeBuilder<ForecastComparacionResultado> e)
    {
        e.ToTable("ForecastComparacionResultado", t =>
        {
            t.HasCheckConstraint("CK_FM_Resultado_Dimension", "NumeroOrden > 0 AND EsSinAsignar IN (0,1) AND ((Granularidad='Total' AND IdForecastParticipante IS NULL AND IdColaborador IS NULL AND IdConceptoPlanilla IS NULL AND IdProyecto IS NULL AND EsSinAsignar=0) OR (Granularidad='Participante' AND (IdForecastParticipante IS NOT NULL OR IdColaborador IS NOT NULL) AND IdConceptoPlanilla IS NULL AND IdProyecto IS NULL AND EsSinAsignar=0) OR (Granularidad='Concepto' AND IdConceptoPlanilla IS NOT NULL AND IdForecastParticipante IS NULL AND IdColaborador IS NULL AND IdProyecto IS NULL AND EsSinAsignar=0) OR (Granularidad='Proyecto' AND IdForecastParticipante IS NULL AND IdColaborador IS NULL AND IdConceptoPlanilla IS NULL AND ((EsSinAsignar=1 AND IdProyecto IS NULL) OR (EsSinAsignar=0 AND IdProyecto IS NOT NULL))))");
            t.HasCheckConstraint("CK_FM_Resultado_Presencia", "TieneForecast IN (0,1) AND TieneReal IN (0,1) AND (TieneForecast=1 OR TieneReal=1) AND ((TieneForecast=1 AND MontoForecast IS NOT NULL) OR (TieneForecast=0 AND MontoForecast IS NULL)) AND ((TieneReal=1 AND MontoReal IS NOT NULL) OR (TieneReal=0 AND MontoReal IS NULL))");
            t.HasCheckConstraint("CK_FM_Resultado_Diferencias", "((TieneForecast=0 OR TieneReal=0) AND MotivoNoComparable IS NOT NULL AND MotivoNoComparable='SinCorrespondencia' AND DiferenciaFirmada IS NULL AND DiferenciaAbsoluta IS NULL AND DiferenciaPorcentual IS NULL) OR (TieneForecast=1 AND TieneReal=1 AND DiferenciaFirmada IS NOT NULL AND DiferenciaFirmada=MontoReal-MontoForecast AND DiferenciaAbsoluta IS NOT NULL AND DiferenciaAbsoluta=ABS(DiferenciaFirmada) AND ((MontoForecast=0 AND MontoReal<>0 AND DiferenciaPorcentual IS NULL AND MotivoNoComparable IS NOT NULL AND MotivoNoComparable='BaseForecastCero') OR (MotivoNoComparable IS NULL AND DiferenciaPorcentual IS NOT NULL AND ((MontoForecast=0 AND MontoReal=0 AND DiferenciaPorcentual=0) OR (MontoForecast<>0 AND DiferenciaPorcentual=ROUND(CAST(DiferenciaFirmada AS DECIMAL(65,28))/ABS(MontoForecast)*100,4))))))");
        });
        e.HasKey(x => x.IdForecastComparacionResultado);
        e.Property(x => x.IdForecastComparacionResultado).ValueGeneratedOnAdd();
        e.Property(x => x.IdForecastComparacionReal).HasColumnType("bigint").IsRequired();
        e.Property(x => x.Granularidad).HasMaxLength(20).IsRequired();
        e.Property(x => x.IdForecastParticipante).HasColumnType("bigint");
        e.Property(x => x.IdColaborador).HasColumnType("bigint");
        e.Property(x => x.IdConceptoPlanilla).HasColumnType("bigint");
        e.Property(x => x.IdProyecto).HasColumnType("bigint");
        e.Property(x => x.EsSinAsignar).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.TieneForecast).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.TieneReal).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.MontoForecast).HasPrecision(18,2);
        e.Property(x => x.MontoReal).HasPrecision(18,2);
        e.Property(x => x.DiferenciaFirmada).HasPrecision(18,2);
        e.Property(x => x.DiferenciaAbsoluta).HasPrecision(18,2);
        e.Property(x => x.DiferenciaPorcentual).HasPrecision(18,4);
        e.Property(x => x.MotivoNoComparable).HasMaxLength(100);
        e.Property(x => x.NumeroOrden).HasColumnType("int").IsRequired();
        e.HasIndex(x => new { x.IdForecastComparacionReal, x.NumeroOrden }).IsUnique().HasDatabaseName("UX_FM_Resultado_Orden");
        e.HasIndex(x => x.IdForecastParticipante).HasDatabaseName("IX_FM_1_0");
        e.HasIndex(x => x.IdColaborador).HasDatabaseName("IX_FM_1_1");
        e.HasIndex(x => x.IdConceptoPlanilla).HasDatabaseName("IX_FM_1_2");
        e.HasIndex(x => x.IdProyecto).HasDatabaseName("IX_FM_1_3");
        e.HasOne<ForecastComparacionReal>().WithMany().HasForeignKey(x => x.IdForecastComparacionReal).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_1_0");
        e.HasOne<ForecastParticipante>().WithMany().HasForeignKey(x => x.IdForecastParticipante).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_1_1");
        e.HasOne<Colaborador>().WithMany().HasForeignKey(x => x.IdColaborador).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_1_2");
        e.HasOne<ConceptoPlanilla>().WithMany().HasForeignKey(x => x.IdConceptoPlanilla).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_1_3");
        e.HasOne<Proyecto>().WithMany().HasForeignKey(x => x.IdProyecto).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_1_4");
    }
}

public sealed class ForecastComparacionResultadoFuenteConfiguracion : IEntityTypeConfiguration<ForecastComparacionResultadoFuente>
{
    public void Configure(EntityTypeBuilder<ForecastComparacionResultadoFuente> e)
    {
        e.ToTable("ForecastComparacionResultadoFuente", t =>
        {
            t.HasCheckConstraint("CK_FM_Fuente_Lado", "((IdForecastDetalle IS NOT NULL)+(IdDetallePlanilla IS NOT NULL)+(IdConceptoDetallePlanilla IS NOT NULL)+(IdDetallePlanillaDistribucionProyecto IS NOT NULL))=1 AND ((Lado='Forecast' AND IdForecastDetalle IS NOT NULL) OR (Lado='Real' AND IdForecastDetalle IS NULL))");
        });
        e.HasKey(x => x.IdForecastComparacionResultadoFuente);
        e.Property(x => x.IdForecastComparacionResultadoFuente).ValueGeneratedOnAdd();
        e.Property(x => x.IdForecastComparacionResultado).HasColumnType("bigint").IsRequired();
        e.Property(x => x.Lado).HasMaxLength(10).IsRequired();
        e.Property(x => x.IdForecastDetalle).HasColumnType("bigint");
        e.Property(x => x.IdDetallePlanilla).HasColumnType("bigint");
        e.Property(x => x.IdConceptoDetallePlanilla).HasColumnType("bigint");
        e.Property(x => x.IdDetallePlanillaDistribucionProyecto).HasColumnType("bigint");
        e.HasIndex(x => new { x.IdForecastComparacionResultado, x.IdForecastDetalle }).IsUnique().HasDatabaseName("UX_FM_Fuente_0");
        e.HasIndex(x => new { x.IdForecastComparacionResultado, x.IdDetallePlanilla }).IsUnique().HasDatabaseName("UX_FM_Fuente_1");
        e.HasIndex(x => new { x.IdForecastComparacionResultado, x.IdConceptoDetallePlanilla }).IsUnique().HasDatabaseName("UX_FM_Fuente_2");
        e.HasIndex(x => new { x.IdForecastComparacionResultado, x.IdDetallePlanillaDistribucionProyecto }).IsUnique().HasDatabaseName("UX_FM_Fuente_3");
        e.HasIndex(x => x.IdForecastDetalle).HasDatabaseName("IX_FM_2_0");
        e.HasIndex(x => x.IdDetallePlanilla).HasDatabaseName("IX_FM_2_1");
        e.HasIndex(x => x.IdConceptoDetallePlanilla).HasDatabaseName("IX_FM_2_2");
        e.HasIndex(x => x.IdDetallePlanillaDistribucionProyecto).HasDatabaseName("IX_FM_2_3");
        e.HasOne<ForecastComparacionResultado>().WithMany().HasForeignKey(x => x.IdForecastComparacionResultado).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_2_0");
        e.HasOne<ForecastDetalle>().WithMany().HasForeignKey(x => x.IdForecastDetalle).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_2_1");
        e.HasOne<DetallePlanilla>().WithMany().HasForeignKey(x => x.IdDetallePlanilla).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_2_2");
        e.HasOne<ConceptoDetallePlanilla>().WithMany().HasForeignKey(x => x.IdConceptoDetallePlanilla).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_2_3");
        e.HasOne<DetallePlanillaDistribucionProyecto>().WithMany().HasForeignKey(x => x.IdDetallePlanillaDistribucionProyecto).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FM_2_4");
    }
}



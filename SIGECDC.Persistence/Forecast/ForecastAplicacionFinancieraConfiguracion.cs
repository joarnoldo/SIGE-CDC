using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastAplicacionFinancieraConfiguracion : IEntityTypeConfiguration<ForecastAplicacionFinanciera>
{
    public void Configure(EntityTypeBuilder<ForecastAplicacionFinanciera> e)
    {
        e.ToTable("ForecastAplicacionFinanciera", t =>
        {
            t.HasCheckConstraint("CK_FL_Aplicacion_Orden", "NumeroOrden > 0 AND FechaFin >= FechaInicio");
            t.HasCheckConstraint("CK_FL_Aplicacion_Fuente", "FuenteEfectiva IN ('Historico','Override','CondicionProyectada','Normativa','Derivado') AND (FuenteEfectiva<>'Historico' OR IdForecastObservacionHistorica IS NOT NULL) AND (FuenteEfectiva<>'Override' OR IdForecastParametro IS NOT NULL) AND (FuenteEfectiva<>'CondicionProyectada' OR IdForecastSegmentoProyectado IS NOT NULL)");
            t.HasCheckConstraint("CK_FL_Aplicacion_Normativa", "((IdValorParametroNormativo IS NOT NULL)+(IdTramoImpuestoSalario IS NOT NULL)+(IdCreditoFiscalNormativo IS NOT NULL)+(IdConfiguracionRiesgoTrabajo IS NOT NULL)) <= 1 AND (FuenteEfectiva<>'Normativa' OR ((IdValorParametroNormativo IS NOT NULL)+(IdTramoImpuestoSalario IS NOT NULL)+(IdCreditoFiscalNormativo IS NOT NULL)+(IdConfiguracionRiesgoTrabajo IS NOT NULL))=1)");
            t.HasCheckConstraint("CK_FL_Aplicacion_Credito", "IdCreditoFiscalColaborador IS NULL OR (IdCreditoFiscalNormativo IS NOT NULL AND Cantidad IS NOT NULL AND Cantidad > 0)");
            t.HasCheckConstraint("CK_FL_Aplicacion_Alcance", "AlcanceAplicacion IN ('CorteActual','MensualDefinitiva','AcumuladoPrevio') AND (AlcanceAplicacion<>'AcumuladoPrevio' OR IdForecastDetallePrevio IS NOT NULL)");
            t.HasCheckConstraint("CK_FL_Aplicacion_Vigencia", "FechaAplicabilidadFin IS NULL OR (FechaAplicabilidadInicio IS NOT NULL AND FechaAplicabilidadFin >= FechaAplicabilidadInicio)");
            t.HasCheckConstraint("CK_FL_Aplicacion_Banderas", "EsSalarial IN (0,1) AND EsContributivoCCSS IN (0,1) AND EsTributario IN (0,1) AND EsPatronal IN (0,1)");
            t.HasCheckConstraint("CK_FL_Aplicacion_RT", "ModalidadRT IS NULL OR (IdConfiguracionRiesgoTrabajo IS NOT NULL AND IdForecastSegmentoProyectado IS NOT NULL AND ModalidadRT='RemuneracionSalarialOrdinaria' AND NumeroPoliza IS NOT NULL AND CodigoActividad IS NOT NULL)");
            t.HasCheckConstraint("CK_FL_Aplicacion_Acreditacion", "ReglaAplicabilidad IS NULL OR (IdValorParametroNormativo IS NOT NULL AND ReglaAplicabilidad IN ('RemuneracionContributiva','MinimoMensualOrdinario','ExcepcionSinMinimo') AND FechaAplicabilidadInicio IS NOT NULL AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND ReferenciaRegla IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaRegla)) > 0 AND AcreditadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AcreditadoPor)) > 0)");
        });
        e.HasKey(x => x.IdForecastAplicacionFinanciera);
        e.Property(x => x.IdForecastAplicacionFinanciera).ValueGeneratedOnAdd();
        e.Property(x => x.IdForecastAplicacionFinanciera).HasColumnType("bigint").IsRequired();
        e.Property(x => x.IdForecastDetalle).HasColumnType("bigint").IsRequired();
        e.Property(x => x.NumeroOrden).HasColumnType("int").IsRequired();
        e.Property(x => x.IdForecastSegmentoProyectado).HasColumnType("bigint");
        e.Property(x => x.IdForecastParametro).HasColumnType("bigint");
        e.Property(x => x.IdForecastObservacionHistorica).HasColumnType("bigint");
        e.Property(x => x.FuenteEfectiva).HasMaxLength(30).IsRequired();
        e.Property(x => x.AlcanceAplicacion).HasMaxLength(30).IsRequired();
        e.Property(x => x.Componente).HasMaxLength(100).IsRequired();
        e.Property(x => x.Sujeto).HasMaxLength(30);
        e.Property(x => x.FechaInicio).HasColumnType("date").IsRequired();
        e.Property(x => x.FechaFin).HasColumnType("date").IsRequired();
        e.Property(x => x.IdForecastDetallePrevio).HasColumnType("bigint");
        e.Property(x => x.IdValorParametroNormativo).HasColumnType("bigint");
        e.Property(x => x.IdTramoImpuestoSalario).HasColumnType("bigint");
        e.Property(x => x.IdCreditoFiscalNormativo).HasColumnType("bigint");
        e.Property(x => x.IdCreditoFiscalColaborador).HasColumnType("bigint");
        e.Property(x => x.IdConfiguracionRiesgoTrabajo).HasColumnType("bigint");
        e.Property(x => x.Unidad).HasMaxLength(30);
        e.Property(x => x.TipoBase).HasMaxLength(100);
        e.Property(x => x.BaseAplicada).HasPrecision(65,28);
        e.Property(x => x.ValorAplicado).HasPrecision(65,28);
        e.Property(x => x.Cantidad).HasPrecision(65,28);
        e.Property(x => x.Multiplicador).HasPrecision(65,28);
        e.Property(x => x.MontoSinRedondear).HasPrecision(65,28).IsRequired();
        e.Property(x => x.MontoMaterializado).HasPrecision(18,2);
        e.Property(x => x.ReglaAplicabilidad).HasMaxLength(40);
        e.Property(x => x.FechaAplicabilidadInicio).HasColumnType("date");
        e.Property(x => x.FechaAplicabilidadFin).HasColumnType("date");
        e.Property(x => x.ReferenciaSoporte).HasMaxLength(500);
        e.Property(x => x.ReferenciaRegla).HasMaxLength(500);
        e.Property(x => x.AcreditadoPor).HasMaxLength(255);
        e.Property(x => x.ModalidadRT).HasMaxLength(40);
        e.Property(x => x.NumeroPoliza).HasMaxLength(100);
        e.Property(x => x.CodigoActividad).HasMaxLength(100);
        e.Property(x => x.DescuentoRT).HasPrecision(65,28);
        e.Property(x => x.RecargoRT).HasPrecision(65,28);
        e.Property(x => x.EsSalarial).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.EsContributivoCCSS).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.EsTributario).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.EsPatronal).HasColumnType("tinyint(1)").IsRequired();
        e.Property(x => x.Efecto).HasMaxLength(40).IsRequired();
        e.Property(x => x.DescripcionConcepto).HasMaxLength(300).IsRequired();
        e.HasIndex(x => new { x.IdForecastDetalle, x.NumeroOrden }).IsUnique().HasDatabaseName("UX_FL_A_Orden");
        e.HasIndex(x => x.IdForecastDetalle).HasDatabaseName("IX_FL_A_IdForecastDetalle");
        e.HasOne<ForecastDetalle>().WithMany().HasForeignKey(x => x.IdForecastDetalle).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdForecastDetalle");
        e.HasIndex(x => x.IdForecastSegmentoProyectado).HasDatabaseName("IX_FL_A_IdForecastSegmentoProyectado");
        e.HasOne<ForecastSegmentoProyectado>().WithMany().HasForeignKey(x => x.IdForecastSegmentoProyectado).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdForecastSegmentoProyectado");
        e.HasIndex(x => x.IdForecastParametro).HasDatabaseName("IX_FL_A_IdForecastParametro");
        e.HasOne<ForecastParametro>().WithMany().HasForeignKey(x => x.IdForecastParametro).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdForecastParametro");
        e.HasIndex(x => x.IdForecastObservacionHistorica).HasDatabaseName("IX_FL_A_IdForecastObservacionHistorica");
        e.HasOne<ForecastObservacionHistorica>().WithMany().HasForeignKey(x => x.IdForecastObservacionHistorica).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdForecastObservacionHistorica");
        e.HasIndex(x => x.IdForecastDetallePrevio).HasDatabaseName("IX_FL_A_IdForecastDetallePrevio");
        e.HasOne<ForecastDetalle>().WithMany().HasForeignKey(x => x.IdForecastDetallePrevio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdForecastDetallePrevio");
        e.HasIndex(x => x.IdValorParametroNormativo).HasDatabaseName("IX_FL_A_IdValorParametroNormativo");
        e.HasOne<ValorParametroNormativo>().WithMany().HasForeignKey(x => x.IdValorParametroNormativo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdValorParametroNormativo");
        e.HasIndex(x => x.IdTramoImpuestoSalario).HasDatabaseName("IX_FL_A_IdTramoImpuestoSalario");
        e.HasOne<TramoImpuestoSalario>().WithMany().HasForeignKey(x => x.IdTramoImpuestoSalario).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdTramoImpuestoSalario");
        e.HasIndex(x => x.IdCreditoFiscalNormativo).HasDatabaseName("IX_FL_A_IdCreditoFiscalNormativo");
        e.HasOne<CreditoFiscalNormativo>().WithMany().HasForeignKey(x => x.IdCreditoFiscalNormativo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdCreditoFiscalNormativo");
        e.HasIndex(x => x.IdCreditoFiscalColaborador).HasDatabaseName("IX_FL_A_IdCreditoFiscalColaborador");
        e.HasOne<CreditoFiscalColaborador>().WithMany().HasForeignKey(x => x.IdCreditoFiscalColaborador).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdCreditoFiscalColaborador");
        e.HasIndex(x => x.IdConfiguracionRiesgoTrabajo).HasDatabaseName("IX_FL_A_IdConfiguracionRiesgoTrabajo");
        e.HasOne<ConfiguracionRiesgoTrabajo>().WithMany().HasForeignKey(x => x.IdConfiguracionRiesgoTrabajo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_A_IdConfiguracionRiesgoTrabajo");
    }
}


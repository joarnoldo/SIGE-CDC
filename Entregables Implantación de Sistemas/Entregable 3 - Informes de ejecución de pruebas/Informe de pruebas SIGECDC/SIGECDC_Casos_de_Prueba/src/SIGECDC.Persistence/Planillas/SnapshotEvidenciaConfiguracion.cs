using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

// Complemento SQL-first J-P01..06; no genera esquema ni escribe snapshots.
internal static class SnapshotEvidenciaConfiguracion
{
    public static void Configurar(EntityTypeBuilder<SegmentoCalculoPlanilla> e)
    {
        e.Property(x => x.IdCondicionLaboralVersion).HasColumnType("bigint");
        e.HasIndex(x => x.IdCondicionLaboralVersion).HasDatabaseName("IX_SJ_IdCondicionLaboralVersion");
        e.HasOne<CondicionLaboralVersion>().WithMany().HasForeignKey(x => x.IdCondicionLaboralVersion)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SJ_IdCondicionLaboralVersion");
        e.Property(x => x.IdJornadaLaboral).HasColumnType("bigint");
        e.HasIndex(x => x.IdJornadaLaboral).HasDatabaseName("IX_SJ_IdJornadaLaboral");
        e.HasOne<JornadaLaboral>().WithMany().HasForeignKey(x => x.IdJornadaLaboral)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SJ_IdJornadaLaboral");
        e.Property(x => x.IdSalarioMinimoVersion).HasColumnType("bigint");
        e.HasIndex(x => x.IdSalarioMinimoVersion).HasDatabaseName("IX_SJ_IdSalarioMinimoVersion");
        e.HasOne<SalarioMinimoVersion>().WithMany().HasForeignKey(x => x.IdSalarioMinimoVersion)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SJ_IdSalarioMinimoVersion");
        e.Property(x => x.NombrePuesto).HasMaxLength(150);
        e.Property(x => x.NombreDepartamento).HasMaxLength(150);
        e.Property(x => x.TipoContrato).HasMaxLength(100);
        e.Property(x => x.PeriodicidadPago).HasMaxLength(20);
        e.Property(x => x.CodigoOcupacion).HasMaxLength(100);
        e.Property(x => x.NombreOcupacion).HasMaxLength(200);
        e.Property(x => x.TipoJornada).HasMaxLength(30);
        e.Property(x => x.HorasOrdinariasSemana).HasPrecision(65, 28);
        e.Property(x => x.FechaJornadaInicio).HasColumnType("date");
        e.Property(x => x.FechaJornadaFin).HasColumnType("date");
        e.Property(x => x.MontoMinimo).HasPrecision(65, 28);
        e.Property(x => x.PeriodicidadMinimo).HasMaxLength(20);
        e.Property(x => x.TipoJornadaMinimo).HasMaxLength(30);
        e.Property(x => x.FechaMinimoInicio).HasColumnType("date");
        e.Property(x => x.FechaMinimoFin).HasColumnType("date");
        e.Property(x => x.ReferenciaRelacionOrdinaria).HasMaxLength(500);
        e.Property(x => x.MontoOrdinarioSinRedondear).HasPrecision(65, 28).IsRequired();
        e.ToTable("SegmentoCalculoPlanilla", t =>
        {
            t.HasCheckConstraint("CK_SJ_FechasJornada", "FechaJornadaFin IS NULL OR (FechaJornadaInicio IS NOT NULL AND FechaJornadaFin >= FechaJornadaInicio)");
            t.HasCheckConstraint("CK_SJ_FechasMinimo", "FechaMinimoFin IS NULL OR (FechaMinimoInicio IS NOT NULL AND FechaMinimoFin >= FechaMinimoInicio)");
        });
    }

    public static void Configurar(EntityTypeBuilder<ConceptoDetallePlanilla> e)
    {
        e.Property(x => x.IdIncidenciaPlanillaConfirmacion).HasColumnType("bigint");
        e.HasIndex(x => x.IdIncidenciaPlanillaConfirmacion).HasDatabaseName("IX_LJ_IdIncidenciaPlanillaConfirmacion");
        e.HasOne<IncidenciaPlanillaConfirmacion>().WithMany().HasForeignKey(x => x.IdIncidenciaPlanillaConfirmacion)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LJ_IdIncidenciaPlanillaConfirmacion");
        e.Property(x => x.IdParametroPlanilla).HasColumnType("int");
        e.HasIndex(x => x.IdParametroPlanilla).HasDatabaseName("IX_LJ_IdParametroPlanilla");
        e.HasOne<ParametroPlanilla>().WithMany().HasForeignKey(x => x.IdParametroPlanilla)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LJ_IdParametroPlanilla");
        e.Property(x => x.AmbitoParametro).HasMaxLength(20);
        e.Property(x => x.IdParametroPlanillaPeriodo).HasColumnType("bigint");
        e.HasIndex(x => x.IdParametroPlanillaPeriodo).HasDatabaseName("IX_LJ_IdParametroPlanillaPeriodo");
        e.HasOne<ParametroPlanillaPeriodo>().WithMany().HasForeignKey(x => x.IdParametroPlanillaPeriodo)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LJ_IdParametroPlanillaPeriodo");
        e.Property(x => x.IdParametroPlanillaColaborador).HasColumnType("bigint");
        e.HasIndex(x => x.IdParametroPlanillaColaborador).HasDatabaseName("IX_LJ_IdParametroPlanillaColaborador");
        e.HasOne<ParametroPlanillaColaborador>().WithMany().HasForeignKey(x => x.IdParametroPlanillaColaborador)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LJ_IdParametroPlanillaColaborador");
        e.Property(x => x.ValorParametroAplicado).HasPrecision(65, 28);
        e.Property(x => x.UnidadParametroAplicada).HasMaxLength(30);
        e.Property(x => x.ClaveHecho).HasMaxLength(300);
        e.Property(x => x.UnidadCantidad).HasMaxLength(30);
        e.Property(x => x.ReferenciaSoporte).HasMaxLength(500);
        e.Property(x => x.ReferenciaAutorizacion).HasMaxLength(500);
        e.Property(x => x.RegistradoPorHecho).HasMaxLength(255);
        e.Property(x => x.AutorizadoPorHecho).HasMaxLength(255);
        e.Property(x => x.IdCondicionLaboralBase).HasColumnType("bigint");
        e.Property(x => x.FechaBaseInicio).HasColumnType("date");
        e.Property(x => x.FechaBaseFin).HasColumnType("date");
        e.HasIndex(x => x.IdCondicionLaboralBase).HasDatabaseName("IX_LJ_IdCondicionLaboralBase");
        e.HasOne<CondicionLaboralVersion>().WithMany().HasForeignKey(x => x.IdCondicionLaboralBase)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LJ_IdCondicionLaboralBase");
        e.Property(x => x.Cantidad).HasPrecision(65, 28);
        e.Property(x => x.BaseCalculo).HasPrecision(65, 28);
        e.Property(x => x.Tasa).HasPrecision(65, 28);
        e.Property(x => x.Multiplicador).HasPrecision(65, 28);
        e.Property(x => x.MontoSinRedondear).HasPrecision(65, 28).IsRequired();
        e.ToTable("ConceptoDetallePlanilla", t =>
        {
            t.HasCheckConstraint("CK_LJ_Parametro", "(IdParametroPlanilla IS NULL AND AmbitoParametro IS NULL AND IdParametroPlanillaPeriodo IS NULL AND IdParametroPlanillaColaborador IS NULL AND ValorParametroAplicado IS NULL AND UnidadParametroAplicada IS NULL) OR (IdParametroPlanilla IS NOT NULL AND AmbitoParametro IS NOT NULL AND ValorParametroAplicado IS NOT NULL AND ValorParametroAplicado >= 0 AND UnidadParametroAplicada IN ('Monto','Porcentaje') AND UnidadParametroAplicada IS NOT NULL AND ((AmbitoParametro='Global' AND IdParametroPlanillaPeriodo IS NULL AND IdParametroPlanillaColaborador IS NULL) OR (AmbitoParametro='Periodo' AND IdParametroPlanillaPeriodo IS NOT NULL AND IdParametroPlanillaColaborador IS NULL) OR (AmbitoParametro='Colaborador' AND IdParametroPlanillaColaborador IS NOT NULL AND IdParametroPlanillaPeriodo IS NULL)))");
            t.HasCheckConstraint("CK_LJ_Confirmacion", "IdIncidenciaPlanillaConfirmacion IS NULL OR (TipoFuente='Incidencia' AND IdFuente IS NOT NULL)");
            t.HasCheckConstraint("CK_LJ_BaseHecho", "(IdCondicionLaboralBase IS NULL AND FechaBaseInicio IS NULL AND FechaBaseFin IS NULL) OR (IdCondicionLaboralBase IS NOT NULL AND FechaBaseInicio IS NOT NULL AND FechaBaseFin IS NOT NULL AND FechaBaseFin>=FechaBaseInicio AND TipoFuente='Derivado' AND EsPatronalSnapshot=1 AND IdSegmentoCalculoPlanilla IS NULL)");
        });
    }

    public static void Configurar(EntityTypeBuilder<AplicacionNormativaDetallePlanilla> e)
    {
        e.Property(x => x.MontoAplicadoSinRedondear).HasPrecision(65, 28);
        e.Property(x => x.IdCreditoFiscalColaborador).HasColumnType("bigint");
        e.HasIndex(x => x.IdCreditoFiscalColaborador).HasDatabaseName("IX_AJ_IdCreditoFiscalColaborador");
        e.HasOne<CreditoFiscalColaborador>().WithMany().HasForeignKey(x => x.IdCreditoFiscalColaborador)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AJ_IdCreditoFiscalColaborador");
        e.Property(x => x.CantidadAplicada).HasPrecision(65, 28);
        e.Property(x => x.IdDetallePlanillaPrevio).HasColumnType("bigint");
        e.HasIndex(x => x.IdDetallePlanillaPrevio).HasDatabaseName("IX_AJ_IdDetallePlanillaPrevio");
        e.HasOne<DetallePlanilla>().WithMany().HasForeignKey(x => x.IdDetallePlanillaPrevio)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AJ_IdDetallePlanillaPrevio");
        e.Property(x => x.AlcanceAplicacion).HasMaxLength(30);
        e.Property(x => x.FechaAplicacionInicio).HasColumnType("date");
        e.Property(x => x.FechaAplicacionFin).HasColumnType("date");
        e.Property(x => x.BaseContributiva).HasPrecision(65, 28);
        e.Property(x => x.ReglaAplicabilidad).HasMaxLength(40);
        e.Property(x => x.FechaAplicabilidadInicio).HasColumnType("date");
        e.Property(x => x.FechaAplicabilidadFin).HasColumnType("date");
        e.Property(x => x.ReferenciaSoporte).HasMaxLength(500);
        e.Property(x => x.ReferenciaRegla).HasMaxLength(500);
        e.Property(x => x.AcreditadoPor).HasMaxLength(255);
        e.Property(x => x.ModalidadRT).HasMaxLength(40);
        e.Property(x => x.IdContratoRT).HasColumnType("bigint");
        e.HasIndex(x => x.IdContratoRT).HasDatabaseName("IX_AJ_IdContratoRT");
        e.HasOne<Contrato>().WithMany().HasForeignKey(x => x.IdContratoRT)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AJ_IdContratoRT");
        e.Property(x => x.IdConfiguracionRTAncla).HasColumnType("bigint");
        e.HasIndex(x => x.IdConfiguracionRTAncla).HasDatabaseName("IX_AJ_IdConfiguracionRTAncla");
        e.HasOne<ConfiguracionRiesgoTrabajo>().WithMany().HasForeignKey(x => x.IdConfiguracionRTAncla)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AJ_IdConfiguracionRTAncla");
        e.Property(x => x.ReferenciaAsignacionRT).HasMaxLength(500);
        e.Property(x => x.AsociadoPorRT).HasMaxLength(255);
        e.Property(x => x.BaseAplicada).HasPrecision(65, 28);
        e.Property(x => x.ValorAplicado).HasPrecision(65, 28);
        e.Property(x => x.MontoAplicado).HasPrecision(18, 2).IsRequired(false);
        e.ToTable("AplicacionNormativaDetallePlanilla", t =>
        {
            t.HasCheckConstraint("CK_AJ_CreditoPersonal", "IdCreditoFiscalColaborador IS NULL OR (IdCreditoFiscalNormativo IS NOT NULL AND CantidadAplicada IS NOT NULL AND CantidadAplicada > 0)");
            t.HasCheckConstraint("CK_AJ_Alcance", "(AlcanceAplicacion IS NULL AND FechaAplicacionInicio IS NULL AND FechaAplicacionFin IS NULL AND IdDetallePlanillaPrevio IS NULL) OR (AlcanceAplicacion IS NOT NULL AND AlcanceAplicacion IN ('CorteActual','MensualDefinitiva','AcumuladoPrevio') AND FechaAplicacionInicio IS NOT NULL AND FechaAplicacionFin IS NOT NULL AND FechaAplicacionFin >= FechaAplicacionInicio AND (AlcanceAplicacion <> 'AcumuladoPrevio' OR IdDetallePlanillaPrevio IS NOT NULL))");
            t.HasCheckConstraint("CK_AJ_Aplicabilidad", "ReglaAplicabilidad IS NULL OR (IdValorParametroNormativo IS NOT NULL AND ReglaAplicabilidad IN ('RemuneracionContributiva','MinimoMensualOrdinario','ExcepcionSinMinimo') AND FechaAplicabilidadInicio IS NOT NULL AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND ReferenciaRegla IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaRegla)) > 0 AND AcreditadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AcreditadoPor)) > 0)");
            t.HasCheckConstraint("CK_AJ_Vigencia", "FechaAplicabilidadFin IS NULL OR (FechaAplicabilidadInicio IS NOT NULL AND FechaAplicabilidadFin >= FechaAplicabilidadInicio)");
            t.HasCheckConstraint("CK_AJ_RT", "ModalidadRT IS NULL OR (IdConfiguracionRiesgoTrabajo IS NOT NULL AND ModalidadRT='RemuneracionSalarialOrdinaria' AND IdContratoRT IS NOT NULL AND IdConfiguracionRTAncla IS NOT NULL AND FechaAplicabilidadInicio IS NOT NULL AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND AcreditadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AcreditadoPor)) > 0 AND ReferenciaAsignacionRT IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAsignacionRT)) > 0 AND AsociadoPorRT IS NOT NULL AND CHAR_LENGTH(TRIM(AsociadoPorRT)) > 0)");
        });
    }

}

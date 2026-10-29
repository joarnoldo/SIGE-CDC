using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastSegmentoProyectadoConfiguracion : IEntityTypeConfiguration<ForecastSegmentoProyectado>
{
    public void Configure(EntityTypeBuilder<ForecastSegmentoProyectado> e)
    {
        e.ToTable("ForecastSegmentoProyectado", t =>
        {
            t.HasCheckConstraint("CK_FL_Segmento_Orden", "NumeroOrden > 0 AND DiasBase30 >= 0");
            t.HasCheckConstraint("CK_FL_Segmento_Fechas", "FechaFin >= FechaInicio AND (FechaJornadaFin IS NULL OR FechaJornadaFin >= FechaJornadaInicio)");
            t.HasCheckConstraint("CK_FL_Segmento_Condicion", "(OrigenCondicion='CondicionExistente' AND IdContrato IS NOT NULL AND IdCondicionLaboralVersion IS NOT NULL) OR (OrigenCondicion IN ('ContratacionFutura','CambioFuturo') AND IdContrato IS NULL AND IdCondicionLaboralVersion IS NULL)");
            t.HasCheckConstraint("CK_FL_Segmento_Valores", "SalarioMensual > 0 AND HorasOrdinariasDia > 0 AND HorasOrdinariasSemana > 0 AND (MontoMinimo IS NULL OR MontoMinimo > 0)");
            t.HasCheckConstraint("CK_FL_Segmento_Soporte", "ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND RegistradoPor IS NOT NULL AND CHAR_LENGTH(TRIM(RegistradoPor)) > 0");
            t.HasCheckConstraint("CK_FL_Segmento_Minimo", "(IdSalarioMinimoVersion IS NULL AND MontoMinimo IS NULL AND PeriodicidadMinimo IS NULL) OR (IdSalarioMinimoVersion IS NOT NULL AND MontoMinimo IS NOT NULL AND PeriodicidadMinimo IS NOT NULL)");
            t.HasCheckConstraint("CK_FL_Segmento_RT", "(IdConfiguracionRiesgoTrabajo IS NULL AND ModalidadRT IS NULL AND ReferenciaAsignacionRT IS NULL AND AsociadoPorRT IS NULL AND FechaRTInicio IS NULL AND FechaRTFin IS NULL) OR (IdConfiguracionRiesgoTrabajo IS NOT NULL AND ModalidadRT IS NOT NULL AND ModalidadRT IN ('RemuneracionSalarialOrdinaria','Especial') AND ReferenciaAsignacionRT IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAsignacionRT)) > 0 AND AsociadoPorRT IS NOT NULL AND CHAR_LENGTH(TRIM(AsociadoPorRT)) > 0 AND FechaRTInicio IS NOT NULL AND (FechaRTFin IS NULL OR FechaRTFin >= FechaRTInicio))");
        });
        e.HasKey(x => x.IdForecastSegmentoProyectado);
        e.Property(x => x.IdForecastSegmentoProyectado).ValueGeneratedOnAdd();
        e.Property(x => x.IdForecastSegmentoProyectado).HasColumnType("bigint").IsRequired();
        e.Property(x => x.IdForecastEscenario).HasColumnType("bigint").IsRequired();
        e.Property(x => x.IdForecastPeriodo).HasColumnType("bigint").IsRequired();
        e.Property(x => x.IdForecastParticipante).HasColumnType("bigint").IsRequired();
        e.Property(x => x.NumeroOrden).HasColumnType("int").IsRequired();
        e.Property(x => x.FechaInicio).HasColumnType("date").IsRequired();
        e.Property(x => x.FechaFin).HasColumnType("date").IsRequired();
        e.Property(x => x.DiasBase30).HasColumnType("int").IsRequired();
        e.Property(x => x.OrigenCondicion).HasMaxLength(30).IsRequired();
        e.Property(x => x.SalarioMensual).HasPrecision(65,28).IsRequired();
        e.Property(x => x.IdContrato).HasColumnType("bigint");
        e.Property(x => x.IdCondicionLaboralVersion).HasColumnType("bigint");
        e.Property(x => x.IdJornadaLaboral).HasColumnType("bigint").IsRequired();
        e.Property(x => x.CodigoJornada).HasMaxLength(50).IsRequired();
        e.Property(x => x.NombreJornada).HasMaxLength(150).IsRequired();
        e.Property(x => x.TipoJornada).HasMaxLength(30).IsRequired();
        e.Property(x => x.HorasOrdinariasDia).HasPrecision(65,28).IsRequired();
        e.Property(x => x.HorasOrdinariasSemana).HasPrecision(65,28).IsRequired();
        e.Property(x => x.FechaJornadaInicio).HasColumnType("date").IsRequired();
        e.Property(x => x.FechaJornadaFin).HasColumnType("date");
        e.Property(x => x.IdOcupacionSalarioMinimo).HasColumnType("bigint").IsRequired();
        e.Property(x => x.CodigoOcupacion).HasMaxLength(100).IsRequired();
        e.Property(x => x.NombreOcupacion).HasMaxLength(200).IsRequired();
        e.Property(x => x.IdSalarioMinimoVersion).HasColumnType("bigint");
        e.Property(x => x.MontoMinimo).HasPrecision(65,28);
        e.Property(x => x.PeriodicidadMinimo).HasMaxLength(20);
        e.Property(x => x.IdPuesto).HasColumnType("int").IsRequired();
        e.Property(x => x.NombrePuesto).HasMaxLength(150).IsRequired();
        e.Property(x => x.IdDepartamento).HasColumnType("int").IsRequired();
        e.Property(x => x.NombreDepartamento).HasMaxLength(150).IsRequired();
        e.Property(x => x.TipoContrato).HasMaxLength(100).IsRequired();
        e.Property(x => x.PeriodicidadPago).HasMaxLength(20).IsRequired();
        e.Property(x => x.ReferenciaSoporte).HasMaxLength(500).IsRequired();
        e.Property(x => x.RegistradoPor).HasMaxLength(255).IsRequired();
        e.Property(x => x.FechaRegistro).HasColumnType("datetime").IsRequired();
        e.Property(x => x.IdConfiguracionRiesgoTrabajo).HasColumnType("bigint");
        e.Property(x => x.ModalidadRT).HasMaxLength(40);
        e.Property(x => x.ReferenciaAsignacionRT).HasMaxLength(500);
        e.Property(x => x.AsociadoPorRT).HasMaxLength(255);
        e.Property(x => x.FechaRTInicio).HasColumnType("date");
        e.Property(x => x.FechaRTFin).HasColumnType("date");
        e.HasIndex(x => new { x.IdForecastEscenario, x.IdForecastPeriodo, x.IdForecastParticipante, x.NumeroOrden }).IsUnique().HasDatabaseName("UX_FL_S_Orden");
        e.HasIndex(x => x.IdForecastEscenario).HasDatabaseName("IX_FL_S_IdForecastEscenario");
        e.HasOne<ForecastEscenario>().WithMany().HasForeignKey(x => x.IdForecastEscenario).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdForecastEscenario");
        e.HasIndex(x => x.IdForecastPeriodo).HasDatabaseName("IX_FL_S_IdForecastPeriodo");
        e.HasOne<ForecastPeriodo>().WithMany().HasForeignKey(x => x.IdForecastPeriodo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdForecastPeriodo");
        e.HasIndex(x => x.IdForecastParticipante).HasDatabaseName("IX_FL_S_IdForecastParticipante");
        e.HasOne<ForecastParticipante>().WithMany().HasForeignKey(x => x.IdForecastParticipante).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdForecastParticipante");
        e.HasIndex(x => x.IdContrato).HasDatabaseName("IX_FL_S_IdContrato");
        e.HasOne<Contrato>().WithMany().HasForeignKey(x => x.IdContrato).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdContrato");
        e.HasIndex(x => x.IdCondicionLaboralVersion).HasDatabaseName("IX_FL_S_IdCondicionLaboralVersion");
        e.HasOne<CondicionLaboralVersion>().WithMany().HasForeignKey(x => x.IdCondicionLaboralVersion).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdCondicionLaboralVersion");
        e.HasIndex(x => x.IdJornadaLaboral).HasDatabaseName("IX_FL_S_IdJornadaLaboral");
        e.HasOne<JornadaLaboral>().WithMany().HasForeignKey(x => x.IdJornadaLaboral).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdJornadaLaboral");
        e.HasIndex(x => x.IdOcupacionSalarioMinimo).HasDatabaseName("IX_FL_S_IdOcupacionSalarioMinimo");
        e.HasOne<OcupacionSalarioMinimo>().WithMany().HasForeignKey(x => x.IdOcupacionSalarioMinimo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdOcupacionSalarioMinimo");
        e.HasIndex(x => x.IdSalarioMinimoVersion).HasDatabaseName("IX_FL_S_IdSalarioMinimoVersion");
        e.HasOne<SalarioMinimoVersion>().WithMany().HasForeignKey(x => x.IdSalarioMinimoVersion).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdSalarioMinimoVersion");
        e.HasIndex(x => x.IdPuesto).HasDatabaseName("IX_FL_S_IdPuesto");
        e.HasOne<Puesto>().WithMany().HasForeignKey(x => x.IdPuesto).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdPuesto");
        e.HasIndex(x => x.IdDepartamento).HasDatabaseName("IX_FL_S_IdDepartamento");
        e.HasOne<Departamento>().WithMany().HasForeignKey(x => x.IdDepartamento).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdDepartamento");
        e.HasIndex(x => x.IdConfiguracionRiesgoTrabajo).HasDatabaseName("IX_FL_S_IdConfiguracionRiesgoTrabajo");
        e.HasOne<ConfiguracionRiesgoTrabajo>().WithMany().HasForeignKey(x => x.IdConfiguracionRiesgoTrabajo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_FL_S_IdConfiguracionRiesgoTrabajo");
    }
}


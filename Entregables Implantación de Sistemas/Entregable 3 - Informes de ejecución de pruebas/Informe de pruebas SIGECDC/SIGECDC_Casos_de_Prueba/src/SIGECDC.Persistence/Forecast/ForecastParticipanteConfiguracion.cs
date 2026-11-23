using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Forecast;

public sealed class ForecastParticipanteConfiguracion
    : IEntityTypeConfiguration<ForecastParticipante>
{
    public void Configure(EntityTypeBuilder<ForecastParticipante> entidad)
    {
        entidad.ToTable("ForecastParticipante", tabla =>
        {
            tabla.HasCheckConstraint(
                "CK_ForecastParticipante_Tipo",
                "(`TipoParticipante` = 'Colaborador' AND `IdColaborador` IS NOT NULL) OR "
                + "(`TipoParticipante` = 'ContratacionPrevista' AND `IdColaborador` IS NULL)");
            tabla.HasCheckConstraint(
                "CK_ForecastParticipante_Salario",
                "`SalarioBaseMensual` >= 0.00");
            tabla.HasCheckConstraint(
                "CK_ForecastParticipante_Fechas",
                "`FechaSalidaPrevista` IS NULL OR `FechaSalidaPrevista` >= `FechaInicioAplicacion`");
            tabla.HasCheckConstraint(
                "CK_ForecastParticipante_Inclusion",
                "`EstaIncluido` IN (0, 1)");
        });

        entidad.HasKey(participante => participante.IdForecastParticipante);

        entidad.Property(participante => participante.IdForecastParticipante)
            .ValueGeneratedOnAdd();

        entidad.Property(participante => participante.CodigoParticipante)
            .HasMaxLength(50)
            .IsRequired();

        entidad.Property(participante => participante.Etiqueta)
            .HasMaxLength(150)
            .IsRequired();

        entidad.Property(participante => participante.TipoParticipante)
            .HasColumnType("enum('Colaborador','ContratacionPrevista')")
            .IsRequired();

        entidad.Property(participante => participante.SalarioBaseMensual)
            .HasPrecision(18, 2)
            .IsRequired();

        entidad.Property(participante => participante.FechaInicioAplicacion)
            .HasColumnType("date")
            .IsRequired();

        entidad.Property(participante => participante.FechaSalidaPrevista)
            .HasColumnType("date");

        entidad.Property(participante => participante.EstaIncluido)
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(true)
            .IsRequired();

        entidad.Property(participante => participante.EstadoRegistro)
            .HasColumnType("enum('Activo','Inactivo')")
            .HasDefaultValue(EstadosRegistro.Activo)
            .IsRequired();

        entidad.HasIndex(participante => new
            {
                participante.IdForecastEscenario,
                participante.CodigoParticipante
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastParticipante_Escenario_Codigo");

        entidad.HasIndex(participante => new
            {
                participante.IdForecastEscenario,
                participante.IdColaborador
            })
            .IsUnique()
            .HasDatabaseName("UX_ForecastParticipante_Escenario_Colaborador");

        entidad.HasAlternateKey(participante => new
            {
                participante.IdForecastEscenario,
                participante.IdForecastParticipante
            })
            .HasName("UX_ForecastParticipante_Escenario_Id");

        entidad.HasIndex(participante => participante.IdDepartamento)
            .HasDatabaseName("IX_ForecastParticipante_IdDepartamento");

        entidad.HasIndex(participante => participante.IdPuesto)
            .HasDatabaseName("IX_ForecastParticipante_IdPuesto");

        entidad.HasOne(participante => participante.ForecastEscenario)
            .WithMany(escenario => escenario.Participantes)
            .HasForeignKey(participante => participante.IdForecastEscenario)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ForecastParticipante_ForecastEscenario");

        entidad.HasOne(participante => participante.Colaborador)
            .WithMany()
            .HasForeignKey(participante => participante.IdColaborador)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastParticipante_Colaborador");

        entidad.HasOne(participante => participante.Departamento)
            .WithMany()
            .HasForeignKey(participante => participante.IdDepartamento)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastParticipante_Departamento");

        entidad.HasOne(participante => participante.Puesto)
            .WithMany()
            .HasForeignKey(participante => participante.IdPuesto)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastParticipante_Puesto");
    }
}

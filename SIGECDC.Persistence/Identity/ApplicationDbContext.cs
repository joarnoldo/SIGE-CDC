using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Identity;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<ConsultaContacto> ConsultasContacto => Set<ConsultaContacto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ConsultaContacto>(entidad =>
        {
            entidad.ToTable("ConsultaContacto");

            entidad.HasKey(consulta => consulta.IdConsultaContacto);

            entidad.Property(consulta => consulta.IdConsultaContacto)
                .ValueGeneratedOnAdd();

            entidad.Property(consulta => consulta.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(consulta => consulta.CorreoElectronico)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(consulta => consulta.Telefono)
                .HasMaxLength(30);

            entidad.Property(consulta => consulta.Asunto)
                .HasMaxLength(200);

            entidad.Property(consulta => consulta.Mensaje)
                .HasColumnType("text")
                .IsRequired();

            entidad.Property(consulta => consulta.FechaEnvio)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(consulta => consulta.EstadoConsulta)
                .HasColumnType("enum('Nueva','EnRevision','Atendida','Descartada')")
                .HasDefaultValue(EstadosConsultaContacto.Nueva)
                .IsRequired();

            entidad.Property(consulta => consulta.ObservacionesInternas)
                .HasMaxLength(500);

            entidad.Property(consulta => consulta.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(consulta => consulta.FechaEnvio)
                .HasDatabaseName("IX_ConsultaContacto_FechaEnvio");

            entidad.HasIndex(consulta => consulta.EstadoConsulta)
                .HasDatabaseName("IX_ConsultaContacto_EstadoConsulta");

            entidad.HasIndex(consulta => consulta.CorreoElectronico)
                .HasDatabaseName("IX_ConsultaContacto_CorreoElectronico");
        });
    }
}

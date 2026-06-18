using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Identity;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<ConsultaContacto> ConsultasContacto => Set<ConsultaContacto>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ConsultaContacto>(entidad =>
        {
            entidad.ToTable("ConsultaContacto");
            entidad.HasKey(consulta => consulta.IdConsultaContacto);
            entidad.Property(consulta => consulta.IdConsultaContacto).ValueGeneratedOnAdd();
            entidad.Property(consulta => consulta.Nombre).HasMaxLength(150).IsRequired();
            entidad.Property(consulta => consulta.CorreoElectronico).HasMaxLength(150).IsRequired();
            entidad.Property(consulta => consulta.Telefono).HasMaxLength(30);
            entidad.Property(consulta => consulta.Asunto).HasMaxLength(200);
            entidad.Property(consulta => consulta.Mensaje).HasColumnType("text").IsRequired();
            entidad.Property(consulta => consulta.FechaEnvio).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
            entidad.Property(consulta => consulta.EstadoConsulta)
                .HasColumnType("enum('Nueva','EnRevision','Atendida','Descartada')")
                .HasDefaultValue(EstadosConsultaContacto.Nueva)
                .IsRequired();
            entidad.Property(consulta => consulta.ObservacionesInternas).HasMaxLength(500);
            entidad.Property(consulta => consulta.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();
            entidad.HasIndex(consulta => consulta.FechaEnvio).HasDatabaseName("IX_ConsultaContacto_FechaEnvio");
            entidad.HasIndex(consulta => consulta.EstadoConsulta).HasDatabaseName("IX_ConsultaContacto_EstadoConsulta");
            entidad.HasIndex(consulta => consulta.CorreoElectronico).HasDatabaseName("IX_ConsultaContacto_CorreoElectronico");
        });

        builder.Entity<FAQ>(entidad =>
        {
            entidad.ToTable("FAQ");
            entidad.HasKey(f => f.IdFAQ);
            entidad.Property(f => f.IdFAQ).ValueGeneratedOnAdd();
            entidad.Property(f => f.Pregunta).HasMaxLength(300).IsRequired();
            entidad.Property(f => f.Respuesta).HasColumnType("text").IsRequired();
            entidad.Property(f => f.Orden).HasDefaultValue(0).IsRequired();
            entidad.Property(f => f.EstaPublicado).HasDefaultValue(false).IsRequired();
            entidad.Property(f => f.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();
            entidad.HasIndex(f => f.EstaPublicado).HasDatabaseName("IX_FAQ_EstaPublicado");
            entidad.HasIndex(f => f.Orden).HasDatabaseName("IX_FAQ_Orden");
        });

        builder.Entity<Departamento>(entidad =>
        {
            entidad.ToTable("Departamento");
            entidad.HasKey(d => d.IdDepartamento);
            entidad.Property(d => d.IdDepartamento).ValueGeneratedOnAdd();
            entidad.Property(d => d.Nombre).HasMaxLength(100).IsRequired();
            entidad.Property(d => d.Descripcion).HasMaxLength(255);
            entidad.Property(d => d.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();
            entidad.HasIndex(d => d.Nombre).IsUnique().HasDatabaseName("UX_Departamento_Nombre");
        });

        builder.Entity<Puesto>(entidad =>
        {
            entidad.ToTable("Puesto");
            entidad.HasKey(p => p.IdPuesto);
            entidad.Property(p => p.IdPuesto).ValueGeneratedOnAdd();
            entidad.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entidad.Property(p => p.Descripcion).HasMaxLength(255);
            entidad.Property(p => p.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();
            entidad.HasIndex(p => p.Nombre).IsUnique().HasDatabaseName("UX_Puesto_Nombre");
            entidad.HasOne(p => p.Departamento)
                .WithMany(d => d.Puestos)
                .HasForeignKey(p => p.IdDepartamento)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Colaborador>(entidad =>
        {
            entidad.ToTable("Colaborador");
            entidad.HasKey(c => c.IdColaborador);
            entidad.Property(c => c.IdColaborador).ValueGeneratedOnAdd();
            entidad.Property(c => c.CodigoColaborador).HasMaxLength(20).IsRequired();
            entidad.Property(c => c.TipoIdentificacion).HasMaxLength(30).IsRequired();
            entidad.Property(c => c.Identificacion).HasMaxLength(30).IsRequired();
            entidad.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            entidad.Property(c => c.PrimerApellido).HasMaxLength(100).IsRequired();
            entidad.Property(c => c.SegundoApellido).HasMaxLength(100);
            entidad.Property(c => c.CorreoElectronico).HasMaxLength(150);
            entidad.Property(c => c.Telefono).HasMaxLength(30);
            entidad.Property(c => c.Direccion).HasMaxLength(300);
            entidad.Property(c => c.Observaciones).HasMaxLength(500);
            entidad.Property(c => c.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();
            entidad.HasOne(c => c.Departamento)
                .WithMany(d => d.Colaboradores)
                .HasForeignKey(c => c.IdDepartamento)
                .OnDelete(DeleteBehavior.Restrict);
            entidad.HasOne(c => c.Puesto)
                .WithMany(p => p.Colaboradores)
                .HasForeignKey(c => c.IdPuesto)
                .OnDelete(DeleteBehavior.Restrict);
            entidad.HasIndex(c => c.CodigoColaborador).IsUnique().HasDatabaseName("UX_Colaborador_Codigo");
            entidad.HasIndex(c => c.Identificacion).IsUnique().HasDatabaseName("UX_Colaborador_Identificacion");
            entidad.HasIndex(c => c.IdDepartamento).HasDatabaseName("IX_Colaborador_IdDepartamento");
            entidad.HasIndex(c => c.IdPuesto).HasDatabaseName("IX_Colaborador_IdPuesto");
        });
    }
}
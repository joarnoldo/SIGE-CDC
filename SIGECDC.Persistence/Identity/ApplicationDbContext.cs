using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Identity;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<ConsultaContacto> ConsultasContacto => Set<ConsultaContacto>();

    public DbSet<PaginaContenido> PaginasContenido => Set<PaginaContenido>();

    public DbSet<EstadoProyecto> EstadosProyecto => Set<EstadoProyecto>();

    public DbSet<Proyecto> Proyectos => Set<Proyecto>();

    public DbSet<ProyectoPublicado> ProyectosPublicados => Set<ProyectoPublicado>();

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

        builder.Entity<PaginaContenido>(entidad =>
        {
            entidad.ToTable("PaginaContenido");

            entidad.HasKey(pagina => pagina.IdPaginaContenido);

            entidad.Property(pagina => pagina.IdPaginaContenido)
                .ValueGeneratedOnAdd();

            entidad.Property(pagina => pagina.CodigoPagina)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(pagina => pagina.Titulo)
                .HasMaxLength(200)
                .IsRequired();

            entidad.Property(pagina => pagina.Contenido)
                .HasColumnType("mediumtext");

            entidad.Property(pagina => pagina.EstaPublicado)
                .HasDefaultValue(false)
                .IsRequired();

            entidad.Property(pagina => pagina.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(pagina => pagina.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(pagina => pagina.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(pagina => pagina.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(pagina => pagina.CodigoPagina)
                .IsUnique()
                .HasDatabaseName("UX_PaginaContenido_CodigoPagina");

            entidad.HasIndex(pagina => pagina.EstaPublicado)
                .HasDatabaseName("IX_PaginaContenido_EstaPublicado");
        });

        builder.Entity<EstadoProyecto>(entidad =>
        {
            entidad.ToTable("EstadoProyecto");

            entidad.HasKey(estado => estado.IdEstadoProyecto);

            entidad.Property(estado => estado.IdEstadoProyecto)
                .ValueGeneratedOnAdd();

            entidad.Property(estado => estado.Nombre)
                .HasMaxLength(50)
                .IsRequired();

            entidad.Property(estado => estado.Descripcion)
                .HasMaxLength(255);

            entidad.Property(estado => estado.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(estado => estado.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_EstadoProyecto_Nombre");
        });

        builder.Entity<Proyecto>(entidad =>
        {
            entidad.ToTable("Proyecto");

            entidad.HasKey(proyecto => proyecto.IdProyecto);

            entidad.Property(proyecto => proyecto.IdProyecto)
                .ValueGeneratedOnAdd();

            entidad.Property(proyecto => proyecto.CodigoProyecto)
                .HasMaxLength(30)
                .IsRequired();

            entidad.Property(proyecto => proyecto.NombreProyecto)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(proyecto => proyecto.Descripcion)
                .HasMaxLength(500);

            entidad.Property(proyecto => proyecto.Responsable)
                .HasMaxLength(150);

            entidad.Property(proyecto => proyecto.Ubicacion)
                .HasMaxLength(200);

            entidad.Property(proyecto => proyecto.Observaciones)
                .HasMaxLength(500);

            entidad.Property(proyecto => proyecto.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(proyecto => proyecto.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(proyecto => proyecto.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(proyecto => proyecto.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(proyecto => proyecto.CodigoProyecto)
                .IsUnique()
                .HasDatabaseName("UX_Proyecto_CodigoProyecto");

            entidad.HasIndex(proyecto => proyecto.NombreProyecto)
                .HasDatabaseName("IX_Proyecto_NombreProyecto");

            entidad.HasIndex(proyecto => proyecto.IdEstadoProyecto)
                .HasDatabaseName("IX_Proyecto_IdEstadoProyecto");

            entidad.HasOne(proyecto => proyecto.EstadoProyecto)
                .WithMany()
                .HasForeignKey(proyecto => proyecto.IdEstadoProyecto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProyectoPublicado>(entidad =>
        {
            entidad.ToTable("ProyectoPublicado");

            entidad.HasKey(proyecto => proyecto.IdProyectoPublicado);

            entidad.Property(proyecto => proyecto.IdProyectoPublicado)
                .ValueGeneratedOnAdd();

            entidad.Property(proyecto => proyecto.Titulo)
                .HasMaxLength(200)
                .IsRequired();

            entidad.Property(proyecto => proyecto.Descripcion)
                .HasMaxLength(500);

            entidad.Property(proyecto => proyecto.EstadoVisual)
                .HasMaxLength(50);

            entidad.Property(proyecto => proyecto.EstaPublicado)
                .HasDefaultValue(false)
                .IsRequired();

            entidad.Property(proyecto => proyecto.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(proyecto => proyecto.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(proyecto => proyecto.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(proyecto => proyecto.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(proyecto => proyecto.IdProyecto)
                .HasDatabaseName("IX_ProyectoPublicado_IdProyecto");

            entidad.HasIndex(proyecto => proyecto.EstaPublicado)
                .HasDatabaseName("IX_ProyectoPublicado_EstaPublicado");

            entidad.HasOne(proyecto => proyecto.Proyecto)
                .WithMany()
                .HasForeignKey(proyecto => proyecto.IdProyecto)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

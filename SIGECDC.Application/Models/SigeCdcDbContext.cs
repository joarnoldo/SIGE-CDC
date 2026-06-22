using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace SIGECDC.Application.Models;

public partial class SigeCdcDbContext : DbContext
{
    public SigeCdcDbContext()
    {
    }

    public SigeCdcDbContext(DbContextOptions<SigeCdcDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aspnetrole> Aspnetroles { get; set; }

    public virtual DbSet<Aspnetroleclaim> Aspnetroleclaims { get; set; }

    public virtual DbSet<Aspnetuser> Aspnetusers { get; set; }

    public virtual DbSet<Aspnetuserclaim> Aspnetuserclaims { get; set; }

    public virtual DbSet<Aspnetuserlogin> Aspnetuserlogins { get; set; }

    public virtual DbSet<Aspnetusertoken> Aspnetusertokens { get; set; }

    public virtual DbSet<Consultacontacto> Consultacontactos { get; set; }

    public virtual DbSet<Efmigrationshistory> Efmigrationshistories { get; set; }

    public virtual DbSet<Estadoproyecto> Estadoproyectos { get; set; }

    public virtual DbSet<Multimedia> Multimedias { get; set; }

    public virtual DbSet<Multimedium> Multimedia { get; set; }

    public virtual DbSet<Noticia> Noticias { get; set; }

    public virtual DbSet<Paginacontenido> Paginacontenidos { get; set; }

    public virtual DbSet<Proyecto> Proyectos { get; set; }

    public virtual DbSet<Proyectopublicado> Proyectopublicados { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{

	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Aspnetrole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("aspnetroles");

            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<Aspnetroleclaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("aspnetroleclaims");

            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.Aspnetroleclaims)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_AspNetRoleClaims_AspNetRoles_RoleId");
        });

        modelBuilder.Entity<Aspnetuser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("aspnetusers");

            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.LockoutEnd).HasColumnType("datetime");
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "Aspnetuserrole",
                    r => r.HasOne<Aspnetrole>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_AspNetUserRoles_AspNetRoles_RoleId"),
                    l => l.HasOne<Aspnetuser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_AspNetUserRoles_AspNetUsers_UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("aspnetuserroles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<Aspnetuserclaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("aspnetuserclaims");

            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.Aspnetuserclaims)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AspNetUserClaims_AspNetUsers_UserId");
        });

        modelBuilder.Entity<Aspnetuserlogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("aspnetuserlogins");

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.Aspnetuserlogins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AspNetUserLogins_AspNetUsers_UserId");
        });

        modelBuilder.Entity<Aspnetusertoken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("aspnetusertokens");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.Aspnetusertokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AspNetUserTokens_AspNetUsers_UserId");
        });

        modelBuilder.Entity<Consultacontacto>(entity =>
        {
            entity.HasKey(e => e.IdConsultaContacto).HasName("PRIMARY");

            entity.ToTable("consultacontacto");

            entity.HasIndex(e => e.CorreoElectronico, "IX_ConsultaContacto_CorreoElectronico");

            entity.HasIndex(e => e.EstadoConsulta, "IX_ConsultaContacto_EstadoConsulta");

            entity.HasIndex(e => e.FechaEnvio, "IX_ConsultaContacto_FechaEnvio");

            entity.Property(e => e.Asunto).HasMaxLength(200);
            entity.Property(e => e.CorreoElectronico).HasMaxLength(150);
            entity.Property(e => e.EstadoConsulta)
                .HasDefaultValueSql("'Nueva'")
                .HasColumnType("enum('Nueva','EnRevision','Atendida','Descartada')");
            entity.Property(e => e.EstadoRegistro)
                .HasDefaultValueSql("'Activo'")
                .HasColumnType("enum('Activo','Inactivo')");
            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp");
            entity.Property(e => e.Mensaje).HasColumnType("text");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.ObservacionesInternas).HasMaxLength(500);
            entity.Property(e => e.Telefono).HasMaxLength(30);
        });

        modelBuilder.Entity<Efmigrationshistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

            entity.ToTable("__efmigrationshistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Estadoproyecto>(entity =>
        {
            entity.HasKey(e => e.IdEstadoProyecto).HasName("PRIMARY");

            entity.ToTable("estadoproyecto");

            entity.HasIndex(e => e.Nombre, "UX_EstadoProyecto_Nombre").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.EstadoRegistro)
                .HasDefaultValueSql("'Activo'")
                .HasColumnType("enum('Activo','Inactivo')");
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Multimedia>(entity =>
        {
            entity.HasKey(e => e.IdMultimedia).HasName("PRIMARY");

            entity.ToTable("multimedias");

            entity.Property(e => e.FechaCreacion).HasMaxLength(6);
        });

        modelBuilder.Entity<Multimedium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("multimedia");

            entity.HasIndex(e => e.NoticiaId, "NoticiaId");

            entity.Property(e => e.FechaSubida)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.RutaArchivo).HasMaxLength(500);
            entity.Property(e => e.TipoArchivo).HasMaxLength(50);
            entity.Property(e => e.Titulo).HasMaxLength(150);

            entity.HasOne(d => d.Noticia).WithMany(p => p.Multimedia)
                .HasForeignKey(d => d.NoticiaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("multimedia_ibfk_1");
        });

        modelBuilder.Entity<Noticia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("noticias");

            entity.Property(e => e.Contenido).HasColumnType("text");
            entity.Property(e => e.FechaPublicacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ImagenUrl).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(250);
        });

        modelBuilder.Entity<Paginacontenido>(entity =>
        {
            entity.HasKey(e => e.IdPaginaContenido).HasName("PRIMARY");

            entity.ToTable("paginacontenido");

            entity.HasIndex(e => e.EstaPublicado, "IX_PaginaContenido_EstaPublicado");

            entity.HasIndex(e => e.CodigoPagina, "UX_PaginaContenido_CodigoPagina").IsUnique();

            entity.Property(e => e.CodigoPagina).HasMaxLength(100);
            entity.Property(e => e.Contenido).HasColumnType("mediumtext");
            entity.Property(e => e.CreadoPor).HasMaxLength(255);
            entity.Property(e => e.EstadoRegistro)
                .HasDefaultValueSql("'Activo'")
                .HasColumnType("enum('Activo','Inactivo')");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp");
            entity.Property(e => e.FechaModificacion).HasMaxLength(6);
            entity.Property(e => e.FechaPublicacion).HasMaxLength(6);
            entity.Property(e => e.ModificadoPor).HasMaxLength(255);
            entity.Property(e => e.Titulo).HasMaxLength(200);
        });

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.HasKey(e => e.IdProyecto).HasName("PRIMARY");

            entity.ToTable("proyecto");

            entity.HasIndex(e => e.IdEstadoProyecto, "IX_Proyecto_IdEstadoProyecto");

            entity.HasIndex(e => e.NombreProyecto, "IX_Proyecto_NombreProyecto");

            entity.HasIndex(e => e.CodigoProyecto, "UX_Proyecto_CodigoProyecto").IsUnique();

            entity.Property(e => e.CodigoProyecto).HasMaxLength(30);
            entity.Property(e => e.CreadoPor).HasMaxLength(255);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.EstadoRegistro)
                .HasDefaultValueSql("'Activo'")
                .HasColumnType("enum('Activo','Inactivo')");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp");
            entity.Property(e => e.FechaFinEstimada).HasMaxLength(6);
            entity.Property(e => e.FechaFinReal).HasMaxLength(6);
            entity.Property(e => e.FechaInicio).HasMaxLength(6);
            entity.Property(e => e.FechaModificacion).HasMaxLength(6);
            entity.Property(e => e.ModificadoPor).HasMaxLength(255);
            entity.Property(e => e.NombreProyecto).HasMaxLength(150);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Responsable).HasMaxLength(150);
            entity.Property(e => e.Ubicacion).HasMaxLength(200);

            entity.HasOne(d => d.IdEstadoProyectoNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdEstadoProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyecto_EstadoProyecto_IdEstadoProyecto");
        });

        modelBuilder.Entity<Proyectopublicado>(entity =>
        {
            entity.HasKey(e => e.IdProyectoPublicado).HasName("PRIMARY");

            entity.ToTable("proyectopublicado");

            entity.HasIndex(e => e.EstaPublicado, "IX_ProyectoPublicado_EstaPublicado");

            entity.HasIndex(e => e.IdProyecto, "IX_ProyectoPublicado_IdProyecto");

            entity.Property(e => e.CreadoPor).HasMaxLength(255);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.EstadoRegistro)
                .HasDefaultValueSql("'Activo'")
                .HasColumnType("enum('Activo','Inactivo')");
            entity.Property(e => e.EstadoVisual).HasMaxLength(50);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp");
            entity.Property(e => e.FechaModificacion).HasMaxLength(6);
            entity.Property(e => e.FechaPublicacion).HasMaxLength(6);
            entity.Property(e => e.ModificadoPor).HasMaxLength(255);
            entity.Property(e => e.Titulo).HasMaxLength(200);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.Proyectopublicados)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ProyectoPublicado_Proyecto_IdProyecto");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

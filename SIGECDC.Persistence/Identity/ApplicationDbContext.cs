using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.Activos;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Persistence.Identity;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<ConsultaContacto> ConsultasContacto => Set<ConsultaContacto>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<Noticia> Noticias => Set<Noticia>();
    public DbSet<Galeria> Galerias => Set<Galeria>();
    public DbSet<ImagenGaleria> ImagenesGaleria => Set<ImagenGaleria>();
    public DbSet<EstadoLaboral> EstadosLaborales => Set<EstadoLaboral>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<DocumentoArchivo> DocumentosArchivo => Set<DocumentoArchivo>();
    public DbSet<ParametroPlanilla> ParametrosPlanilla => Set<ParametroPlanilla>();
    public DbSet<ParametroPlanillaColaborador> ParametrosPlanillaColaborador => Set<ParametroPlanillaColaborador>();
    public DbSet<ParametroPlanillaPeriodo> ParametrosPlanillaPeriodo => Set<ParametroPlanillaPeriodo>();
    public DbSet<EstadoPlanilla> EstadosPlanilla => Set<EstadoPlanilla>();
    public DbSet<TipoIncidenciaPlanilla> TiposIncidenciaPlanilla => Set<TipoIncidenciaPlanilla>();
    public DbSet<PeriodoPlanilla> PeriodosPlanilla => Set<PeriodoPlanilla>();
    public DbSet<Planilla> Planillas => Set<Planilla>();
    public DbSet<DetallePlanilla> DetallesPlanilla => Set<DetallePlanilla>();
    public DbSet<IncidenciaPlanilla> IncidenciasPlanilla => Set<IncidenciaPlanilla>();
    public DbSet<BitacoraAuditoria> BitacoraAuditoria => Set<BitacoraAuditoria>();

    public DbSet<EstadoActivo> EstadosActivo => Set<EstadoActivo>();

    public DbSet<TipoActivo> TiposActivo => Set<TipoActivo>();

    public DbSet<CategoriaActivo> CategoriasActivo => Set<CategoriaActivo>();

    public DbSet<Activo> Activos => Set<Activo>();

    public DbSet<AsignacionActivoProyecto> AsignacionesActivoProyecto => Set<AsignacionActivoProyecto>();

    public DbSet<PaginaContenido> PaginasContenido => Set<PaginaContenido>();

    public DbSet<EstadoProyecto> EstadosProyecto => Set<EstadoProyecto>();

    public DbSet<Proyecto> Proyectos => Set<Proyecto>();

    public DbSet<ProyectoPublicado> ProyectosPublicados => Set<ProyectoPublicado>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entidad =>
        {
            entidad.Property(usuario => usuario.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.Property(usuario => usuario.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
        });

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

        builder.Entity<FAQ>(entidad =>
        {
            entidad.ToTable("FAQ");

            entidad.HasKey(faq => faq.IdFAQ);

            entidad.Property(faq => faq.IdFAQ)
                .ValueGeneratedOnAdd();

            entidad.Property(faq => faq.Pregunta)
                .HasMaxLength(300)
                .IsRequired();

            entidad.Property(faq => faq.Respuesta)
                .HasColumnType("text")
                .IsRequired();

            entidad.Property(faq => faq.Orden)
                .HasDefaultValue(0)
                .IsRequired();

            entidad.Property(faq => faq.EstaPublicado)
                .HasDefaultValue(false)
                .IsRequired();

            entidad.Property(faq => faq.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(faq => faq.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(faq => faq.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(faq => faq.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(faq => faq.EstaPublicado)
                .HasDatabaseName("IX_FAQ_EstaPublicado");

            entidad.HasIndex(faq => faq.Orden)
                .HasDatabaseName("IX_FAQ_Orden");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(faq => faq.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FAQ_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(faq => faq.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FAQ_ModificadoPor");
        });

        builder.Entity<Noticia>(entidad =>
        {
            entidad.ToTable("Noticia");

            entidad.HasKey(noticia => noticia.IdNoticia);

            entidad.Property(noticia => noticia.IdNoticia)
                .ValueGeneratedOnAdd();

            entidad.Property(noticia => noticia.Titulo)
                .HasMaxLength(200)
                .IsRequired();

            entidad.Property(noticia => noticia.Resumen)
                .HasMaxLength(500);

            entidad.Property(noticia => noticia.Contenido)
                .HasColumnType("mediumtext");

            entidad.Property(noticia => noticia.EstaPublicado)
                .HasDefaultValue(false)
                .IsRequired();

            entidad.Property(noticia => noticia.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(noticia => noticia.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(noticia => noticia.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(noticia => noticia.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(noticia => noticia.EstaPublicado)
                .HasDatabaseName("IX_Noticia_EstaPublicado");

            entidad.HasIndex(noticia => noticia.FechaPublicacion)
                .HasDatabaseName("IX_Noticia_FechaPublicacion");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(noticia => noticia.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Noticia_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(noticia => noticia.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Noticia_ModificadoPor");
        });

        builder.Entity<Galeria>(entidad =>
        {
            entidad.ToTable("Galeria");

            entidad.HasKey(galeria => galeria.IdGaleria);

            entidad.Property(galeria => galeria.IdGaleria)
                .ValueGeneratedOnAdd();

            entidad.Property(galeria => galeria.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(galeria => galeria.Descripcion)
                .HasMaxLength(500);

            entidad.Property(galeria => galeria.EstaPublicado)
                .HasDefaultValue(false)
                .IsRequired();

            entidad.Property(galeria => galeria.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(galeria => galeria.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(galeria => galeria.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(galeria => galeria.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(galeria => galeria.EstaPublicado)
                .HasDatabaseName("IX_Galeria_EstaPublicado");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(galeria => galeria.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Galeria_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(galeria => galeria.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Galeria_ModificadoPor");
        });

        builder.Entity<ImagenGaleria>(entidad =>
        {
            entidad.ToTable("ImagenGaleria");

            entidad.HasKey(imagen => imagen.IdImagenGaleria);

            entidad.Property(imagen => imagen.IdImagenGaleria)
                .ValueGeneratedOnAdd();

            entidad.Property(imagen => imagen.Titulo)
                .HasMaxLength(150);

            entidad.Property(imagen => imagen.Descripcion)
                .HasMaxLength(300);

            entidad.Property(imagen => imagen.Orden)
                .HasDefaultValue(0)
                .IsRequired();

            entidad.Property(imagen => imagen.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(imagen => imagen.IdGaleria)
                .HasDatabaseName("IX_ImagenGaleria_IdGaleria");

            entidad.HasIndex(imagen => imagen.IdDocumentoArchivo)
                .HasDatabaseName("IX_ImagenGaleria_IdDocumentoArchivo");

            entidad.HasOne(imagen => imagen.Galeria)
                .WithMany(galeria => galeria.Imagenes)
                .HasForeignKey(imagen => imagen.IdGaleria)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ImagenGaleria_Galeria");

            entidad.HasOne(imagen => imagen.DocumentoArchivo)
                .WithMany()
                .HasForeignKey(imagen => imagen.IdDocumentoArchivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImagenGaleria_DocumentoArchivo");
        });
        builder.Entity<EstadoLaboral>(entidad =>
        {
            entidad.ToTable("EstadoLaboral");

            entidad.HasKey(estado => estado.IdEstadoLaboral);

            entidad.Property(estado => estado.IdEstadoLaboral)
                .ValueGeneratedOnAdd();

            entidad.Property(estado => estado.Nombre)
                .HasMaxLength(50)
                .IsRequired();

            entidad.Property(estado => estado.Descripcion)
                .HasMaxLength(255);

            entidad.Property(estado => estado.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(estado => estado.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_EstadoLaboral_Nombre");
        });

        builder.Entity<Departamento>(entidad =>
        {
            entidad.ToTable("Departamento");

            entidad.HasKey(departamento => departamento.IdDepartamento);

            entidad.Property(departamento => departamento.IdDepartamento)
                .ValueGeneratedOnAdd();

            entidad.Property(departamento => departamento.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(departamento => departamento.Descripcion)
                .HasMaxLength(255);

            entidad.Property(departamento => departamento.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(departamento => departamento.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_Departamento_Nombre");
        });

        builder.Entity<Puesto>(entidad =>
        {
            entidad.ToTable("Puesto");

            entidad.HasKey(puesto => puesto.IdPuesto);

            entidad.Property(puesto => puesto.IdPuesto)
                .ValueGeneratedOnAdd();

            entidad.Property(puesto => puesto.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(puesto => puesto.Descripcion)
                .HasMaxLength(255);

            entidad.Property(puesto => puesto.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(puesto => puesto.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_Puesto_Nombre");

            entidad.HasIndex(puesto => puesto.IdDepartamento)
                .HasDatabaseName("IX_Puesto_IdDepartamento");

            entidad.HasOne(puesto => puesto.Departamento)
                .WithMany()
                .HasForeignKey(puesto => puesto.IdDepartamento)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Puesto_Departamento");
        });

        builder.Entity<Colaborador>(entidad =>
        {
            entidad.ToTable("Colaborador");

            entidad.HasKey(colaborador => colaborador.IdColaborador);

            entidad.Property(colaborador => colaborador.IdColaborador)
                .ValueGeneratedOnAdd();

            entidad.Property(colaborador => colaborador.CodigoColaborador)
                .HasMaxLength(20)
                .IsRequired();

            entidad.Property(colaborador => colaborador.TipoIdentificacion)
                .HasMaxLength(30)
                .IsRequired();

            entidad.Property(colaborador => colaborador.Identificacion)
                .HasMaxLength(30)
                .IsRequired();

            entidad.Property(colaborador => colaborador.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(colaborador => colaborador.PrimerApellido)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(colaborador => colaborador.SegundoApellido)
                .HasMaxLength(100);

            entidad.Property(colaborador => colaborador.FechaNacimiento)
                .HasColumnType("date");

            entidad.Property(colaborador => colaborador.CorreoElectronico)
                .HasMaxLength(150);

            entidad.Property(colaborador => colaborador.Telefono)
                .HasMaxLength(30);

            entidad.Property(colaborador => colaborador.Direccion)
                .HasMaxLength(300);

            entidad.Property(colaborador => colaborador.FechaIngreso)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(colaborador => colaborador.FechaSalida)
                .HasColumnType("date");

            entidad.Property(colaborador => colaborador.IdUsuario)
                .HasMaxLength(255);

            entidad.Property(colaborador => colaborador.Observaciones)
                .HasMaxLength(500);

            entidad.Property(colaborador => colaborador.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(colaborador => colaborador.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(colaborador => colaborador.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(colaborador => colaborador.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(colaborador => colaborador.CodigoColaborador)
                .IsUnique()
                .HasDatabaseName("UX_Colaborador_Codigo");

            entidad.HasIndex(colaborador => colaborador.Identificacion)
                .IsUnique()
                .HasDatabaseName("UX_Colaborador_Identificacion");

            entidad.HasIndex(colaborador => colaborador.IdUsuario)
                .IsUnique()
                .HasDatabaseName("UX_Colaborador_IdUsuario");

            entidad.HasIndex(colaborador => colaborador.IdEstadoLaboral)
                .HasDatabaseName("IX_Colaborador_IdEstadoLaboral");

            entidad.HasIndex(colaborador => colaborador.IdDepartamento)
                .HasDatabaseName("IX_Colaborador_IdDepartamento");

            entidad.HasIndex(colaborador => colaborador.IdPuesto)
                .HasDatabaseName("IX_Colaborador_IdPuesto");

            entidad.HasIndex(colaborador => new
                {
                    colaborador.Nombre,
                    colaborador.PrimerApellido,
                    colaborador.SegundoApellido
                })
                .HasDatabaseName("IX_Colaborador_Nombre");

            entidad.HasOne(colaborador => colaborador.EstadoLaboral)
                .WithMany()
                .HasForeignKey(colaborador => colaborador.IdEstadoLaboral)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Colaborador_EstadoLaboral");

            entidad.HasOne(colaborador => colaborador.Departamento)
                .WithMany()
                .HasForeignKey(colaborador => colaborador.IdDepartamento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Colaborador_Departamento");

            entidad.HasOne(colaborador => colaborador.Puesto)
                .WithMany()
                .HasForeignKey(colaborador => colaborador.IdPuesto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Colaborador_Puesto");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(colaborador => colaborador.IdUsuario)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Colaborador_AspNetUsers");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(colaborador => colaborador.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Colaborador_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(colaborador => colaborador.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Colaborador_ModificadoPor");
        });

        builder.Entity<Contrato>(entidad =>
        {
            entidad.ToTable("Contrato");

            entidad.HasKey(contrato => contrato.IdContrato);

            entidad.Property(contrato => contrato.IdContrato)
                .ValueGeneratedOnAdd();

            entidad.Property(contrato => contrato.TipoContrato)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(contrato => contrato.FechaInicio)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(contrato => contrato.FechaFin)
                .HasColumnType("date");

            entidad.Property(contrato => contrato.SalarioBase)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            entidad.Property(contrato => contrato.Jornada)
                .HasMaxLength(100);

            entidad.Property(contrato => contrato.PeriodicidadPago)
                .HasColumnType("enum('Mensual','Quincenal')")
                .HasDefaultValue(PeriodicidadesPago.Quincenal)
                .IsRequired();

            entidad.Property(contrato => contrato.EstadoContrato)
                .HasColumnType("enum('Activo','Finalizado','Suspendido','Anulado')")
                .HasDefaultValue(EstadosContrato.Activo)
                .IsRequired();

            entidad.Property(contrato => contrato.Observaciones)
                .HasMaxLength(500);

            entidad.Property(contrato => contrato.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(contrato => contrato.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(contrato => contrato.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(contrato => contrato.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(contrato => contrato.IdColaborador)
                .HasDatabaseName("IX_Contrato_IdColaborador");

            entidad.HasIndex(contrato => contrato.FechaInicio)
                .HasDatabaseName("IX_Contrato_FechaInicio");

            entidad.HasIndex(contrato => contrato.EstadoContrato)
                .HasDatabaseName("IX_Contrato_EstadoContrato");

            entidad.HasOne(contrato => contrato.Colaborador)
                .WithMany()
                .HasForeignKey(contrato => contrato.IdColaborador)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Contrato_Colaborador");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(contrato => contrato.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Contrato_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(contrato => contrato.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Contrato_ModificadoPor");
        });

        builder.Entity<TipoDocumento>(entidad =>
        {
            entidad.ToTable("TipoDocumento");

            entidad.HasKey(tipo => tipo.IdTipoDocumento);

            entidad.Property(tipo => tipo.IdTipoDocumento)
                .ValueGeneratedOnAdd();

            entidad.Property(tipo => tipo.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(tipo => tipo.Descripcion)
                .HasMaxLength(255);

            entidad.Property(tipo => tipo.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(tipo => tipo.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_TipoDocumento_Nombre");
        });

        builder.Entity<DocumentoArchivo>(entidad =>
        {
            entidad.ToTable("DocumentoArchivo");

            entidad.HasKey(documento => documento.IdDocumentoArchivo);

            entidad.Property(documento => documento.IdDocumentoArchivo)
                .ValueGeneratedOnAdd();

            entidad.Property(documento => documento.EntidadRelacionada)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(documento => documento.NombreOriginal)
                .HasMaxLength(255)
                .IsRequired();

            entidad.Property(documento => documento.NombreAlmacenado)
                .HasMaxLength(255)
                .IsRequired();

            entidad.Property(documento => documento.RutaRelativa)
                .HasMaxLength(500)
                .IsRequired();

            entidad.Property(documento => documento.MimeType)
                .HasMaxLength(150);

            entidad.Property(documento => documento.FechaCarga)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(documento => documento.CargadoPor)
                .HasMaxLength(255);

            entidad.Property(documento => documento.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(documento => new
                {
                    documento.EntidadRelacionada,
                    documento.IdEntidadRelacionada
                })
                .HasDatabaseName("IX_DocumentoArchivo_Entidad");

            entidad.HasIndex(documento => documento.IdTipoDocumento)
                .HasDatabaseName("IX_DocumentoArchivo_IdTipoDocumento");

            entidad.HasIndex(documento => documento.FechaCarga)
                .HasDatabaseName("IX_DocumentoArchivo_FechaCarga");

            entidad.HasOne(documento => documento.TipoDocumento)
                .WithMany()
                .HasForeignKey(documento => documento.IdTipoDocumento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DocumentoArchivo_TipoDocumento");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(documento => documento.CargadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_DocumentoArchivo_CargadoPor");
        });

        builder.Entity<ParametroPlanilla>(entidad =>
        {
            entidad.ToTable("ParametroPlanilla");

            entidad.HasKey(parametro => parametro.IdParametroPlanilla);

            entidad.Property(parametro => parametro.IdParametroPlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(parametro => parametro.Codigo)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(parametro => parametro.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(parametro => parametro.Descripcion)
                .HasMaxLength(300);

            entidad.Property(parametro => parametro.TipoParametro)
                .HasColumnType("enum('Porcentaje','Monto','Cantidad','Texto')")
                .HasDefaultValue(TiposParametroPlanilla.Porcentaje)
                .IsRequired();

            entidad.Property(parametro => parametro.Naturaleza)
                .HasColumnType("enum('Deduccion','Beneficio')");

            entidad.Property(parametro => parametro.ValorDecimal)
                .HasPrecision(18, 4);

            entidad.Property(parametro => parametro.ValorTexto)
                .HasMaxLength(255);

            entidad.Property(parametro => parametro.EsEditable)
                .HasDefaultValue(true)
                .IsRequired();

            entidad.Property(parametro => parametro.FechaVigenciaInicio)
                .HasColumnType("date");

            entidad.Property(parametro => parametro.FechaVigenciaFin)
                .HasColumnType("date");

            entidad.Property(parametro => parametro.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(parametro => parametro.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(parametro => parametro.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(parametro => parametro.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(parametro => parametro.Codigo)
                .IsUnique()
                .HasDatabaseName("UX_ParametroPlanilla_Codigo");

            entidad.HasIndex(parametro => parametro.EstadoRegistro)
                .HasDatabaseName("IX_ParametroPlanilla_EstadoRegistro");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(parametro => parametro.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ParametroPlanilla_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(parametro => parametro.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ParametroPlanilla_ModificadoPor");
        });

        builder.Entity<ParametroPlanillaColaborador>(entidad =>
        {
            entidad.ToTable("ParametroPlanillaColaborador");

            entidad.HasKey(asignacion => asignacion.IdParametroPlanillaColaborador);

            entidad.Property(asignacion => asignacion.IdParametroPlanillaColaborador)
                .ValueGeneratedOnAdd();

            entidad.Property(asignacion => asignacion.ValorDecimalOverride)
                .HasPrecision(18, 4);

            entidad.Property(asignacion => asignacion.ValorTextoOverride)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(asignacion => asignacion.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(asignacion => new
                {
                    asignacion.IdParametroPlanilla,
                    asignacion.IdColaborador
                })
                .IsUnique()
                .HasDatabaseName("UX_ParametroPlanillaColaborador_Parametro_Colaborador");

            entidad.HasOne(asignacion => asignacion.ParametroPlanilla)
                .WithMany(parametro => parametro.AsignacionesColaborador)
                .HasForeignKey(asignacion => asignacion.IdParametroPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ParametroPlanillaColaborador_ParametroPlanilla");

            entidad.HasOne(asignacion => asignacion.Colaborador)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.IdColaborador)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ParametroPlanillaColaborador_Colaborador");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(asignacion => asignacion.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ParametroPlanillaColaborador_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(asignacion => asignacion.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ParametroPlanillaColaborador_ModificadoPor");
        });

        builder.Entity<ParametroPlanillaPeriodo>(entidad =>
        {
            entidad.ToTable("ParametroPlanillaPeriodo");

            entidad.HasKey(asignacion => asignacion.IdParametroPlanillaPeriodo);

            entidad.Property(asignacion => asignacion.IdParametroPlanillaPeriodo)
                .ValueGeneratedOnAdd();

            entidad.Property(asignacion => asignacion.ValorDecimalOverride)
                .HasPrecision(18, 4);

            entidad.Property(asignacion => asignacion.ValorTextoOverride)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(asignacion => asignacion.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(asignacion => new
                {
                    asignacion.IdParametroPlanilla,
                    asignacion.IdPeriodoPlanilla
                })
                .IsUnique()
                .HasDatabaseName("UX_ParametroPlanillaPeriodo_Parametro_Periodo");

            entidad.HasOne(asignacion => asignacion.ParametroPlanilla)
                .WithMany(parametro => parametro.AsignacionesPeriodo)
                .HasForeignKey(asignacion => asignacion.IdParametroPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ParametroPlanillaPeriodo_ParametroPlanilla");

            entidad.HasOne(asignacion => asignacion.PeriodoPlanilla)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.IdPeriodoPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ParametroPlanillaPeriodo_PeriodoPlanilla");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(asignacion => asignacion.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ParametroPlanillaPeriodo_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(asignacion => asignacion.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ParametroPlanillaPeriodo_ModificadoPor");
        });

        builder.Entity<EstadoPlanilla>(entidad =>
        {
            entidad.ToTable("EstadoPlanilla");

            entidad.HasKey(estado => estado.IdEstadoPlanilla);

            entidad.Property(estado => estado.IdEstadoPlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(estado => estado.Nombre)
                .HasMaxLength(50)
                .IsRequired();

            entidad.Property(estado => estado.Descripcion)
                .HasMaxLength(255);

            entidad.Property(estado => estado.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(estado => estado.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_EstadoPlanilla_Nombre");
        });

        builder.Entity<TipoIncidenciaPlanilla>(entidad =>
        {
            entidad.ToTable("TipoIncidenciaPlanilla");

            entidad.HasKey(tipo => tipo.IdTipoIncidenciaPlanilla);

            entidad.Property(tipo => tipo.IdTipoIncidenciaPlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(tipo => tipo.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(tipo => tipo.Naturaleza)
                .HasColumnType("enum('Ingreso','Deduccion')")
                .IsRequired();

            entidad.Property(tipo => tipo.Descripcion)
                .HasMaxLength(255);

            entidad.Property(tipo => tipo.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(tipo => tipo.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_TipoIncidenciaPlanilla_Nombre");
        });

        builder.Entity<PeriodoPlanilla>(entidad =>
        {
            entidad.ToTable("PeriodoPlanilla");

            entidad.HasKey(periodo => periodo.IdPeriodoPlanilla);

            entidad.Property(periodo => periodo.IdPeriodoPlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(periodo => periodo.CodigoPeriodo)
                .HasMaxLength(30)
                .IsRequired();

            entidad.Property(periodo => periodo.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(periodo => periodo.TipoPeriodo)
                .HasColumnType("enum('Mensual','Quincenal')")
                .HasDefaultValue("Quincenal")
                .IsRequired();

            entidad.Property(periodo => periodo.FechaInicio)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(periodo => periodo.FechaFin)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(periodo => periodo.Observaciones)
                .HasMaxLength(500);

            entidad.Property(periodo => periodo.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(periodo => periodo.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(periodo => periodo.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(periodo => periodo.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(periodo => periodo.CodigoPeriodo)
                .IsUnique()
                .HasDatabaseName("UX_PeriodoPlanilla_CodigoPeriodo");

            entidad.HasIndex(periodo => new
                {
                    periodo.FechaInicio,
                    periodo.FechaFin
                })
                .HasDatabaseName("IX_PeriodoPlanilla_Fechas");

            entidad.HasIndex(periodo => periodo.IdEstadoPlanilla)
                .HasDatabaseName("IX_PeriodoPlanilla_IdEstadoPlanilla");

            entidad.HasOne(periodo => periodo.EstadoPlanilla)
                .WithMany()
                .HasForeignKey(periodo => periodo.IdEstadoPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PeriodoPlanilla_EstadoPlanilla");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(periodo => periodo.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PeriodoPlanilla_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(periodo => periodo.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PeriodoPlanilla_ModificadoPor");
        });

        builder.Entity<Planilla>(entidad =>
        {
            entidad.ToTable("Planilla");

            entidad.HasKey(planilla => planilla.IdPlanilla);

            entidad.Property(planilla => planilla.IdPlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(planilla => planilla.FechaCalculo);

            entidad.Property(planilla => planilla.SalarioBrutoTotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            entidad.Property(planilla => planilla.DeduccionesTotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            entidad.Property(planilla => planilla.SalarioNetoTotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            entidad.Property(planilla => planilla.CostoPatronalEstimadoTotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            entidad.Property(planilla => planilla.AprobadoPor)
                .HasMaxLength(255);

            entidad.Property(planilla => planilla.CerradoPor)
                .HasMaxLength(255);

            entidad.Property(planilla => planilla.Observaciones)
                .HasMaxLength(500);

            entidad.Property(planilla => planilla.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(planilla => planilla.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(planilla => planilla.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(planilla => planilla.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(planilla => planilla.IdPeriodoPlanilla)
                .IsUnique()
                .HasDatabaseName("UX_Planilla_IdPeriodoPlanilla");

            entidad.HasIndex(planilla => planilla.IdEstadoPlanilla)
                .HasDatabaseName("IX_Planilla_IdEstadoPlanilla");

            entidad.HasOne(planilla => planilla.PeriodoPlanilla)
                .WithOne(periodo => periodo.Planilla)
                .HasForeignKey<Planilla>(planilla => planilla.IdPeriodoPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Planilla_PeriodoPlanilla");

            entidad.HasOne(planilla => planilla.EstadoPlanilla)
                .WithMany()
                .HasForeignKey(planilla => planilla.IdEstadoPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Planilla_EstadoPlanilla");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(planilla => planilla.AprobadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Planilla_AprobadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(planilla => planilla.CerradoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Planilla_CerradoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(planilla => planilla.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Planilla_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(planilla => planilla.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Planilla_ModificadoPor");
        });

        builder.Entity<DetallePlanilla>(entidad =>
        {
            entidad.ToTable("DetallePlanilla");

            entidad.HasKey(detalle => detalle.IdDetallePlanilla);

            entidad.Property(detalle => detalle.IdDetallePlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(detalle => detalle.SalarioBase).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.SalarioProporcional).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.TotalHorasExtra).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.TotalBonos).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.TotalBeneficiosConfigurables)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();
            entidad.Property(detalle => detalle.TotalAusencias).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.SalarioBruto).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.TotalDeducciones).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.SalarioNeto).HasPrecision(18, 2).IsRequired();
            entidad.Property(detalle => detalle.CostoPatronalEstimado).HasPrecision(18, 2).IsRequired();

            entidad.Property(detalle => detalle.Observaciones)
                .HasMaxLength(500);

            entidad.Property(detalle => detalle.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.HasIndex(detalle => new { detalle.IdPlanilla, detalle.IdColaborador })
                .IsUnique()
                .HasDatabaseName("UX_DetallePlanilla_Planilla_Colaborador");

            entidad.HasIndex(detalle => detalle.IdColaborador)
                .HasDatabaseName("IX_DetallePlanilla_IdColaborador");

            entidad.HasOne(detalle => detalle.Planilla)
                .WithMany(planilla => planilla.Detalles)
                .HasForeignKey(detalle => detalle.IdPlanilla)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DetallePlanilla_Planilla");

            entidad.HasOne(detalle => detalle.Colaborador)
                .WithMany()
                .HasForeignKey(detalle => detalle.IdColaborador)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DetallePlanilla_Colaborador");
        });

        builder.Entity<IncidenciaPlanilla>(entidad =>
        {
            entidad.ToTable("IncidenciaPlanilla");

            entidad.HasKey(incidencia => incidencia.IdIncidenciaPlanilla);

            entidad.Property(incidencia => incidencia.IdIncidenciaPlanilla)
                .ValueGeneratedOnAdd();

            entidad.Property(incidencia => incidencia.FechaIncidencia)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(incidencia => incidencia.Cantidad)
                .HasPrecision(18, 2);

            entidad.Property(incidencia => incidencia.Monto)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            entidad.Property(incidencia => incidencia.Descripcion)
                .HasMaxLength(300);

            entidad.Property(incidencia => incidencia.RegistradoPor)
                .HasMaxLength(255);

            entidad.Property(incidencia => incidencia.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(incidencia => incidencia.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue("Activo")
                .IsRequired();

            entidad.HasIndex(incidencia => incidencia.IdPeriodoPlanilla)
                .HasDatabaseName("IX_IncidenciaPlanilla_IdPeriodoPlanilla");

            entidad.HasIndex(incidencia => incidencia.IdColaborador)
                .HasDatabaseName("IX_IncidenciaPlanilla_IdColaborador");

            entidad.HasIndex(incidencia => incidencia.IdTipoIncidenciaPlanilla)
                .HasDatabaseName("IX_IncidenciaPlanilla_IdTipoIncidenciaPlanilla");

            entidad.HasIndex(incidencia => incidencia.FechaIncidencia)
                .HasDatabaseName("IX_IncidenciaPlanilla_FechaIncidencia");

            entidad.HasOne(incidencia => incidencia.PeriodoPlanilla)
                .WithMany()
                .HasForeignKey(incidencia => incidencia.IdPeriodoPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_IncidenciaPlanilla_PeriodoPlanilla");

            entidad.HasOne(incidencia => incidencia.Colaborador)
                .WithMany()
                .HasForeignKey(incidencia => incidencia.IdColaborador)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_IncidenciaPlanilla_Colaborador");

            entidad.HasOne(incidencia => incidencia.TipoIncidenciaPlanilla)
                .WithMany()
                .HasForeignKey(incidencia => incidencia.IdTipoIncidenciaPlanilla)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_IncidenciaPlanilla_TipoIncidencia");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(incidencia => incidencia.RegistradoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_IncidenciaPlanilla_RegistradoPor");
        });

        builder.Entity<BitacoraAuditoria>(entidad =>
        {
            entidad.ToTable("BitacoraAuditoria");

            entidad.HasKey(registro => registro.IdBitacoraAuditoria);

            entidad.Property(registro => registro.IdBitacoraAuditoria)
                .ValueGeneratedOnAdd();

            entidad.Property(registro => registro.IdUsuario)
                .HasMaxLength(255);

            entidad.Property(registro => registro.FechaHora)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(registro => registro.Accion)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(registro => registro.Entidad)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(registro => registro.IdRegistro)
                .HasMaxLength(100);

            entidad.Property(registro => registro.ValoresAnteriores)
                .HasColumnType("json");

            entidad.Property(registro => registro.ValoresNuevos)
                .HasColumnType("json");

            entidad.Property(registro => registro.DireccionIP)
                .HasMaxLength(45);

            entidad.Property(registro => registro.Observacion)
                .HasMaxLength(500);

            entidad.HasIndex(registro => registro.IdUsuario)
                .HasDatabaseName("IX_BitacoraAuditoria_IdUsuario");

            entidad.HasIndex(registro => registro.FechaHora)
                .HasDatabaseName("IX_BitacoraAuditoria_FechaHora");

            entidad.HasIndex(registro => registro.Entidad)
                .HasDatabaseName("IX_BitacoraAuditoria_Entidad");

            entidad.HasIndex(registro => registro.Accion)
                .HasDatabaseName("IX_BitacoraAuditoria_Accion");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(registro => registro.IdUsuario)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BitacoraAuditoria_AspNetUsers");
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

        builder.Entity<EstadoActivo>(entidad =>
        {
            entidad.ToTable("EstadoActivo");

            entidad.HasKey(estado => estado.IdEstadoActivo);

            entidad.Property(estado => estado.IdEstadoActivo)
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
                .HasDatabaseName("UX_EstadoActivo_Nombre");
        });

        builder.Entity<TipoActivo>(entidad =>
        {
            entidad.ToTable("TipoActivo");

            entidad.HasKey(tipo => tipo.IdTipoActivo);

            entidad.Property(tipo => tipo.IdTipoActivo)
                .ValueGeneratedOnAdd();

            entidad.Property(tipo => tipo.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(tipo => tipo.Descripcion)
                .HasMaxLength(255);

            entidad.Property(tipo => tipo.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(tipo => tipo.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_TipoActivo_Nombre");
        });

        builder.Entity<CategoriaActivo>(entidad =>
        {
            entidad.ToTable("CategoriaActivo");

            entidad.HasKey(categoria => categoria.IdCategoriaActivo);

            entidad.Property(categoria => categoria.IdCategoriaActivo)
                .ValueGeneratedOnAdd();

            entidad.Property(categoria => categoria.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(categoria => categoria.Descripcion)
                .HasMaxLength(255);

            entidad.Property(categoria => categoria.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(categoria => categoria.IdTipoActivo)
                .HasDatabaseName("IX_CategoriaActivo_IdTipoActivo");

            entidad.HasIndex(categoria => categoria.Nombre)
                .HasDatabaseName("IX_CategoriaActivo_Nombre");

            entidad.HasOne(categoria => categoria.TipoActivo)
                .WithMany()
                .HasForeignKey(categoria => categoria.IdTipoActivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CategoriaActivo_TipoActivo");
        });

        builder.Entity<Activo>(entidad =>
        {
            entidad.ToTable("Activo");

            entidad.HasKey(activo => activo.IdActivo);

            entidad.Property(activo => activo.IdActivo)
                .ValueGeneratedOnAdd();

            entidad.Property(activo => activo.CodigoActivo)
                .HasMaxLength(30)
                .IsRequired();

            entidad.Property(activo => activo.NombreActivo)
                .HasMaxLength(150)
                .IsRequired();

            entidad.Property(activo => activo.Marca)
                .HasMaxLength(100);

            entidad.Property(activo => activo.Modelo)
                .HasMaxLength(100);

            entidad.Property(activo => activo.NumeroSerie)
                .HasMaxLength(100);

            entidad.Property(activo => activo.Placa)
                .HasMaxLength(30);

            entidad.Property(activo => activo.Descripcion)
                .HasMaxLength(500);

            entidad.Property(activo => activo.FechaAdquisicion)
                .HasColumnType("date");

            entidad.Property(activo => activo.ValorAdquisicion)
                .HasPrecision(18, 2);

            entidad.Property(activo => activo.UbicacionActual)
                .HasMaxLength(150);

            entidad.Property(activo => activo.Observaciones)
                .HasMaxLength(500);

            entidad.Property(activo => activo.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(activo => activo.CreadoPor)
                .HasMaxLength(255);

            entidad.Property(activo => activo.FechaModificacion);

            entidad.Property(activo => activo.ModificadoPor)
                .HasMaxLength(255);

            entidad.Property(activo => activo.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(activo => activo.CodigoActivo)
                .IsUnique()
                .HasDatabaseName("UX_Activo_CodigoActivo");

            entidad.HasIndex(activo => activo.NombreActivo)
                .HasDatabaseName("IX_Activo_NombreActivo");

            entidad.HasIndex(activo => activo.IdTipoActivo)
                .HasDatabaseName("IX_Activo_IdTipoActivo");

            entidad.HasIndex(activo => activo.IdCategoriaActivo)
                .HasDatabaseName("IX_Activo_IdCategoriaActivo");

            entidad.HasIndex(activo => activo.IdEstadoActivo)
                .HasDatabaseName("IX_Activo_IdEstadoActivo");

            entidad.HasOne(activo => activo.TipoActivo)
                .WithMany()
                .HasForeignKey(activo => activo.IdTipoActivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Activo_TipoActivo");

            entidad.HasOne(activo => activo.CategoriaActivo)
                .WithMany()
                .HasForeignKey(activo => activo.IdCategoriaActivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Activo_CategoriaActivo");

            entidad.HasOne(activo => activo.EstadoActivo)
                .WithMany()
                .HasForeignKey(activo => activo.IdEstadoActivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Activo_EstadoActivo");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(activo => activo.CreadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Activo_CreadoPor");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(activo => activo.ModificadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Activo_ModificadoPor");
        });

        builder.Entity<AsignacionActivoProyecto>(entidad =>
        {
            entidad.ToTable("AsignacionActivoProyecto");

            entidad.HasKey(asignacion => asignacion.IdAsignacionActivoProyecto);

            entidad.Property(asignacion => asignacion.IdAsignacionActivoProyecto)
                .ValueGeneratedOnAdd();

            entidad.Property(asignacion => asignacion.FechaInicio)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(asignacion => asignacion.FechaFin)
                .HasColumnType("date")
                .IsRequired();

            entidad.Property(asignacion => asignacion.Observaciones)
                .HasMaxLength(500);

            entidad.Property(asignacion => asignacion.AsignadoPor)
                .HasMaxLength(255);

            entidad.Property(asignacion => asignacion.FechaAsignacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entidad.Property(asignacion => asignacion.EstadoRegistro)
                .HasColumnType("enum('Activo','Inactivo')")
                .HasDefaultValue(EstadosRegistro.Activo)
                .IsRequired();

            entidad.HasIndex(asignacion => asignacion.IdActivo)
                .HasDatabaseName("IX_AsignacionActivoProyecto_IdActivo");

            entidad.HasIndex(asignacion => asignacion.IdProyecto)
                .HasDatabaseName("IX_AsignacionActivoProyecto_IdProyecto");

            entidad.HasIndex(asignacion => new { asignacion.FechaInicio, asignacion.FechaFin })
                .HasDatabaseName("IX_AsignacionActivoProyecto_Fechas");

            entidad.HasOne(asignacion => asignacion.Activo)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.IdActivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsignacionActivoProyecto_Activo");

            entidad.HasOne(asignacion => asignacion.Proyecto)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.IdProyecto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsignacionActivoProyecto_Proyecto");

            entidad.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(asignacion => asignacion.AsignadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AsignacionActivoProyecto_AsignadoPor");
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

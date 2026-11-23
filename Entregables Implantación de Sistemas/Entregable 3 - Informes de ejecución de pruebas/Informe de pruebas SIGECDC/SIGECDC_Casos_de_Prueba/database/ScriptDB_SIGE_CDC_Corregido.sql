/* ============================================================
   SIGE-CDC - Script inicial de base de datos
   Base de datos: SIGE_CDC_DB
   Nota: Durante el desarrollo de la base de datos se usó ChatGpt para
   corregir errores, resivir sugerencias y mantener la consistencia
   del script con la documentación del proyecto
   ============================================================ */

CREATE DATABASE IF NOT EXISTS SIGE_CDC_DB
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE SIGE_CDC_DB;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

/* ============================================================
   1. TABLAS COMPATIBLES CON ASP.NET CORE IDENTITY
   ============================================================ */

CREATE TABLE IF NOT EXISTS AspNetRoles (
    Id VARCHAR(255) NOT NULL,
    Name VARCHAR(256) NULL,
    NormalizedName VARCHAR(256) NULL,
    ConcurrencyStamp LONGTEXT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_AspNetRoles_NormalizedName (NormalizedName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AspNetUsers (
    Id VARCHAR(255) NOT NULL,
    UserName VARCHAR(256) NULL,
    NormalizedUserName VARCHAR(256) NULL,
    Email VARCHAR(256) NULL,
    NormalizedEmail VARCHAR(256) NULL,
    EmailConfirmed TINYINT(1) NOT NULL DEFAULT 0,
    PasswordHash LONGTEXT NULL,
    SecurityStamp LONGTEXT NULL,
    ConcurrencyStamp LONGTEXT NULL,
    PhoneNumber LONGTEXT NULL,
    PhoneNumberConfirmed TINYINT(1) NOT NULL DEFAULT 0,
    TwoFactorEnabled TINYINT(1) NOT NULL DEFAULT 0,
    LockoutEnd DATETIME(6) NULL,
    LockoutEnabled TINYINT(1) NOT NULL DEFAULT 1,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaModificacion DATETIME NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_AspNetUsers_NormalizedUserName (NormalizedUserName),
    KEY IX_AspNetUsers_NormalizedEmail (NormalizedEmail),
    KEY IX_AspNetUsers_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AspNetRoleClaims (
    Id INT NOT NULL AUTO_INCREMENT,
    RoleId VARCHAR(255) NOT NULL,
    ClaimType LONGTEXT NULL,
    ClaimValue LONGTEXT NULL,
    PRIMARY KEY (Id),
    KEY IX_AspNetRoleClaims_RoleId (RoleId),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId
        FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AspNetUserClaims (
    Id INT NOT NULL AUTO_INCREMENT,
    UserId VARCHAR(255) NOT NULL,
    ClaimType LONGTEXT NULL,
    ClaimValue LONGTEXT NULL,
    PRIMARY KEY (Id),
    KEY IX_AspNetUserClaims_UserId (UserId),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AspNetUserLogins (
    LoginProvider VARCHAR(128) NOT NULL,
    ProviderKey VARCHAR(128) NOT NULL,
    ProviderDisplayName LONGTEXT NULL,
    UserId VARCHAR(255) NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    KEY IX_AspNetUserLogins_UserId (UserId),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AspNetUserRoles (
    UserId VARCHAR(255) NOT NULL,
    RoleId VARCHAR(255) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    KEY IX_AspNetUserRoles_RoleId (RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId
        FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AspNetUserTokens (
    UserId VARCHAR(255) NOT NULL,
    LoginProvider VARCHAR(128) NOT NULL,
    Name VARCHAR(128) NOT NULL,
    Value LONGTEXT NULL,
    PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   2. CATÁLOGOS GENERALES
   ============================================================ */

CREATE TABLE IF NOT EXISTS EstadoLaboral (
    IdEstadoLaboral INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdEstadoLaboral),
    UNIQUE KEY UX_EstadoLaboral_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS EstadoProyecto (
    IdEstadoProyecto INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdEstadoProyecto),
    UNIQUE KEY UX_EstadoProyecto_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS EstadoActivo (
    IdEstadoActivo INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdEstadoActivo),
    UNIQUE KEY UX_EstadoActivo_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS EstadoMantenimiento (
    IdEstadoMantenimiento INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdEstadoMantenimiento),
    UNIQUE KEY UX_EstadoMantenimiento_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS EstadoPlanilla (
    IdEstadoPlanilla INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdEstadoPlanilla),
    UNIQUE KEY UX_EstadoPlanilla_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS TipoDocumento (
    IdTipoDocumento INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdTipoDocumento),
    UNIQUE KEY UX_TipoDocumento_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS TipoIncidenciaPlanilla (
    IdTipoIncidenciaPlanilla INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Naturaleza ENUM('Ingreso','Deduccion') NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdTipoIncidenciaPlanilla),
    UNIQUE KEY UX_TipoIncidenciaPlanilla_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS TipoMedicionUso (
    IdTipoMedicionUso INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdTipoMedicionUso),
    UNIQUE KEY UX_TipoMedicionUso_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS TipoActivo (
    IdTipoActivo INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdTipoActivo),
    UNIQUE KEY UX_TipoActivo_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS CategoriaActivo (
    IdCategoriaActivo INT NOT NULL AUTO_INCREMENT,
    IdTipoActivo INT NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdCategoriaActivo),
    UNIQUE KEY UX_CategoriaActivo_Tipo_Nombre (IdTipoActivo, Nombre),
    KEY IX_CategoriaActivo_IdTipoActivo (IdTipoActivo),
    CONSTRAINT FK_CategoriaActivo_TipoActivo
        FOREIGN KEY (IdTipoActivo) REFERENCES TipoActivo(IdTipoActivo)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   3. RECURSOS HUMANOS
   ============================================================ */

CREATE TABLE IF NOT EXISTS Departamento (
    IdDepartamento INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdDepartamento),
    UNIQUE KEY UX_Departamento_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Puesto (
    IdPuesto INT NOT NULL AUTO_INCREMENT,
    IdDepartamento INT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdPuesto),
    UNIQUE KEY UX_Puesto_Nombre (Nombre),
    KEY IX_Puesto_IdDepartamento (IdDepartamento),
    CONSTRAINT FK_Puesto_Departamento
        FOREIGN KEY (IdDepartamento) REFERENCES Departamento(IdDepartamento)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Colaborador (
    IdColaborador BIGINT NOT NULL AUTO_INCREMENT,
    CodigoColaborador VARCHAR(20) NOT NULL,
    TipoIdentificacion VARCHAR(30) NOT NULL,
    Identificacion VARCHAR(30) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    PrimerApellido VARCHAR(100) NOT NULL,
    SegundoApellido VARCHAR(100) NULL,
    FechaNacimiento DATE NULL,
    CorreoElectronico VARCHAR(150) NULL,
    Telefono VARCHAR(30) NULL,
    Direccion VARCHAR(300) NULL,
    FechaIngreso DATE NOT NULL,
    FechaSalida DATE NULL,
    IdEstadoLaboral INT NOT NULL,
    IdDepartamento INT NOT NULL,
    IdPuesto INT NOT NULL,
    IdUsuario VARCHAR(255) NULL,
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdColaborador),
    UNIQUE KEY UX_Colaborador_Codigo (CodigoColaborador),
    UNIQUE KEY UX_Colaborador_Identificacion (Identificacion),
    UNIQUE KEY UX_Colaborador_IdUsuario (IdUsuario),
    KEY IX_Colaborador_IdEstadoLaboral (IdEstadoLaboral),
    KEY IX_Colaborador_IdDepartamento (IdDepartamento),
    KEY IX_Colaborador_IdPuesto (IdPuesto),
    KEY IX_Colaborador_Nombre (Nombre, PrimerApellido, SegundoApellido),
    CONSTRAINT FK_Colaborador_EstadoLaboral
        FOREIGN KEY (IdEstadoLaboral) REFERENCES EstadoLaboral(IdEstadoLaboral)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Colaborador_Departamento
        FOREIGN KEY (IdDepartamento) REFERENCES Departamento(IdDepartamento)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Colaborador_Puesto
        FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Colaborador_AspNetUsers
        FOREIGN KEY (IdUsuario) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Colaborador_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Colaborador_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Contrato (
    IdContrato BIGINT NOT NULL AUTO_INCREMENT,
    IdColaborador BIGINT NOT NULL,
    TipoContrato VARCHAR(100) NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NULL,
    SalarioBase DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Jornada VARCHAR(100) NULL,
    PeriodicidadPago ENUM('Mensual','Quincenal') NOT NULL DEFAULT 'Quincenal',
    EstadoContrato ENUM('Activo','Finalizado','Suspendido','Anulado') NOT NULL DEFAULT 'Activo',
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdContrato),
    KEY IX_Contrato_IdColaborador (IdColaborador),
    KEY IX_Contrato_FechaInicio (FechaInicio),
    KEY IX_Contrato_EstadoContrato (EstadoContrato),
    CONSTRAINT FK_Contrato_Colaborador
        FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Contrato_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Contrato_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   4. DOCUMENTOS Y ARCHIVOS
   Nota: no almacena binarios. Solo metadatos.
   ============================================================ */

CREATE TABLE IF NOT EXISTS DocumentoArchivo (
    IdDocumentoArchivo BIGINT NOT NULL AUTO_INCREMENT,
    EntidadRelacionada VARCHAR(100) NOT NULL,
    IdEntidadRelacionada BIGINT NOT NULL,
    IdTipoDocumento INT NOT NULL,
    NombreOriginal VARCHAR(255) NOT NULL,
    NombreAlmacenado VARCHAR(255) NOT NULL,
    RutaRelativa VARCHAR(500) NOT NULL,
    MimeType VARCHAR(150) NULL,
    TamanoBytes BIGINT NULL,
    FechaCarga DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CargadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdDocumentoArchivo),
    KEY IX_DocumentoArchivo_Entidad (EntidadRelacionada, IdEntidadRelacionada),
    KEY IX_DocumentoArchivo_IdTipoDocumento (IdTipoDocumento),
    KEY IX_DocumentoArchivo_FechaCarga (FechaCarga),
    CONSTRAINT FK_DocumentoArchivo_TipoDocumento
        FOREIGN KEY (IdTipoDocumento) REFERENCES TipoDocumento(IdTipoDocumento)
        ON DELETE RESTRICT,
    CONSTRAINT FK_DocumentoArchivo_CargadoPor
        FOREIGN KEY (CargadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   5. PLANILLAS
   ============================================================ */

CREATE TABLE IF NOT EXISTS ParametroPlanilla (
    IdParametroPlanilla INT NOT NULL AUTO_INCREMENT,
    Codigo VARCHAR(100) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(300) NULL,
    TipoParametro ENUM('Porcentaje','Monto','Cantidad','Texto') NOT NULL DEFAULT 'Porcentaje',
    Naturaleza ENUM('Deduccion','Beneficio') NULL,
    ValorDecimal DECIMAL(18,4) NULL,
    ValorTexto VARCHAR(255) NULL,
    EsEditable TINYINT(1) NOT NULL DEFAULT 1,
    FechaVigenciaInicio DATE NULL,
    FechaVigenciaFin DATE NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdParametroPlanilla),
    UNIQUE KEY UX_ParametroPlanilla_Codigo (Codigo),
    KEY IX_ParametroPlanilla_EstadoRegistro (EstadoRegistro),
    CONSTRAINT FK_ParametroPlanilla_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_ParametroPlanilla_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS PeriodoPlanilla (
    IdPeriodoPlanilla BIGINT NOT NULL AUTO_INCREMENT,
    CodigoPeriodo VARCHAR(30) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    TipoPeriodo ENUM('Mensual','Quincenal') NOT NULL DEFAULT 'Quincenal',
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    IdEstadoPlanilla INT NOT NULL,
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdPeriodoPlanilla),
    UNIQUE KEY UX_PeriodoPlanilla_CodigoPeriodo (CodigoPeriodo),
    KEY IX_PeriodoPlanilla_Fechas (FechaInicio, FechaFin),
    KEY IX_PeriodoPlanilla_IdEstadoPlanilla (IdEstadoPlanilla),
    CONSTRAINT FK_PeriodoPlanilla_EstadoPlanilla
        FOREIGN KEY (IdEstadoPlanilla) REFERENCES EstadoPlanilla(IdEstadoPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_PeriodoPlanilla_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_PeriodoPlanilla_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ParametroPlanillaColaborador (
    IdParametroPlanillaColaborador BIGINT NOT NULL AUTO_INCREMENT,
    IdParametroPlanilla INT NOT NULL,
    IdColaborador BIGINT NOT NULL,
    ValorDecimalOverride DECIMAL(18,4) NULL,
    ValorTextoOverride VARCHAR(255) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdParametroPlanillaColaborador),
    UNIQUE KEY UX_ParametroPlanillaColaborador_Parametro_Colaborador
        (IdParametroPlanilla, IdColaborador),
    KEY IX_ParametroPlanillaColaborador_IdColaborador (IdColaborador),
    CONSTRAINT FK_ParametroPlanillaColaborador_ParametroPlanilla
        FOREIGN KEY (IdParametroPlanilla) REFERENCES ParametroPlanilla(IdParametroPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_ParametroPlanillaColaborador_Colaborador
        FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador)
        ON DELETE RESTRICT,
    CONSTRAINT FK_ParametroPlanillaColaborador_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_ParametroPlanillaColaborador_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ParametroPlanillaPeriodo (
    IdParametroPlanillaPeriodo BIGINT NOT NULL AUTO_INCREMENT,
    IdParametroPlanilla INT NOT NULL,
    IdPeriodoPlanilla BIGINT NOT NULL,
    ValorDecimalOverride DECIMAL(18,4) NULL,
    ValorTextoOverride VARCHAR(255) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdParametroPlanillaPeriodo),
    UNIQUE KEY UX_ParametroPlanillaPeriodo_Parametro_Periodo
        (IdParametroPlanilla, IdPeriodoPlanilla),
    KEY IX_ParametroPlanillaPeriodo_IdPeriodoPlanilla (IdPeriodoPlanilla),
    CONSTRAINT FK_ParametroPlanillaPeriodo_ParametroPlanilla
        FOREIGN KEY (IdParametroPlanilla) REFERENCES ParametroPlanilla(IdParametroPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_ParametroPlanillaPeriodo_PeriodoPlanilla
        FOREIGN KEY (IdPeriodoPlanilla) REFERENCES PeriodoPlanilla(IdPeriodoPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_ParametroPlanillaPeriodo_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_ParametroPlanillaPeriodo_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Planilla (
    IdPlanilla BIGINT NOT NULL AUTO_INCREMENT,
    IdPeriodoPlanilla BIGINT NOT NULL,
    IdEstadoPlanilla INT NOT NULL,
    FechaCalculo DATETIME NULL,
    SalarioBrutoTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    DeduccionesTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    SalarioNetoTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CostoPatronalEstimadoTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    AprobadoPor VARCHAR(255) NULL,
    FechaAprobacion DATETIME NULL,
    CerradoPor VARCHAR(255) NULL,
    FechaCierre DATETIME NULL,
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdPlanilla),
    UNIQUE KEY UX_Planilla_IdPeriodoPlanilla (IdPeriodoPlanilla),
    KEY IX_Planilla_IdEstadoPlanilla (IdEstadoPlanilla),
    CONSTRAINT FK_Planilla_PeriodoPlanilla
        FOREIGN KEY (IdPeriodoPlanilla) REFERENCES PeriodoPlanilla(IdPeriodoPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Planilla_EstadoPlanilla
        FOREIGN KEY (IdEstadoPlanilla) REFERENCES EstadoPlanilla(IdEstadoPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Planilla_AprobadoPor
        FOREIGN KEY (AprobadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Planilla_CerradoPor
        FOREIGN KEY (CerradoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Planilla_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Planilla_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS IncidenciaPlanilla (
    IdIncidenciaPlanilla BIGINT NOT NULL AUTO_INCREMENT,
    IdPeriodoPlanilla BIGINT NOT NULL,
    IdColaborador BIGINT NOT NULL,
    IdTipoIncidenciaPlanilla INT NOT NULL,
    FechaIncidencia DATE NOT NULL,
    Cantidad DECIMAL(18,2) NULL,
    Monto DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Descripcion VARCHAR(300) NULL,
    RegistradoPor VARCHAR(255) NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdIncidenciaPlanilla),
    KEY IX_IncidenciaPlanilla_IdPeriodoPlanilla (IdPeriodoPlanilla),
    KEY IX_IncidenciaPlanilla_IdColaborador (IdColaborador),
    KEY IX_IncidenciaPlanilla_IdTipoIncidenciaPlanilla (IdTipoIncidenciaPlanilla),
    KEY IX_IncidenciaPlanilla_FechaIncidencia (FechaIncidencia),
    CONSTRAINT FK_IncidenciaPlanilla_PeriodoPlanilla
        FOREIGN KEY (IdPeriodoPlanilla) REFERENCES PeriodoPlanilla(IdPeriodoPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_IncidenciaPlanilla_Colaborador
        FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador)
        ON DELETE RESTRICT,
    CONSTRAINT FK_IncidenciaPlanilla_TipoIncidencia
        FOREIGN KEY (IdTipoIncidenciaPlanilla) REFERENCES TipoIncidenciaPlanilla(IdTipoIncidenciaPlanilla)
        ON DELETE RESTRICT,
    CONSTRAINT FK_IncidenciaPlanilla_RegistradoPor
        FOREIGN KEY (RegistradoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS DetallePlanilla (
    IdDetallePlanilla BIGINT NOT NULL AUTO_INCREMENT,
    IdPlanilla BIGINT NOT NULL,
    IdColaborador BIGINT NOT NULL,
    SalarioBase DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    SalarioProporcional DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TotalHorasExtra DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TotalBonos DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TotalBeneficiosConfigurables DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TotalAusencias DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    SalarioBruto DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TotalDeducciones DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    SalarioNeto DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CostoPatronalEstimado DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (IdDetallePlanilla),
    UNIQUE KEY UX_DetallePlanilla_Planilla_Colaborador (IdPlanilla, IdColaborador),
    KEY IX_DetallePlanilla_IdColaborador (IdColaborador),
    CONSTRAINT FK_DetallePlanilla_Planilla
        FOREIGN KEY (IdPlanilla) REFERENCES Planilla(IdPlanilla)
        ON DELETE CASCADE,
    CONSTRAINT FK_DetallePlanilla_Colaborador
        FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ColillaPago (
    IdColillaPago BIGINT NOT NULL AUTO_INCREMENT,
    IdDetallePlanilla BIGINT NOT NULL,
    CodigoColilla VARCHAR(50) NOT NULL,
    RutaArchivo VARCHAR(500) NULL,
    FechaGeneracion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    GeneradoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdColillaPago),
    UNIQUE KEY UX_ColillaPago_IdDetallePlanilla (IdDetallePlanilla),
    UNIQUE KEY UX_ColillaPago_CodigoColilla (CodigoColilla),
    CONSTRAINT FK_ColillaPago_DetallePlanilla
        FOREIGN KEY (IdDetallePlanilla) REFERENCES DetallePlanilla(IdDetallePlanilla)
        ON DELETE CASCADE,
    CONSTRAINT FK_ColillaPago_GeneradoPor
        FOREIGN KEY (GeneradoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   6. PROYECTOS Y ACTIVOS
   ============================================================ */

CREATE TABLE IF NOT EXISTS Proyecto (
    IdProyecto BIGINT NOT NULL AUTO_INCREMENT,
    CodigoProyecto VARCHAR(30) NOT NULL,
    NombreProyecto VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    FechaInicio DATE NULL,
    FechaFinEstimada DATE NULL,
    FechaFinReal DATE NULL,
    Responsable VARCHAR(150) NULL,
    Ubicacion VARCHAR(200) NULL,
    IdEstadoProyecto INT NOT NULL,
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdProyecto),
    UNIQUE KEY UX_Proyecto_CodigoProyecto (CodigoProyecto),
    KEY IX_Proyecto_NombreProyecto (NombreProyecto),
    KEY IX_Proyecto_IdEstadoProyecto (IdEstadoProyecto),
    KEY IX_Proyecto_Fechas (FechaInicio, FechaFinEstimada),
    CONSTRAINT FK_Proyecto_EstadoProyecto
        FOREIGN KEY (IdEstadoProyecto) REFERENCES EstadoProyecto(IdEstadoProyecto)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Proyecto_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Proyecto_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Activo (
    IdActivo BIGINT NOT NULL AUTO_INCREMENT,
    CodigoActivo VARCHAR(30) NOT NULL,
    NombreActivo VARCHAR(150) NOT NULL,
    IdTipoActivo INT NOT NULL,
    IdCategoriaActivo INT NOT NULL,
    Marca VARCHAR(100) NULL,
    Modelo VARCHAR(100) NULL,
    NumeroSerie VARCHAR(100) NULL,
    Placa VARCHAR(30) NULL,
    Descripcion VARCHAR(500) NULL,
    FechaAdquisicion DATE NULL,
    ValorAdquisicion DECIMAL(18,2) NULL,
    UbicacionActual VARCHAR(150) NULL,
    IdEstadoActivo INT NOT NULL,
    IdTipoMedicionUso INT NULL,
    LecturaUsoActual DECIMAL(18,2) NULL,
    FechaUltimoMantenimiento DATE NULL,
    FechaProximoMantenimiento DATE NULL,
    IdResponsableActual BIGINT NULL,
    Observaciones VARCHAR(500) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdActivo),
    UNIQUE KEY UX_Activo_CodigoActivo (CodigoActivo),
    KEY IX_Activo_NombreActivo (NombreActivo),
    KEY IX_Activo_IdTipoActivo (IdTipoActivo),
    KEY IX_Activo_IdCategoriaActivo (IdCategoriaActivo),
    KEY IX_Activo_IdEstadoActivo (IdEstadoActivo),
    KEY IX_Activo_IdResponsableActual (IdResponsableActual),
    KEY IX_Activo_FechaProximoMantenimiento (FechaProximoMantenimiento),
    CONSTRAINT FK_Activo_TipoActivo
        FOREIGN KEY (IdTipoActivo) REFERENCES TipoActivo(IdTipoActivo)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Activo_CategoriaActivo
        FOREIGN KEY (IdCategoriaActivo) REFERENCES CategoriaActivo(IdCategoriaActivo)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Activo_EstadoActivo
        FOREIGN KEY (IdEstadoActivo) REFERENCES EstadoActivo(IdEstadoActivo)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Activo_TipoMedicionUso
        FOREIGN KEY (IdTipoMedicionUso) REFERENCES TipoMedicionUso(IdTipoMedicionUso)
        ON DELETE SET NULL,
    CONSTRAINT FK_Activo_ResponsableActual
        FOREIGN KEY (IdResponsableActual) REFERENCES Colaborador(IdColaborador)
        ON DELETE SET NULL,
    CONSTRAINT FK_Activo_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Activo_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AsignacionActivoProyecto (
    IdAsignacionActivoProyecto BIGINT NOT NULL AUTO_INCREMENT,
    IdActivo BIGINT NOT NULL,
    IdProyecto BIGINT NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    Observaciones VARCHAR(500) NULL,
    AsignadoPor VARCHAR(255) NULL,
    FechaAsignacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdAsignacionActivoProyecto),
    KEY IX_AsignacionActivoProyecto_IdActivo (IdActivo),
    KEY IX_AsignacionActivoProyecto_IdProyecto (IdProyecto),
    KEY IX_AsignacionActivoProyecto_Fechas (FechaInicio, FechaFin),
    CONSTRAINT FK_AsignacionActivoProyecto_Activo
        FOREIGN KEY (IdActivo) REFERENCES Activo(IdActivo)
        ON DELETE RESTRICT,
    CONSTRAINT FK_AsignacionActivoProyecto_Proyecto
        FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto)
        ON DELETE RESTRICT,
    CONSTRAINT FK_AsignacionActivoProyecto_AsignadoPor
        FOREIGN KEY (AsignadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS RegistroUsoActivo (
    IdRegistroUsoActivo BIGINT NOT NULL AUTO_INCREMENT,
    IdActivo BIGINT NOT NULL,
    IdProyecto BIGINT NULL,
    FechaRegistro DATE NOT NULL,
    LecturaAnterior DECIMAL(18,2) NULL,
    LecturaNueva DECIMAL(18,2) NOT NULL,
    CantidadUso DECIMAL(18,2) NULL,
    Observaciones VARCHAR(500) NULL,
    RegistradoPor VARCHAR(255) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdRegistroUsoActivo),
    KEY IX_RegistroUsoActivo_IdActivo (IdActivo),
    KEY IX_RegistroUsoActivo_IdProyecto (IdProyecto),
    KEY IX_RegistroUsoActivo_FechaRegistro (FechaRegistro),
    CONSTRAINT FK_RegistroUsoActivo_Activo
        FOREIGN KEY (IdActivo) REFERENCES Activo(IdActivo)
        ON DELETE RESTRICT,
    CONSTRAINT FK_RegistroUsoActivo_Proyecto
        FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto)
        ON DELETE SET NULL,
    CONSTRAINT FK_RegistroUsoActivo_RegistradoPor
        FOREIGN KEY (RegistradoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   7. MANTENIMIENTO
   ============================================================ */

CREATE TABLE IF NOT EXISTS Mantenimiento (
    IdMantenimiento BIGINT NOT NULL AUTO_INCREMENT,
    IdActivo BIGINT NOT NULL,
    IdProyecto BIGINT NULL,
    TipoMantenimiento ENUM('Preventivo','Correctivo') NOT NULL,
    IdEstadoMantenimiento INT NOT NULL,
    FechaProgramada DATE NOT NULL,
    FechaInicio DATETIME NULL,
    FechaFin DATETIME NULL,
    Descripcion VARCHAR(500) NULL,
    CostoEstimado DECIMAL(18,2) NULL,
    CostoReal DECIMAL(18,2) NULL,
    TiempoFueraServicioHoras DECIMAL(18,2) NULL,
    Resultado VARCHAR(500) NULL,
    Responsable VARCHAR(150) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdMantenimiento),
    KEY IX_Mantenimiento_IdActivo (IdActivo),
    KEY IX_Mantenimiento_IdProyecto (IdProyecto),
    KEY IX_Mantenimiento_IdEstadoMantenimiento (IdEstadoMantenimiento),
    KEY IX_Mantenimiento_FechaProgramada (FechaProgramada),
    CONSTRAINT FK_Mantenimiento_Activo
        FOREIGN KEY (IdActivo) REFERENCES Activo(IdActivo)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Mantenimiento_Proyecto
        FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto)
        ON DELETE SET NULL,
    CONSTRAINT FK_Mantenimiento_EstadoMantenimiento
        FOREIGN KEY (IdEstadoMantenimiento) REFERENCES EstadoMantenimiento(IdEstadoMantenimiento)
        ON DELETE RESTRICT,
    CONSTRAINT FK_Mantenimiento_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Mantenimiento_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   8. SITIO PÚBLICO Y GESTIÓN DE CONTENIDO
   ============================================================ */

CREATE TABLE IF NOT EXISTS PaginaContenido (
    IdPaginaContenido BIGINT NOT NULL AUTO_INCREMENT,
    CodigoPagina VARCHAR(100) NOT NULL,
    Titulo VARCHAR(200) NOT NULL,
    Contenido MEDIUMTEXT NULL,
    EstaPublicado TINYINT(1) NOT NULL DEFAULT 0,
    FechaPublicacion DATETIME NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdPaginaContenido),
    UNIQUE KEY UX_PaginaContenido_CodigoPagina (CodigoPagina),
    KEY IX_PaginaContenido_EstaPublicado (EstaPublicado),
    CONSTRAINT FK_PaginaContenido_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_PaginaContenido_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Noticia (
    IdNoticia BIGINT NOT NULL AUTO_INCREMENT,
    Titulo VARCHAR(200) NOT NULL,
    Resumen VARCHAR(500) NULL,
    Contenido MEDIUMTEXT NULL,
    EstaPublicado TINYINT(1) NOT NULL DEFAULT 0,
    FechaPublicacion DATETIME NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdNoticia),
    KEY IX_Noticia_EstaPublicado (EstaPublicado),
    KEY IX_Noticia_FechaPublicacion (FechaPublicacion),
    CONSTRAINT FK_Noticia_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Noticia_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Galeria (
    IdGaleria BIGINT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    EstaPublicado TINYINT(1) NOT NULL DEFAULT 0,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdGaleria),
    KEY IX_Galeria_EstaPublicado (EstaPublicado),
    CONSTRAINT FK_Galeria_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Galeria_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ImagenGaleria (
    IdImagenGaleria BIGINT NOT NULL AUTO_INCREMENT,
    IdGaleria BIGINT NOT NULL,
    IdDocumentoArchivo BIGINT NOT NULL,
    Titulo VARCHAR(150) NULL,
    Descripcion VARCHAR(300) NULL,
    Orden INT NOT NULL DEFAULT 0,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdImagenGaleria),
    KEY IX_ImagenGaleria_IdGaleria (IdGaleria),
    KEY IX_ImagenGaleria_IdDocumentoArchivo (IdDocumentoArchivo),
    CONSTRAINT FK_ImagenGaleria_Galeria
        FOREIGN KEY (IdGaleria) REFERENCES Galeria(IdGaleria)
        ON DELETE CASCADE,
    CONSTRAINT FK_ImagenGaleria_DocumentoArchivo
        FOREIGN KEY (IdDocumentoArchivo) REFERENCES DocumentoArchivo(IdDocumentoArchivo)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ProyectoPublicado (
    IdProyectoPublicado BIGINT NOT NULL AUTO_INCREMENT,
    IdProyecto BIGINT NULL,
    Titulo VARCHAR(200) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    EstadoVisual VARCHAR(50) NULL,
    EstaPublicado TINYINT(1) NOT NULL DEFAULT 0,
    FechaPublicacion DATETIME NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdProyectoPublicado),
    KEY IX_ProyectoPublicado_IdProyecto (IdProyecto),
    KEY IX_ProyectoPublicado_EstaPublicado (EstaPublicado),
    CONSTRAINT FK_ProyectoPublicado_Proyecto
        FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto)
        ON DELETE SET NULL,
    CONSTRAINT FK_ProyectoPublicado_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_ProyectoPublicado_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ConsultaContacto (
    IdConsultaContacto BIGINT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(150) NOT NULL,
    CorreoElectronico VARCHAR(150) NOT NULL,
    Telefono VARCHAR(30) NULL,
    Asunto VARCHAR(200) NULL,
    Mensaje TEXT NOT NULL,
    FechaEnvio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EstadoConsulta ENUM('Nueva','EnRevision','Atendida','Descartada') NOT NULL DEFAULT 'Nueva',
    ObservacionesInternas VARCHAR(500) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdConsultaContacto),
    KEY IX_ConsultaContacto_FechaEnvio (FechaEnvio),
    KEY IX_ConsultaContacto_EstadoConsulta (EstadoConsulta),
    KEY IX_ConsultaContacto_CorreoElectronico (CorreoElectronico)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS FAQ (
    IdFAQ BIGINT NOT NULL AUTO_INCREMENT,
    Pregunta VARCHAR(300) NOT NULL,
    Respuesta TEXT NOT NULL,
    Orden INT NOT NULL DEFAULT 0,
    EstaPublicado TINYINT(1) NOT NULL DEFAULT 0,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdFAQ),
    KEY IX_FAQ_EstaPublicado (EstaPublicado),
    KEY IX_FAQ_Orden (Orden),
    CONSTRAINT FK_FAQ_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_FAQ_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   9. FORECAST DE PLANILLAS
   Proyección simple basada en históricos y parámetros.
   ============================================================ */

CREATE TABLE IF NOT EXISTS ForecastEscenario (
    IdForecastEscenario BIGINT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    FechaInicioProyeccion DATE NOT NULL,
    FechaFinProyeccion DATE NOT NULL,
    PeriodosHistoricosConsiderados INT NOT NULL DEFAULT 3,
    EstadoEscenario ENUM('Borrador','Calculado','Guardado','Comparado','Anulado') NOT NULL DEFAULT 'Borrador',
    MontoProyectadoTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MontoRealTotal DECIMAL(18,2) NULL,
    DiferenciaTotal DECIMAL(18,2) NULL,
    FechaCalculo DATETIME NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdForecastEscenario),
    KEY IX_ForecastEscenario_Fechas (FechaInicioProyeccion, FechaFinProyeccion),
    KEY IX_ForecastEscenario_EstadoEscenario (EstadoEscenario),
    CONSTRAINT CK_ForecastEscenario_Fechas
        CHECK (FechaInicioProyeccion <= FechaFinProyeccion),
    CONSTRAINT CK_ForecastEscenario_Historicos
        CHECK (PeriodosHistoricosConsiderados >= 0),
    CONSTRAINT FK_ForecastEscenario_CreadoPor
        FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,
    CONSTRAINT FK_ForecastEscenario_ModificadoPor
        FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ForecastPeriodo (
    IdForecastPeriodo BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    TipoPeriodo ENUM('Mensual','Quincenal') NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdForecastPeriodo),
    UNIQUE KEY UX_ForecastPeriodo_Escenario_Orden (IdForecastEscenario, NumeroOrden),
    UNIQUE KEY UX_ForecastPeriodo_Escenario_Fechas (IdForecastEscenario, FechaInicio, FechaFin),
    UNIQUE KEY UX_ForecastPeriodo_Escenario_Id (IdForecastEscenario, IdForecastPeriodo),
    CONSTRAINT CK_ForecastPeriodo_Orden
        CHECK (NumeroOrden > 0),
    CONSTRAINT CK_ForecastPeriodo_Fechas
        CHECK (FechaInicio <= FechaFin),
    CONSTRAINT FK_ForecastPeriodo_ForecastEscenario
        FOREIGN KEY (IdForecastEscenario) REFERENCES ForecastEscenario(IdForecastEscenario)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ForecastFuenteHistorica (
    IdForecastFuenteHistorica BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    IdPlanilla BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    PRIMARY KEY (IdForecastFuenteHistorica),
    UNIQUE KEY UX_ForecastFuente_Escenario_Planilla (IdForecastEscenario, IdPlanilla),
    UNIQUE KEY UX_ForecastFuente_Escenario_Orden (IdForecastEscenario, NumeroOrden),
    CONSTRAINT CK_ForecastFuente_Orden
        CHECK (NumeroOrden > 0),
    CONSTRAINT FK_ForecastFuente_ForecastEscenario
        FOREIGN KEY (IdForecastEscenario) REFERENCES ForecastEscenario(IdForecastEscenario)
        ON DELETE CASCADE,
    CONSTRAINT FK_ForecastFuente_Planilla
        FOREIGN KEY (IdPlanilla) REFERENCES Planilla(IdPlanilla)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ForecastParticipante (
    IdForecastParticipante BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    CodigoParticipante VARCHAR(50) NOT NULL,
    Etiqueta VARCHAR(150) NOT NULL,
    TipoParticipante ENUM('Colaborador','ContratacionPrevista') NOT NULL,
    IdColaborador BIGINT NULL,
    IdDepartamento INT NOT NULL,
    IdPuesto INT NOT NULL,
    SalarioBaseMensual DECIMAL(18,2) NOT NULL,
    FechaInicioAplicacion DATE NOT NULL,
    FechaSalidaPrevista DATE NULL,
    EstaIncluido TINYINT(1) NOT NULL DEFAULT 1,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdForecastParticipante),
    UNIQUE KEY UX_ForecastParticipante_Escenario_Codigo (IdForecastEscenario, CodigoParticipante),
    UNIQUE KEY UX_ForecastParticipante_Escenario_Colaborador (IdForecastEscenario, IdColaborador),
    UNIQUE KEY UX_ForecastParticipante_Escenario_Id (IdForecastEscenario, IdForecastParticipante),
    KEY IX_ForecastParticipante_IdDepartamento (IdDepartamento),
    KEY IX_ForecastParticipante_IdPuesto (IdPuesto),
    CONSTRAINT CK_ForecastParticipante_Tipo
        CHECK (
            (TipoParticipante = 'Colaborador' AND IdColaborador IS NOT NULL)
            OR
            (TipoParticipante = 'ContratacionPrevista' AND IdColaborador IS NULL)
        ),
    CONSTRAINT CK_ForecastParticipante_Salario
        CHECK (SalarioBaseMensual >= 0.00),
    CONSTRAINT CK_ForecastParticipante_Fechas
        CHECK (FechaSalidaPrevista IS NULL OR FechaSalidaPrevista >= FechaInicioAplicacion),
    CONSTRAINT CK_ForecastParticipante_Inclusion
        CHECK (EstaIncluido IN (0, 1)),
    CONSTRAINT FK_ForecastParticipante_ForecastEscenario
        FOREIGN KEY (IdForecastEscenario) REFERENCES ForecastEscenario(IdForecastEscenario)
        ON DELETE CASCADE,
    CONSTRAINT FK_ForecastParticipante_Colaborador
        FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador)
        ON DELETE RESTRICT,
    CONSTRAINT FK_ForecastParticipante_Departamento
        FOREIGN KEY (IdDepartamento) REFERENCES Departamento(IdDepartamento)
        ON DELETE RESTRICT,
    CONSTRAINT FK_ForecastParticipante_Puesto
        FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ForecastAsignacionProyecto (
    IdForecastAsignacionProyecto BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    IdForecastPeriodo BIGINT NOT NULL,
    IdForecastParticipante BIGINT NOT NULL,
    IdProyecto BIGINT NOT NULL,
    Porcentaje DECIMAL(7,4) NOT NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdForecastAsignacionProyecto),
    UNIQUE KEY UX_FcstAsign_Esc_Per_Part_Proy
        (IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyecto),
    KEY IX_FcstAsign_Esc_Part (IdForecastEscenario, IdForecastParticipante),
    KEY IX_FcstAsign_Esc_Per (IdForecastEscenario, IdForecastPeriodo),
    KEY IX_ForecastAsignacionProyecto_IdProyecto (IdProyecto),
    CONSTRAINT CK_ForecastAsignacionProyecto_Porcentaje
        CHECK (Porcentaje > 0.0000 AND Porcentaje <= 100.0000),
    CONSTRAINT FK_FcstAsign_Participante
        FOREIGN KEY (IdForecastEscenario, IdForecastParticipante)
        REFERENCES ForecastParticipante(IdForecastEscenario, IdForecastParticipante)
        ON DELETE CASCADE,
    CONSTRAINT FK_FcstAsign_Periodo
        FOREIGN KEY (IdForecastEscenario, IdForecastPeriodo)
        REFERENCES ForecastPeriodo(IdForecastEscenario, IdForecastPeriodo)
        ON DELETE CASCADE,
    CONSTRAINT FK_FcstAsign_Proyecto
        FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ForecastParametro (
    IdForecastParametro BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    Codigo VARCHAR(100) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    TipoParametro ENUM('Porcentaje','Monto','Cantidad','Texto') NOT NULL DEFAULT 'Porcentaje',
    ValorDecimal DECIMAL(18,4) NULL,
    ValorTexto VARCHAR(255) NULL,
    Descripcion VARCHAR(300) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdForecastParametro),
    UNIQUE KEY UX_ForecastParametro_Escenario_Codigo (IdForecastEscenario, Codigo),
    CONSTRAINT FK_ForecastParametro_ForecastEscenario
        FOREIGN KEY (IdForecastEscenario) REFERENCES ForecastEscenario(IdForecastEscenario)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ForecastDetalle (
    IdForecastDetalle BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    IdForecastPeriodo BIGINT NOT NULL,
    IdForecastParticipante BIGINT NOT NULL,
    IdProyecto BIGINT NOT NULL,
    Concepto VARCHAR(150) NOT NULL,
    MontoBase DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MontoAjuste DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MontoProyectado DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MontoReal DECIMAL(18,2) NULL,
    Diferencia DECIMAL(18,2) NULL,
    Observaciones VARCHAR(500) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdForecastDetalle),
    UNIQUE KEY UX_FcstDetalle_Esc_Per_Part_Proy_Concepto
        (IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyecto, Concepto),
    CONSTRAINT FK_FcstDetalle_AsignacionProyecto
        FOREIGN KEY (IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyecto)
        REFERENCES ForecastAsignacionProyecto
            (IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyecto)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   10. BITÁCORA DE AUDITORÍA
   ============================================================ */

CREATE TABLE IF NOT EXISTS BitacoraAuditoria (
    IdBitacoraAuditoria BIGINT NOT NULL AUTO_INCREMENT,
    IdUsuario VARCHAR(255) NULL,
    FechaHora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Accion VARCHAR(100) NOT NULL,
    Entidad VARCHAR(100) NOT NULL,
    IdRegistro VARCHAR(100) NULL,
    ValoresAnteriores JSON NULL,
    ValoresNuevos JSON NULL,
    DireccionIP VARCHAR(45) NULL,
    Observacion VARCHAR(500) NULL,
    PRIMARY KEY (IdBitacoraAuditoria),
    KEY IX_BitacoraAuditoria_IdUsuario (IdUsuario),
    KEY IX_BitacoraAuditoria_FechaHora (FechaHora),
    KEY IX_BitacoraAuditoria_Entidad (Entidad),
    KEY IX_BitacoraAuditoria_Accion (Accion),
    CONSTRAINT FK_BitacoraAuditoria_AspNetUsers
        FOREIGN KEY (IdUsuario) REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* ============================================================
   11. DATOS SEMILLA
   ============================================================ */

INSERT IGNORE INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
VALUES
('ROL-ADMINISTRADOR', 'Administrador', 'ADMINISTRADOR', UUID()),
('ROL-RECURSOS-HUMANOS', 'Recursos Humanos', 'RECURSOS HUMANOS', UUID()),
('ROL-OPERACIONES', 'Operaciones', 'OPERACIONES', UUID()),
('ROL-EMPLEADO', 'Empleado', 'EMPLEADO', UUID());

INSERT IGNORE INTO EstadoLaboral (Nombre, Descripcion)
VALUES
('Activo', 'Colaborador activo en la empresa.'),
('Inactivo', 'Colaborador inactivo.'),
('Suspendido', 'Colaborador suspendido temporalmente.'),
('Finalizado', 'Relación laboral finalizada.');

INSERT IGNORE INTO EstadoProyecto (Nombre, Descripcion)
VALUES
('Planificado', 'Proyecto registrado, pendiente de iniciar.'),
('En ejecución', 'Proyecto activo en ejecución.'),
('Pausado', 'Proyecto detenido temporalmente.'),
('Finalizado', 'Proyecto concluido.'),
('Cancelado', 'Proyecto cancelado.');

INSERT IGNORE INTO EstadoActivo (Nombre, Descripcion)
VALUES
('Disponible', 'Activo disponible para asignación.'),
('Asignado', 'Activo asignado a un proyecto o responsable.'),
('En mantenimiento', 'Activo no disponible por mantenimiento.'),
('Fuera de servicio', 'Activo no disponible por daño o restricción.'),
('Dado de baja', 'Activo retirado del uso operativo.');

INSERT IGNORE INTO EstadoMantenimiento (Nombre, Descripcion)
VALUES
('Programado', 'Mantenimiento creado, pendiente de iniciar.'),
('En proceso', 'Mantenimiento en ejecución.'),
('Finalizado', 'Mantenimiento cerrado.'),
('Cancelado', 'Mantenimiento anulado.');

INSERT IGNORE INTO EstadoPlanilla (Nombre, Descripcion)
VALUES
('Borrador', 'Periodo o planilla editable.'),
('Calculada', 'Cálculo de planilla ejecutado.'),
('En revisión', 'Planilla pendiente de revisión por Recursos Humanos.'),
('Aprobada', 'Planilla aprobada por Recursos Humanos.'),
('Cerrada', 'Planilla bloqueada para cambios posteriores.'),
('Anulada', 'Planilla cancelada por error o decisión administrativa.');

INSERT IGNORE INTO TipoActivo (Nombre, Descripcion)
VALUES
('Maquinaria', 'Maquinaria utilizada en proyectos.'),
('Equipo', 'Equipo operativo o administrativo.'),
('Herramienta', 'Herramienta de trabajo.'),
('Vehículo', 'Vehículo asociado a la operación.');

INSERT IGNORE INTO CategoriaActivo (IdTipoActivo, Nombre, Descripcion)
SELECT IdTipoActivo, 'General', CONCAT('Categoría general para ', Nombre)
FROM TipoActivo;

INSERT IGNORE INTO TipoMedicionUso (Nombre, Descripcion)
VALUES
('Horas', 'Medición de uso por horas.'),
('Kilómetros', 'Medición de uso por kilometraje.'),
('Unidades', 'Medición de uso por unidades.'),
('No aplica', 'Activo sin medición de uso.');

INSERT IGNORE INTO TipoDocumento (Nombre, Descripcion)
VALUES
('Contrato', 'Contrato laboral o documento contractual.'),
('Documento colaborador', 'Documento asociado al expediente del colaborador.'),
('Evidencia mantenimiento', 'Archivo de evidencia asociado a mantenimiento.'),
('Imagen galería', 'Imagen utilizada en galerías o contenido público.'),
('Otro', 'Otro tipo de documento.');

INSERT IGNORE INTO TipoIncidenciaPlanilla (Nombre, Naturaleza, Descripcion)
VALUES
('Hora extra', 'Ingreso', 'Ingreso por horas extra registradas.'),
('Ausencia sin goce', 'Deduccion', 'Deducción por ausencia sin goce.'),
('Bono', 'Ingreso', 'Ingreso adicional registrado manualmente.'),
('Deducción', 'Deduccion', 'Deducción configurable registrada manualmente.');

INSERT IGNORE INTO Departamento (Nombre, Descripcion)
VALUES
('Administración', 'Departamento administrativo general.'),
('Recursos Humanos', 'Departamento encargado de colaboradores y planillas.'),
('Operaciones', 'Departamento encargado de activos, mantenimiento y proyectos.');

INSERT IGNORE INTO Puesto (IdDepartamento, Nombre, Descripcion)
VALUES
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Administración'), 'Administrador del sistema', 'Responsable de administración general del sistema.'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Recursos Humanos'), 'Responsable de Recursos Humanos', 'Responsable de RRHH y planillas.'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Operaciones'), 'Responsable de Operaciones', 'Responsable de activos, mantenimiento y proyectos.'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Operaciones'), 'Empleado operativo', 'Empleado con acceso limitado a información autorizada.');

INSERT IGNORE INTO ParametroPlanilla (
    Codigo,
    Nombre,
    Descripcion,
    TipoParametro,
    Naturaleza,
    ValorDecimal,
    ValorTexto,
    EsEditable
)
VALUES
('PORCENTAJE_CARGAS_OBRERAS', 'Porcentaje de cargas obreras', 'Parámetro configurable para deducciones del trabajador. Debe ser revisado por Recursos Humanos antes de uso real.', 'Porcentaje', 'Deduccion', 0.0000, NULL, 1),
('PORCENTAJE_CARGAS_PATRONALES', 'Porcentaje de cargas patronales', 'Parámetro configurable para estimar costo patronal. Debe ser revisado por Recursos Humanos antes de uso real.', 'Porcentaje', NULL, 0.0000, NULL, 1),
('PORCENTAJE_RIESGO_TRABAJO', 'Porcentaje de riesgo de trabajo', 'Parámetro configurable para estimación de póliza o costo asociado. Debe validarse antes de uso real.', 'Porcentaje', NULL, 0.0000, NULL, 1),
('PORCENTAJE_PROVISIONES', 'Porcentaje de provisiones', 'Parámetro configurable para estimaciones internas de costo de planilla.', 'Porcentaje', NULL, 0.0000, NULL, 1),
('HORAS_MENSUALES_REFERENCIA', 'Horas mensuales de referencia', 'Parámetro editable usado para calcular valor de hora ordinaria en la primera versión.', 'Cantidad', NULL, 0.0000, NULL, 1);

UPDATE ParametroPlanilla
SET Naturaleza = 'Deduccion'
WHERE Codigo = 'PORCENTAJE_CARGAS_OBRERAS';

INSERT IGNORE INTO PaginaContenido (CodigoPagina, Titulo, Contenido, EstaPublicado)
VALUES
('INICIO', 'Inicio', 'Contenido inicial de la página de inicio.', 1),
('QUIENES_SOMOS', 'Quiénes Somos', 'Contenido institucional de la empresa.', 1),
('SERVICIOS', 'Servicios', 'Descripción general de servicios.', 1),
('CONTACTO', 'Contacto', 'Información de contacto y formulario.', 1),
('POLITICAS', 'Políticas de privacidad y uso', 'Contenido inicial de políticas del sitio.', 1);

SET FOREIGN_KEY_CHECKS = 1;

/* ============================================================
   12. MODELO FINANCIERO DE PRODUCCION - F1-FIN-02B
   El mismo DDL se publica por separado como patch de upgrade.
   ============================================================ */
/*
  F1-FIN-02B - Upgrade aditivo del esquema financiero.
  Destino: una base SIGE-CDC existente respaldada y validada en MySQL 8.
  Ejecución: una sola vez, exclusivamente QA/Staging autorizado; nunca Production en este loop.
  El script oficial completo sigue siendo database/ScriptDB_SIGE_CDC_Corregido.sql.
*/

SET NAMES utf8mb4;

/* F1-FIN-02B-R PREFLIGHT: solo SELECT/SET; ninguna tabla/procedimiento auxiliar.
   Ejecutar con aplicación detenida, sin escritores ni DDL concurrente, backup probado.
   El cliente DEBE detenerse al primer error (mysql sin --force).
   Un error JSON intencional aborta tras mostrar Control/Tabla/PK; nunca transforma datos.
   Fallos de recursos/disco/conexión no son predecibles: conservar backup ante DDL parcial. */
WITH bloqueos AS (
SELECT 'TABLA_NUEVA_YA_EXISTE' Control, TABLE_NAME Tabla, TABLE_NAME Id FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('ConceptoPlanilla','ConceptoPlanillaVersion','VersionNormativa','ParametroNormativo','ValorParametroNormativo','TramoImpuestoSalario','CreditoFiscalNormativo','JornadaLaboral','OcupacionSalarioMinimo','SalarioMinimoVersion','ConfiguracionRiesgoTrabajo','CreditoFiscalColaborador','EjecucionPlanilla','SegmentoCalculoPlanilla','ConceptoDetallePlanilla','AplicacionNormativaDetallePlanilla','DetallePlanillaDistribucionProyecto','ForecastObservacionHistorica')
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'TipoIncidenciaPlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='TipoIncidenciaPlanilla' AND COLUMN_NAME='IdConceptoPlanilla'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ParametroPlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanilla' AND COLUMN_NAME='IdConceptoPlanilla'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ParametroPlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanilla' AND COLUMN_NAME='EsNormativo'
UNION ALL
SELECT 'INDICE_ORIGEN_AUSENTE', 'ParametroPlanilla', 'UX_ParametroPlanilla_Codigo' WHERE NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanilla' AND INDEX_NAME='UX_ParametroPlanilla_Codigo')
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'IncidenciaPlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='IncidenciaPlanilla' AND COLUMN_NAME='Unidad'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'IncidenciaPlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='IncidenciaPlanilla' AND COLUMN_NAME='IdConceptoPlanillaVersion'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'Puesto', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Puesto' AND COLUMN_NAME='IdOcupacionSalarioMinimo'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'Contrato', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND COLUMN_NAME='IdJornadaLaboral'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'Contrato', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND COLUMN_NAME='IdConfiguracionRiesgoTrabajo'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'Planilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Planilla' AND COLUMN_NAME='IdEjecucionPlanillaVigente'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='IdEjecucionPlanilla'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='CodigoColaborador'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='NombreCompleto'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='NombrePuesto'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='NombreDepartamento'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='TipoContrato'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='PeriodicidadPago'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='CodigoJornada'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='HorasOrdinariasDia'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='SalarioContractual'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='FuenteDimensionesSnapshot'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='SnapshotCompleto'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='IngresosNoSalariales'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='CuotasObreras'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='ImpuestoSalario'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='OtrasDeducciones'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='CargasPatronales'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='RiesgosTrabajo'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='OtrosCostosPatronales'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'DetallePlanilla', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='CostoLaboralTotal'
UNION ALL
SELECT 'INDICE_ORIGEN_AUSENTE', 'DetallePlanilla', 'UX_DetallePlanilla_Planilla_Colaborador' WHERE NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND INDEX_NAME='UX_DetallePlanilla_Planilla_Colaborador')
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastEscenario', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastEscenario' AND COLUMN_NAME='VersionMotor'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastEscenario', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastEscenario' AND COLUMN_NAME='Moneda'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastEscenario', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastEscenario' AND COLUMN_NAME='TieneHistoricoLimitado'
UNION ALL
SELECT 'FK_ORIGEN_AUSENTE', 'ForecastDetalle', 'FK_FcstDetalle_AsignacionProyecto' WHERE NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND CONSTRAINT_NAME='FK_FcstDetalle_AsignacionProyecto')
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastAsignacionProyecto', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAsignacionProyecto' AND COLUMN_NAME='EsSinAsignar'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastAsignacionProyecto', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAsignacionProyecto' AND COLUMN_NAME='IdProyectoNormalizado'
UNION ALL
SELECT 'INDICE_ORIGEN_AUSENTE', 'ForecastAsignacionProyecto', 'UX_FcstAsign_Esc_Per_Part_Proy' WHERE NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAsignacionProyecto' AND INDEX_NAME='UX_FcstAsign_Esc_Per_Part_Proy')
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='IdForecastAsignacionProyecto'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='IdConceptoPlanillaVersion'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='MontoBaseSinRedondear'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='MontoProyectadoSinRedondear'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='CantidadObservaciones'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='HistoricoLimitado'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='VersionMotor'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='DiferenciaFirmada'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='DiferenciaAbsoluta'
UNION ALL
SELECT 'COLUMNA_NUEVA_YA_EXISTE', 'ForecastDetalle', COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND COLUMN_NAME='DiferenciaPorcentual'
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'AspNetUsers', 'Id' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AspNetUsers' AND COLUMN_NAME='Id' AND COLUMN_TYPE='varchar(255)' AND IS_NULLABLE='NO' AND COLLATION_NAME='utf8mb4_unicode_ci')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'TipoIncidenciaPlanilla', 'IX_TipoIncidencia_IdConceptoPlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='TipoIncidenciaPlanilla' AND INDEX_NAME='IX_TipoIncidencia_IdConceptoPlanilla')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'ParametroPlanilla', 'UX_ParametroPlanilla_Codigo_VigenciaInicio' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanilla' AND INDEX_NAME='UX_ParametroPlanilla_Codigo_VigenciaInicio')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'ParametroPlanilla', 'IX_ParametroPlanilla_IdConceptoPlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanilla' AND INDEX_NAME='IX_ParametroPlanilla_IdConceptoPlanilla')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'IncidenciaPlanilla', 'IX_IncidenciaPlanilla_ConceptoVersion' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='IncidenciaPlanilla' AND INDEX_NAME='IX_IncidenciaPlanilla_ConceptoVersion')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'Puesto', 'IX_Puesto_IdOcupacionSalarioMinimo' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Puesto' AND INDEX_NAME='IX_Puesto_IdOcupacionSalarioMinimo')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'Contrato', 'IX_Contrato_IdJornadaLaboral' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND INDEX_NAME='IX_Contrato_IdJornadaLaboral')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'Contrato', 'IX_Contrato_IdConfiguracionRiesgoTrabajo' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND INDEX_NAME='IX_Contrato_IdConfiguracionRiesgoTrabajo')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'Contrato', 'IX_Contrato_ResolucionTemporal' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND INDEX_NAME='IX_Contrato_ResolucionTemporal')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'Planilla', 'UX_Planilla_EjecucionVigente' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Planilla' AND INDEX_NAME='UX_Planilla_EjecucionVigente')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'DetallePlanilla', 'IX_DetallePlanilla_IdPlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND INDEX_NAME='IX_DetallePlanilla_IdPlanilla')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'DetallePlanilla', 'UX_DetallePlanilla_Ejecucion_Colaborador' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND INDEX_NAME='UX_DetallePlanilla_Ejecucion_Colaborador')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'ForecastAsignacionProyecto', 'UX_FcstAsign_Esc_Per_Part_ProyNorm' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAsignacionProyecto' AND INDEX_NAME='UX_FcstAsign_Esc_Per_Part_ProyNorm')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'ForecastDetalle', 'IX_FcstDetalle_ConceptoVersion' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND INDEX_NAME='IX_FcstDetalle_ConceptoVersion')
UNION ALL
SELECT 'INDICE_NUEVO_YA_EXISTE', 'ForecastDetalle', 'UX_FcstDetalle_Asignacion_Concepto' WHERE EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND INDEX_NAME='UX_FcstDetalle_Asignacion_Concepto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ConceptoPlanilla_CreadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ConceptoPlanilla_CreadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ConceptoVersion_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ConceptoVersion_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ConceptoVersion_Clasificacion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ConceptoVersion_Clasificacion')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ConceptoVersion_Banderas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ConceptoVersion_Banderas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ConceptoVersion_Concepto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ConceptoVersion_Concepto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_VersionNormativa_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_VersionNormativa_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_VersionNormativa_CreadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_VersionNormativa_CreadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_VersionNormativa_ActivadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_VersionNormativa_ActivadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ParametroNormativo_Sujeto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ParametroNormativo_Sujeto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ParametroNormativo_Obligatorio' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ParametroNormativo_Obligatorio')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ValorNormativo_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ValorNormativo_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ValorNormativo_Parametro' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ValorNormativo_Parametro')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ValorNormativo_Version' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ValorNormativo_Version')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_TramoImpuesto_Orden' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_TramoImpuesto_Orden')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_TramoImpuesto_Limites' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_TramoImpuesto_Limites')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_TramoImpuesto_Tasa' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_TramoImpuesto_Tasa')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_TramoImpuesto_Version' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_TramoImpuesto_Version')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_CreditoNormativo_Monto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_CreditoNormativo_Monto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_CreditoNormativo_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_CreditoNormativo_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_CreditoNormativo_Version' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_CreditoNormativo_Version')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_JornadaLaboral_Horas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_JornadaLaboral_Horas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_JornadaLaboral_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_JornadaLaboral_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_JornadaLaboral_CreadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_JornadaLaboral_CreadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_JornadaLaboral_ModificadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_JornadaLaboral_ModificadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_SalarioMinimo_Monto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_SalarioMinimo_Monto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_SalarioMinimo_Periodicidad' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_SalarioMinimo_Periodicidad')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_SalarioMinimo_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_SalarioMinimo_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_SalarioMinimo_Ocupacion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_SalarioMinimo_Ocupacion')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_SalarioMinimo_VersionNormativa' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_SalarioMinimo_VersionNormativa')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_RiesgoTrabajo_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_RiesgoTrabajo_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_RiesgoTrabajo_Tasas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_RiesgoTrabajo_Tasas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_RiesgoTrabajo_VersionNormativa' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_RiesgoTrabajo_VersionNormativa')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_RiesgoTrabajo_CreadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_RiesgoTrabajo_CreadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_RiesgoTrabajo_ModificadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_RiesgoTrabajo_ModificadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_TipoIncidencia_ConceptoPlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_TipoIncidencia_ConceptoPlanilla')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ParametroPlanilla_Vigencia' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ParametroPlanilla_Vigencia')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ParametroPlanilla_NoNormativo' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ParametroPlanilla_NoNormativo')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ParametroPlanilla_Valor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ParametroPlanilla_Valor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ParametroPlanilla_ConceptoPlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ParametroPlanilla_ConceptoPlanilla')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_IncidenciaPlanilla_ConceptoVersion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_IncidenciaPlanilla_ConceptoVersion')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_Puesto_OcupacionSalarioMinimo' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_Puesto_OcupacionSalarioMinimo')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_Contrato_JornadaLaboral' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_Contrato_JornadaLaboral')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_Contrato_RiesgoTrabajo' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_Contrato_RiesgoTrabajo')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_CreditoColaborador_Cantidad' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_CreditoColaborador_Cantidad')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_CreditoColaborador_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_CreditoColaborador_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_CreditoColaborador_Colaborador' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_CreditoColaborador_Colaborador')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_CreditoColaborador_Normativo' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_CreditoColaborador_Normativo')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_CreditoColaborador_AprobadoPor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_CreditoColaborador_AprobadoPor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_EjecucionPlanilla_Version' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_EjecucionPlanilla_Version')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_EjecucionPlanilla_Estado' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_EjecucionPlanilla_Estado')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_EjecucionPlanilla_Reproducibilidad' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_EjecucionPlanilla_Reproducibilidad')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_EjecucionPlanilla_Snapshot' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_EjecucionPlanilla_Snapshot')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_EjecucionPlanilla_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_EjecucionPlanilla_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_EjecucionPlanilla_Invalidacion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_EjecucionPlanilla_Invalidacion')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_EjecucionPlanilla_Planilla' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_EjecucionPlanilla_Planilla')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_Planilla_EjecucionVigente' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_Planilla_EjecucionVigente')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_DetallePlanilla_SnapshotCompleto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_DetallePlanilla_SnapshotCompleto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_DetallePlanilla_EjecucionPlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_DetallePlanilla_EjecucionPlanilla')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_SegmentoPlanilla_Orden' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_SegmentoPlanilla_Orden')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_SegmentoPlanilla_Fechas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_SegmentoPlanilla_Fechas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_SegmentoPlanilla_Valores' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_SegmentoPlanilla_Valores')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_SegmentoPlanilla_Detalle' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_SegmentoPlanilla_Detalle')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_SegmentoPlanilla_Contrato' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_SegmentoPlanilla_Contrato')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ConceptoDetalle_Orden' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ConceptoDetalle_Orden')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ConceptoDetalle_Monto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ConceptoDetalle_Monto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ConceptoDetalle_Banderas' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ConceptoDetalle_Banderas')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ConceptoDetalle_Detalle' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ConceptoDetalle_Detalle')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ConceptoDetalle_Segmento' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ConceptoDetalle_Segmento')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ConceptoDetalle_ConceptoVersion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ConceptoDetalle_ConceptoVersion')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_AplicacionNormativa_Referencia' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_AplicacionNormativa_Referencia')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_AplicacionNormativa_ConceptoDetalle' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_AplicacionNormativa_ConceptoDetalle')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_AplicacionNormativa_Valor' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_AplicacionNormativa_Valor')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_AplicacionNormativa_Tramo' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_AplicacionNormativa_Tramo')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_AplicacionNormativa_Credito' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_AplicacionNormativa_Credito')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_AplicacionNormativa_Riesgo' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_AplicacionNormativa_Riesgo')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_DetalleDistribucion_Destino' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_DetalleDistribucion_Destino')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_DetalleDistribucion_Porcentaje' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_DetalleDistribucion_Porcentaje')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_DetalleDistribucion_Valores' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_DetalleDistribucion_Valores')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_DetalleDistribucion_Detalle' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_DetalleDistribucion_Detalle')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_DetalleDistribucion_Proyecto' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_DetalleDistribucion_Proyecto')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ForecastEscenario_HistoricoLimitado' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ForecastEscenario_HistoricoLimitado')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ForecastAsignacionProyecto_Destino' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ForecastAsignacionProyecto_Destino')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ForecastDetalle_Observaciones' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ForecastDetalle_Observaciones')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ForecastDetalle_DiferenciaAbsoluta' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ForecastDetalle_DiferenciaAbsoluta')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_FcstDetalle_ConceptoVersion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_FcstDetalle_ConceptoVersion')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'CK_ForecastObservacion_Comparabilidad' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='CK_ForecastObservacion_Comparabilidad')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ForecastObservacion_Escenario' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ForecastObservacion_Escenario')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ForecastObservacion_Participante' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ForecastObservacion_Participante')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ForecastObservacion_Fuente' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ForecastObservacion_Fuente')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ForecastObservacion_DetallePlanilla' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ForecastObservacion_DetallePlanilla')
UNION ALL
SELECT 'CONSTRAINT_NUEVO_YA_EXISTE', 'ESQUEMA', 'FK_ForecastObservacion_ConceptoVersion' WHERE EXISTS(SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_ForecastObservacion_ConceptoVersion')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'Colaborador', 'IdColaborador' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Colaborador' AND COLUMN_NAME='IdColaborador' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'Planilla', 'IdPlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Planilla' AND COLUMN_NAME='IdPlanilla' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'DetallePlanilla', 'IdDetallePlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND COLUMN_NAME='IdDetallePlanilla' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'Proyecto', 'IdProyecto' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Proyecto' AND COLUMN_NAME='IdProyecto' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'Contrato', 'IdContrato' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND COLUMN_NAME='IdContrato' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'ForecastEscenario', 'IdForecastEscenario' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastEscenario' AND COLUMN_NAME='IdForecastEscenario' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'ForecastParticipante', 'IdForecastParticipante' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastParticipante' AND COLUMN_NAME='IdForecastParticipante' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'ForecastFuenteHistorica', 'IdForecastFuenteHistorica' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastFuenteHistorica' AND COLUMN_NAME='IdForecastFuenteHistorica' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TIPO_PK_INCOMPATIBLE_O_AUSENTE', 'ForecastAsignacionProyecto', 'IdForecastAsignacionProyecto' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAsignacionProyecto' AND COLUMN_NAME='IdForecastAsignacionProyecto' AND COLUMN_TYPE='bigint' AND IS_NULLABLE='NO')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'TipoIncidenciaPlanilla', 'TipoIncidenciaPlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='TipoIncidenciaPlanilla' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ParametroPlanilla', 'ParametroPlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanilla' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ParametroPlanillaPeriodo', 'ParametroPlanillaPeriodo' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanillaPeriodo' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ParametroPlanillaColaborador', 'ParametroPlanillaColaborador' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ParametroPlanillaColaborador' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'IncidenciaPlanilla', 'IncidenciaPlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='IncidenciaPlanilla' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'Puesto', 'Puesto' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Puesto' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'Departamento', 'Departamento' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Departamento' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'Contrato', 'Contrato' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Contrato' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'PeriodoPlanilla', 'PeriodoPlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='PeriodoPlanilla' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'Planilla', 'Planilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Planilla' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'DetallePlanilla', 'DetallePlanilla' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='DetallePlanilla' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'Colaborador', 'Colaborador' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Colaborador' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ColillaPago', 'ColillaPago' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ColillaPago' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ForecastEscenario', 'ForecastEscenario' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastEscenario' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ForecastDetalle', 'ForecastDetalle' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastDetalle' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ForecastAsignacionProyecto', 'ForecastAsignacionProyecto' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastAsignacionProyecto' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'AspNetUsers', 'AspNetUsers' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AspNetUsers' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'Proyecto', 'Proyecto' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Proyecto' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ForecastParticipante', 'ForecastParticipante' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastParticipante' AND ENGINE='InnoDB')
UNION ALL
SELECT 'TABLA_ORIGEN_AUSENTE_O_NO_INNODB', 'ForecastFuenteHistorica', 'ForecastFuenteHistorica' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ForecastFuenteHistorica' AND ENGINE='InnoDB')
)
SELECT COALESCE(JSON_ARRAYAGG(JSON_OBJECT('Control',Control,'Tabla',Tabla,'Id',CAST(Id AS CHAR))),JSON_ARRAY()) INTO @fin02b_bloqueos_esquema FROM bloqueos;
SELECT Control, Tabla, Id FROM JSON_TABLE(@fin02b_bloqueos_esquema, '$[*]' COLUMNS(Control VARCHAR(100) PATH '$.Control', Tabla VARCHAR(100) PATH '$.Tabla', Id VARCHAR(255) PATH '$.Id')) b;
SET @fin02b_guardia = IF(JSON_LENGTH(@fin02b_bloqueos_esquema)=0,1,JSON_EXTRACT('FIN02B_ABORTADO_REVISAR_BLOQUEOS_PREVIOS','$'));

WITH bloqueos AS (
SELECT 'PARAMETRO_CHECK_VALOR' Control,'ParametroPlanilla' Tabla,IdParametroPlanilla Id FROM ParametroPlanilla WHERE NOT ((TipoParametro='Texto' AND ValorTexto IS NOT NULL AND ValorDecimal IS NULL) OR (TipoParametro<>'Texto' AND ValorDecimal IS NOT NULL AND ValorTexto IS NULL)) OR TipoParametro IS NULL
UNION ALL
SELECT 'PARAMETRO_CHECK_VIGENCIA','ParametroPlanilla',IdParametroPlanilla FROM ParametroPlanilla WHERE FechaVigenciaFin<FechaVigenciaInicio
UNION ALL
SELECT 'PARAMETRO_DESCRIPCION_EXCEDE_300','ParametroPlanilla',IdParametroPlanilla FROM ParametroPlanilla WHERE Codigo IN ('PORCENTAJE_CARGAS_OBRERAS','PORCENTAJE_CARGAS_PATRONALES','PORCENTAJE_RIESGO_TRABAJO','PORCENTAJE_PROVISIONES','HORAS_MENSUALES_REFERENCIA') AND CHAR_LENGTH(CONCAT(COALESCE(Descripcion,''),' [LEGADO / NO NORMATIVO / NO REPRODUCIBLE]'))>300
UNION ALL
SELECT 'PARAMETRO_UNIQUE_DUPLICADO','ParametroPlanilla',p.IdParametroPlanilla FROM ParametroPlanilla p WHERE EXISTS (SELECT 1 FROM ParametroPlanilla q WHERE q.IdParametroPlanilla<>p.IdParametroPlanilla AND q.Codigo=p.Codigo AND q.FechaVigenciaInicio=p.FechaVigenciaInicio)
UNION ALL
SELECT 'PLANILLA_PERIODO_INVALIDO','Planilla',p.IdPlanilla FROM Planilla p LEFT JOIN PeriodoPlanilla pp ON pp.IdPeriodoPlanilla=p.IdPeriodoPlanilla WHERE pp.IdPeriodoPlanilla IS NULL OR pp.FechaInicio IS NULL OR pp.FechaFin IS NULL OR pp.FechaInicio>pp.FechaFin OR pp.TipoPeriodo IS NULL OR CHAR_LENGTH(pp.TipoPeriodo)>20 OR COALESCE(p.FechaCalculo,p.FechaCreacion) IS NULL
UNION ALL
SELECT 'DETALLE_ORIGEN_INVALIDO','DetallePlanilla',d.IdDetallePlanilla FROM DetallePlanilla d LEFT JOIN Planilla p ON p.IdPlanilla=d.IdPlanilla LEFT JOIN Colaborador c ON c.IdColaborador=d.IdColaborador LEFT JOIN Puesto pu ON pu.IdPuesto=c.IdPuesto LEFT JOIN Departamento de ON de.IdDepartamento=c.IdDepartamento WHERE p.IdPlanilla IS NULL OR c.IdColaborador IS NULL OR pu.IdPuesto IS NULL OR de.IdDepartamento IS NULL
UNION ALL
SELECT 'DETALLE_DIMENSION_EXCEDE_DESTINO','DetallePlanilla',d.IdDetallePlanilla FROM DetallePlanilla d JOIN Colaborador c ON c.IdColaborador=d.IdColaborador LEFT JOIN Puesto pu ON pu.IdPuesto=c.IdPuesto LEFT JOIN Departamento de ON de.IdDepartamento=c.IdDepartamento WHERE CHAR_LENGTH(c.CodigoColaborador)>20 OR CHAR_LENGTH(CONCAT_WS(' ',c.Nombre,c.PrimerApellido,c.SegundoApellido))>320 OR CHAR_LENGTH(pu.Nombre)>150 OR CHAR_LENGTH(de.Nombre)>150
UNION ALL
SELECT 'DETALLE_UNIQUE_DUPLICADO','DetallePlanilla',d.IdDetallePlanilla FROM DetallePlanilla d WHERE EXISTS(SELECT 1 FROM DetallePlanilla x WHERE x.IdDetallePlanilla<>d.IdDetallePlanilla AND x.IdPlanilla=d.IdPlanilla AND x.IdColaborador=d.IdColaborador)
UNION ALL
SELECT 'FORECAST_ASIGNACION_ORIGEN_INVALIDO','ForecastAsignacionProyecto',a.IdForecastAsignacionProyecto FROM ForecastAsignacionProyecto a LEFT JOIN Proyecto p ON p.IdProyecto=a.IdProyecto LEFT JOIN ForecastPeriodo fp ON fp.IdForecastPeriodo=a.IdForecastPeriodo AND fp.IdForecastEscenario=a.IdForecastEscenario LEFT JOIN ForecastParticipante fpa ON fpa.IdForecastParticipante=a.IdForecastParticipante AND fpa.IdForecastEscenario=a.IdForecastEscenario LEFT JOIN ForecastEscenario e ON e.IdForecastEscenario=a.IdForecastEscenario WHERE p.IdProyecto IS NULL OR fp.IdForecastPeriodo IS NULL OR fpa.IdForecastParticipante IS NULL OR e.IdForecastEscenario IS NULL OR a.Porcentaje IS NULL OR a.Porcentaje<=0 OR a.Porcentaje>100
UNION ALL
SELECT 'FORECAST_ASIGNACION_DUPLICADA','ForecastAsignacionProyecto',a.IdForecastAsignacionProyecto FROM ForecastAsignacionProyecto a WHERE EXISTS(SELECT 1 FROM ForecastAsignacionProyecto x WHERE x.IdForecastAsignacionProyecto<>a.IdForecastAsignacionProyecto AND x.IdForecastEscenario=a.IdForecastEscenario AND x.IdForecastPeriodo=a.IdForecastPeriodo AND x.IdForecastParticipante=a.IdForecastParticipante AND COALESCE(x.IdProyecto,0)=COALESCE(a.IdProyecto,0))
UNION ALL
SELECT 'FORECAST_DETALLE_SIN_ASIGNACION_ORIGEN','ForecastDetalle',d.IdForecastDetalle FROM ForecastDetalle d LEFT JOIN ForecastAsignacionProyecto a ON a.IdForecastEscenario=d.IdForecastEscenario AND a.IdForecastPeriodo=d.IdForecastPeriodo AND a.IdForecastParticipante=d.IdForecastParticipante AND a.IdProyecto=d.IdProyecto WHERE a.IdForecastAsignacionProyecto IS NULL
UNION ALL
SELECT 'FORECAST_DETALLE_DUPLICADO','ForecastDetalle',d.IdForecastDetalle FROM ForecastDetalle d WHERE EXISTS(SELECT 1 FROM ForecastDetalle x WHERE x.IdForecastDetalle<>d.IdForecastDetalle AND x.IdForecastEscenario=d.IdForecastEscenario AND x.IdForecastPeriodo=d.IdForecastPeriodo AND x.IdForecastParticipante=d.IdForecastParticipante AND x.IdProyecto=d.IdProyecto AND x.Concepto=d.Concepto)
)
SELECT COALESCE(JSON_ARRAYAGG(JSON_OBJECT('Control',Control,'Tabla',Tabla,'Id',CAST(Id AS CHAR))),JSON_ARRAY()) INTO @fin02b_bloqueos_datos FROM bloqueos;
SELECT Control, Tabla, Id FROM JSON_TABLE(@fin02b_bloqueos_datos, '$[*]' COLUMNS(Control VARCHAR(100) PATH '$.Control', Tabla VARCHAR(100) PATH '$.Tabla', Id VARCHAR(255) PATH '$.Id')) b;
SET @fin02b_guardia = IF(JSON_LENGTH(@fin02b_bloqueos_datos)=0,1,JSON_EXTRACT('FIN02B_ABORTADO_REVISAR_BLOQUEOS_PREVIOS','$'));

/* Huellas por PK de TODOS los importes originales; NULL conserva su significado.
   Guardar @fin02b_importes_antes fuera de MySQL junto al backup antes del upgrade.
   No guardar ni publicar la cadena de conexión. No depende del orden de filas. */
SELECT COALESCE(JSON_ARRAYAGG(JSON_OBJECT('Tabla', Tabla, 'Id', CAST(Id AS CHAR),
    'Huella', SHA2(CAST(Datos AS CHAR), 256))), JSON_ARRAY())
INTO @fin02b_importes_actuales
FROM (
    SELECT 'Contrato' Tabla, IdContrato Id, JSON_ARRAY(IdContrato, IdColaborador, SalarioBase) Datos FROM Contrato
    UNION ALL
    SELECT 'ParametroPlanilla' Tabla, IdParametroPlanilla Id, JSON_ARRAY(IdParametroPlanilla, ValorDecimal) Datos FROM ParametroPlanilla
    UNION ALL
    SELECT 'ParametroPlanillaColaborador' Tabla, IdParametroPlanillaColaborador Id, JSON_ARRAY(IdParametroPlanillaColaborador, IdParametroPlanilla, IdColaborador, ValorDecimalOverride) Datos FROM ParametroPlanillaColaborador
    UNION ALL
    SELECT 'ParametroPlanillaPeriodo' Tabla, IdParametroPlanillaPeriodo Id, JSON_ARRAY(IdParametroPlanillaPeriodo, IdParametroPlanilla, IdPeriodoPlanilla, ValorDecimalOverride) Datos FROM ParametroPlanillaPeriodo
    UNION ALL
    SELECT 'IncidenciaPlanilla' Tabla, IdIncidenciaPlanilla Id, JSON_ARRAY(IdIncidenciaPlanilla, IdPeriodoPlanilla, IdColaborador, Cantidad, Monto) Datos FROM IncidenciaPlanilla
    UNION ALL
    SELECT 'Planilla' Tabla, IdPlanilla Id, JSON_ARRAY(IdPlanilla, IdPeriodoPlanilla, SalarioBrutoTotal, DeduccionesTotal, SalarioNetoTotal, CostoPatronalEstimadoTotal) Datos FROM Planilla
    UNION ALL
    SELECT 'DetallePlanilla' Tabla, IdDetallePlanilla Id, JSON_ARRAY(IdDetallePlanilla, IdPlanilla, IdColaborador, SalarioBase, SalarioProporcional, TotalHorasExtra, TotalBonos, TotalBeneficiosConfigurables, TotalAusencias, SalarioBruto, TotalDeducciones, SalarioNeto, CostoPatronalEstimado) Datos FROM DetallePlanilla
    UNION ALL
    SELECT 'ForecastEscenario' Tabla, IdForecastEscenario Id, JSON_ARRAY(IdForecastEscenario, MontoProyectadoTotal, MontoRealTotal, DiferenciaTotal) Datos FROM ForecastEscenario
    UNION ALL
    SELECT 'ForecastParticipante' Tabla, IdForecastParticipante Id, JSON_ARRAY(IdForecastParticipante, IdForecastEscenario, SalarioBaseMensual) Datos FROM ForecastParticipante
    UNION ALL
    SELECT 'ForecastParametro' Tabla, IdForecastParametro Id, JSON_ARRAY(IdForecastParametro, IdForecastEscenario, ValorDecimal) Datos FROM ForecastParametro
    UNION ALL
    SELECT 'ForecastAsignacionProyecto' Tabla, IdForecastAsignacionProyecto Id, JSON_ARRAY(IdForecastAsignacionProyecto, IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyecto, Porcentaje) Datos FROM ForecastAsignacionProyecto
    UNION ALL
    SELECT 'ForecastDetalle' Tabla, IdForecastDetalle Id, JSON_ARRAY(IdForecastDetalle, IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyecto, MontoBase, MontoAjuste, MontoProyectado, MontoReal, Diferencia) Datos FROM ForecastDetalle
    UNION ALL
    SELECT 'ColillaPago' Tabla, IdColillaPago Id, JSON_ARRAY(IdColillaPago,IdDetallePlanilla,CodigoColilla,RutaArchivo,FechaGeneracion,GeneradoPor,EstadoRegistro) Datos FROM ColillaPago
    UNION ALL
    SELECT 'PeriodoPlanilla' Tabla, IdPeriodoPlanilla Id, JSON_ARRAY(IdPeriodoPlanilla,CodigoPeriodo,TipoPeriodo,FechaInicio,FechaFin) Datos FROM PeriodoPlanilla
    UNION ALL
    SELECT 'ForecastPeriodo' Tabla, IdForecastPeriodo Id, JSON_ARRAY(IdForecastPeriodo,IdForecastEscenario,NumeroOrden,TipoPeriodo,FechaInicio,FechaFin) Datos FROM ForecastPeriodo
    UNION ALL
    SELECT 'ForecastFuenteHistorica' Tabla, IdForecastFuenteHistorica Id, JSON_ARRAY(IdForecastFuenteHistorica,IdForecastEscenario,IdPlanilla,NumeroOrden) Datos FROM ForecastFuenteHistorica
) importes;

SET @fin02b_importes_antes = @fin02b_importes_actuales;
/* FIN PREFLIGHT F1-FIN-02B-R */

CREATE TABLE ConceptoPlanilla (
    IdConceptoPlanilla BIGINT NOT NULL AUTO_INCREMENT,
    Codigo VARCHAR(80) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    FechaCreacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreadoPor VARCHAR(255) NULL,
    PRIMARY KEY (IdConceptoPlanilla),
    UNIQUE KEY UX_ConceptoPlanilla_Codigo (Codigo),
    CONSTRAINT FK_ConceptoPlanilla_CreadoPor FOREIGN KEY (CreadoPor)
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ConceptoPlanillaVersion (
    IdConceptoPlanillaVersion BIGINT NOT NULL AUTO_INCREMENT,
    IdConceptoPlanilla BIGINT NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    Clasificacion VARCHAR(40) NOT NULL,
    EsSalarial TINYINT(1) NOT NULL,
    EsContributivoCCSS TINYINT(1) NOT NULL,
    EsTributario TINYINT(1) NOT NULL,
    EsPatronal TINYINT(1) NOT NULL,
    TipoBase VARCHAR(50) NOT NULL,
    UnidadPermitida VARCHAR(30) NOT NULL,
    PermiteMontoManual TINYINT(1) NOT NULL DEFAULT 0,
    RequiereAutorizacion TINYINT(1) NOT NULL DEFAULT 0,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdConceptoPlanillaVersion),
    UNIQUE KEY UX_ConceptoVersion_Concepto_Inicio (IdConceptoPlanilla, FechaVigenciaInicio),
    KEY IX_ConceptoVersion_Vigencia (IdConceptoPlanilla, FechaVigenciaInicio, FechaVigenciaFin),
    CONSTRAINT CK_ConceptoVersion_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT CK_ConceptoVersion_Clasificacion CHECK (Clasificacion IN ('IngresoSalarial','IngresoNoSalarial','DeduccionLegal','DeduccionInterna','CargaPatronal')),
    CONSTRAINT CK_ConceptoVersion_Banderas CHECK (EsSalarial IN (0,1) AND EsContributivoCCSS IN (0,1) AND EsTributario IN (0,1) AND EsPatronal IN (0,1) AND PermiteMontoManual IN (0,1) AND RequiereAutorizacion IN (0,1)),
    CONSTRAINT FK_ConceptoVersion_Concepto FOREIGN KEY (IdConceptoPlanilla)
        REFERENCES ConceptoPlanilla(IdConceptoPlanilla) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE VersionNormativa (
    IdVersionNormativa BIGINT NOT NULL AUTO_INCREMENT,
    Codigo VARCHAR(100) NOT NULL,
    Institucion VARCHAR(100) NOT NULL,
    Nombre VARCHAR(200) NOT NULL,
    FuenteOficialUrl VARCHAR(1000) NOT NULL,
    IdentificadorPublicacion VARCHAR(150) NULL,
    FechaPublicacion DATE NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    HuellaContenido CHAR(64) NULL,
    FechaCreacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreadoPor VARCHAR(255) NULL,
    FechaActivacion DATETIME(6) NULL,
    ActivadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdVersionNormativa),
    UNIQUE KEY UX_VersionNormativa_Codigo (Codigo),
    KEY IX_VersionNormativa_Vigencia (Institucion, FechaVigenciaInicio, FechaVigenciaFin),
    CONSTRAINT CK_VersionNormativa_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT FK_VersionNormativa_CreadoPor FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_VersionNormativa_ActivadoPor FOREIGN KEY (ActivadoPor) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ParametroNormativo (
    IdParametroNormativo BIGINT NOT NULL AUTO_INCREMENT,
    Codigo VARCHAR(100) NOT NULL,
    Nombre VARCHAR(200) NOT NULL,
    Institucion VARCHAR(100) NOT NULL,
    Sujeto VARCHAR(30) NOT NULL,
    Unidad VARCHAR(30) NOT NULL,
    CodigoBase VARCHAR(80) NOT NULL,
    EsObligatorio TINYINT(1) NOT NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdParametroNormativo),
    UNIQUE KEY UX_ParametroNormativo_Codigo (Codigo),
    CONSTRAINT CK_ParametroNormativo_Sujeto CHECK (Sujeto IN ('Trabajador','Patrono','Estado','Empresa')),
    CONSTRAINT CK_ParametroNormativo_Obligatorio CHECK (EsObligatorio IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ValorParametroNormativo (
    IdValorParametroNormativo BIGINT NOT NULL AUTO_INCREMENT,
    IdParametroNormativo BIGINT NOT NULL,
    IdVersionNormativa BIGINT NOT NULL,
    ClaveAplicacion VARCHAR(100) NOT NULL,
    ValorDecimal DECIMAL(28,10) NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    Observaciones VARCHAR(500) NULL,
    PRIMARY KEY (IdValorParametroNormativo),
    UNIQUE KEY UX_ValorNormativo_Parametro_Clave_Inicio (IdParametroNormativo, ClaveAplicacion, FechaVigenciaInicio),
    KEY IX_ValorNormativo_Vigencia (IdParametroNormativo, FechaVigenciaInicio, FechaVigenciaFin),
    KEY IX_ValorNormativo_Version (IdVersionNormativa),
    CONSTRAINT CK_ValorNormativo_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT FK_ValorNormativo_Parametro FOREIGN KEY (IdParametroNormativo) REFERENCES ParametroNormativo(IdParametroNormativo) ON DELETE RESTRICT,
    CONSTRAINT FK_ValorNormativo_Version FOREIGN KEY (IdVersionNormativa) REFERENCES VersionNormativa(IdVersionNormativa) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE TramoImpuestoSalario (
    IdTramoImpuestoSalario BIGINT NOT NULL AUTO_INCREMENT,
    IdVersionNormativa BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    LimiteInferior DECIMAL(18,2) NOT NULL,
    LimiteSuperior DECIMAL(18,2) NULL,
    Tasa DECIMAL(18,10) NOT NULL,
    PRIMARY KEY (IdTramoImpuestoSalario),
    UNIQUE KEY UX_TramoImpuesto_Version_Orden (IdVersionNormativa, NumeroOrden),
    UNIQUE KEY UX_TramoImpuesto_Version_Inferior (IdVersionNormativa, LimiteInferior),
    CONSTRAINT CK_TramoImpuesto_Orden CHECK (NumeroOrden > 0),
    CONSTRAINT CK_TramoImpuesto_Limites CHECK (LimiteInferior >= 0 AND (LimiteSuperior IS NULL OR LimiteSuperior > LimiteInferior)),
    CONSTRAINT CK_TramoImpuesto_Tasa CHECK (Tasa >= 0 AND Tasa <= 1),
    CONSTRAINT FK_TramoImpuesto_Version FOREIGN KEY (IdVersionNormativa) REFERENCES VersionNormativa(IdVersionNormativa) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE CreditoFiscalNormativo (
    IdCreditoFiscalNormativo BIGINT NOT NULL AUTO_INCREMENT,
    IdVersionNormativa BIGINT NOT NULL,
    CodigoTipo VARCHAR(80) NOT NULL,
    Nombre VARCHAR(200) NOT NULL,
    MontoUnitario DECIMAL(18,2) NOT NULL,
    Requisitos VARCHAR(1000) NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdCreditoFiscalNormativo),
    UNIQUE KEY UX_CreditoNormativo_Version_Tipo (IdVersionNormativa, CodigoTipo),
    CONSTRAINT CK_CreditoNormativo_Monto CHECK (MontoUnitario >= 0),
    CONSTRAINT CK_CreditoNormativo_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT FK_CreditoNormativo_Version FOREIGN KEY (IdVersionNormativa) REFERENCES VersionNormativa(IdVersionNormativa) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE JornadaLaboral (
    IdJornadaLaboral BIGINT NOT NULL AUTO_INCREMENT,
    Codigo VARCHAR(50) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    TipoJornada VARCHAR(30) NOT NULL,
    HorasOrdinariasDia DECIMAL(8,4) NOT NULL,
    HorasOrdinariasSemana DECIMAL(8,4) NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    FechaCreacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME(6) NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdJornadaLaboral),
    UNIQUE KEY UX_JornadaLaboral_Codigo_Inicio (Codigo, FechaVigenciaInicio),
    CONSTRAINT CK_JornadaLaboral_Horas CHECK (HorasOrdinariasDia > 0 AND HorasOrdinariasSemana > 0),
    CONSTRAINT CK_JornadaLaboral_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT FK_JornadaLaboral_CreadoPor FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_JornadaLaboral_ModificadoPor FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE OcupacionSalarioMinimo (
    IdOcupacionSalarioMinimo BIGINT NOT NULL AUTO_INCREMENT,
    CodigoOficial VARCHAR(100) NOT NULL,
    Nombre VARCHAR(200) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdOcupacionSalarioMinimo),
    UNIQUE KEY UX_OcupacionSalarioMinimo_Codigo (CodigoOficial)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE SalarioMinimoVersion (
    IdSalarioMinimoVersion BIGINT NOT NULL AUTO_INCREMENT,
    IdOcupacionSalarioMinimo BIGINT NOT NULL,
    IdVersionNormativa BIGINT NOT NULL,
    PeriodicidadReferencia VARCHAR(20) NOT NULL,
    TipoJornadaAplicable VARCHAR(30) NULL,
    Monto DECIMAL(18,2) NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    PRIMARY KEY (IdSalarioMinimoVersion),
    UNIQUE KEY UX_SalarioMinimo_Ocupacion_Periodo_Jornada_Inicio (IdOcupacionSalarioMinimo, PeriodicidadReferencia, TipoJornadaAplicable, FechaVigenciaInicio),
    KEY IX_SalarioMinimo_Vigencia (IdOcupacionSalarioMinimo, FechaVigenciaInicio, FechaVigenciaFin),
    CONSTRAINT CK_SalarioMinimo_Monto CHECK (Monto > 0),
    CONSTRAINT CK_SalarioMinimo_Periodicidad CHECK (PeriodicidadReferencia IN ('Diario','Mensual')),
    CONSTRAINT CK_SalarioMinimo_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT FK_SalarioMinimo_Ocupacion FOREIGN KEY (IdOcupacionSalarioMinimo) REFERENCES OcupacionSalarioMinimo(IdOcupacionSalarioMinimo) ON DELETE RESTRICT,
    CONSTRAINT FK_SalarioMinimo_VersionNormativa FOREIGN KEY (IdVersionNormativa) REFERENCES VersionNormativa(IdVersionNormativa) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ConfiguracionRiesgoTrabajo (
    IdConfiguracionRiesgoTrabajo BIGINT NOT NULL AUTO_INCREMENT,
    NumeroPoliza VARCHAR(100) NOT NULL,
    CodigoActividad VARCHAR(100) NOT NULL,
    DescripcionActividad VARCHAR(300) NULL,
    Tarifa DECIMAL(18,10) NOT NULL,
    Descuento DECIMAL(18,10) NOT NULL DEFAULT 0,
    Recargo DECIMAL(18,10) NOT NULL DEFAULT 0,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    ReferenciaDocumento VARCHAR(500) NOT NULL,
    IdVersionNormativa BIGINT NULL,
    FechaCreacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreadoPor VARCHAR(255) NULL,
    FechaModificacion DATETIME(6) NULL,
    ModificadoPor VARCHAR(255) NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdConfiguracionRiesgoTrabajo),
    UNIQUE KEY UX_RiesgoTrabajo_Poliza_Actividad_Inicio (NumeroPoliza, CodigoActividad, FechaVigenciaInicio),
    KEY IX_RiesgoTrabajo_Vigencia (FechaVigenciaInicio, FechaVigenciaFin),
    CONSTRAINT CK_RiesgoTrabajo_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT CK_RiesgoTrabajo_Tasas CHECK (Tarifa >= 0 AND Descuento >= 0 AND Descuento <= 1 AND Recargo >= 0 AND Recargo <= 1 AND (1 - Descuento + Recargo) >= 0),
    CONSTRAINT FK_RiesgoTrabajo_VersionNormativa FOREIGN KEY (IdVersionNormativa) REFERENCES VersionNormativa(IdVersionNormativa) ON DELETE RESTRICT,
    CONSTRAINT FK_RiesgoTrabajo_CreadoPor FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_RiesgoTrabajo_ModificadoPor FOREIGN KEY (ModificadoPor) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE TipoIncidenciaPlanilla
    ADD COLUMN IdConceptoPlanilla BIGINT NULL,
    ADD KEY IX_TipoIncidencia_IdConceptoPlanilla (IdConceptoPlanilla),
    ADD CONSTRAINT FK_TipoIncidencia_ConceptoPlanilla FOREIGN KEY (IdConceptoPlanilla) REFERENCES ConceptoPlanilla(IdConceptoPlanilla) ON DELETE RESTRICT;

ALTER TABLE ParametroPlanilla
    DROP INDEX UX_ParametroPlanilla_Codigo,
    ADD COLUMN IdConceptoPlanilla BIGINT NULL,
    ADD COLUMN EsNormativo TINYINT(1) NOT NULL DEFAULT 0,
    ADD UNIQUE KEY UX_ParametroPlanilla_Codigo_VigenciaInicio (Codigo, FechaVigenciaInicio),
    ADD KEY IX_ParametroPlanilla_IdConceptoPlanilla (IdConceptoPlanilla),
    ADD CONSTRAINT CK_ParametroPlanilla_Vigencia CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaInicio IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    ADD CONSTRAINT CK_ParametroPlanilla_NoNormativo CHECK (EsNormativo = 0),
    ADD CONSTRAINT CK_ParametroPlanilla_Valor CHECK ((TipoParametro = 'Texto' AND ValorTexto IS NOT NULL AND ValorDecimal IS NULL) OR (TipoParametro <> 'Texto' AND ValorDecimal IS NOT NULL AND ValorTexto IS NULL)),
    ADD CONSTRAINT FK_ParametroPlanilla_ConceptoPlanilla FOREIGN KEY (IdConceptoPlanilla) REFERENCES ConceptoPlanilla(IdConceptoPlanilla) ON DELETE RESTRICT;

ALTER TABLE IncidenciaPlanilla
    ADD COLUMN Unidad VARCHAR(30) NULL,
    ADD COLUMN IdConceptoPlanillaVersion BIGINT NULL,
    ADD KEY IX_IncidenciaPlanilla_ConceptoVersion (IdConceptoPlanillaVersion),
    ADD CONSTRAINT FK_IncidenciaPlanilla_ConceptoVersion FOREIGN KEY (IdConceptoPlanillaVersion) REFERENCES ConceptoPlanillaVersion(IdConceptoPlanillaVersion) ON DELETE RESTRICT;

ALTER TABLE Puesto
    ADD COLUMN IdOcupacionSalarioMinimo BIGINT NULL,
    ADD KEY IX_Puesto_IdOcupacionSalarioMinimo (IdOcupacionSalarioMinimo),
    ADD CONSTRAINT FK_Puesto_OcupacionSalarioMinimo FOREIGN KEY (IdOcupacionSalarioMinimo) REFERENCES OcupacionSalarioMinimo(IdOcupacionSalarioMinimo) ON DELETE RESTRICT;

ALTER TABLE Contrato
    ADD COLUMN IdJornadaLaboral BIGINT NULL,
    ADD COLUMN IdConfiguracionRiesgoTrabajo BIGINT NULL,
    ADD KEY IX_Contrato_IdJornadaLaboral (IdJornadaLaboral),
    ADD KEY IX_Contrato_IdConfiguracionRiesgoTrabajo (IdConfiguracionRiesgoTrabajo),
    ADD KEY IX_Contrato_ResolucionTemporal (IdColaborador, FechaInicio, FechaFin, EstadoRegistro),
    ADD CONSTRAINT FK_Contrato_JornadaLaboral FOREIGN KEY (IdJornadaLaboral) REFERENCES JornadaLaboral(IdJornadaLaboral) ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Contrato_RiesgoTrabajo FOREIGN KEY (IdConfiguracionRiesgoTrabajo) REFERENCES ConfiguracionRiesgoTrabajo(IdConfiguracionRiesgoTrabajo) ON DELETE RESTRICT;

CREATE TABLE CreditoFiscalColaborador (
    IdCreditoFiscalColaborador BIGINT NOT NULL AUTO_INCREMENT,
    IdColaborador BIGINT NOT NULL,
    IdCreditoFiscalNormativo BIGINT NOT NULL,
    CantidadAcreditada DECIMAL(8,2) NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    ReferenciaSoporte VARCHAR(300) NOT NULL,
    AprobadoPor VARCHAR(255) NOT NULL,
    FechaAprobacion DATETIME(6) NOT NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdCreditoFiscalColaborador),
    UNIQUE KEY UX_CreditoColab_Concepto_Inicio (IdColaborador, IdCreditoFiscalNormativo, FechaVigenciaInicio),
    KEY IX_CreditoColaborador_Vigencia (IdColaborador, FechaVigenciaInicio, FechaVigenciaFin),
    CONSTRAINT CK_CreditoColaborador_Cantidad CHECK (CantidadAcreditada > 0),
    CONSTRAINT CK_CreditoColaborador_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT FK_CreditoColaborador_Colaborador FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador) ON DELETE RESTRICT,
    CONSTRAINT FK_CreditoColaborador_Normativo FOREIGN KEY (IdCreditoFiscalNormativo) REFERENCES CreditoFiscalNormativo(IdCreditoFiscalNormativo) ON DELETE RESTRICT,
    CONSTRAINT FK_CreditoColaborador_AprobadoPor FOREIGN KEY (AprobadoPor) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE EjecucionPlanilla (
    IdEjecucionPlanilla BIGINT NOT NULL AUTO_INCREMENT,
    IdPlanilla BIGINT NOT NULL,
    NumeroVersion INT NOT NULL,
    EstadoEjecucion VARCHAR(40) NOT NULL,
    EstadoReproducibilidad VARCHAR(40) NOT NULL,
    SnapshotCompleto TINYINT(1) NOT NULL,
    Moneda CHAR(3) NOT NULL DEFAULT 'CRC',
    VersionMotor VARCHAR(100) NULL,
    FechaInicioSnapshot DATE NOT NULL,
    FechaFinSnapshot DATE NOT NULL,
    TipoPeriodoSnapshot VARCHAR(20) NOT NULL,
    FechaCalculo DATETIME(6) NOT NULL,
    CalculadoPor VARCHAR(255) NULL,
    SalarioBrutoTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    IngresosNoSalarialesTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    DeduccionesTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    SalarioNetoTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    CargasPatronalesTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    RiesgosTrabajoTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    OtrosCostosPatronalesTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    CostoLaboralTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    MotivoInvalidacion VARCHAR(500) NULL,
    FechaInvalidacion DATETIME(6) NULL,
    InvalidadoPor VARCHAR(255) NULL,
    PRIMARY KEY (IdEjecucionPlanilla),
    UNIQUE KEY UX_EjecucionPlanilla_Planilla_Version (IdPlanilla, NumeroVersion),
    KEY IX_EjecucionPlanilla_Estado_Fecha (EstadoEjecucion, FechaCalculo),
    CONSTRAINT CK_EjecucionPlanilla_Version CHECK (NumeroVersion > 0),
    CONSTRAINT CK_EjecucionPlanilla_Estado CHECK (EstadoEjecucion IN ('Calculada','InvalidadaPorReapertura','Autorizada','LegadoNoReproducible')),
    CONSTRAINT CK_EjecucionPlanilla_Reproducibilidad CHECK (EstadoReproducibilidad IN ('REPRODUCIBLE','LEGADO_NO_REPRODUCIBLE')),
    CONSTRAINT CK_EjecucionPlanilla_Snapshot CHECK (SnapshotCompleto IN (0,1) AND ((EstadoReproducibilidad = 'REPRODUCIBLE' AND SnapshotCompleto = 1) OR (EstadoReproducibilidad = 'LEGADO_NO_REPRODUCIBLE' AND SnapshotCompleto = 0))),
    CONSTRAINT CK_EjecucionPlanilla_Fechas CHECK (FechaInicioSnapshot <= FechaFinSnapshot),
    CONSTRAINT CK_EjecucionPlanilla_Invalidacion CHECK (EstadoEjecucion <> 'InvalidadaPorReapertura' OR (MotivoInvalidacion IS NOT NULL AND FechaInvalidacion IS NOT NULL AND InvalidadoPor IS NOT NULL)),
    CONSTRAINT FK_EjecucionPlanilla_Planilla FOREIGN KEY (IdPlanilla) REFERENCES Planilla(IdPlanilla) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE Planilla
    ADD COLUMN IdEjecucionPlanillaVigente BIGINT NULL AFTER IdEstadoPlanilla,
    ADD UNIQUE KEY UX_Planilla_EjecucionVigente (IdEjecucionPlanillaVigente),
    ADD CONSTRAINT FK_Planilla_EjecucionVigente FOREIGN KEY (IdEjecucionPlanillaVigente) REFERENCES EjecucionPlanilla(IdEjecucionPlanilla) ON DELETE RESTRICT;

ALTER TABLE DetallePlanilla
    DROP INDEX UX_DetallePlanilla_Planilla_Colaborador,
    ADD COLUMN IdEjecucionPlanilla BIGINT NULL AFTER IdPlanilla,
    ADD COLUMN CodigoColaborador VARCHAR(20) NULL,
    ADD COLUMN NombreCompleto VARCHAR(320) NULL,
    ADD COLUMN NombrePuesto VARCHAR(150) NULL,
    ADD COLUMN NombreDepartamento VARCHAR(150) NULL,
    ADD COLUMN TipoContrato VARCHAR(100) NULL,
    ADD COLUMN PeriodicidadPago VARCHAR(20) NULL,
    ADD COLUMN CodigoJornada VARCHAR(50) NULL,
    ADD COLUMN HorasOrdinariasDia DECIMAL(8,4) NULL,
    ADD COLUMN SalarioContractual DECIMAL(18,2) NULL,
    ADD COLUMN FuenteDimensionesSnapshot VARCHAR(60) NULL,
    ADD COLUMN SnapshotCompleto TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN IngresosNoSalariales DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN CuotasObreras DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN ImpuestoSalario DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN OtrasDeducciones DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN CargasPatronales DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN RiesgosTrabajo DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN OtrosCostosPatronales DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN CostoLaboralTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD KEY IX_DetallePlanilla_IdPlanilla (IdPlanilla),
    ADD CONSTRAINT CK_DetallePlanilla_SnapshotCompleto CHECK (SnapshotCompleto IN (0,1)),
    ADD CONSTRAINT FK_DetallePlanilla_EjecucionPlanilla FOREIGN KEY (IdEjecucionPlanilla) REFERENCES EjecucionPlanilla(IdEjecucionPlanilla) ON DELETE RESTRICT;

INSERT INTO EjecucionPlanilla (
    IdPlanilla, NumeroVersion, EstadoEjecucion, EstadoReproducibilidad, SnapshotCompleto,
    Moneda, VersionMotor, FechaInicioSnapshot, FechaFinSnapshot, TipoPeriodoSnapshot,
    FechaCalculo, CalculadoPor, SalarioBrutoTotal, IngresosNoSalarialesTotal,
    DeduccionesTotal, SalarioNetoTotal, CargasPatronalesTotal, RiesgosTrabajoTotal,
    OtrosCostosPatronalesTotal, CostoLaboralTotal)
SELECT
    p.IdPlanilla, 1, 'LegadoNoReproducible', 'LEGADO_NO_REPRODUCIBLE', 0,
    'CRC', 'LEGADO-SIN-VERSION', pp.FechaInicio, pp.FechaFin, pp.TipoPeriodo,
    COALESCE(p.FechaCalculo, p.FechaCreacion), NULL, p.SalarioBrutoTotal, 0,
    p.DeduccionesTotal, p.SalarioNetoTotal, p.CostoPatronalEstimadoTotal, 0, 0, 0
FROM Planilla p
INNER JOIN PeriodoPlanilla pp ON pp.IdPeriodoPlanilla = p.IdPeriodoPlanilla;

UPDATE Planilla p
INNER JOIN EjecucionPlanilla e ON e.IdPlanilla = p.IdPlanilla AND e.NumeroVersion = 1
SET p.IdEjecucionPlanillaVigente = e.IdEjecucionPlanilla;

UPDATE DetallePlanilla d
INNER JOIN EjecucionPlanilla e ON e.IdPlanilla = d.IdPlanilla AND e.NumeroVersion = 1
INNER JOIN Colaborador c ON c.IdColaborador = d.IdColaborador
LEFT JOIN Puesto pu ON pu.IdPuesto = c.IdPuesto
LEFT JOIN Departamento de ON de.IdDepartamento = c.IdDepartamento
SET d.IdEjecucionPlanilla = e.IdEjecucionPlanilla,
    d.CodigoColaborador = c.CodigoColaborador,
    d.NombreCompleto = CONCAT_WS(' ', c.Nombre, c.PrimerApellido, c.SegundoApellido),
    d.NombrePuesto = pu.Nombre,
    d.NombreDepartamento = de.Nombre,
    d.SalarioContractual = d.SalarioBase,
    d.FuenteDimensionesSnapshot = 'LEGADO_DATOS_VIGENTES_AL_UPGRADE',
    d.SnapshotCompleto = 0;

ALTER TABLE DetallePlanilla
    ADD UNIQUE KEY UX_DetallePlanilla_Ejecucion_Colaborador (IdEjecucionPlanilla, IdColaborador);

CREATE TABLE SegmentoCalculoPlanilla (
    IdSegmentoCalculoPlanilla BIGINT NOT NULL AUTO_INCREMENT,
    IdDetallePlanilla BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    IdContratoOrigen BIGINT NULL,
    SalarioMensual DECIMAL(18,2) NOT NULL,
    CodigoJornada VARCHAR(50) NOT NULL,
    HorasOrdinariasDia DECIMAL(8,4) NOT NULL,
    DiasBase30 DECIMAL(8,4) NOT NULL,
    MontoOrdinarioSinRedondear DECIMAL(28,10) NOT NULL,
    MontoOrdinario DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (IdSegmentoCalculoPlanilla),
    UNIQUE KEY UX_SegmentoPlanilla_Detalle_Orden (IdDetallePlanilla, NumeroOrden),
    KEY IX_SegmentoPlanilla_Fechas (IdDetallePlanilla, FechaInicio, FechaFin),
    KEY IX_SegmentoPlanilla_Contrato (IdContratoOrigen),
    CONSTRAINT CK_SegmentoPlanilla_Orden CHECK (NumeroOrden > 0),
    CONSTRAINT CK_SegmentoPlanilla_Fechas CHECK (FechaInicio <= FechaFin),
    CONSTRAINT CK_SegmentoPlanilla_Valores CHECK (SalarioMensual >= 0 AND HorasOrdinariasDia > 0 AND DiasBase30 >= 0 AND MontoOrdinario >= 0),
    CONSTRAINT FK_SegmentoPlanilla_Detalle FOREIGN KEY (IdDetallePlanilla) REFERENCES DetallePlanilla(IdDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_SegmentoPlanilla_Contrato FOREIGN KEY (IdContratoOrigen) REFERENCES Contrato(IdContrato) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ConceptoDetallePlanilla (
    IdConceptoDetallePlanilla BIGINT NOT NULL AUTO_INCREMENT,
    IdDetallePlanilla BIGINT NOT NULL,
    IdSegmentoCalculoPlanilla BIGINT NULL,
    IdConceptoPlanillaVersion BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    TipoFuente VARCHAR(30) NOT NULL,
    IdFuente BIGINT NULL,
    FechaHecho DATE NULL,
    Cantidad DECIMAL(28,10) NULL,
    Unidad VARCHAR(30) NULL,
    BaseCalculo DECIMAL(28,10) NULL,
    Tasa DECIMAL(28,10) NULL,
    Multiplicador DECIMAL(28,10) NULL,
    MontoSinRedondear DECIMAL(28,10) NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    EsSalarialSnapshot TINYINT(1) NOT NULL,
    EsContributivoCCSSSnapshot TINYINT(1) NOT NULL,
    EsTributarioSnapshot TINYINT(1) NOT NULL,
    EsPatronalSnapshot TINYINT(1) NOT NULL,
    DescripcionSnapshot VARCHAR(300) NOT NULL,
    PRIMARY KEY (IdConceptoDetallePlanilla),
    UNIQUE KEY UX_ConceptoDetalle_Detalle_Orden (IdDetallePlanilla, NumeroOrden),
    KEY IX_ConceptoDetalle_ConceptoVersion (IdConceptoPlanillaVersion),
    KEY IX_ConceptoDetalle_Fuente (TipoFuente, IdFuente),
    KEY IX_ConceptoDetalle_Segmento (IdSegmentoCalculoPlanilla),
    CONSTRAINT CK_ConceptoDetalle_Orden CHECK (NumeroOrden > 0),
    CONSTRAINT CK_ConceptoDetalle_Monto CHECK (Monto >= 0),
    CONSTRAINT CK_ConceptoDetalle_Banderas CHECK (EsSalarialSnapshot IN (0,1) AND EsContributivoCCSSSnapshot IN (0,1) AND EsTributarioSnapshot IN (0,1) AND EsPatronalSnapshot IN (0,1)),
    CONSTRAINT FK_ConceptoDetalle_Detalle FOREIGN KEY (IdDetallePlanilla) REFERENCES DetallePlanilla(IdDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_ConceptoDetalle_Segmento FOREIGN KEY (IdSegmentoCalculoPlanilla) REFERENCES SegmentoCalculoPlanilla(IdSegmentoCalculoPlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_ConceptoDetalle_ConceptoVersion FOREIGN KEY (IdConceptoPlanillaVersion) REFERENCES ConceptoPlanillaVersion(IdConceptoPlanillaVersion) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE AplicacionNormativaDetallePlanilla (
    IdAplicacionNormativaDetalle BIGINT NOT NULL AUTO_INCREMENT,
    IdConceptoDetallePlanilla BIGINT NOT NULL,
    IdValorParametroNormativo BIGINT NULL,
    IdTramoImpuestoSalario BIGINT NULL,
    IdCreditoFiscalNormativo BIGINT NULL,
    IdConfiguracionRiesgoTrabajo BIGINT NULL,
    BaseAplicada DECIMAL(28,10) NULL,
    ValorAplicado DECIMAL(28,10) NULL,
    MontoAplicado DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (IdAplicacionNormativaDetalle),
    KEY IX_AplicacionNormativa_ConceptoDetalle (IdConceptoDetallePlanilla),
    KEY IX_AplicacionNormativa_Valor (IdValorParametroNormativo),
    KEY IX_AplicacionNormativa_Tramo (IdTramoImpuestoSalario),
    KEY IX_AplicacionNormativa_Credito (IdCreditoFiscalNormativo),
    KEY IX_AplicacionNormativa_Riesgo (IdConfiguracionRiesgoTrabajo),
    CONSTRAINT CK_AplicacionNormativa_Referencia CHECK (((IdValorParametroNormativo IS NOT NULL) + (IdTramoImpuestoSalario IS NOT NULL) + (IdCreditoFiscalNormativo IS NOT NULL) + (IdConfiguracionRiesgoTrabajo IS NOT NULL)) = 1),
    CONSTRAINT FK_AplicacionNormativa_ConceptoDetalle FOREIGN KEY (IdConceptoDetallePlanilla) REFERENCES ConceptoDetallePlanilla(IdConceptoDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_AplicacionNormativa_Valor FOREIGN KEY (IdValorParametroNormativo) REFERENCES ValorParametroNormativo(IdValorParametroNormativo) ON DELETE RESTRICT,
    CONSTRAINT FK_AplicacionNormativa_Tramo FOREIGN KEY (IdTramoImpuestoSalario) REFERENCES TramoImpuestoSalario(IdTramoImpuestoSalario) ON DELETE RESTRICT,
    CONSTRAINT FK_AplicacionNormativa_Credito FOREIGN KEY (IdCreditoFiscalNormativo) REFERENCES CreditoFiscalNormativo(IdCreditoFiscalNormativo) ON DELETE RESTRICT,
    CONSTRAINT FK_AplicacionNormativa_Riesgo FOREIGN KEY (IdConfiguracionRiesgoTrabajo) REFERENCES ConfiguracionRiesgoTrabajo(IdConfiguracionRiesgoTrabajo) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE DetallePlanillaDistribucionProyecto (
    IdDetallePlanillaDistribucionProyecto BIGINT NOT NULL AUTO_INCREMENT,
    IdDetallePlanilla BIGINT NOT NULL,
    IdProyecto BIGINT NULL,
    EsSinAsignar TINYINT(1) NOT NULL,
    IdProyectoNormalizado BIGINT GENERATED ALWAYS AS (COALESCE(IdProyecto, 0)) STORED,
    Porcentaje DECIMAL(7,4) NOT NULL,
    MontoReal DECIMAL(18,2) NOT NULL,
    MontoSinRedondear DECIMAL(28,10) NOT NULL,
    NumeroOrden INT NOT NULL,
    PRIMARY KEY (IdDetallePlanillaDistribucionProyecto),
    UNIQUE KEY UX_DetalleDistribucion_Detalle_Proyecto (IdDetallePlanilla, IdProyectoNormalizado),
    KEY IX_DetalleDistribucion_Proyecto (IdProyecto),
    CONSTRAINT CK_DetalleDistribucion_Destino CHECK ((EsSinAsignar = 1 AND IdProyecto IS NULL) OR (EsSinAsignar = 0 AND IdProyecto IS NOT NULL)),
    CONSTRAINT CK_DetalleDistribucion_Porcentaje CHECK (Porcentaje > 0 AND Porcentaje <= 100),
    CONSTRAINT CK_DetalleDistribucion_Valores CHECK (MontoReal >= 0 AND NumeroOrden > 0),
    CONSTRAINT FK_DetalleDistribucion_Detalle FOREIGN KEY (IdDetallePlanilla) REFERENCES DetallePlanilla(IdDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_DetalleDistribucion_Proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE ForecastEscenario
    ADD COLUMN VersionMotor VARCHAR(100) NULL,
    ADD COLUMN Moneda CHAR(3) NOT NULL DEFAULT 'CRC',
    ADD COLUMN TieneHistoricoLimitado TINYINT(1) NOT NULL DEFAULT 0,
    ADD CONSTRAINT CK_ForecastEscenario_HistoricoLimitado CHECK (TieneHistoricoLimitado IN (0,1));

UPDATE ForecastEscenario SET VersionMotor = 'LEGADO-BRUTO' WHERE VersionMotor IS NULL;

ALTER TABLE ForecastDetalle DROP FOREIGN KEY FK_FcstDetalle_AsignacionProyecto;

ALTER TABLE ForecastAsignacionProyecto
    DROP INDEX UX_FcstAsign_Esc_Per_Part_Proy,
    MODIFY COLUMN IdProyecto BIGINT NULL,
    ADD COLUMN EsSinAsignar TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN IdProyectoNormalizado BIGINT GENERATED ALWAYS AS (COALESCE(IdProyecto, 0)) STORED,
    ADD UNIQUE KEY UX_FcstAsign_Esc_Per_Part_ProyNorm (IdForecastEscenario, IdForecastPeriodo, IdForecastParticipante, IdProyectoNormalizado),
    ADD CONSTRAINT CK_ForecastAsignacionProyecto_Destino CHECK ((EsSinAsignar = 1 AND IdProyecto IS NULL) OR (EsSinAsignar = 0 AND IdProyecto IS NOT NULL));

ALTER TABLE ForecastDetalle
    MODIFY COLUMN IdProyecto BIGINT NULL,
    ADD COLUMN IdForecastAsignacionProyecto BIGINT NULL,
    ADD COLUMN IdConceptoPlanillaVersion BIGINT NULL,
    ADD COLUMN MontoBaseSinRedondear DECIMAL(28,10) NULL,
    ADD COLUMN MontoProyectadoSinRedondear DECIMAL(28,10) NULL,
    ADD COLUMN CantidadObservaciones INT NOT NULL DEFAULT 0,
    ADD COLUMN HistoricoLimitado TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN VersionMotor VARCHAR(100) NULL,
    ADD COLUMN DiferenciaFirmada DECIMAL(18,2) NULL,
    ADD COLUMN DiferenciaAbsoluta DECIMAL(18,2) NULL,
    ADD COLUMN DiferenciaPorcentual DECIMAL(18,4) NULL,
    ADD KEY IX_FcstDetalle_ConceptoVersion (IdConceptoPlanillaVersion),
    ADD CONSTRAINT CK_ForecastDetalle_Observaciones CHECK (CantidadObservaciones >= 0 AND HistoricoLimitado IN (0,1)),
    ADD CONSTRAINT CK_ForecastDetalle_DiferenciaAbsoluta CHECK (DiferenciaAbsoluta IS NULL OR DiferenciaAbsoluta >= 0);

UPDATE ForecastDetalle d
INNER JOIN ForecastAsignacionProyecto a
    ON a.IdForecastEscenario = d.IdForecastEscenario
    AND a.IdForecastPeriodo = d.IdForecastPeriodo
    AND a.IdForecastParticipante = d.IdForecastParticipante
    AND a.IdProyecto = d.IdProyecto
SET d.IdForecastAsignacionProyecto = a.IdForecastAsignacionProyecto,
    d.VersionMotor = 'LEGADO-BRUTO';

ALTER TABLE ForecastDetalle
    ADD UNIQUE KEY UX_FcstDetalle_Asignacion_Concepto (IdForecastAsignacionProyecto, Concepto),
    ADD CONSTRAINT FK_FcstDetalle_AsignacionProyecto FOREIGN KEY (IdForecastAsignacionProyecto) REFERENCES ForecastAsignacionProyecto(IdForecastAsignacionProyecto) ON DELETE CASCADE,
    ADD CONSTRAINT FK_FcstDetalle_ConceptoVersion FOREIGN KEY (IdConceptoPlanillaVersion) REFERENCES ConceptoPlanillaVersion(IdConceptoPlanillaVersion) ON DELETE RESTRICT;

CREATE TABLE ForecastObservacionHistorica (
    IdForecastObservacionHistorica BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    IdForecastParticipante BIGINT NOT NULL,
    IdForecastFuenteHistorica BIGINT NOT NULL,
    IdDetallePlanilla BIGINT NULL,
    IdConceptoPlanillaVersion BIGINT NOT NULL,
    MontoObservado DECIMAL(18,2) NULL,
    EsComparable TINYINT(1) NOT NULL,
    MotivoNoComparable VARCHAR(100) NULL,
    PRIMARY KEY (IdForecastObservacionHistorica),
    UNIQUE KEY UX_ForecastObservacion_Origen_Concepto (IdForecastEscenario, IdForecastParticipante, IdForecastFuenteHistorica, IdConceptoPlanillaVersion),
    KEY IX_ForecastObservacion_Fuente (IdForecastFuenteHistorica),
    KEY IX_ForecastObservacion_Participante (IdForecastParticipante),
    KEY IX_ForecastObservacion_Detalle (IdDetallePlanilla),
    KEY IX_ForecastObservacion_Concepto (IdConceptoPlanillaVersion),
    CONSTRAINT CK_ForecastObservacion_Comparabilidad CHECK ((EsComparable = 1 AND IdDetallePlanilla IS NOT NULL AND MontoObservado IS NOT NULL AND MotivoNoComparable IS NULL) OR (EsComparable = 0 AND MotivoNoComparable IS NOT NULL)),
    CONSTRAINT FK_ForecastObservacion_Escenario FOREIGN KEY (IdForecastEscenario) REFERENCES ForecastEscenario(IdForecastEscenario) ON DELETE CASCADE,
    CONSTRAINT FK_ForecastObservacion_Participante FOREIGN KEY (IdForecastParticipante) REFERENCES ForecastParticipante(IdForecastParticipante) ON DELETE CASCADE,
    CONSTRAINT FK_ForecastObservacion_Fuente FOREIGN KEY (IdForecastFuenteHistorica) REFERENCES ForecastFuenteHistorica(IdForecastFuenteHistorica) ON DELETE RESTRICT,
    CONSTRAINT FK_ForecastObservacion_DetallePlanilla FOREIGN KEY (IdDetallePlanilla) REFERENCES DetallePlanilla(IdDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_ForecastObservacion_ConceptoVersion FOREIGN KEY (IdConceptoPlanillaVersion) REFERENCES ConceptoPlanillaVersion(IdConceptoPlanillaVersion) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

UPDATE ParametroPlanilla
SET EstadoRegistro = 'Inactivo',
    Descripcion = CONCAT(COALESCE(Descripcion, ''), ' [LEGADO / NO NORMATIVO / NO REPRODUCIBLE]')
WHERE Codigo IN (
    'PORCENTAJE_CARGAS_OBRERAS',
    'PORCENTAJE_CARGAS_PATRONALES',
    'PORCENTAJE_RIESGO_TRABAJO',
    'PORCENTAJE_PROVISIONES',
    'HORAS_MENSUALES_REFERENCIA'
);

/* Fin. Validar con 20260831_F1_FIN_02B_Verify.sql antes de desplegar código. */

/* F1-FIN-02D: condiciones laborales efectivas. Sin backfill histórico. */
CREATE TABLE CondicionLaboralVersion (
    IdCondicionLaboralVersion BIGINT NOT NULL AUTO_INCREMENT,
    IdContrato BIGINT NOT NULL,
    FechaVigenciaInicio DATE NOT NULL,
    FechaVigenciaFin DATE NULL,
    SalarioBase DECIMAL(18,2) NOT NULL,
    IdPuesto INT NOT NULL,
    IdDepartamento INT NOT NULL,
    IdOcupacionSalarioMinimo BIGINT NOT NULL,
    IdJornadaLaboral BIGINT NOT NULL,
    ReferenciaSoporte VARCHAR(500) NOT NULL,
    FechaCreacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreadoPor VARCHAR(255) NOT NULL,
    EstadoRegistro ENUM('Activo','Inactivo') NOT NULL DEFAULT 'Activo',
    PRIMARY KEY (IdCondicionLaboralVersion),
    UNIQUE KEY UX_CondicionLaboral_Contrato_Inicio (IdContrato,FechaVigenciaInicio),
    KEY IX_CondicionLaboral_Contrato_Vigencia (IdContrato,FechaVigenciaInicio,FechaVigenciaFin),
    KEY IX_CondicionLaboral_Puesto (IdPuesto),
    KEY IX_CondicionLaboral_Departamento (IdDepartamento),
    KEY IX_CondicionLaboral_Ocupacion (IdOcupacionSalarioMinimo),
    KEY IX_CondicionLaboral_Jornada (IdJornadaLaboral),
    KEY IX_CondicionLaboral_CreadoPor (CreadoPor),
    CONSTRAINT CK_CondicionLaboral_Fechas CHECK (FechaVigenciaFin IS NULL OR FechaVigenciaFin >= FechaVigenciaInicio),
    CONSTRAINT CK_CondicionLaboral_Salario CHECK (SalarioBase > 0),
    CONSTRAINT CK_CondicionLaboral_Soporte CHECK (CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0),
    CONSTRAINT FK_CondicionLaboral_Contrato FOREIGN KEY (IdContrato) REFERENCES Contrato(IdContrato) ON DELETE RESTRICT,
    CONSTRAINT FK_CondicionLaboral_Puesto FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto) ON DELETE RESTRICT,
    CONSTRAINT FK_CondicionLaboral_Departamento FOREIGN KEY (IdDepartamento) REFERENCES Departamento(IdDepartamento) ON DELETE RESTRICT,
    CONSTRAINT FK_CondicionLaboral_Ocupacion FOREIGN KEY (IdOcupacionSalarioMinimo) REFERENCES OcupacionSalarioMinimo(IdOcupacionSalarioMinimo) ON DELETE RESTRICT,
    CONSTRAINT FK_CondicionLaboral_Jornada FOREIGN KEY (IdJornadaLaboral) REFERENCES JornadaLaboral(IdJornadaLaboral) ON DELETE RESTRICT,
    CONSTRAINT FK_CondicionLaboral_CreadoPor FOREIGN KEY (CreadoPor) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* F1-FIN-02F: confirmaciones inmutables de incidencias. Sin backfill. */
CREATE TABLE IncidenciaPlanillaConfirmacion (
    IdIncidenciaPlanillaConfirmacion BIGINT NOT NULL AUTO_INCREMENT,
    IdIncidenciaPlanilla BIGINT NOT NULL,
    NumeroVersion INT NOT NULL,
    FechaHecho DATE NOT NULL,
    IdConceptoPlanillaVersion BIGINT NOT NULL,
    Unidad VARCHAR(30) NOT NULL,
    Cantidad DECIMAL(18,2) NULL,
    MontoManual DECIMAL(18,2) NULL,
    TipoDiaHE VARCHAR(20) NULL,
    ReferenciaSoporte VARCHAR(500) NOT NULL,
    HuellaIncidencia CHAR(64) NOT NULL,
    ConfirmadoPor VARCHAR(255) NOT NULL,
    FechaConfirmacion DATETIME(6) NOT NULL,
    AutorizacionRequerida TINYINT(1) NOT NULL,
    AutorizadoPor VARCHAR(255) NULL,
    FechaAutorizacion DATETIME(6) NULL,
    ReferenciaAutorizacion VARCHAR(500) NULL,
    IdParametroPlanilla INT NULL,
    IdCondicionBase BIGINT NULL,
    FechaBaseInicio DATE NULL,
    FechaBaseFin DATE NULL,
    PRIMARY KEY (IdIncidenciaPlanillaConfirmacion),
    UNIQUE KEY UX_IPC_Incidencia_Version (IdIncidenciaPlanilla,NumeroVersion),
    KEY IX_IPC_ConceptoVersion (IdConceptoPlanillaVersion),
    KEY IX_IPC_ConfirmadoPor (ConfirmadoPor),
    KEY IX_IPC_AutorizadoPor (AutorizadoPor),
    KEY IX_IPC_Parametro (IdParametroPlanilla),
    KEY IX_IPC_CondicionBase (IdCondicionBase),
    CONSTRAINT CK_IPC_Version CHECK (NumeroVersion > 0),
    CONSTRAINT CK_IPC_UnidadCantidad CHECK ((Unidad IN ('Hora','Día') AND Cantidad IS NOT NULL AND Cantidad > 0) OR (Unidad IN ('Monto','Porcentaje') AND Cantidad IS NULL)),
    CONSTRAINT CK_IPC_Monto CHECK (MontoManual IS NULL OR (Unidad='Monto' AND MontoManual >= 0)),
    CONSTRAINT CK_IPC_TipoDia CHECK (TipoDiaHE IS NULL OR TipoDiaHE IN ('Ordinario','Feriado','Descanso','Especial')),
    CONSTRAINT CK_IPC_Soporte CHECK (CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0),
    CONSTRAINT CK_IPC_Huella CHECK (CHAR_LENGTH(HuellaIncidencia)=64 AND REGEXP_LIKE(HuellaIncidencia,'^[A-F0-9]{64}$','c')),
    CONSTRAINT CK_IPC_Autorizacion CHECK (AutorizacionRequerida IN (0,1) AND (
      (AutorizacionRequerida=0 AND AutorizadoPor IS NULL AND FechaAutorizacion IS NULL AND ReferenciaAutorizacion IS NULL)
      OR (AutorizadoPor IS NOT NULL AND FechaAutorizacion IS NOT NULL AND ReferenciaAutorizacion IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAutorizacion)) > 0))),
    CONSTRAINT CK_IPC_Base CHECK ((IdCondicionBase IS NULL AND FechaBaseInicio IS NULL AND FechaBaseFin IS NULL)
      OR (IdParametroPlanilla IS NOT NULL AND IdCondicionBase IS NOT NULL AND FechaBaseInicio IS NOT NULL AND FechaBaseFin IS NOT NULL AND FechaBaseFin >= FechaBaseInicio AND FechaHecho BETWEEN FechaBaseInicio AND FechaBaseFin)),
    CONSTRAINT FK_IPC_Incidencia FOREIGN KEY (IdIncidenciaPlanilla) REFERENCES IncidenciaPlanilla(IdIncidenciaPlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_IPC_ConceptoVersion FOREIGN KEY (IdConceptoPlanillaVersion) REFERENCES ConceptoPlanillaVersion(IdConceptoPlanillaVersion) ON DELETE RESTRICT,
    CONSTRAINT FK_IPC_ConfirmadoPor FOREIGN KEY (ConfirmadoPor) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_IPC_AutorizadoPor FOREIGN KEY (AutorizadoPor) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_IPC_Parametro FOREIGN KEY (IdParametroPlanilla) REFERENCES ParametroPlanilla(IdParametroPlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_IPC_CondicionBase FOREIGN KEY (IdCondicionBase) REFERENCES CondicionLaboralVersion(IdCondicionLaboralVersion) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
DELIMITER $$
CREATE TRIGGER TR_IPC_NoUpdate BEFORE UPDATE ON IncidenciaPlanillaConfirmacion FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='CONFIRMACION_INMUTABLE_CREAR_NUEVA_VERSION';
END$$
CREATE TRIGGER TR_IPC_NoDelete BEFORE DELETE ON IncidenciaPlanillaConfirmacion FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='CONFIRMACION_INMUTABLE_CONSERVAR_EVIDENCIA';
END$$
DELIMITER ;

/* F1-FIN-02J: evidencia de snapshot J-P01..06, sin backfill. */
ALTER TABLE SegmentoCalculoPlanilla
    ADD COLUMN IdCondicionLaboralVersion BIGINT NULL,
    ADD COLUMN IdJornadaLaboral BIGINT NULL,
    ADD COLUMN IdSalarioMinimoVersion BIGINT NULL,
    ADD COLUMN NombrePuesto VARCHAR(150) NULL,
    ADD COLUMN NombreDepartamento VARCHAR(150) NULL,
    ADD COLUMN TipoContrato VARCHAR(100) NULL,
    ADD COLUMN PeriodicidadPago VARCHAR(20) NULL,
    ADD COLUMN CodigoOcupacion VARCHAR(100) NULL,
    ADD COLUMN NombreOcupacion VARCHAR(200) NULL,
    ADD COLUMN TipoJornada VARCHAR(30) NULL,
    ADD COLUMN HorasOrdinariasSemana DECIMAL(65,28) NULL,
    ADD COLUMN FechaJornadaInicio DATE NULL,
    ADD COLUMN FechaJornadaFin DATE NULL,
    ADD COLUMN MontoMinimo DECIMAL(65,28) NULL,
    ADD COLUMN PeriodicidadMinimo VARCHAR(20) NULL,
    ADD COLUMN TipoJornadaMinimo VARCHAR(30) NULL,
    ADD COLUMN FechaMinimoInicio DATE NULL,
    ADD COLUMN FechaMinimoFin DATE NULL,
    ADD COLUMN ReferenciaRelacionOrdinaria VARCHAR(500) NULL,
    MODIFY COLUMN MontoOrdinarioSinRedondear DECIMAL(65,28) NOT NULL,
    ADD KEY IX_SJ_IdCondicionLaboralVersion (IdCondicionLaboralVersion),
    ADD CONSTRAINT FK_SJ_IdCondicionLaboralVersion FOREIGN KEY (IdCondicionLaboralVersion) REFERENCES CondicionLaboralVersion(IdCondicionLaboralVersion) ON DELETE RESTRICT,
    ADD KEY IX_SJ_IdJornadaLaboral (IdJornadaLaboral),
    ADD CONSTRAINT FK_SJ_IdJornadaLaboral FOREIGN KEY (IdJornadaLaboral) REFERENCES JornadaLaboral(IdJornadaLaboral) ON DELETE RESTRICT,
    ADD KEY IX_SJ_IdSalarioMinimoVersion (IdSalarioMinimoVersion),
    ADD CONSTRAINT FK_SJ_IdSalarioMinimoVersion FOREIGN KEY (IdSalarioMinimoVersion) REFERENCES SalarioMinimoVersion(IdSalarioMinimoVersion) ON DELETE RESTRICT,
    ADD CONSTRAINT CK_SJ_FechasJornada CHECK (FechaJornadaFin IS NULL OR (FechaJornadaInicio IS NOT NULL AND FechaJornadaFin >= FechaJornadaInicio)),
    ADD CONSTRAINT CK_SJ_FechasMinimo CHECK (FechaMinimoFin IS NULL OR (FechaMinimoInicio IS NOT NULL AND FechaMinimoFin >= FechaMinimoInicio));

ALTER TABLE ConceptoDetallePlanilla
    ADD COLUMN IdIncidenciaPlanillaConfirmacion BIGINT NULL,
    ADD COLUMN IdParametroPlanilla INT NULL,
    ADD COLUMN AmbitoParametro VARCHAR(20) NULL,
    ADD COLUMN IdParametroPlanillaPeriodo BIGINT NULL,
    ADD COLUMN IdParametroPlanillaColaborador BIGINT NULL,
    ADD COLUMN ValorParametroAplicado DECIMAL(65,28) NULL,
    ADD COLUMN UnidadParametroAplicada VARCHAR(30) NULL,
    ADD COLUMN ClaveHecho VARCHAR(300) NULL,
    ADD COLUMN UnidadCantidad VARCHAR(30) NULL,
    ADD COLUMN ReferenciaSoporte VARCHAR(500) NULL,
    ADD COLUMN ReferenciaAutorizacion VARCHAR(500) NULL,
    ADD COLUMN RegistradoPorHecho VARCHAR(255) NULL,
    ADD COLUMN AutorizadoPorHecho VARCHAR(255) NULL,
    ADD COLUMN IdCondicionLaboralBase BIGINT NULL,
    ADD COLUMN FechaBaseInicio DATE NULL,
    ADD COLUMN FechaBaseFin DATE NULL,
    MODIFY COLUMN Cantidad DECIMAL(65,28) NULL,
    MODIFY COLUMN BaseCalculo DECIMAL(65,28) NULL,
    MODIFY COLUMN Tasa DECIMAL(65,28) NULL,
    MODIFY COLUMN Multiplicador DECIMAL(65,28) NULL,
    MODIFY COLUMN MontoSinRedondear DECIMAL(65,28) NOT NULL,
    ADD KEY IX_LJ_IdIncidenciaPlanillaConfirmacion (IdIncidenciaPlanillaConfirmacion),
    ADD KEY IX_LJ_IdCondicionLaboralBase (IdCondicionLaboralBase),
    ADD CONSTRAINT FK_LJ_IdCondicionLaboralBase FOREIGN KEY (IdCondicionLaboralBase) REFERENCES CondicionLaboralVersion(IdCondicionLaboralVersion) ON DELETE RESTRICT,
    ADD CONSTRAINT CK_LJ_BaseHecho CHECK ((IdCondicionLaboralBase IS NULL AND FechaBaseInicio IS NULL AND FechaBaseFin IS NULL) OR (IdCondicionLaboralBase IS NOT NULL AND FechaBaseInicio IS NOT NULL AND FechaBaseFin IS NOT NULL AND FechaBaseFin>=FechaBaseInicio AND TipoFuente='Derivado' AND EsPatronalSnapshot=1 AND IdSegmentoCalculoPlanilla IS NULL)),
    ADD CONSTRAINT FK_LJ_IdIncidenciaPlanillaConfirmacion FOREIGN KEY (IdIncidenciaPlanillaConfirmacion) REFERENCES IncidenciaPlanillaConfirmacion(IdIncidenciaPlanillaConfirmacion) ON DELETE RESTRICT,
    ADD KEY IX_LJ_IdParametroPlanilla (IdParametroPlanilla),
    ADD CONSTRAINT FK_LJ_IdParametroPlanilla FOREIGN KEY (IdParametroPlanilla) REFERENCES ParametroPlanilla(IdParametroPlanilla) ON DELETE RESTRICT,
    ADD KEY IX_LJ_IdParametroPlanillaPeriodo (IdParametroPlanillaPeriodo),
    ADD CONSTRAINT FK_LJ_IdParametroPlanillaPeriodo FOREIGN KEY (IdParametroPlanillaPeriodo) REFERENCES ParametroPlanillaPeriodo(IdParametroPlanillaPeriodo) ON DELETE RESTRICT,
    ADD KEY IX_LJ_IdParametroPlanillaColaborador (IdParametroPlanillaColaborador),
    ADD CONSTRAINT FK_LJ_IdParametroPlanillaColaborador FOREIGN KEY (IdParametroPlanillaColaborador) REFERENCES ParametroPlanillaColaborador(IdParametroPlanillaColaborador) ON DELETE RESTRICT,
    ADD CONSTRAINT CK_LJ_Parametro CHECK ((IdParametroPlanilla IS NULL AND AmbitoParametro IS NULL AND IdParametroPlanillaPeriodo IS NULL AND IdParametroPlanillaColaborador IS NULL AND ValorParametroAplicado IS NULL AND UnidadParametroAplicada IS NULL) OR (IdParametroPlanilla IS NOT NULL AND AmbitoParametro IS NOT NULL AND ValorParametroAplicado IS NOT NULL AND ValorParametroAplicado >= 0 AND UnidadParametroAplicada IN ('Monto','Porcentaje') AND UnidadParametroAplicada IS NOT NULL AND ((AmbitoParametro='Global' AND IdParametroPlanillaPeriodo IS NULL AND IdParametroPlanillaColaborador IS NULL) OR (AmbitoParametro='Periodo' AND IdParametroPlanillaPeriodo IS NOT NULL AND IdParametroPlanillaColaborador IS NULL) OR (AmbitoParametro='Colaborador' AND IdParametroPlanillaColaborador IS NOT NULL AND IdParametroPlanillaPeriodo IS NULL)))),
    ADD CONSTRAINT CK_LJ_Confirmacion CHECK (IdIncidenciaPlanillaConfirmacion IS NULL OR (TipoFuente='Incidencia' AND IdFuente IS NOT NULL));

ALTER TABLE AplicacionNormativaDetallePlanilla
    ADD COLUMN MontoAplicadoSinRedondear DECIMAL(65,28) NULL,
    ADD COLUMN IdCreditoFiscalColaborador BIGINT NULL,
    ADD COLUMN CantidadAplicada DECIMAL(65,28) NULL,
    ADD COLUMN IdDetallePlanillaPrevio BIGINT NULL,
    ADD COLUMN AlcanceAplicacion VARCHAR(30) NULL,
    ADD COLUMN FechaAplicacionInicio DATE NULL,
    ADD COLUMN FechaAplicacionFin DATE NULL,
    ADD COLUMN BaseContributiva DECIMAL(65,28) NULL,
    ADD COLUMN ReglaAplicabilidad VARCHAR(40) NULL,
    ADD COLUMN FechaAplicabilidadInicio DATE NULL,
    ADD COLUMN FechaAplicabilidadFin DATE NULL,
    ADD COLUMN ReferenciaSoporte VARCHAR(500) NULL,
    ADD COLUMN ReferenciaRegla VARCHAR(500) NULL,
    ADD COLUMN AcreditadoPor VARCHAR(255) NULL,
    ADD COLUMN ModalidadRT VARCHAR(40) NULL,
    ADD COLUMN IdContratoRT BIGINT NULL,
    ADD COLUMN IdConfiguracionRTAncla BIGINT NULL,
    ADD COLUMN ReferenciaAsignacionRT VARCHAR(500) NULL,
    ADD COLUMN AsociadoPorRT VARCHAR(255) NULL,
    MODIFY COLUMN BaseAplicada DECIMAL(65,28) NULL,
    MODIFY COLUMN ValorAplicado DECIMAL(65,28) NULL,
    MODIFY COLUMN MontoAplicado DECIMAL(18,2) NULL,
    ADD KEY IX_AJ_IdCreditoFiscalColaborador (IdCreditoFiscalColaborador),
    ADD CONSTRAINT FK_AJ_IdCreditoFiscalColaborador FOREIGN KEY (IdCreditoFiscalColaborador) REFERENCES CreditoFiscalColaborador(IdCreditoFiscalColaborador) ON DELETE RESTRICT,
    ADD KEY IX_AJ_IdDetallePlanillaPrevio (IdDetallePlanillaPrevio),
    ADD CONSTRAINT FK_AJ_IdDetallePlanillaPrevio FOREIGN KEY (IdDetallePlanillaPrevio) REFERENCES DetallePlanilla(IdDetallePlanilla) ON DELETE RESTRICT,
    ADD KEY IX_AJ_IdContratoRT (IdContratoRT),
    ADD CONSTRAINT FK_AJ_IdContratoRT FOREIGN KEY (IdContratoRT) REFERENCES Contrato(IdContrato) ON DELETE RESTRICT,
    ADD KEY IX_AJ_IdConfiguracionRTAncla (IdConfiguracionRTAncla),
    ADD CONSTRAINT FK_AJ_IdConfiguracionRTAncla FOREIGN KEY (IdConfiguracionRTAncla) REFERENCES ConfiguracionRiesgoTrabajo(IdConfiguracionRiesgoTrabajo) ON DELETE RESTRICT,
    ADD CONSTRAINT CK_AJ_CreditoPersonal CHECK (IdCreditoFiscalColaborador IS NULL OR (IdCreditoFiscalNormativo IS NOT NULL AND CantidadAplicada IS NOT NULL AND CantidadAplicada > 0)),
    ADD CONSTRAINT CK_AJ_Alcance CHECK ((AlcanceAplicacion IS NULL AND FechaAplicacionInicio IS NULL AND FechaAplicacionFin IS NULL AND IdDetallePlanillaPrevio IS NULL) OR (AlcanceAplicacion IS NOT NULL AND AlcanceAplicacion IN ('CorteActual','MensualDefinitiva','AcumuladoPrevio') AND FechaAplicacionInicio IS NOT NULL AND FechaAplicacionFin IS NOT NULL AND FechaAplicacionFin >= FechaAplicacionInicio AND (AlcanceAplicacion <> 'AcumuladoPrevio' OR IdDetallePlanillaPrevio IS NOT NULL))),
    ADD CONSTRAINT CK_AJ_Aplicabilidad CHECK (ReglaAplicabilidad IS NULL OR (IdValorParametroNormativo IS NOT NULL AND ReglaAplicabilidad IN ('RemuneracionContributiva','MinimoMensualOrdinario','ExcepcionSinMinimo') AND FechaAplicabilidadInicio IS NOT NULL AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND ReferenciaRegla IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaRegla)) > 0 AND AcreditadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AcreditadoPor)) > 0)),
    ADD CONSTRAINT CK_AJ_Vigencia CHECK (FechaAplicabilidadFin IS NULL OR (FechaAplicabilidadInicio IS NOT NULL AND FechaAplicabilidadFin >= FechaAplicabilidadInicio)),
ADD CONSTRAINT CK_AJ_RT CHECK (ModalidadRT IS NULL OR (IdConfiguracionRiesgoTrabajo IS NOT NULL AND ModalidadRT='RemuneracionSalarialOrdinaria' AND IdContratoRT IS NOT NULL AND IdConfiguracionRTAncla IS NOT NULL AND FechaAplicabilidadInicio IS NOT NULL AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND AcreditadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AcreditadoPor)) > 0 AND ReferenciaAsignacionRT IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAsignacionRT)) > 0 AND AsociadoPorRT IS NOT NULL AND CHAR_LENGTH(TRIM(AsociadoPorRT)) > 0));

/* F1-FIN-02L: evidencia Forecast L-P01..04. Sin backfill ni contratos ficticios. */
ALTER TABLE ForecastParametro
    ADD COLUMN IdConceptoPlanillaVersion BIGINT NULL,
    ADD COLUMN Unidad VARCHAR(30) NULL,
    ADD COLUMN ReferenciaSoporte VARCHAR(500) NULL,
    ADD COLUMN RegistradoPor VARCHAR(255) NULL,
    ADD COLUMN FechaRegistro DATETIME NULL,
    ADD COLUMN AutorizacionRequerida TINYINT(1) NULL,
    ADD COLUMN AutorizadoPor VARCHAR(255) NULL,
    ADD COLUMN FechaAutorizacion DATETIME NULL,
    ADD COLUMN ReferenciaAutorizacion VARCHAR(500) NULL,
    ADD CONSTRAINT CK_FL_Parametro_Evidencia CHECK ((IdConceptoPlanillaVersion IS NULL AND Unidad IS NULL AND ReferenciaSoporte IS NULL AND RegistradoPor IS NULL AND FechaRegistro IS NULL AND AutorizacionRequerida IS NULL AND AutorizadoPor IS NULL AND FechaAutorizacion IS NULL AND ReferenciaAutorizacion IS NULL) OR (IdConceptoPlanillaVersion IS NOT NULL AND Unidad IS NOT NULL AND CHAR_LENGTH(TRIM(Unidad)) > 0 AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND RegistradoPor IS NOT NULL AND CHAR_LENGTH(TRIM(RegistradoPor)) > 0 AND FechaRegistro IS NOT NULL AND AutorizacionRequerida IS NOT NULL AND AutorizacionRequerida IN (0,1) AND TipoParametro IN ('Monto','Porcentaje','Cantidad') AND ValorDecimal IS NOT NULL AND ValorTexto IS NULL AND ValorDecimal >= 0)),
    ADD CONSTRAINT CK_FL_Parametro_Autorizacion CHECK ((AutorizadoPor IS NULL AND FechaAutorizacion IS NULL AND ReferenciaAutorizacion IS NULL AND (AutorizacionRequerida IS NULL OR AutorizacionRequerida=0)) OR (AutorizacionRequerida IS NOT NULL AND AutorizadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AutorizadoPor)) > 0 AND FechaAutorizacion IS NOT NULL AND ReferenciaAutorizacion IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAutorizacion)) > 0));
ALTER TABLE ForecastParametro ADD KEY IX_FL_Parametro_Concepto (IdConceptoPlanillaVersion), ADD CONSTRAINT FK_FL_Parametro_Concepto FOREIGN KEY (IdConceptoPlanillaVersion) REFERENCES ConceptoPlanillaVersion(IdConceptoPlanillaVersion) ON DELETE RESTRICT;
ALTER TABLE ForecastAsignacionProyecto
    ADD COLUMN CodigoProyectoSnapshot VARCHAR(50) NULL,
    ADD COLUMN NombreProyectoSnapshot VARCHAR(200) NULL;
ALTER TABLE ForecastDetalle
    MODIFY MontoBaseSinRedondear DECIMAL(65,28) NULL,
    MODIFY MontoProyectadoSinRedondear DECIMAL(65,28) NULL,
    DROP INDEX UX_FcstDetalle_Esc_Per_Part_Proy_Concepto,
    DROP INDEX UX_FcstDetalle_Asignacion_Concepto,
    ADD UNIQUE KEY UX_FL_Detalle_Asignacion_Version (IdForecastAsignacionProyecto,IdConceptoPlanillaVersion);
CREATE TABLE ForecastSegmentoProyectado (
    IdForecastSegmentoProyectado BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastEscenario BIGINT NOT NULL,
    IdForecastPeriodo BIGINT NOT NULL,
    IdForecastParticipante BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    DiasBase30 INT NOT NULL,
    OrigenCondicion VARCHAR(30) NOT NULL,
    SalarioMensual DECIMAL(65,28) NOT NULL,
    IdContrato BIGINT NULL,
    IdCondicionLaboralVersion BIGINT NULL,
    IdJornadaLaboral BIGINT NOT NULL,
    CodigoJornada VARCHAR(50) NOT NULL,
    NombreJornada VARCHAR(150) NOT NULL,
    TipoJornada VARCHAR(30) NOT NULL,
    HorasOrdinariasDia DECIMAL(65,28) NOT NULL,
    HorasOrdinariasSemana DECIMAL(65,28) NOT NULL,
    FechaJornadaInicio DATE NOT NULL,
    FechaJornadaFin DATE NULL,
    IdOcupacionSalarioMinimo BIGINT NOT NULL,
    CodigoOcupacion VARCHAR(100) NOT NULL,
    NombreOcupacion VARCHAR(200) NOT NULL,
    IdSalarioMinimoVersion BIGINT NULL,
    MontoMinimo DECIMAL(65,28) NULL,
    PeriodicidadMinimo VARCHAR(20) NULL,
    IdPuesto INT NOT NULL,
    NombrePuesto VARCHAR(150) NOT NULL,
    IdDepartamento INT NOT NULL,
    NombreDepartamento VARCHAR(150) NOT NULL,
    TipoContrato VARCHAR(100) NOT NULL,
    PeriodicidadPago VARCHAR(20) NOT NULL,
    ReferenciaSoporte VARCHAR(500) NOT NULL,
    RegistradoPor VARCHAR(255) NOT NULL,
    FechaRegistro DATETIME NOT NULL,
    IdConfiguracionRiesgoTrabajo BIGINT NULL,
    ModalidadRT VARCHAR(40) NULL,
    ReferenciaAsignacionRT VARCHAR(500) NULL,
    AsociadoPorRT VARCHAR(255) NULL,
    FechaRTInicio DATE NULL,
    FechaRTFin DATE NULL,
    PRIMARY KEY (IdForecastSegmentoProyectado),
    UNIQUE KEY UX_FL_S_Orden (IdForecastEscenario,IdForecastPeriodo,IdForecastParticipante,NumeroOrden),
    KEY IX_FL_S_IdForecastEscenario (IdForecastEscenario),
    KEY IX_FL_S_IdForecastPeriodo (IdForecastPeriodo),
    KEY IX_FL_S_IdForecastParticipante (IdForecastParticipante),
    KEY IX_FL_S_IdContrato (IdContrato),
    KEY IX_FL_S_IdCondicionLaboralVersion (IdCondicionLaboralVersion),
    KEY IX_FL_S_IdJornadaLaboral (IdJornadaLaboral),
    KEY IX_FL_S_IdOcupacionSalarioMinimo (IdOcupacionSalarioMinimo),
    KEY IX_FL_S_IdSalarioMinimoVersion (IdSalarioMinimoVersion),
    KEY IX_FL_S_IdPuesto (IdPuesto),
    KEY IX_FL_S_IdDepartamento (IdDepartamento),
    KEY IX_FL_S_IdConfiguracionRiesgoTrabajo (IdConfiguracionRiesgoTrabajo),
    CONSTRAINT FK_FL_S_IdForecastEscenario FOREIGN KEY (IdForecastEscenario) REFERENCES ForecastEscenario(IdForecastEscenario) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdForecastPeriodo FOREIGN KEY (IdForecastPeriodo) REFERENCES ForecastPeriodo(IdForecastPeriodo) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdForecastParticipante FOREIGN KEY (IdForecastParticipante) REFERENCES ForecastParticipante(IdForecastParticipante) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdContrato FOREIGN KEY (IdContrato) REFERENCES Contrato(IdContrato) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdCondicionLaboralVersion FOREIGN KEY (IdCondicionLaboralVersion) REFERENCES CondicionLaboralVersion(IdCondicionLaboralVersion) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdJornadaLaboral FOREIGN KEY (IdJornadaLaboral) REFERENCES JornadaLaboral(IdJornadaLaboral) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdOcupacionSalarioMinimo FOREIGN KEY (IdOcupacionSalarioMinimo) REFERENCES OcupacionSalarioMinimo(IdOcupacionSalarioMinimo) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdSalarioMinimoVersion FOREIGN KEY (IdSalarioMinimoVersion) REFERENCES SalarioMinimoVersion(IdSalarioMinimoVersion) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdPuesto FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdDepartamento FOREIGN KEY (IdDepartamento) REFERENCES Departamento(IdDepartamento) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_S_IdConfiguracionRiesgoTrabajo FOREIGN KEY (IdConfiguracionRiesgoTrabajo) REFERENCES ConfiguracionRiesgoTrabajo(IdConfiguracionRiesgoTrabajo) ON DELETE RESTRICT,
    CONSTRAINT CK_FL_Segmento_Orden CHECK (NumeroOrden > 0 AND DiasBase30 >= 0),
    CONSTRAINT CK_FL_Segmento_Fechas CHECK (FechaFin >= FechaInicio AND (FechaJornadaFin IS NULL OR FechaJornadaFin >= FechaJornadaInicio)),
    CONSTRAINT CK_FL_Segmento_Condicion CHECK ((OrigenCondicion='CondicionExistente' AND IdContrato IS NOT NULL AND IdCondicionLaboralVersion IS NOT NULL) OR (OrigenCondicion IN ('ContratacionFutura','CambioFuturo') AND IdContrato IS NULL AND IdCondicionLaboralVersion IS NULL)),
    CONSTRAINT CK_FL_Segmento_Valores CHECK (SalarioMensual > 0 AND HorasOrdinariasDia > 0 AND HorasOrdinariasSemana > 0 AND (MontoMinimo IS NULL OR MontoMinimo > 0)),
    CONSTRAINT CK_FL_Segmento_Soporte CHECK (ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND RegistradoPor IS NOT NULL AND CHAR_LENGTH(TRIM(RegistradoPor)) > 0),
    CONSTRAINT CK_FL_Segmento_Minimo CHECK ((IdSalarioMinimoVersion IS NULL AND MontoMinimo IS NULL AND PeriodicidadMinimo IS NULL) OR (IdSalarioMinimoVersion IS NOT NULL AND MontoMinimo IS NOT NULL AND PeriodicidadMinimo IS NOT NULL)),
    CONSTRAINT CK_FL_Segmento_RT CHECK ((IdConfiguracionRiesgoTrabajo IS NULL AND ModalidadRT IS NULL AND ReferenciaAsignacionRT IS NULL AND AsociadoPorRT IS NULL AND FechaRTInicio IS NULL AND FechaRTFin IS NULL) OR (IdConfiguracionRiesgoTrabajo IS NOT NULL AND ModalidadRT IS NOT NULL AND ModalidadRT IN ('RemuneracionSalarialOrdinaria','Especial') AND ReferenciaAsignacionRT IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaAsignacionRT)) > 0 AND AsociadoPorRT IS NOT NULL AND CHAR_LENGTH(TRIM(AsociadoPorRT)) > 0 AND FechaRTInicio IS NOT NULL AND (FechaRTFin IS NULL OR FechaRTFin >= FechaRTInicio)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE ForecastAplicacionFinanciera (
    IdForecastAplicacionFinanciera BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastDetalle BIGINT NOT NULL,
    NumeroOrden INT NOT NULL,
    IdForecastSegmentoProyectado BIGINT NULL,
    IdForecastParametro BIGINT NULL,
    IdForecastObservacionHistorica BIGINT NULL,
    FuenteEfectiva VARCHAR(30) NOT NULL,
    AlcanceAplicacion VARCHAR(30) NOT NULL,
    Componente VARCHAR(100) NOT NULL,
    Sujeto VARCHAR(30) NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    IdForecastDetallePrevio BIGINT NULL,
    IdValorParametroNormativo BIGINT NULL,
    IdTramoImpuestoSalario BIGINT NULL,
    IdCreditoFiscalNormativo BIGINT NULL,
    IdCreditoFiscalColaborador BIGINT NULL,
    IdConfiguracionRiesgoTrabajo BIGINT NULL,
    Unidad VARCHAR(30) NULL,
    TipoBase VARCHAR(100) NULL,
    BaseAplicada DECIMAL(65,28) NULL,
    ValorAplicado DECIMAL(65,28) NULL,
    Cantidad DECIMAL(65,28) NULL,
    Multiplicador DECIMAL(65,28) NULL,
    MontoSinRedondear DECIMAL(65,28) NOT NULL,
    MontoMaterializado DECIMAL(18,2) NULL,
    ReglaAplicabilidad VARCHAR(40) NULL,
    FechaAplicabilidadInicio DATE NULL,
    FechaAplicabilidadFin DATE NULL,
    ReferenciaSoporte VARCHAR(500) NULL,
    ReferenciaRegla VARCHAR(500) NULL,
    AcreditadoPor VARCHAR(255) NULL,
    ModalidadRT VARCHAR(40) NULL,
    NumeroPoliza VARCHAR(100) NULL,
    CodigoActividad VARCHAR(100) NULL,
    DescuentoRT DECIMAL(65,28) NULL,
    RecargoRT DECIMAL(65,28) NULL,
    EsSalarial TINYINT(1) NOT NULL,
    EsContributivoCCSS TINYINT(1) NOT NULL,
    EsTributario TINYINT(1) NOT NULL,
    EsPatronal TINYINT(1) NOT NULL,
    Efecto VARCHAR(40) NOT NULL,
    DescripcionConcepto VARCHAR(300) NOT NULL,
    PRIMARY KEY (IdForecastAplicacionFinanciera),
    UNIQUE KEY UX_FL_A_Orden (IdForecastDetalle,NumeroOrden),
    KEY IX_FL_A_IdForecastDetalle (IdForecastDetalle),
    KEY IX_FL_A_IdForecastSegmentoProyectado (IdForecastSegmentoProyectado),
    KEY IX_FL_A_IdForecastParametro (IdForecastParametro),
    KEY IX_FL_A_IdForecastObservacionHistorica (IdForecastObservacionHistorica),
    KEY IX_FL_A_IdForecastDetallePrevio (IdForecastDetallePrevio),
    KEY IX_FL_A_IdValorParametroNormativo (IdValorParametroNormativo),
    KEY IX_FL_A_IdTramoImpuestoSalario (IdTramoImpuestoSalario),
    KEY IX_FL_A_IdCreditoFiscalNormativo (IdCreditoFiscalNormativo),
    KEY IX_FL_A_IdCreditoFiscalColaborador (IdCreditoFiscalColaborador),
    KEY IX_FL_A_IdConfiguracionRiesgoTrabajo (IdConfiguracionRiesgoTrabajo),
    CONSTRAINT FK_FL_A_IdForecastDetalle FOREIGN KEY (IdForecastDetalle) REFERENCES ForecastDetalle(IdForecastDetalle) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdForecastSegmentoProyectado FOREIGN KEY (IdForecastSegmentoProyectado) REFERENCES ForecastSegmentoProyectado(IdForecastSegmentoProyectado) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdForecastParametro FOREIGN KEY (IdForecastParametro) REFERENCES ForecastParametro(IdForecastParametro) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdForecastObservacionHistorica FOREIGN KEY (IdForecastObservacionHistorica) REFERENCES ForecastObservacionHistorica(IdForecastObservacionHistorica) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdForecastDetallePrevio FOREIGN KEY (IdForecastDetallePrevio) REFERENCES ForecastDetalle(IdForecastDetalle) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdValorParametroNormativo FOREIGN KEY (IdValorParametroNormativo) REFERENCES ValorParametroNormativo(IdValorParametroNormativo) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdTramoImpuestoSalario FOREIGN KEY (IdTramoImpuestoSalario) REFERENCES TramoImpuestoSalario(IdTramoImpuestoSalario) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdCreditoFiscalNormativo FOREIGN KEY (IdCreditoFiscalNormativo) REFERENCES CreditoFiscalNormativo(IdCreditoFiscalNormativo) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdCreditoFiscalColaborador FOREIGN KEY (IdCreditoFiscalColaborador) REFERENCES CreditoFiscalColaborador(IdCreditoFiscalColaborador) ON DELETE RESTRICT,
    CONSTRAINT FK_FL_A_IdConfiguracionRiesgoTrabajo FOREIGN KEY (IdConfiguracionRiesgoTrabajo) REFERENCES ConfiguracionRiesgoTrabajo(IdConfiguracionRiesgoTrabajo) ON DELETE RESTRICT,
    CONSTRAINT CK_FL_Aplicacion_Orden CHECK (NumeroOrden > 0 AND FechaFin >= FechaInicio),
    CONSTRAINT CK_FL_Aplicacion_Fuente CHECK (FuenteEfectiva IN ('Historico','Override','CondicionProyectada','Normativa','Derivado') AND (FuenteEfectiva<>'Historico' OR IdForecastObservacionHistorica IS NOT NULL) AND (FuenteEfectiva<>'Override' OR IdForecastParametro IS NOT NULL) AND (FuenteEfectiva<>'CondicionProyectada' OR IdForecastSegmentoProyectado IS NOT NULL)),
    CONSTRAINT CK_FL_Aplicacion_Normativa CHECK (((IdValorParametroNormativo IS NOT NULL)+(IdTramoImpuestoSalario IS NOT NULL)+(IdCreditoFiscalNormativo IS NOT NULL)+(IdConfiguracionRiesgoTrabajo IS NOT NULL)) <= 1 AND (FuenteEfectiva<>'Normativa' OR ((IdValorParametroNormativo IS NOT NULL)+(IdTramoImpuestoSalario IS NOT NULL)+(IdCreditoFiscalNormativo IS NOT NULL)+(IdConfiguracionRiesgoTrabajo IS NOT NULL))=1)),
    CONSTRAINT CK_FL_Aplicacion_Credito CHECK (IdCreditoFiscalColaborador IS NULL OR (IdCreditoFiscalNormativo IS NOT NULL AND Cantidad IS NOT NULL AND Cantidad > 0)),
    CONSTRAINT CK_FL_Aplicacion_Alcance CHECK (AlcanceAplicacion IN ('CorteActual','MensualDefinitiva','AcumuladoPrevio') AND (AlcanceAplicacion<>'AcumuladoPrevio' OR IdForecastDetallePrevio IS NOT NULL)),
    CONSTRAINT CK_FL_Aplicacion_Vigencia CHECK (FechaAplicabilidadFin IS NULL OR (FechaAplicabilidadInicio IS NOT NULL AND FechaAplicabilidadFin >= FechaAplicabilidadInicio)),
    CONSTRAINT CK_FL_Aplicacion_Banderas CHECK (EsSalarial IN (0,1) AND EsContributivoCCSS IN (0,1) AND EsTributario IN (0,1) AND EsPatronal IN (0,1)),
    CONSTRAINT CK_FL_Aplicacion_RT CHECK (ModalidadRT IS NULL OR (IdConfiguracionRiesgoTrabajo IS NOT NULL AND IdForecastSegmentoProyectado IS NOT NULL AND ModalidadRT='RemuneracionSalarialOrdinaria' AND NumeroPoliza IS NOT NULL AND CodigoActividad IS NOT NULL)),
    CONSTRAINT CK_FL_Aplicacion_Acreditacion CHECK (ReglaAplicabilidad IS NULL OR (IdValorParametroNormativo IS NOT NULL AND ReglaAplicabilidad IN ('RemuneracionContributiva','MinimoMensualOrdinario','ExcepcionSinMinimo') AND FechaAplicabilidadInicio IS NOT NULL AND ReferenciaSoporte IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaSoporte)) > 0 AND ReferenciaRegla IS NOT NULL AND CHAR_LENGTH(TRIM(ReferenciaRegla)) > 0 AND AcreditadoPor IS NOT NULL AND CHAR_LENGTH(TRIM(AcreditadoPor)) > 0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/* F1-FIN-02M: comparacion persistida M-P01..03. Sin backfill. */
ALTER TABLE DetallePlanillaDistribucionProyecto
    ADD COLUMN CodigoProyectoSnapshot VARCHAR(50) NULL,
    ADD COLUMN NombreProyectoSnapshot VARCHAR(200) NULL;

CREATE TABLE ForecastComparacionReal (
    IdForecastComparacionReal BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastPeriodo BIGINT NOT NULL,
    IdEjecucionPlanillaReal BIGINT NULL,
    FechaEvaluacion DATETIME NOT NULL,
    EvaluadoPor VARCHAR(255) NOT NULL,
    Disponibilidad VARCHAR(30) NOT NULL,
    MotivoNoComparable VARCHAR(100) NULL,
    EstadoPlanillaObservado VARCHAR(24) NULL,
    EstadoPeriodoObservado VARCHAR(24) NULL,
    EstadoEjecucionObservado VARCHAR(30) NULL,
    SnapshotCompletoObservado TINYINT(1) NULL,
    PRIMARY KEY (IdForecastComparacionReal),
    UNIQUE KEY UX_FM_Comparacion_Periodo (IdForecastPeriodo),
    KEY IX_FM_0_0 (IdEjecucionPlanillaReal),
    CONSTRAINT FK_FM_0_0 FOREIGN KEY (IdForecastPeriodo) REFERENCES ForecastPeriodo(IdForecastPeriodo) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_0_1 FOREIGN KEY (IdEjecucionPlanillaReal) REFERENCES EjecucionPlanilla(IdEjecucionPlanilla) ON DELETE RESTRICT,
    CONSTRAINT CK_FM_Cabecera CHECK (CHAR_LENGTH(TRIM(EvaluadoPor)) > 0 AND ((Disponibilidad='SinRealComparable' AND MotivoNoComparable='SinRealComparable' AND MotivoNoComparable IS NOT NULL AND IdEjecucionPlanillaReal IS NULL AND EstadoPlanillaObservado IS NULL AND EstadoPeriodoObservado IS NULL AND EstadoEjecucionObservado IS NULL AND SnapshotCompletoObservado IS NULL) OR (Disponibilidad='Disponible' AND MotivoNoComparable IS NULL AND IdEjecucionPlanillaReal IS NOT NULL AND EstadoPlanillaObservado IS NOT NULL AND EstadoPlanillaObservado IN ('Aprobada','Cerrada') AND EstadoPeriodoObservado IS NOT NULL AND EstadoPeriodoObservado=EstadoPlanillaObservado AND EstadoEjecucionObservado IS NOT NULL AND EstadoEjecucionObservado='Autorizada' AND SnapshotCompletoObservado IS NOT NULL AND SnapshotCompletoObservado=1)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ForecastComparacionResultado (
    IdForecastComparacionResultado BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastComparacionReal BIGINT NOT NULL,
    Granularidad VARCHAR(20) NOT NULL,
    IdForecastParticipante BIGINT NULL,
    IdColaborador BIGINT NULL,
    IdConceptoPlanilla BIGINT NULL,
    IdProyecto BIGINT NULL,
    EsSinAsignar TINYINT(1) NOT NULL,
    TieneForecast TINYINT(1) NOT NULL,
    TieneReal TINYINT(1) NOT NULL,
    MontoForecast DECIMAL(18,2) NULL,
    MontoReal DECIMAL(18,2) NULL,
    DiferenciaFirmada DECIMAL(18,2) NULL,
    DiferenciaAbsoluta DECIMAL(18,2) NULL,
    DiferenciaPorcentual DECIMAL(18,4) NULL,
    MotivoNoComparable VARCHAR(100) NULL,
    NumeroOrden INT NOT NULL,
    PRIMARY KEY (IdForecastComparacionResultado),
    UNIQUE KEY UX_FM_Resultado_Orden (IdForecastComparacionReal,NumeroOrden),
    KEY IX_FM_1_0 (IdForecastParticipante),
    KEY IX_FM_1_1 (IdColaborador),
    KEY IX_FM_1_2 (IdConceptoPlanilla),
    KEY IX_FM_1_3 (IdProyecto),
    CONSTRAINT FK_FM_1_0 FOREIGN KEY (IdForecastComparacionReal) REFERENCES ForecastComparacionReal(IdForecastComparacionReal) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_1_1 FOREIGN KEY (IdForecastParticipante) REFERENCES ForecastParticipante(IdForecastParticipante) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_1_2 FOREIGN KEY (IdColaborador) REFERENCES Colaborador(IdColaborador) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_1_3 FOREIGN KEY (IdConceptoPlanilla) REFERENCES ConceptoPlanilla(IdConceptoPlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_1_4 FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto) ON DELETE RESTRICT,
    CONSTRAINT CK_FM_Resultado_Dimension CHECK (NumeroOrden > 0 AND EsSinAsignar IN (0,1) AND ((Granularidad='Total' AND IdForecastParticipante IS NULL AND IdColaborador IS NULL AND IdConceptoPlanilla IS NULL AND IdProyecto IS NULL AND EsSinAsignar=0) OR (Granularidad='Participante' AND (IdForecastParticipante IS NOT NULL OR IdColaborador IS NOT NULL) AND IdConceptoPlanilla IS NULL AND IdProyecto IS NULL AND EsSinAsignar=0) OR (Granularidad='Concepto' AND IdConceptoPlanilla IS NOT NULL AND IdForecastParticipante IS NULL AND IdColaborador IS NULL AND IdProyecto IS NULL AND EsSinAsignar=0) OR (Granularidad='Proyecto' AND IdForecastParticipante IS NULL AND IdColaborador IS NULL AND IdConceptoPlanilla IS NULL AND ((EsSinAsignar=1 AND IdProyecto IS NULL) OR (EsSinAsignar=0 AND IdProyecto IS NOT NULL))))),
    CONSTRAINT CK_FM_Resultado_Presencia CHECK (TieneForecast IN (0,1) AND TieneReal IN (0,1) AND (TieneForecast=1 OR TieneReal=1) AND ((TieneForecast=1 AND MontoForecast IS NOT NULL) OR (TieneForecast=0 AND MontoForecast IS NULL)) AND ((TieneReal=1 AND MontoReal IS NOT NULL) OR (TieneReal=0 AND MontoReal IS NULL))),
    CONSTRAINT CK_FM_Resultado_Diferencias CHECK (((TieneForecast=0 OR TieneReal=0) AND MotivoNoComparable IS NOT NULL AND MotivoNoComparable='SinCorrespondencia' AND DiferenciaFirmada IS NULL AND DiferenciaAbsoluta IS NULL AND DiferenciaPorcentual IS NULL) OR (TieneForecast=1 AND TieneReal=1 AND DiferenciaFirmada IS NOT NULL AND DiferenciaFirmada=MontoReal-MontoForecast AND DiferenciaAbsoluta IS NOT NULL AND DiferenciaAbsoluta=ABS(DiferenciaFirmada) AND ((MontoForecast=0 AND MontoReal<>0 AND DiferenciaPorcentual IS NULL AND MotivoNoComparable IS NOT NULL AND MotivoNoComparable='BaseForecastCero') OR (MotivoNoComparable IS NULL AND DiferenciaPorcentual IS NOT NULL AND ((MontoForecast=0 AND MontoReal=0 AND DiferenciaPorcentual=0) OR (MontoForecast<>0 AND DiferenciaPorcentual=ROUND(CAST(DiferenciaFirmada AS DECIMAL(65,28))/ABS(MontoForecast)*100,4)))))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ForecastComparacionResultadoFuente (
    IdForecastComparacionResultadoFuente BIGINT NOT NULL AUTO_INCREMENT,
    IdForecastComparacionResultado BIGINT NOT NULL,
    Lado VARCHAR(10) NOT NULL,
    IdForecastDetalle BIGINT NULL,
    IdDetallePlanilla BIGINT NULL,
    IdConceptoDetallePlanilla BIGINT NULL,
    IdDetallePlanillaDistribucionProyecto BIGINT NULL,
    PRIMARY KEY (IdForecastComparacionResultadoFuente),
    UNIQUE KEY UX_FM_Fuente_0 (IdForecastComparacionResultado,IdForecastDetalle),
    UNIQUE KEY UX_FM_Fuente_1 (IdForecastComparacionResultado,IdDetallePlanilla),
    UNIQUE KEY UX_FM_Fuente_2 (IdForecastComparacionResultado,IdConceptoDetallePlanilla),
    UNIQUE KEY UX_FM_Fuente_3 (IdForecastComparacionResultado,IdDetallePlanillaDistribucionProyecto),
    KEY IX_FM_2_0 (IdForecastDetalle),
    KEY IX_FM_2_1 (IdDetallePlanilla),
    KEY IX_FM_2_2 (IdConceptoDetallePlanilla),
    KEY IX_FM_2_3 (IdDetallePlanillaDistribucionProyecto),
    CONSTRAINT FK_FM_2_0 FOREIGN KEY (IdForecastComparacionResultado) REFERENCES ForecastComparacionResultado(IdForecastComparacionResultado) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_2_1 FOREIGN KEY (IdForecastDetalle) REFERENCES ForecastDetalle(IdForecastDetalle) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_2_2 FOREIGN KEY (IdDetallePlanilla) REFERENCES DetallePlanilla(IdDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_2_3 FOREIGN KEY (IdConceptoDetallePlanilla) REFERENCES ConceptoDetallePlanilla(IdConceptoDetallePlanilla) ON DELETE RESTRICT,
    CONSTRAINT FK_FM_2_4 FOREIGN KEY (IdDetallePlanillaDistribucionProyecto) REFERENCES DetallePlanillaDistribucionProyecto(IdDetallePlanillaDistribucionProyecto) ON DELETE RESTRICT,
    CONSTRAINT CK_FM_Fuente_Lado CHECK (((IdForecastDetalle IS NOT NULL)+(IdDetallePlanilla IS NOT NULL)+(IdConceptoDetallePlanilla IS NOT NULL)+(IdDetallePlanillaDistribucionProyecto IS NOT NULL))=1 AND ((Lado='Forecast' AND IdForecastDetalle IS NOT NULL) OR (Lado='Real' AND IdForecastDetalle IS NULL)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


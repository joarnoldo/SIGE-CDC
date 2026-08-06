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
        CHECK (PeriodosHistoricosConsiderados > 0),
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

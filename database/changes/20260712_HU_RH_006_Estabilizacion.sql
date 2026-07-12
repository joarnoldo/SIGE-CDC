/* ============================================================
   SIGE-CDC - Estabilización HU-RH-006
   Base de datos: SIGE_CDC_DB

   IMPORTANTE:
   - Script DDL separado del script oficial.
   - No se ejecuta automáticamente ni mediante migraciones.
   - Respaldar la base y revisar el bloque de prevalidación antes
     de aplicarlo manualmente en MySQL Workbench.
   - Ejecutar una sola vez sobre una base creada con el script
     oficial vigente al 2026-07-12.
   ============================================================ */

USE SIGE_CDC_DB;

/* Prevalidación: las consultas deben devolver las tablas esperadas. */
SELECT TABLE_NAME
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN (
      'ParametroPlanilla',
      'Colaborador',
      'PeriodoPlanilla',
      'DetallePlanilla',
      'AspNetUsers'
  )
ORDER BY TABLE_NAME;

ALTER TABLE ParametroPlanilla
    ADD COLUMN Naturaleza ENUM('Deduccion','Beneficio') NULL
    AFTER TipoParametro;

CREATE TABLE ParametroPlanillaColaborador (
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

CREATE TABLE ParametroPlanillaPeriodo (
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

/* Preparación del detalle para distinguir beneficios configurables en HU-RH-005. */
ALTER TABLE DetallePlanilla
    ADD COLUMN TotalBeneficiosConfigurables DECIMAL(18,2) NOT NULL DEFAULT 0.00
    AFTER TotalBonos;

UPDATE ParametroPlanilla
SET Naturaleza = 'Deduccion'
WHERE Codigo = 'PORCENTAJE_CARGAS_OBRERAS';

/* Validación posterior. */
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'ParametroPlanilla'
  AND column_name = 'Naturaleza';

SELECT TABLE_NAME
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('ParametroPlanillaColaborador', 'ParametroPlanillaPeriodo')
ORDER BY TABLE_NAME;

/*
   ROLLBACK MANUAL - EJECUTAR SOLO SI SE DECIDE REVERTIR Y DESPUÉS
   DE CONFIRMAR QUE NINGÚN CÁLCULO DEPENDE DE ESTAS ASIGNACIONES.

   DROP TABLE ParametroPlanillaPeriodo;
   DROP TABLE ParametroPlanillaColaborador;
   ALTER TABLE DetallePlanilla DROP COLUMN TotalBeneficiosConfigurables;
   ALTER TABLE ParametroPlanilla DROP COLUMN Naturaleza;
*/

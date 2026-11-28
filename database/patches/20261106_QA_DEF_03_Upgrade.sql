-- QA-DEF-03. Ejecutar sobre la base seleccionada, con respaldo verificado y sin escrituras concurrentes.
-- Autorización de este loop: SOLO QA local. No ejecutar en MonsterASP sin autorización posterior.
-- MySQL 8 / MariaDB 10.11. DDL con commit implícito; no es una migración EF.
-- No modifica filas, imágenes ni tarjetas de portafolio. Las asociaciones anteriores quedan NULL.
DELIMITER $$
DROP PROCEDURE IF EXISTS qa_def03_upgrade$$
CREATE PROCEDURE qa_def03_upgrade()
BEGIN
    DECLARE n INT DEFAULT 0;
    IF DATABASE() IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: seleccione la base autorizada';
    END IF;
    SELECT COUNT(*) INTO n FROM information_schema.TABLES
      WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('Galeria','Proyecto') AND ENGINE='InnoDB';
    IF n<>2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: Galeria y Proyecto deben existir en InnoDB'; END IF;
    SELECT COUNT(*) INTO n FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Proyecto' AND COLUMN_NAME='IdProyecto'
        AND DATA_TYPE='bigint' AND COLUMN_TYPE NOT LIKE '%unsigned%' AND COLUMN_KEY='PRI';
    IF n<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: PK Proyecto incompatible'; END IF;
    SELECT COUNT(*) INTO n FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND COLUMN_NAME='IdProyecto';
    IF n=0 THEN
        SELECT COUNT(*) INTO n FROM information_schema.STATISTICS
          WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND INDEX_NAME='IX_Galeria_IdProyecto';
        IF n<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: indice previo incompatible'; END IF;
        SELECT COUNT(*) INTO n FROM information_schema.TABLE_CONSTRAINTS
          WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME='FK_Galeria_Proyecto';
        IF n<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: restriccion previa incompatible'; END IF;
        ALTER TABLE Galeria
          ADD COLUMN IdProyecto BIGINT NULL AFTER IdGaleria,
          ADD INDEX IX_Galeria_IdProyecto (IdProyecto),
          ADD CONSTRAINT FK_Galeria_Proyecto FOREIGN KEY (IdProyecto)
            REFERENCES Proyecto(IdProyecto) ON DELETE RESTRICT;
    END IF;
    -- Reejecución segura únicamente si la estructura completa coincide.
    SELECT COUNT(*) INTO n FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND COLUMN_NAME='IdProyecto'
        AND DATA_TYPE='bigint' AND COLUMN_TYPE NOT LIKE '%unsigned%' AND IS_NULLABLE='YES';
    IF n<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: columna parcial o incompatible'; END IF;
    SELECT COUNT(*) INTO n FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND INDEX_NAME='IX_Galeria_IdProyecto';
    IF n<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: indice parcial o incompatible'; END IF;
    SELECT COUNT(*) INTO n FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND INDEX_NAME='IX_Galeria_IdProyecto'
        AND COLUMN_NAME='IdProyecto' AND NON_UNIQUE=1 AND SEQ_IN_INDEX=1 AND SUB_PART IS NULL;
    IF n<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: definicion de indice incompatible'; END IF;
    SELECT COUNT(*) INTO n FROM information_schema.KEY_COLUMN_USAGE k
      JOIN information_schema.REFERENTIAL_CONSTRAINTS r
        ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME
      WHERE k.CONSTRAINT_SCHEMA=DATABASE() AND k.TABLE_NAME='Galeria'
        AND k.CONSTRAINT_NAME='FK_Galeria_Proyecto' AND k.COLUMN_NAME='IdProyecto'
        AND k.REFERENCED_TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME='Proyecto'
        AND k.REFERENCED_COLUMN_NAME='IdProyecto' AND r.DELETE_RULE='RESTRICT'
        AND r.UPDATE_RULE IN ('RESTRICT','NO ACTION');
    IF n<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: FK parcial o incompatible'; END IF;
    IF EXISTS(SELECT 1 FROM Galeria g LEFT JOIN Proyecto p ON p.IdProyecto=g.IdProyecto
              WHERE g.IdProyecto IS NOT NULL AND p.IdProyecto IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: asociacion huerfana';
    END IF;
    SELECT 'QADEF03_UPGRADE_OK' AS Resultado, COUNT(*) AS Galerias,
      COUNT(IdProyecto) AS Asociadas FROM Galeria;
END$$
CALL qa_def03_upgrade()$$
DROP PROCEDURE qa_def03_upgrade$$
DELIMITER ;

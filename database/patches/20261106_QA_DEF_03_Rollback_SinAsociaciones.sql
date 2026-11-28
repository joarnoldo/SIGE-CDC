-- Volver primero al producto anterior, sin escritores concurrentes. Conservar respaldo privado.
-- No destruye asociaciones para permitir rollback. Si existen, conservar columna nullable y producto anterior.
-- DDL: commit implícito. SOLO local en QA-DEF-03; remoto requiere autorización posterior.
DELIMITER $$
DROP PROCEDURE IF EXISTS qa_def03_rollback$$
CREATE PROCEDURE qa_def03_rollback()
BEGIN
    DECLARE n INT DEFAULT 0;
    IF DATABASE() IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: seleccione la base autorizada'; END IF;
    SELECT COUNT(*) INTO n FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND COLUMN_NAME='IdProyecto';
    IF n=1 THEN
        IF EXISTS(SELECT 1 FROM Galeria WHERE IdProyecto IS NOT NULL) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'QADEF03: rollback rechazado; existen asociaciones que conservar';
        END IF;
        -- La ausencia de índice/FK detiene una estructura parcial en lugar de borrar a ciegas.
        ALTER TABLE Galeria DROP FOREIGN KEY FK_Galeria_Proyecto,
          DROP INDEX IX_Galeria_IdProyecto, DROP COLUMN IdProyecto;
    END IF;
    SELECT 'QADEF03_ROLLBACK_SIN_PERDIDA' AS Resultado;
END$$
CALL qa_def03_rollback()$$
DROP PROCEDURE qa_def03_rollback$$
DELIMITER ;

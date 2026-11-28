-- Solo lectura. Comparar conteos y huellas de todas las columnas anteriores contra el respaldo.
SELECT DATABASE() AS BaseSeleccionada, VERSION() AS Motor;
SELECT COLUMN_TYPE, IS_NULLABLE FROM information_schema.COLUMNS
 WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND COLUMN_NAME='IdProyecto';
SELECT INDEX_NAME, COLUMN_NAME, NON_UNIQUE, SEQ_IN_INDEX FROM information_schema.STATISTICS
 WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Galeria' AND INDEX_NAME='IX_Galeria_IdProyecto';
SELECT k.CONSTRAINT_NAME,k.COLUMN_NAME,k.REFERENCED_TABLE_NAME,k.REFERENCED_COLUMN_NAME,r.DELETE_RULE,r.UPDATE_RULE
 FROM information_schema.KEY_COLUMN_USAGE k JOIN information_schema.REFERENTIAL_CONSTRAINTS r
 ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME
 WHERE k.CONSTRAINT_SCHEMA=DATABASE() AND k.TABLE_NAME='Galeria' AND k.CONSTRAINT_NAME='FK_Galeria_Proyecto';
SELECT COUNT(*) AS Galerias,COUNT(IdProyecto) AS Asociadas,SUM(EstaPublicado=1) AS Publicadas FROM Galeria;
SELECT COUNT(*) AS Imagenes FROM ImagenGaleria;
SELECT COUNT(*) AS Documentos FROM DocumentoArchivo;
SELECT COUNT(*) AS Proyectos FROM Proyecto;
SELECT COUNT(*) AS TarjetasPortafolio FROM ProyectoPublicado;
SELECT COUNT(*) AS Huerfanas FROM Galeria g LEFT JOIN Proyecto p ON p.IdProyecto=g.IdProyecto
 WHERE g.IdProyecto IS NOT NULL AND p.IdProyecto IS NULL;

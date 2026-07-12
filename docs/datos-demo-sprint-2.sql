/* ============================================================
   SIGE-CDC - Datos de demostracion Sprint 2
   Uso: pegar y ejecutar en MySQL Workbench sobre SIGE_CDC_DB.

   Este script NO cambia esquema, NO crea roles, NO crea usuarios,
   NO inserta contrasenas y NO almacena binarios en MySQL.
   ============================================================ */

USE SIGE_CDC_DB;
SET NAMES utf8mb4;

START TRANSACTION;

SET @DemoUsuarioId := (
    SELECT Id
    FROM AspNetUsers
    ORDER BY FechaCreacion
    LIMIT 1
);

INSERT IGNORE INTO EstadoLaboral (Nombre, Descripcion)
VALUES
('Activo', 'Colaborador activo en la empresa.'),
('Inactivo', 'Colaborador inactivo.');

INSERT IGNORE INTO EstadoProyecto (Nombre, Descripcion)
VALUES
('Planificado', 'Proyecto registrado, pendiente de iniciar.'),
('En ejecución', 'Proyecto activo en ejecución.'),
('Finalizado', 'Proyecto concluido.');

INSERT IGNORE INTO EstadoActivo (Nombre, Descripcion)
VALUES
('Disponible', 'Activo disponible para asignación.'),
('Asignado', 'Activo asignado a un proyecto o responsable.'),
('En mantenimiento', 'Activo no disponible por mantenimiento.');

INSERT IGNORE INTO EstadoPlanilla (Nombre, Descripcion)
VALUES
('Borrador', 'Periodo o planilla editable.'),
('En revisión', 'Planilla pendiente de revisión.'),
('Aprobada', 'Planilla aprobada por Recursos Humanos.');

INSERT IGNORE INTO TipoActivo (Nombre, Descripcion)
VALUES
('Maquinaria', 'Maquinaria utilizada en proyectos.'),
('Equipo', 'Equipo operativo o administrativo.');

INSERT IGNORE INTO CategoriaActivo (IdTipoActivo, Nombre, Descripcion)
SELECT IdTipoActivo, 'General', CONCAT('Categoría general para ', Nombre)
FROM TipoActivo;

INSERT IGNORE INTO TipoMedicionUso (Nombre, Descripcion)
VALUES
('Horas', 'Medición de uso por horas.'),
('Kilómetros', 'Medición de uso por kilometraje.');

INSERT IGNORE INTO TipoDocumento (Nombre, Descripcion)
VALUES
('Contrato', 'Contrato laboral o documento contractual.'),
('Documento colaborador', 'Documento asociado al expediente del colaborador.'),
('Imagen galeria', 'Imagen utilizada en galerías o contenido público.');

INSERT IGNORE INTO TipoIncidenciaPlanilla (Nombre, Naturaleza, Descripcion)
VALUES
('Hora extra', 'Ingreso', 'Ingreso por horas extra registradas.'),
('Ausencia sin goce', 'Deduccion', 'Deducción por ausencia sin goce.'),
('Bono', 'Ingreso', 'Ingreso adicional registrado manualmente.'),
('Deducción', 'Deduccion', 'Deducción configurable registrada manualmente.');

INSERT INTO Departamento (Nombre, Descripcion, EstadoRegistro)
VALUES
('Administración', 'Gestión administrativa y atención institucional.', 'Activo'),
('Recursos Humanos', 'Gestión de colaboradores, contratos y planilla.', 'Activo'),
('Operaciones', 'Coordinación de proyectos, maquinaria y cuadrillas.', 'Activo'),
('Ingeniería', 'Planificación técnica y seguimiento de obras.', 'Activo')
ON DUPLICATE KEY UPDATE
Descripcion = VALUES(Descripcion),
EstadoRegistro = 'Activo';

INSERT INTO Puesto (IdDepartamento, Nombre, Descripcion, EstadoRegistro)
VALUES
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Recursos Humanos'), 'Coordinadora de Recursos Humanos', 'Responsable de expediente laboral y planillas.', 'Activo'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Operaciones'), 'Supervisor de Campo', 'Responsable de cuadrillas y control operativo.', 'Activo'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Ingeniería'), 'Ingeniero de Proyecto', 'Responsable técnico de obras hidráulicas.', 'Activo'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Operaciones'), 'Operador de Maquinaria', 'Operación de maquinaria pesada en proyectos.', 'Activo'),
((SELECT IdDepartamento FROM Departamento WHERE Nombre = 'Administración'), 'Asistente Administrativa', 'Soporte administrativo y atención de consultas.', 'Activo')
ON DUPLICATE KEY UPDATE
IdDepartamento = VALUES(IdDepartamento),
Descripcion = VALUES(Descripcion),
EstadoRegistro = 'Activo';

SET @EstadoLaboralActivo := (SELECT IdEstadoLaboral FROM EstadoLaboral WHERE Nombre = 'Activo' LIMIT 1);

INSERT INTO Colaborador (
    CodigoColaborador, TipoIdentificacion, Identificacion, Nombre, PrimerApellido, SegundoApellido,
    FechaNacimiento, CorreoElectronico, Telefono, Direccion, FechaIngreso, IdEstadoLaboral,
    IdDepartamento, IdPuesto, IdUsuario, Observaciones, CreadoPor, EstadoRegistro
)
VALUES
('CDC-001', 'Cédula física', '1-1456-0789', 'Mariana', 'Solís', 'Araya', '1988-04-18', 'mariana.solis@cdc.cr', '8888-1101', 'Cartago, Paraíso', '2021-03-15', @EstadoLaboralActivo, (SELECT IdDepartamento FROM Departamento WHERE Nombre='Recursos Humanos'), (SELECT IdPuesto FROM Puesto WHERE Nombre='Coordinadora de Recursos Humanos'), NULL, 'Colaboradora responsable del expediente laboral.', @DemoUsuarioId, 'Activo'),
('CDC-002', 'Cédula física', '1-1188-0456', 'Carlos', 'Méndez', 'Vargas', '1982-09-07', 'carlos.mendez@cdc.cr', '8888-1102', 'San José, Curridabat', '2020-02-10', @EstadoLaboralActivo, (SELECT IdDepartamento FROM Departamento WHERE Nombre='Operaciones'), (SELECT IdPuesto FROM Puesto WHERE Nombre='Supervisor de Campo'), NULL, 'Supervisor asignado a proyectos de drenaje urbano.', @DemoUsuarioId, 'Activo'),
('CDC-003', 'Cédula física', '3-0777-0333', 'Daniela', 'Rojas', 'Mora', '1991-12-02', 'daniela.rojas@cdc.cr', '8888-1103', 'Heredia, San Pablo', '2022-07-01', @EstadoLaboralActivo, (SELECT IdDepartamento FROM Departamento WHERE Nombre='Ingeniería'), (SELECT IdPuesto FROM Puesto WHERE Nombre='Ingeniero de Proyecto'), NULL, 'Encargada de seguimiento técnico.', @DemoUsuarioId, 'Activo'),
('CDC-004', 'Cédula física', '2-0999-0444', 'Luis', 'Castro', 'Jiménez', '1979-06-22', 'luis.castro@cdc.cr', '8888-1104', 'Alajuela, Grecia', '2019-11-20', @EstadoLaboralActivo, (SELECT IdDepartamento FROM Departamento WHERE Nombre='Operaciones'), (SELECT IdPuesto FROM Puesto WHERE Nombre='Operador de Maquinaria'), NULL, 'Operador con experiencia en excavación y canalización.', @DemoUsuarioId, 'Activo')
ON DUPLICATE KEY UPDATE
Nombre = VALUES(Nombre),
PrimerApellido = VALUES(PrimerApellido),
SegundoApellido = VALUES(SegundoApellido),
CorreoElectronico = VALUES(CorreoElectronico),
Telefono = VALUES(Telefono),
Direccion = VALUES(Direccion),
IdEstadoLaboral = VALUES(IdEstadoLaboral),
IdDepartamento = VALUES(IdDepartamento),
IdPuesto = VALUES(IdPuesto),
Observaciones = VALUES(Observaciones),
FechaModificacion = NOW(),
ModificadoPor = @DemoUsuarioId,
EstadoRegistro = 'Activo';

SET @ColMariana := (SELECT IdColaborador FROM Colaborador WHERE CodigoColaborador='CDC-001');
SET @ColCarlos := (SELECT IdColaborador FROM Colaborador WHERE CodigoColaborador='CDC-002');
SET @ColDaniela := (SELECT IdColaborador FROM Colaborador WHERE CodigoColaborador='CDC-003');
SET @ColLuis := (SELECT IdColaborador FROM Colaborador WHERE CodigoColaborador='CDC-004');

INSERT INTO Contrato (IdColaborador, TipoContrato, FechaInicio, FechaFin, SalarioBase, Jornada, PeriodicidadPago, EstadoContrato, Observaciones, CreadoPor, EstadoRegistro)
SELECT @ColMariana, 'Contrato indefinido', '2021-03-15', NULL, 925000.00, 'Tiempo completo', 'Quincenal', 'Activo', 'Contrato vigente para gestión de RRHH.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Contrato WHERE IdColaborador=@ColMariana AND FechaInicio='2021-03-15');

INSERT INTO Contrato (IdColaborador, TipoContrato, FechaInicio, FechaFin, SalarioBase, Jornada, PeriodicidadPago, EstadoContrato, Observaciones, CreadoPor, EstadoRegistro)
SELECT @ColCarlos, 'Contrato indefinido', '2020-02-10', NULL, 980000.00, 'Tiempo completo', 'Quincenal', 'Activo', 'Contrato vigente para supervisión operativa.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Contrato WHERE IdColaborador=@ColCarlos AND FechaInicio='2020-02-10');

INSERT INTO Contrato (IdColaborador, TipoContrato, FechaInicio, FechaFin, SalarioBase, Jornada, PeriodicidadPago, EstadoContrato, Observaciones, CreadoPor, EstadoRegistro)
SELECT @ColDaniela, 'Contrato indefinido', '2022-07-01', NULL, 1050000.00, 'Tiempo completo', 'Quincenal', 'Activo', 'Contrato vigente para ingeniería de proyectos.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Contrato WHERE IdColaborador=@ColDaniela AND FechaInicio='2022-07-01');

INSERT INTO Contrato (IdColaborador, TipoContrato, FechaInicio, FechaFin, SalarioBase, Jornada, PeriodicidadPago, EstadoContrato, Observaciones, CreadoPor, EstadoRegistro)
SELECT @ColLuis, 'Contrato indefinido', '2019-11-20', NULL, 790000.00, 'Tiempo completo', 'Quincenal', 'Activo', 'Contrato vigente para operación de maquinaria.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Contrato WHERE IdColaborador=@ColLuis AND FechaInicio='2019-11-20');

INSERT INTO ParametroPlanilla (Codigo, Nombre, Descripcion, TipoParametro, ValorDecimal, ValorTexto, EsEditable, FechaVigenciaInicio, CreadoPor, EstadoRegistro)
VALUES
('PORCENTAJE_CARGAS_OBRERAS', 'Porcentaje de cargas obreras', 'Parámetro configurable para deducciones del trabajador.', 'Porcentaje', 10.6700, NULL, 1, '2026-01-01', @DemoUsuarioId, 'Activo'),
('PORCENTAJE_CARGAS_PATRONALES', 'Porcentaje de cargas patronales', 'Parámetro configurable para estimación patronal.', 'Porcentaje', 26.6700, NULL, 1, '2026-01-01', @DemoUsuarioId, 'Activo'),
('PORCENTAJE_PROVISIONES', 'Porcentaje de provisiones', 'Estimación interna de provisiones laborales.', 'Porcentaje', 8.3300, NULL, 1, '2026-01-01', @DemoUsuarioId, 'Activo'),
('MONTO_BONO_MOVILIZACION', 'Bono de movilización', 'Monto configurable para apoyo de movilización.', 'Monto', 35000.0000, NULL, 1, '2026-01-01', @DemoUsuarioId, 'Activo'),
('HORAS_MENSUALES_REFERENCIA', 'Horas mensuales de referencia', 'Referencia para cálculos simples de valor hora.', 'Cantidad', 240.0000, NULL, 1, '2026-01-01', @DemoUsuarioId, 'Activo')
ON DUPLICATE KEY UPDATE
Nombre = VALUES(Nombre),
Descripcion = VALUES(Descripcion),
TipoParametro = VALUES(TipoParametro),
ValorDecimal = VALUES(ValorDecimal),
FechaVigenciaInicio = VALUES(FechaVigenciaInicio),
FechaModificacion = NOW(),
ModificadoPor = @DemoUsuarioId,
EstadoRegistro = 'Activo';

SET @EstadoPlanillaBorrador := (SELECT IdEstadoPlanilla FROM EstadoPlanilla WHERE Nombre='Borrador' LIMIT 1);
SET @EstadoPlanillaRevision := (SELECT IdEstadoPlanilla FROM EstadoPlanilla WHERE Nombre='En revisión' LIMIT 1);

INSERT INTO PeriodoPlanilla (CodigoPeriodo, Nombre, TipoPeriodo, FechaInicio, FechaFin, IdEstadoPlanilla, Observaciones, CreadoPor, EstadoRegistro)
VALUES
('2026-Q06-02', 'Segunda quincena de junio 2026', 'Quincenal', '2026-06-16', '2026-06-30', @EstadoPlanillaRevision, 'Periodo preparado para demo de incidencias.', @DemoUsuarioId, 'Activo'),
('2026-Q07-01', 'Primera quincena de julio 2026', 'Quincenal', '2026-07-01', '2026-07-15', @EstadoPlanillaBorrador, 'Periodo disponible para nuevos registros.', @DemoUsuarioId, 'Activo')
ON DUPLICATE KEY UPDATE
Nombre = VALUES(Nombre),
FechaInicio = VALUES(FechaInicio),
FechaFin = VALUES(FechaFin),
IdEstadoPlanilla = VALUES(IdEstadoPlanilla),
Observaciones = VALUES(Observaciones),
EstadoRegistro = 'Activo';

SET @PeriodoJunio := (SELECT IdPeriodoPlanilla FROM PeriodoPlanilla WHERE CodigoPeriodo='2026-Q06-02');
SET @TipoHoraExtra := (SELECT IdTipoIncidenciaPlanilla FROM TipoIncidenciaPlanilla WHERE Nombre='Hora extra' LIMIT 1);
SET @TipoBono := (SELECT IdTipoIncidenciaPlanilla FROM TipoIncidenciaPlanilla WHERE Nombre='Bono' LIMIT 1);
SET @TipoAusencia := (SELECT IdTipoIncidenciaPlanilla FROM TipoIncidenciaPlanilla WHERE Nombre='Ausencia sin goce' LIMIT 1);
SET @TipoDeduccion := (SELECT IdTipoIncidenciaPlanilla FROM TipoIncidenciaPlanilla WHERE Nombre IN ('Deducción','Deduccion') LIMIT 1);

INSERT INTO IncidenciaPlanilla (IdPeriodoPlanilla, IdColaborador, IdTipoIncidenciaPlanilla, FechaIncidencia, Cantidad, Monto, Descripcion, RegistradoPor, EstadoRegistro)
SELECT @PeriodoJunio, @ColCarlos, @TipoHoraExtra, '2026-06-20', 6.00, 42000.00, 'Atención de emergencia por obstrucción pluvial.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM IncidenciaPlanilla WHERE IdPeriodoPlanilla=@PeriodoJunio AND IdColaborador=@ColCarlos AND FechaIncidencia='2026-06-20' AND Descripcion='Atención de emergencia por obstrucción pluvial.');

INSERT INTO IncidenciaPlanilla (IdPeriodoPlanilla, IdColaborador, IdTipoIncidenciaPlanilla, FechaIncidencia, Cantidad, Monto, Descripcion, RegistradoPor, EstadoRegistro)
SELECT @PeriodoJunio, @ColLuis, @TipoBono, '2026-06-22', 1.00, 35000.00, 'Bono por disponibilidad de maquinaria en fin de semana.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM IncidenciaPlanilla WHERE IdPeriodoPlanilla=@PeriodoJunio AND IdColaborador=@ColLuis AND FechaIncidencia='2026-06-22' AND Descripcion='Bono por disponibilidad de maquinaria en fin de semana.');

INSERT INTO IncidenciaPlanilla (IdPeriodoPlanilla, IdColaborador, IdTipoIncidenciaPlanilla, FechaIncidencia, Cantidad, Monto, Descripcion, RegistradoPor, EstadoRegistro)
SELECT @PeriodoJunio, @ColMariana, @TipoAusencia, '2026-06-24', 0.50, 18500.00, 'Ausencia parcial registrada con autorización administrativa.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM IncidenciaPlanilla WHERE IdPeriodoPlanilla=@PeriodoJunio AND IdColaborador=@ColMariana AND FechaIncidencia='2026-06-24' AND Descripcion='Ausencia parcial registrada con autorización administrativa.');

SET @EstadoProyectoEjecucion := (SELECT IdEstadoProyecto FROM EstadoProyecto WHERE Nombre='En ejecución' LIMIT 1);
SET @EstadoProyectoPlanificado := (SELECT IdEstadoProyecto FROM EstadoProyecto WHERE Nombre='Planificado' LIMIT 1);

INSERT INTO Proyecto (CodigoProyecto, NombreProyecto, Descripcion, FechaInicio, FechaFinEstimada, Responsable, Ubicacion, IdEstadoProyecto, Observaciones, CreadoPor, EstadoRegistro)
VALUES
('PRY-CD-2026-001', 'Canalización pluvial Barrio El Carmen', 'Mejora de drenaje urbano y limpieza de cauce secundario.', '2026-05-06', '2026-08-30', 'Carlos Méndez Vargas', 'Cartago, Barrio El Carmen', @EstadoProyectoEjecucion, 'Proyecto prioritario para temporada lluviosa.', @DemoUsuarioId, 'Activo'),
('PRY-CD-2026-002', 'Rehabilitación de alcantarillado La Ribera', 'Sustitución de tramos críticos y reparación de cajas pluviales.', '2026-06-03', '2026-09-15', 'Daniela Rojas Mora', 'Heredia, La Ribera', @EstadoProyectoEjecucion, 'Incluye coordinación con cuadrilla operativa.', @DemoUsuarioId, 'Activo'),
('PRY-CD-2026-003', 'Mantenimiento preventivo Quebrada Azul', 'Limpieza programada y control de sedimentos.', '2026-07-10', '2026-10-20', 'Carlos Méndez Vargas', 'Alajuela, Grecia', @EstadoProyectoPlanificado, 'Preparado para mostrar planificación operativa.', @DemoUsuarioId, 'Activo')
ON DUPLICATE KEY UPDATE
NombreProyecto = VALUES(NombreProyecto),
Descripcion = VALUES(Descripcion),
FechaInicio = VALUES(FechaInicio),
FechaFinEstimada = VALUES(FechaFinEstimada),
Responsable = VALUES(Responsable),
Ubicacion = VALUES(Ubicacion),
IdEstadoProyecto = VALUES(IdEstadoProyecto),
Observaciones = VALUES(Observaciones),
EstadoRegistro = 'Activo';

SET @TipoMaquinaria := (SELECT IdTipoActivo FROM TipoActivo WHERE Nombre='Maquinaria' LIMIT 1);
SET @TipoEquipo := (SELECT IdTipoActivo FROM TipoActivo WHERE Nombre='Equipo' LIMIT 1);
SET @CategoriaMaquinaria := (SELECT IdCategoriaActivo FROM CategoriaActivo WHERE IdTipoActivo=@TipoMaquinaria AND Nombre='General' LIMIT 1);
SET @CategoriaEquipo := (SELECT IdCategoriaActivo FROM CategoriaActivo WHERE IdTipoActivo=@TipoEquipo AND Nombre='General' LIMIT 1);
SET @EstadoActivoDisponible := (SELECT IdEstadoActivo FROM EstadoActivo WHERE Nombre='Disponible' LIMIT 1);
SET @EstadoActivoAsignado := (SELECT IdEstadoActivo FROM EstadoActivo WHERE Nombre='Asignado' LIMIT 1);
SET @MedicionHoras := (SELECT IdTipoMedicionUso FROM TipoMedicionUso WHERE Nombre='Horas' LIMIT 1);

INSERT INTO Activo (CodigoActivo, NombreActivo, IdTipoActivo, IdCategoriaActivo, Marca, Modelo, NumeroSerie, Descripcion, FechaAdquisicion, ValorAdquisicion, UbicacionActual, IdEstadoActivo, IdTipoMedicionUso, LecturaUsoActual, IdResponsableActual, Observaciones, CreadoPor, EstadoRegistro)
VALUES
('ACT-MAQ-001', 'Retroexcavadora CAT 420F2', @TipoMaquinaria, @CategoriaMaquinaria, 'Caterpillar', '420F2', 'CAT420F2-CR-118', 'Equipo para excavación y limpieza de canales.', '2021-04-10', 58500000.00, 'Plantel Cartago', @EstadoActivoAsignado, @MedicionHoras, 1285.50, @ColLuis, 'Asignada a obras de canalización.', @DemoUsuarioId, 'Activo'),
('ACT-MAQ-002', 'Minicargador Bobcat S650', @TipoMaquinaria, @CategoriaMaquinaria, 'Bobcat', 'S650', 'BOB-S650-221', 'Equipo compacto para remoción de material.', '2022-08-15', 31200000.00, 'Plantel Heredia', @EstadoActivoDisponible, @MedicionHoras, 745.00, @ColLuis, 'Disponible para proyectos urbanos.', @DemoUsuarioId, 'Activo'),
('ACT-EQP-001', 'Estación total Topcon GM-55', @TipoEquipo, @CategoriaEquipo, 'Topcon', 'GM-55', 'TPC-GM55-771', 'Equipo de medición para levantamientos.', '2023-02-20', 6200000.00, 'Oficina de Ingeniería', @EstadoActivoAsignado, @MedicionHoras, 210.00, @ColDaniela, 'Asignado a seguimiento técnico.', @DemoUsuarioId, 'Activo')
ON DUPLICATE KEY UPDATE
NombreActivo = VALUES(NombreActivo),
UbicacionActual = VALUES(UbicacionActual),
IdEstadoActivo = VALUES(IdEstadoActivo),
LecturaUsoActual = VALUES(LecturaUsoActual),
IdResponsableActual = VALUES(IdResponsableActual),
Observaciones = VALUES(Observaciones),
EstadoRegistro = 'Activo';

SET @ProyectoCarmen := (SELECT IdProyecto FROM Proyecto WHERE CodigoProyecto='PRY-CD-2026-001');
SET @ProyectoRibera := (SELECT IdProyecto FROM Proyecto WHERE CodigoProyecto='PRY-CD-2026-002');
SET @ActivoRetro := (SELECT IdActivo FROM Activo WHERE CodigoActivo='ACT-MAQ-001');
SET @ActivoTopcon := (SELECT IdActivo FROM Activo WHERE CodigoActivo='ACT-EQP-001');

INSERT INTO AsignacionActivoProyecto (IdActivo, IdProyecto, FechaInicio, FechaFin, Observaciones, AsignadoPor, EstadoRegistro)
SELECT @ActivoRetro, @ProyectoCarmen, '2026-06-01', '2026-07-15', 'Asignación para limpieza y excavación de canal.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM AsignacionActivoProyecto WHERE IdActivo=@ActivoRetro AND IdProyecto=@ProyectoCarmen AND FechaInicio='2026-06-01');

INSERT INTO AsignacionActivoProyecto (IdActivo, IdProyecto, FechaInicio, FechaFin, Observaciones, AsignadoPor, EstadoRegistro)
SELECT @ActivoTopcon, @ProyectoRibera, '2026-06-10', '2026-07-05', 'Asignación para levantamiento y control de avance.', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM AsignacionActivoProyecto WHERE IdActivo=@ActivoTopcon AND IdProyecto=@ProyectoRibera AND FechaInicio='2026-06-10');

INSERT INTO PaginaContenido (CodigoPagina, Titulo, Contenido, EstaPublicado, FechaPublicacion, CreadoPor, EstadoRegistro)
VALUES
('INICIO', 'Gestión operativa para Canales y Drenajes del Caribe', 'Centralizamos información de recursos humanos, proyectos, activos y comunicación institucional para mejorar la trazabilidad operativa.', 1, NOW(), @DemoUsuarioId, 'Activo'),
('QUIENES_SOMOS', 'Quiénes Somos', 'Canales y Drenajes del Caribe S.R.L. brinda soluciones de drenaje, canalización y mantenimiento pluvial con enfoque técnico, seguridad operativa y seguimiento responsable.', 1, NOW(), @DemoUsuarioId, 'Activo'),
('SERVICIOS', 'Servicios', 'Diseño y ejecución de obras de drenaje, mantenimiento de canales, limpieza de cauces, inspección técnica y soporte operativo para proyectos públicos y privados.', 1, NOW(), @DemoUsuarioId, 'Activo'),
('CONTACTO', 'Contacto', 'Nuestro equipo atiende consultas sobre proyectos, servicios y coordinación operativa mediante canales formales de atención.', 1, NOW(), @DemoUsuarioId, 'Activo'),
('POLITICAS', 'Políticas de privacidad y uso', 'La información recibida se utiliza únicamente para gestionar consultas, servicios y procesos internos autorizados.', 1, NOW(), @DemoUsuarioId, 'Activo')
ON DUPLICATE KEY UPDATE
Titulo = VALUES(Titulo),
Contenido = VALUES(Contenido),
EstaPublicado = 1,
FechaPublicacion = COALESCE(FechaPublicacion, NOW()),
FechaModificacion = NOW(),
ModificadoPor = @DemoUsuarioId,
EstadoRegistro = 'Activo';

INSERT INTO Noticia (Titulo, Resumen, Contenido, EstaPublicado, FechaPublicacion, CreadoPor, EstadoRegistro)
SELECT 'CDC inicia trabajos de limpieza preventiva antes de la temporada lluviosa', 'La empresa reforzó cuadrillas y maquinaria para atender puntos críticos de drenaje.', 'El plan operativo contempla inspección de cauces, limpieza de material sedimentado y coordinación con comunidades para reducir riesgos de obstrucción.', 1, '2026-06-10 09:00:00', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Noticia WHERE Titulo='CDC inicia trabajos de limpieza preventiva antes de la temporada lluviosa');

INSERT INTO Noticia (Titulo, Resumen, Contenido, EstaPublicado, FechaPublicacion, CreadoPor, EstadoRegistro)
SELECT 'Nuevo control interno mejora la trazabilidad de proyectos', 'SIGE-CDC permite consultar avances, responsables y activos asignados.', 'La digitalización de procesos facilita que administración, recursos humanos y operaciones compartan información actualizada durante la ejecución de obras.', 1, '2026-06-14 10:30:00', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Noticia WHERE Titulo='Nuevo control interno mejora la trazabilidad de proyectos');

INSERT INTO Noticia (Titulo, Resumen, Contenido, EstaPublicado, FechaPublicacion, CreadoPor, EstadoRegistro)
SELECT 'Programa de mantenimiento reduce tiempos de respuesta operativa', 'La coordinación anticipada de equipo pesado ayuda a atender incidencias con mayor precisión.', 'El seguimiento preventivo de maquinaria y proyectos priorizados contribuye a una operación más ordenada y medible.', 1, '2026-06-18 08:15:00', @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Noticia WHERE Titulo='Programa de mantenimiento reduce tiempos de respuesta operativa');

INSERT INTO Galeria (Nombre, Descripcion, EstaPublicado, CreadoPor, EstadoRegistro)
SELECT 'Obras de canalización y drenaje', 'Registro visual de trabajos de limpieza, canalización y control pluvial.', 1, @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Galeria WHERE Nombre='Obras de canalización y drenaje');

INSERT INTO Galeria (Nombre, Descripcion, EstaPublicado, CreadoPor, EstadoRegistro)
SELECT 'Equipo operativo en campo', 'Galería institucional de maquinaria, cuadrillas y seguimiento técnico.', 1, @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM Galeria WHERE Nombre='Equipo operativo en campo');

INSERT INTO ProyectoPublicado (IdProyecto, Titulo, Descripcion, EstadoVisual, EstaPublicado, FechaPublicacion, CreadoPor, EstadoRegistro)
SELECT @ProyectoCarmen, 'Canalización pluvial Barrio El Carmen', 'Intervención integral para mejorar el flujo pluvial y reducir puntos de obstrucción.', 'En ejecución', 1, NOW(), @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM ProyectoPublicado WHERE Titulo='Canalización pluvial Barrio El Carmen');

INSERT INTO ProyectoPublicado (IdProyecto, Titulo, Descripcion, EstadoVisual, EstaPublicado, FechaPublicacion, CreadoPor, EstadoRegistro)
SELECT @ProyectoRibera, 'Rehabilitación de alcantarillado La Ribera', 'Sustitución de tramos críticos con seguimiento técnico y control operativo.', 'En ejecución', 1, NOW(), @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM ProyectoPublicado WHERE Titulo='Rehabilitación de alcantarillado La Ribera');

INSERT INTO FAQ (Pregunta, Respuesta, Orden, EstaPublicado, CreadoPor, EstadoRegistro)
SELECT '¿Qué tipo de proyectos atiende la empresa?', 'Atendemos proyectos de canalización, drenaje pluvial, mantenimiento de cauces, limpieza preventiva y soporte operativo especializado.', 1, 1, @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM FAQ WHERE Pregunta='¿Qué tipo de proyectos atiende la empresa?');

INSERT INTO FAQ (Pregunta, Respuesta, Orden, EstaPublicado, CreadoPor, EstadoRegistro)
SELECT '¿Cómo puedo solicitar información sobre un servicio?', 'Puede enviar una consulta desde el formulario de contacto. El equipo administrativo revisará la solicitud y dará seguimiento.', 2, 1, @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM FAQ WHERE Pregunta='¿Cómo puedo solicitar información sobre un servicio?');

INSERT INTO FAQ (Pregunta, Respuesta, Orden, EstaPublicado, CreadoPor, EstadoRegistro)
SELECT '¿La empresa trabaja con maquinaria propia?', 'Sí. La operación cuenta con maquinaria y equipos registrados para facilitar la planificación y trazabilidad de asignaciones.', 3, 1, @DemoUsuarioId, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM FAQ WHERE Pregunta='¿La empresa trabaja con maquinaria propia?');

INSERT INTO ConsultaContacto (Nombre, CorreoElectronico, Telefono, Asunto, Mensaje, FechaEnvio, EstadoConsulta, ObservacionesInternas, EstadoRegistro)
SELECT 'Laura Hernández', 'laura.hernandez@example.com', '8888-2201', 'Consulta por limpieza de canal', 'Necesitamos valorar una limpieza preventiva en una comunidad con problemas de escorrentía.', '2026-06-19 08:35:00', 'Nueva', NULL, 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM ConsultaContacto WHERE CorreoElectronico='laura.hernandez@example.com' AND FechaEnvio='2026-06-19 08:35:00');

INSERT INTO ConsultaContacto (Nombre, CorreoElectronico, Telefono, Asunto, Mensaje, FechaEnvio, EstadoConsulta, ObservacionesInternas, EstadoRegistro)
SELECT 'Municipalidad de San Rafael', 'infraestructura@sanrafael.example.com', '8888-2202', 'Solicitud de inspección', 'Solicitamos coordinar inspección técnica en alcantarillado pluvial cercano al parque central.', '2026-06-20 11:10:00', 'EnRevision', 'Pendiente coordinar visita técnica.', 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM ConsultaContacto WHERE CorreoElectronico='infraestructura@sanrafael.example.com' AND FechaEnvio='2026-06-20 11:10:00');

INSERT INTO ConsultaContacto (Nombre, CorreoElectronico, Telefono, Asunto, Mensaje, FechaEnvio, EstadoConsulta, ObservacionesInternas, EstadoRegistro)
SELECT 'Andrés Mora', 'andres.mora@example.com', '8888-2203', 'Cotización de obra menor', 'Requiero información para una intervención de drenaje en propiedad privada.', '2026-06-21 14:25:00', 'Atendida', 'Se respondió por correo con solicitud de visita.', 'Activo'
WHERE NOT EXISTS (SELECT 1 FROM ConsultaContacto WHERE CorreoElectronico='andres.mora@example.com' AND FechaEnvio='2026-06-21 14:25:00');

INSERT INTO BitacoraAuditoria (IdUsuario, FechaHora, Accion, Entidad, IdRegistro, ValoresAnteriores, ValoresNuevos, DireccionIP, Observacion)
SELECT @DemoUsuarioId, '2026-06-21 08:00:00', 'Acceso exitoso', 'Autenticacion', NULL, NULL, JSON_OBJECT('resultado','exitoso'), '127.0.0.1', 'Ingreso administrativo de demostración.'
WHERE NOT EXISTS (SELECT 1 FROM BitacoraAuditoria WHERE Accion='Acceso exitoso' AND FechaHora='2026-06-21 08:00:00');

INSERT INTO BitacoraAuditoria (IdUsuario, FechaHora, Accion, Entidad, IdRegistro, ValoresAnteriores, ValoresNuevos, DireccionIP, Observacion)
SELECT @DemoUsuarioId, '2026-06-21 08:12:00', 'Consulta', 'Colaborador', 'CDC-002', NULL, JSON_OBJECT('modulo','Recursos Humanos'), '127.0.0.1', 'Consulta de expediente laboral.'
WHERE NOT EXISTS (SELECT 1 FROM BitacoraAuditoria WHERE Accion='Consulta' AND Entidad='Colaborador' AND FechaHora='2026-06-21 08:12:00');

INSERT INTO BitacoraAuditoria (IdUsuario, FechaHora, Accion, Entidad, IdRegistro, ValoresAnteriores, ValoresNuevos, DireccionIP, Observacion)
SELECT @DemoUsuarioId, '2026-06-21 08:20:00', 'Registro', 'IncidenciaPlanilla', '2026-Q06-02', NULL, JSON_OBJECT('periodo','2026-Q06-02'), '127.0.0.1', 'Registro de incidencia de planilla para demo.'
WHERE NOT EXISTS (SELECT 1 FROM BitacoraAuditoria WHERE Accion='Registro' AND Entidad='IncidenciaPlanilla' AND FechaHora='2026-06-21 08:20:00');

COMMIT;

/* ============================================================
   Bloque opcional para imagenes de galeria

   Ejecutar solo si antes se colocan estos archivos fisicos:
   %LOCALAPPDATA%\SIGE-CDC\archivos-protegidos\galerias\demo\canalizacion-barrio-el-carmen.jpg
   %LOCALAPPDATA%\SIGE-CDC\archivos-protegidos\galerias\demo\equipo-operativo-campo.jpg
   ============================================================ */

/*
USE SIGE_CDC_DB;
SET NAMES utf8mb4;

START TRANSACTION;

SET @DemoUsuarioId := (SELECT Id FROM AspNetUsers ORDER BY FechaCreacion LIMIT 1);
SET @TipoImagen := (
    SELECT IdTipoDocumento
    FROM TipoDocumento
    WHERE Nombre IN ('Imagen galería', 'Imagen galeria')
    ORDER BY IdTipoDocumento
    LIMIT 1
);
SET @GaleriaObras := (SELECT IdGaleria FROM Galeria WHERE Nombre='Obras de canalización y drenaje' LIMIT 1);
SET @GaleriaEquipo := (SELECT IdGaleria FROM Galeria WHERE Nombre='Equipo operativo en campo' LIMIT 1);

INSERT INTO DocumentoArchivo (
    EntidadRelacionada, IdEntidadRelacionada, IdTipoDocumento, NombreOriginal, NombreAlmacenado,
    RutaRelativa, MimeType, TamanoBytes, CargadoPor, EstadoRegistro
)
SELECT 'Galeria', @GaleriaObras, @TipoImagen, 'canalizacion-barrio-el-carmen.jpg', 'canalizacion-barrio-el-carmen.jpg',
       'galerias/demo/canalizacion-barrio-el-carmen.jpg', 'image/jpeg', 245000, @DemoUsuarioId, 'Activo'
WHERE @GaleriaObras IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DocumentoArchivo WHERE RutaRelativa='galerias/demo/canalizacion-barrio-el-carmen.jpg');

SET @DocObras := (SELECT IdDocumentoArchivo FROM DocumentoArchivo WHERE RutaRelativa='galerias/demo/canalizacion-barrio-el-carmen.jpg' LIMIT 1);

INSERT INTO ImagenGaleria (IdGaleria, IdDocumentoArchivo, Titulo, Descripcion, Orden, EstadoRegistro)
SELECT @GaleriaObras, @DocObras, 'Canalización en Barrio El Carmen', 'Avance de limpieza y conformación del canal pluvial.', 1, 'Activo'
WHERE @DocObras IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM ImagenGaleria WHERE IdDocumentoArchivo=@DocObras);

INSERT INTO DocumentoArchivo (
    EntidadRelacionada, IdEntidadRelacionada, IdTipoDocumento, NombreOriginal, NombreAlmacenado,
    RutaRelativa, MimeType, TamanoBytes, CargadoPor, EstadoRegistro
)
SELECT 'Galeria', @GaleriaEquipo, @TipoImagen, 'equipo-operativo-campo.jpg', 'equipo-operativo-campo.jpg',
       'galerias/demo/equipo-operativo-campo.jpg', 'image/jpeg', 230000, @DemoUsuarioId, 'Activo'
WHERE @GaleriaEquipo IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DocumentoArchivo WHERE RutaRelativa='galerias/demo/equipo-operativo-campo.jpg');

SET @DocEquipo := (SELECT IdDocumentoArchivo FROM DocumentoArchivo WHERE RutaRelativa='galerias/demo/equipo-operativo-campo.jpg' LIMIT 1);

INSERT INTO ImagenGaleria (IdGaleria, IdDocumentoArchivo, Titulo, Descripcion, Orden, EstadoRegistro)
SELECT @GaleriaEquipo, @DocEquipo, 'Equipo operativo en campo', 'Maquinaria asignada para atención de obras pluviales.', 1, 'Activo'
WHERE @DocEquipo IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM ImagenGaleria WHERE IdDocumentoArchivo=@DocEquipo);

COMMIT;
*/

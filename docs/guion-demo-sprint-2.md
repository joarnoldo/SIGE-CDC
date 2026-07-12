# Guion de demo - Sprint 2 SIGE-CDC

## Objetivo

Validar que la rama integrada de Sprint 2 se puede mostrar con un recorrido claro por el sitio público, administración, recursos humanos, planillas y operaciones.

## Datos mínimos requeridos

- Base `SIGE_CDC_DB` creada con `database/ScriptDB_SIGE_CDC_Corregido.sql`.
- Roles oficiales existentes: Administrador, Recursos Humanos, Operaciones y Empleado.
- Usuario con rol Administrador.
- Usuario con rol Recursos Humanos.
- Usuario con rol Operaciones.
- Catálogos semilla activos: departamentos, puestos, estados laborales, tipos de documento, tipos de incidencia y estados de proyecto.
- Al menos un período de planilla activo para probar incidencias.

## Recorrido público

1. Abrir Inicio, Quiénes Somos, Servicios, Portafolio, Noticias, Galería, Preguntas Frecuentes, Contacto y Políticas.
2. Enviar una consulta desde Contacto.
3. Confirmar que la consulta queda registrada para revisión administrativa.

## Recorrido Administrador

1. Iniciar sesión como Administrador.
2. Administrar contenido de páginas y publicar una sección visible.
3. Registrar y publicar una noticia; verificar que aparece en Noticias.
4. Registrar una galería, adjuntar una imagen autorizada, publicar la galería y verificar que aparece en Galería.
5. Registrar o seleccionar un proyecto interno y publicar una tarjeta en Portafolio.
6. Registrar una pregunta frecuente y verificar que aparece en Preguntas Frecuentes.
7. Abrir Historial y validar accesos internos y formularios en modo de solo lectura.

## Recorrido Recursos Humanos

1. Crear un colaborador con código manual, identificación, departamento, puesto y estado laboral.
2. Editar el colaborador y confirmar los cambios.
3. Adjuntar contrato o documento al expediente; verificar que se registra como metadato y descarga protegida.
4. Crear o editar un parámetro de planilla.
5. Registrar una incidencia de planilla para un período existente.
6. Desactivar colaborador, documento o incidencia solo cuando aplique al flujo de prueba.

## Recorrido Operaciones

1. Iniciar sesión como Operaciones o Administrador.
2. Crear un proyecto con fechas, responsable, estado y observaciones.
3. Confirmar que el proyecto queda disponible para publicación administrativa.

## Validaciones de cierre

- Ejecutar `dotnet restore SIGECDC.slnx`.
- Ejecutar `dotnet build SIGECDC.slnx`.
- Confirmar en MySQL Workbench que no cambió el esquema.
- Confirmar que los archivos se guardan como metadatos en `DocumentoArchivo` y físicamente fuera de `wwwroot`.
- Confirmar que no se crearon roles adicionales.

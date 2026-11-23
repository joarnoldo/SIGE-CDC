# Registro de hallazgos e incidencias — Entregable 1

Proyecto: SIGE-CDC. Cliente: Canales y Drenajes del Caribe S.R.L.

Este registro corresponde al desarrollo y la validación técnica de la nueva suite académica. No contiene resultados de ejecución formal de QA02 y no autoriza correcciones del producto.

Resultado de la revisión de cierre: **ningún defecto del producto confirmado y ningún HALLAZGO-QA-XX abierto por estos 42 casos**. `validacion-tecnica-cierre.trx` registra 42 correctos, 0 fallidos y 0 omitidos. Esto no constituye una afirmación de ausencia total de defectos en SIGE-CDC.

## Incidencias corregidas en la automatización

Los siguientes fallos de diagnóstico se resolvieron dentro de `EntregableImplantacion` o en la precisión de los pasos de QA02. No se clasifican como defectos confirmados del producto ni originan un HALLAZGO-QA de producto.

| Casos | Causa en la prueba | Corrección acotada | Evidencia de resolución |
|---|---|---|---|
| PUB-001, PUB-005, AUTH-001–004, PLA-005–006, PLA-008, AUD-001–003 | El host temporal se quedó sin una fuente de configuración después de aislarlo. | Registrar una fuente en memoria exclusiva del host académico. | `validacion-tecnica-04.trx` supera los recorridos HTTP; las verificaciones posteriores de documentos se completan en `diagnostico-ajustes-05.trx`. |
| PUB-003–004 | Faltaba el contexto de navegación del renderizador de componentes. | Agregar NavigationManager únicamente al soporte de componentes; el servidor HTTP conserva su navegación real. | `diagnostico-soporte-03.trx`: ambos casos correctos. |
| PUB-005 | La carga de referencia ya ocupaba el código de la sección que el caso debía crear. | Preparar esa sección mediante el servicio real dentro de la base desechable. | `diagnostico-soporte-03.trx`: correcto. |
| RH-003 | Se comprobaba el estado del registro en vez del estado laboral. | Comprobar estado laboral Inactivo y conservación del expediente y la bitácora. | `diagnostico-soporte-03.trx`: correcto. |
| RH-006 | Se esperaba una clase de excepción distinta de la que expresa el rechazo real. | Mantener rechazo, ausencia de metadata y ausencia de archivos; ajustar el tipo de excepción del test. | `diagnostico-soporte-03.trx`: correcto. |
| FOR-001, FOR-003–006 | Los contratos sintéticos comenzaban después del día de consulta de elegibilidad. | Extender su vigencia sintética para cumplir las precondiciones actuales y futuras del caso. | FOR-001 y FOR-006 correctos en `validacion-tecnica-04.trx`; restantes, en `diagnostico-ajustes-05.trx`. |
| PLA-003 | La aserción buscaba una palabra que no identifica el diagnóstico real de historia laboral incompleta. | Verificar el bloqueo por historia no demostrable y que no exista persistencia parcial. | `validacion-tecnica-04.trx`: correcto. |
| FOR-003, FOR-005 | El oráculo sumaba un beneficio no salarial al costo laboral. La especificación del proyecto distingue costo laboral y neto. | Comprobar neto de 592.000, costo laboral de 672.000 y diferencia de neto de 10.000 entre escenarios. | `diagnostico-ajustes-05.trx`: ambos correctos. |
| FOR-004 | La secuencia redactada intentaba editar después de calcular y dejaba de cumplir la precondición aprobada de escenario editable. | Usar dos borradores equivalentes: exclusión y exclusión seguida de reincorporación. Mantener los importes esperados y comprobar diferencia de 403.200. QA02 solo precisa precondiciones y pasos; su resultado esperado no cambió. | `diagnostico-ajustes-05.trx`: correcto. |
| PLA-005, PLA-008 | Se exigía un código del período que no es la identidad mostrada por la publicación del snapshot. | Comprobar la ejecución correcta, las fechas inicial y final del período, los colaboradores y los importes exactos en PDF/Excel. | `diagnostico-ajustes-05.trx`: ambos correctos. |
| PLA-003, preparación del entorno | Windows rechazó la apertura de un socket MySQL durante la creación de cuentas sintéticas. El soporte genérico deshabilitaba el pool y producía numerosas conexiones cortas, compatible con agotamiento de puertos temporales. El escenario funcional aún no se había ejecutado. | El soporte académico habilita un pool máximo de diez conexiones por base de TC, lo libera al finalizar y dispone el entorno también si falla su preparación. No se modificó el soporte genérico anterior ni la configuración del sistema operativo. | `validacion-tecnica-cierre.trx`: 42 correctos. El intento previo `validacion-tecnica-final.trx` conserva 41 correctos y un fallo de infraestructura. |

La restricción de edición en Borrador observada en FOR-004 no demuestra por sí sola un defecto: es coherente con la precondición de la selección aprobada. No se cambió el producto, no se forzó el estado mediante SQL y no se omitió el caso.

## Posible hallazgo previo PH-01

La diferencia de fuentes sobre acceso del Administrador a páginas financieras queda fuera de la resolución de este entregable, por instrucción expresa. AUTH-004 utiliza la separación conservadora aprobada. PH-01 no se presenta como resultado fallido de un TC, no se da por resuelto y no se le asigna una severidad nueva sin investigación autorizada.

## Tratamiento de defectos posteriores

Si una ejecución posterior confirma un incumplimiento funcional, se registrará como HALLAZGO-QA-XX con TC, HU, escenario, esperado, observado, evidencia, severidad, componente sospechoso y recomendación. Las correcciones del producto requieren otro proceso autorizado. El resultado global de esta validación se documenta en [ValidacionTecnica.md](ValidacionTecnica.md).

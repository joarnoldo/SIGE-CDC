# Validación técnica de automatizaciones

SIGE-CDC — Entregable 1 de Implantación de Sistemas. Registro de revisión: 5 de octubre de 2026.

**Fase B completada. Validación técnica de cierre: 42 ejecutados, 42 correctos, 0 fallidos y 0 omitidos.** Los resultados corresponden a una única ejecución completa de la nueva suite y no se trasladaron a QA02.

## Documentos y alcance

Se completaron las plantillas originales `QA01-Plan de Pruebas.docx` y `QA02-Casos de Pruebas.xlsx`, ubicadas en `SIGECDC/Entregables Implantación de Sistemas/Entregable 1`. Las copias anteriores están en su subcarpeta `Respaldo Fase B`.

QA01 contiene portada, registro de cambios, introducción, alcance y exclusiones, objetivos de calidad, roles, metodología, niveles, clasificación de errores, suspensión/reanudación, completitud, entregables, herramientas, ambiente y términos. Se redactó en español, identifica al cliente correcto y presenta Codex como apoyo a QA. No certifica producción ni declara UAT ejecutada. Conserva la apariencia institucional y sus cuatro recursos gráficos originales; se inspeccionaron visualmente sus nueve páginas.

QA02 contiene 42 filas, 42 IDs únicos y definición funcional de precondiciones, pasos y resultados esperados. Se conservaron hoja, columnas, encabezados, anchos, combinaciones, estilos institucionales y validaciones, con ajuste de altura y salto de línea para hacer legibles los casos. Se revisaron visualmente los seis bloques del libro y la precisión posterior de FOR-004.

**Cobertura funcional planificada: 30 / 39 HU = 76,92 %.** No es cobertura de código, líneas, ramas o métodos, ni implica cubrir todos los criterios de cada HU.

| Suite | Casos | HU seleccionadas |
|---|---:|---:|
| Sitio público y contenido | 6 | 5 |
| Identidad y autorización | 4 | Transversal, sin aumentar el numerador |
| Recursos Humanos | 6 | 3 |
| Planilla, colillas y reportes | 8 | 6 |
| Activos, proyectos y mantenimiento | 9 | 8 |
| Forecast | 6 | 5 |
| Auditoría | 3 | 3 |
| Total | 42 | 30 |

Distribución: 24 positivos, 4 negativos, 6 de seguridad y 8 de reglas de negocio. Prioridades: 26 ALTA, 15 MEDIA y 1 BAJA. Planilla y Forecast reúnen 14 de los 42 casos; no dominan la selección.

Las 9 HU excluidas son HU-INFO-005, HU-INFO-006, HU-INFO-009, HU-INFO-010, HU-ACT-009, HU-FOR-004, HU-FOR-005, HU-FOR-008 y HU-FOR-009. Ningún módulo principal quedó excluido.

En QA02 se verificó, leyendo el archivo guardado:

- `Execution Type`: **Automatizada** en los 42 casos.
- `Execution Date`: **42 celdas realmente vacías**.
- `Result`: **42 celdas realmente vacías**.
- `Comments`: **42 celdas realmente vacías**.

La corrección de FOR-004 precisó su secuencia para respetar la precondición aprobada de escenario editable. El resultado esperado, TC ID, HU, prioridad y tipo de ejecución se conservaron.

## Automatización y entorno

Hay siete clases de casos y cinco archivos de soporte, con un método xUnit propio por TC. Los 42 métodos tienen TC ID en DisplayName y la HU en Trait; AUTH conserva la relación transversal. No se emplean Skip ni omisiones silenciosas.

La suite utiliza componentes Blazor reales y sus eventos, Identity/cookies/antiforgery y rutas HTTP reales en un servidor local, servicios reales, EF Core, MySQL 8, archivos temporales y generación/lectura de PDF y Excel. No se instaló Selenium, Playwright ni ningún paquete nuevo.

Se reutilizó el proyecto de pruebas y sus dependencias existentes, además de la infraestructura genérica `Fin02BMySqlSandbox`. No se reutilizaron casos, escenarios, datos dorados, aserciones ni resultados de la suite anterior. Los datos de las pruebas académicas son nuevos y sintéticos. Los valores financieros son una simulación descrita en [README.md](README.md), no normativa vigente.

MySQL se inicia en una instancia local nueva, con un puerto temporal y una base independiente por TC. No se leen conexiones empresariales ni SIGECDC_QA_CONNECTION, no se usa Production y no se accede a MonsterASP. La composición financiera habilitada existe únicamente dentro del soporte de QA.

## Restauración y compilación

Comandos ejecutados desde `SIGECDC/`:

```powershell
dotnet restore SIGECDC.slnx
dotnet build SIGECDC.slnx --no-restore -m:1 -p:UseSharedCompilation=false -v:minimal
```

Restauración correcta, sin dependencias nuevas. La compilación final terminó con **0 errores y 0 advertencias**. Un intento previo de compilación no pudo copiar el ensamblado mientras el proceso de pruebas lo tenía abierto; se repitió correctamente después de finalizar ese proceso. No fue un error del código del producto.

## Ejecución técnica de cierre

```powershell
dotnet test SIGECDC.Tests/SIGECDC.Tests.csproj --no-build --filter "FullyQualifiedName~SIGECDC.Tests.EntregableImplantacion" --logger "trx;LogFileName=validacion-tecnica-cierre.trx" --results-directory SIGECDC.Tests/EntregableImplantacion/Evidencia -v:minimal
```

Resultado: **TOTAL 42 / PASSED 42 / FAILED 0 / SKIPPED 0**. El proceso terminó con código 0 y una duración informada de 3 minutos y 41 segundos. Evidencia: [validacion-tecnica-cierre.trx](Evidencia/validacion-tecnica-cierre.trx). No se combinaron ejecuciones parciales para obtener estos conteos ni se ejecutó la suite técnica anterior como sustituto.

No se confirmaron defectos del producto que deban abrirse como HALLAZGO-QA-XX. PH-01 sigue fuera de esta resolución. Las incidencias de diseño y soporte de la automatización están documentadas separadamente.

## Evidencia de diagnóstico conservada

| Archivo TRX en Evidencia | Total | Correctos | Fallidos | Omitidos | Interpretación |
|---|---:|---:|---:|---:|---|
| validacion-tecnica-01.trx | 42 | 20 | 22 | 0 | Primer diagnóstico del soporte y los datos. |
| diagnostico-soporte-02.trx | 0 | 0 | 0 | 0 | Filtro sin coincidencias; no cuenta como ejecución ni aprobación. |
| diagnostico-soporte-03.trx | 8 | 7 | 1 | 0 | Comprobación acotada del soporte. |
| validacion-tecnica-04.trx | 42 | 37 | 5 | 0 | Diagnóstico de oráculos y secuencia editable. |
| diagnostico-ajustes-05.trx | 5 | 5 | 0 | 0 | Correcciones de los cinco casos verificadas. |
| validacion-tecnica-final.trx | 42 | 41 | 1 | 0 | Intento previo al cierre; fallo de apertura de socket durante la preparación de PLA-003. |
| validacion-tecnica-cierre.trx | 42 | 42 | 0 | 0 | Ejecución completa definitiva con soporte de conexiones corregido. |

Los fallos de construcción de la automatización se explican en [Hallazgos.md](Hallazgos.md). Los intentos iniciales sin TRX completo no aportan resultados a este informe.

## Trazabilidad e integridad

La matriz completa TC ID ↔ HU ↔ archivo ↔ método se encuentra en [README.md](README.md#matriz-tc--hu--archivo--método). [Evidencia/verificacion-entregable.json](Evidencia/verificacion-entregable.json) contiene la comprobación del libro guardado y los 42 métodos.

Se compararon huellas SHA-256 de **651 archivos protegidos**, incluidos producto, SQL, documentación principal y ejemplos: **ninguno alterado**. No se usó Git, no se modificó hosting, no se cambiaron Historias de Usuario, contexto, índice, reglas financieras, roles ni configuración del producto.

Esta evidencia corresponde exclusivamente a la validación técnica de las automatizaciones. No llena resultados formales de QA02, no sustituye aceptación humana y no inicia la ejecución formal ni la corrección del producto.

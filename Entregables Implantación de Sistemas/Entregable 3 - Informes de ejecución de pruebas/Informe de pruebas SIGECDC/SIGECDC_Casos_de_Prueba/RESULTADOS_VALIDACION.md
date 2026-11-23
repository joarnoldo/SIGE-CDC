# Validación real de QA 51

25/10/2026, hora de Costa Rica. **51 automatizaciones implementadas y 51 IDs distintos ejecutados en la corrida definitiva.** Resultado: **49 PASS, 2 FAIL, 0 BLOCKED, 0 omitidos, 0 duplicados**. Los 51 resultados pertenecen a los 51 IDs de QA02; hay trazabilidad documental de 39 HU y los casos transversales. No se declara que todas las HU estén aprobadas.

## Evidencia definitiva

- Ejecutor: Windows PowerShell 5.1, `Ejecutar-QA51.ps1 -Modo LocalAislado`, con dotnet/ASP.NET Core 10.0.12, VSTest 18.7 y MySQL privados del paquete. No utiliza SDK ni paquetes NuGet del equipo durante la ejecución.
- Corrida: `resultados/20261025-130620-LocalAislado/`. Finalizó a las **13:10:54 -06:00**, duración de pruebas aproximadamente **4 min 32 s**.
- `QA51.trx`: total=51, executed=51, passed=49, failed=2, notExecuted=0, aborted=0, error=0, timeout=0. Outcome Failed corresponde a los dos defectos detectados, no a una corrida interrumpida.
- `ResultadosPorCaso.csv`: 51 filas con 51 IDs distintos; `Resumen.json` conserva los conteos del ejecutor. Copias de consulta en `validacion/`.
- Los 42 casos originales pasaron; sus IDs coinciden exactamente con los primeros 42 de QA02. Se cotejaron escenario, pasos relevantes y oráculos. PUB-006 añadió consulta HTTP de la tarjeta publicada/oculta, RH-002 comprobó expediente único y PLA-001 añadió una incidencia de otro período para detectar mezcla. El nivel local de servicios/HTTP/componentes se declara en la matriz; no se presenta como ejecución E2E sobre MonsterASP.
- Los nueve añadidos son PUB-007/008/009/010, ACT-010 y FOR-007/008/009/010. Siete pasaron; PUB-008 y FOR-009 fallan por diferencias reales con QA02 descritas en `LIMITACIONES.md`. No se rebajaron sus resultados esperados ni se cambió el producto.
- Build Release del arnés: **0 errores, 0 advertencias**. Ninguna modificación de fuentes de producto respecto al estado publicado de PRESENTACION-01.

## Subconjunto remoto

`Ejecutar-QA51.ps1 -Modo RemotoLectura` ejecutó **PUB-001 y PUB-009** contra `https://sigecdc.runasp.net/`: **2 PASS, 0 FAIL, 0 BLOCKED**. Carpeta `resultados/20261025-131119-RemotoLectura/`, final **13:11:28 -06:00**, aproximadamente 9 s. Solo GET anónimos por HTTPS, sin SQL, autenticación ni escritura. Estos dos IDs ya forman parte de los 51; no se suman como otros dos casos ni representan una ejecución remota completa de 51.

## Antecedentes conservados

La primera corrida de 51 terminó 45 PASS/6 FAIL/0 omitidos. Se corrigieron exclusivamente datos de preparación y acciones del arnés: respaldo obligatorio en condiciones, vigencia de proyectos para dos meses, lectura de la respuesta FAQ tras abrir su acordeón e identificación de la contratación por su código persistido. Sus importes esperados no se alteraron. El reintento dirigido de cinco casos conserva el estado previo a la corrección final de identificación. No usar esos TRX parciales como resultado definitivo. La prueba inicial del ejecutor descubrió un registrador TRX faltante y se incluyó antes de la corrida final.

No se ejecutó la suite histórica completa. Sin Git, DDL remoto, pagos, cambios de fórmulas, hosting ni datos de presentación. QA01/QA02 copiados byte por byte; huellas en `Originales/SHA256.json`. El ZIP no contiene respaldos privados, perfiles WebDeploy, secretos ni claves Data Protection.

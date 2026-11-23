# Ejecutar las 51 pruebas locales

Requisitos: Windows 10/11 de 64 bits, Windows PowerShell 5.1 o posterior, aproximadamente 2 GB libres y permisos para escribir archivos e iniciar procesos locales.

El paquete incluye .NET 10, VSTest, xUnit, MySQL portátil y las dependencias necesarias. No requiere Visual Studio, instalación manual de paquetes ni cuentas o contraseñas de MonsterASP.

1. Extraiga **todo** el ZIP en una carpeta local, por ejemplo `C:\QA51`. No ejecute dentro del ZIP.
2. Abra `Ejecutar-QA51.cmd` con doble clic, o desde una terminal en esa carpeta:

```bat
Ejecutar-QA51.cmd
```

3. Espere a que termine y consulte `resultados\<fecha>-LocalAislado\`:
   - `QA51.trx`: resultados de VSTest.
   - `ResultadosPorCaso.csv`: resultado individual de los 51 casos.
   - `Resumen.json`: conteos de PASS, FAIL y BLOCKED.

Las 51 pruebas se ejecutan en un ambiente local aislado; no comprueban el sitio publicado en MonsterASP. Cada ejecución prepara sus datos temporales. No ejecute dos copias simultáneamente.

Para verificar dependencias e inventario sin ejecutar pruebas:

```bat
Ejecutar-QA51.cmd -Inventario
```

El ejecutor verifica la integridad del paquete antes de comenzar. Mantenga sus archivos incluidos sin modificaciones. El código de salida es 0 si todos los casos pasan y 1 si hay fallos, bloqueos o resultados faltantes.

# Dependencias redistribuidas para ejecución local

Se conservan las licencias y avisos disponibles en las distribuciones locales originales y los paquetes NuGet utilizados. No se incluyen configuraciones ni bases de los servidores instalados.

- .NET y ASP.NET Core 10.0.12: `dotnet/LICENSE.txt`, `dotnet/ThirdPartyNotices.txt`. Fuentes: https://github.com/dotnet/runtime y https://github.com/dotnet/aspnetcore.
- VSTest 18.7.0: distribución incluida en SDK .NET 10.0.401; proyecto https://github.com/microsoft/vstest. Solo se empaqueta su ejecutor y extensiones, no se instala el SDK.
- MySQL Community Server portátil: `mysql/LICENSE`, `mysql/README`. Distribución de Oracle; fuentes y licencias en https://dev.mysql.com/downloads/mysql/ y https://dev.mysql.com/doc/. No se incluyen datos de la instalación local ni sus archivos de configuración.
- Bibliotecas NuGet del producto/xUnit: avisos y metadatos originales en `licencias/NuGet`. Versiones resueltas en los `.deps.json` y proyectos fuente.
- DLL del runtime C/C++ redistribuible: proceden de `Microsoft.VC145.CRT` del redistribuible de Visual Studio instalado, copiadas para uso privado del proceso MySQL. Fuente del redistribuible: https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist.

Este paquete no cambia la licencia de los componentes de terceros ni atribuye su autoría al equipo SIGE-CDC. Para ejecutar no es necesario descargar ni instalar paquetes.

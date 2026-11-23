@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Ejecutar-QA51.ps1" %*
set "QA51_RESULT=%ERRORLEVEL%"
echo.
echo Consulte la carpeta resultados para los 51 resultados individuales.
exit /b %QA51_RESULT%

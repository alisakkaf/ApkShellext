@ECHO OFF
CLS
ECHO ===================================================================
ECHO   AliSakkaF ApkShellext - Auto-Update Safe Test Launcher
ECHO ===================================================================
ECHO.
ECHO   Running Auto-Update test simulation...
ECHO.
powershell -ExecutionPolicy Bypass -File "%~dp0TestAutoUpdate.ps1"
PAUSE
@ECHO ON

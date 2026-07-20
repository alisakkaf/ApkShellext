@ECHO OFF
:: Automatically unblock all downloaded project files to prevent SmartScreen/Security block warnings
powershell -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File" 2>nul
CLS
ECHO ===================================================================
ECHO   AliSakkaF ApkShellext - Shell Extension Uninstaller
ECHO ===================================================================
ECHO.
ECHO   * GitHub:   https://github.com/alisakkaf
ECHO   * Facebook: https://www.facebook.com/AliSakkaf.Dev/
ECHO   * Website:  https://alisakkaf.com/
ECHO.
ECHO -------------------------------------------------------------------
ECHO   Checking Administrative Privileges...
ECHO -------------------------------------------------------------------

REM === check and get the UAC for administrator privilege ===
REM === code from https://sites.google.com/site/eneerge/scripts/batchgotadmin
REM === 
:: BatchGotAdmin
:-------------------------------------
REM  --> Check for permissions
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo   Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"

    "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
    pushd "%CD%"
    CD /D "%~dp0"
:--------------------------------------

echo   [INFO] Platform Architecture: %PROCESSOR_IDENTIFIER%
echo.

set FRAMEWORK=%windir%\Microsoft.NET\Framework
set DOTNETVERSION=v4.0.30319
echo %PROCESSOR_IDENTIFIER% | FIND /i "x86" > nul
IF %ERRORLEVEL%==1 (
  set FRAMEWORK=%FRAMEWORK%64
)
set REGASM="%FRAMEWORK%\%DOTNETVERSION%\regasm.exe"

ECHO -------------------------------------------------------------------
ECHO   Unregistering Shell Extension...
ECHO -------------------------------------------------------------------
%REGASM% /unregister "%~dp0\ApkShellext.dll"
echo.

ECHO -------------------------------------------------------------------
ECHO   Cleaning up registry settings...
ECHO -------------------------------------------------------------------
reg delete "HKCU\Software\ApkShellext" /f >nul 2>&1
echo   [STATUS] Registry keys successfully removed.
echo.

ECHO ===================================================================
ECHO   Uninstallation Completed Successfully!
ECHO.
ECHO   * GitHub:   https://github.com/alisakkaf
ECHO   * Facebook: https://www.facebook.com/AliSakkaf.Dev/
ECHO   * Website:  https://alisakkaf.com/
ECHO ===================================================================

PAUSE
@ECHO ON

@ECHO OFF
CLS
ECHO ===================================================================
ECHO   AliSakkaF ApkShellext - Shell Extension Installer
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
:: BatchGotAdmin
:-------------------------------------
REM  --> Check for permissions
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
	if '%1' EQU '1' (
		echo   [ERROR] Cannot elevate administrator privilege.
		echo   Please try again with "Run as Administrator".
		echo   Installation failed.
		pause
		exit /B
	) else (
		echo   Requesting administrative privileges...
		goto UACPrompt
	)
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "1", "", "runas", 1 >> "%temp%\getadmin.vbs"

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
ECHO   Registering Shell Extension...
ECHO -------------------------------------------------------------------
%REGASM% /codebase "%~dp0\ApkShellext.dll"
echo.

ECHO -------------------------------------------------------------------
ECHO   Configuring Watermark Settings...
ECHO -------------------------------------------------------------------
CHOICE /C YN /M "   Would you like to enable the 'By AliSakkaF' watermark overlay on icons?"
IF ERRORLEVEL 2 (
    reg add "HKCU\Software\ApkShellext" /v ShowAliSakkaFWatermark /t REG_SZ /d "False" /f >nul
    echo   [STATUS] Watermark overlay has been disabled.
) ELSE (
    reg add "HKCU\Software\ApkShellext" /v ShowAliSakkaFWatermark /t REG_SZ /d "True" /f >nul
    echo   [STATUS] Watermark overlay has been enabled.
)
echo.

ECHO ===================================================================
ECHO   Installation Completed Successfully!
ECHO.
ECHO   AliSakkaF ApkShellext is based on ApkShellext by kkguo.
ECHO   * GitHub:   https://github.com/alisakkaf
ECHO   * Facebook: https://www.facebook.com/AliSakkaf.Dev/
ECHO   * Website:  https://alisakkaf.com/
ECHO ===================================================================

PAUSE
@ECHO ON

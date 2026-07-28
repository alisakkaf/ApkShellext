@ECHO OFF
SETLOCAL EnableExtensions

:: Check for Administrative Privileges using native CMD net session
net session >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    IF "%~1"=="ELEVATED" (
        echo.
        echo ===================================================================
        echo    [ERROR] Administrative privileges required.
        echo    Please right-click debug_Lite.bat and select Run as Administrator.
        echo ===================================================================
        echo.
        echo Press Enter to exit...
        pause >nul
        exit /B 1
    ) ELSE (
        echo Requesting administrative privileges...
        echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
        echo UAC.ShellExecute "%~f0", "ELEVATED", "", "runas", 1 >> "%temp%\getadmin.vbs"
        "%temp%\getadmin.vbs"
        del "%temp%\getadmin.vbs" >nul 2>&1
        exit /B 0
    )
)

PUSHD "%CD%"
CD /D "%~dp0"
CLS

echo ===================================================================
echo   AliSakkaF ApkShellext - Diagnostics Log Generator (Lite)
echo ===================================================================
echo.

set LOGFILE="%~dp0ApkShellext_Debug_Log.txt"
echo ApkShellext Diagnostic Log - %DATE% %TIME% > %LOGFILE%
echo ====================================================== >> %LOGFILE%

echo [1/5] Gathering System Information...
echo OS Architecture: %PROCESSOR_ARCHITECTURE% >> %LOGFILE%
echo System Directory: %SYSTEMROOT% >> %LOGFILE%

set REGASM32="%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe"
set REGASM64="%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"

echo [2/5] Checking .NET Framework Installation...
if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" (
    echo .NET Framework 32-bit: INSTALLED >> %LOGFILE%
) else (
    echo .NET Framework 32-bit: MISSING >> %LOGFILE%
)
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" (
    echo .NET Framework 64-bit: INSTALLED >> %LOGFILE%
) else (
    echo .NET Framework 64-bit: MISSING >> %LOGFILE%
)

echo [3/5] Verifying Registered CLSIDs in Registry...
reg query "HKCR\CLSID\{9C5B86A2-4D1F-4A5B-8D7E-3C2B4A5D9E8F}" >> %LOGFILE% 2>&1
reg query "HKCR\CLSID\{6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF}" >> %LOGFILE% 2>&1

echo [4/5] Testing COM Registration via RegAsm...
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" (
    %REGASM64% /codebase "%~dp0ApkShellext.dll" >> %LOGFILE% 2>&1
)
if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" (
    %REGASM32% /codebase "%~dp0ApkShellext.dll" >> %LOGFILE% 2>&1
)

echo [5/5] Flushing Explorer Icon and Thumbnail Caches...
taskkill /F /IM explorer.exe >nul 2>&1
taskkill /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\iconcache_*.db" >nul 2>&1
del /f /q "%localappdata%\IconCache.db" >nul 2>&1

start explorer.exe

echo.
echo ===================================================================
echo   Diagnostics Completed!
echo   Log saved to: %LOGFILE%
echo ===================================================================
echo.
echo Press Enter to finish...
pause >nul
@ECHO ON

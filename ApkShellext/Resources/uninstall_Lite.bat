@ECHO OFF
SETLOCAL EnableExtensions

net session >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    IF "%~1"=="ELEVATED" (
        echo.
        echo ===================================================================
        echo     [ERROR] Administrative privileges required.
        echo     Please right-click uninstall_Lite.bat and Run as Administrator.
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
echo    AliSakkaF ApkShellext - Shell Extension Uninstaller (Lite Version)
echo ===================================================================
echo.
echo    * GitHub:   https://github.com/alisakkaf
echo    * Facebook: https://www.facebook.com/AliSakkaf.Dev/
echo    * Website:  https://alisakkaf.com/
echo.

echo [1/4] Unregistering Shell Extension COM Server...

set REGASM32="%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe"
set REGASM64="%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"

set FOUND_NET=0
if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" set FOUND_NET=1
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" set FOUND_NET=1

if "%FOUND_NET%"=="0" (
    echo.
    echo ===================================================================
    echo     [ERROR] Microsoft .NET Framework 4.8 ^(or 4.0+^) was not found.
    echo     Please download .NET Framework 4.8 from:
    echo     https://dotnet.microsoft.com/download/dotnet-framework/net48
    echo ===================================================================
    echo.
    echo Press Enter to exit...
    pause >nul
    exit /B 1
)

if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" (
    echo        Unregistering 64-bit COM Server...
    %REGASM64% /unregister "%~dp0ApkShellext.dll" >nul 2>&1
    %REGASM64% /unregister "%~dp0apkshellext2.dll" >nul 2>&1
    %REGASM64% /unregister "%~dp0ApkShellext2.dll" >nul 2>&1
)
if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" (
    echo        Unregistering 32-bit COM Server...
    %REGASM32% /unregister "%~dp0ApkShellext.dll" >nul 2>&1
    %REGASM32% /unregister "%~dp0apkshellext2.dll" >nul 2>&1
    %REGASM32% /unregister "%~dp0ApkShellext2.dll" >nul 2>&1
)
echo        COM Server Unregistered.

echo.
echo [2/4] Completely Removing Registry Keys and Extension Associations...

reg delete "HKCU\Software\ApkShellext" /f >nul 2>&1
reg delete "HKCU\Software\ApkShellext2" /f >nul 2>&1
reg delete "HKLM\Software\ApkShellext" /f >nul 2>&1
reg delete "HKLM\Software\ApkShellext2" /f >nul 2>&1
reg delete "HKLM\Software\WOW6432Node\ApkShellext" /f >nul 2>&1
reg delete "HKLM\Software\WOW6432Node\ApkShellext2" /f >nul 2>&1
reg delete "HKCR\KKHomeProj.ApkShellExt.ApkShellExt" /f >nul 2>&1

set CLSIDS={6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF} {7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B} {8D6C97F3-5E2A-4B6E-9E8D-2C1F5A6D9B4F} {9C5B86A2-4D1F-4A5B-8D7E-3C2B4A5D9E8F} {d5ff6172-1ae5-4c4a-a207-5a2dd100891e} {a0ac4758-12d3-4dcf-9d12-03faaa3c0a9d}

for %%C in (%CLSIDS%) do (
    reg delete "HKCR\CLSID\%%C" /f >nul 2>&1
    reg delete "HKCR\WOW6432Node\CLSID\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\Classes\CLSID\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\Classes\WOW6432Node\CLSID\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved\%%C" /f >nul 2>&1
)

reg delete "HKCR\Applications\ApkShellextService.exe" /f >nul 2>&1
reg delete "HKCR\ApkShellext.Package" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\ApkShellext.Install" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\ApkShellext.Preferences" /f >nul 2>&1

set EXTS=.apk .xapk .apks .apkm .ipa .appx .appxbundle

for %%E in (%EXTS%) do (
    reg delete "HKCR\%%E\shellex" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\ApkShellext" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\ApkShellext.Install" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\ApkShellext.Preferences" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\open" /f >nul 2>&1
    reg delete "HKCR\%%E\OpenWithList\ApkShellextService.exe" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shellex" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\ApkShellext" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\ApkShellext.Install" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\ApkShellext.Preferences" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\open" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\OpenWithList\ApkShellextService.exe" /f >nul 2>&1
    reg delete "HKCR\%%E" /v "Treatment" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E" /v "Treatment" /f >nul 2>&1
)

echo        Registry keys and shell associations removed successfully.

echo.
echo [3/4] Clearing Thumbnail and Icon Caches...
taskkill /F /IM explorer.exe >nul 2>&1
taskkill /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\iconcache_*.db" >nul 2>&1
del /f /q "%localappdata%\IconCache.db" >nul 2>&1

echo        Cache files cleared.

echo.
echo [4/4] Restarting Windows Explorer Shell...
start explorer.exe

echo.
echo ===================================================================
echo    Uninstallation Completed Successfully!
echo ===================================================================
echo.
echo Press Enter to finish...
pause >nul
@ECHO ON
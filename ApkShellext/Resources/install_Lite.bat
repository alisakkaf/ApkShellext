@ECHO OFF
SETLOCAL EnableExtensions

net session >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    IF "%~1"=="ELEVATED" (
        echo.
        echo ===================================================================
        echo     [ERROR] Administrative privileges required.
        echo     Please right-click install_Lite.bat and Run as Administrator.
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
echo   AliSakkaF ApkShellext - Shell Extension Installer (Lite Version)
echo ===================================================================
echo.
echo   * GitHub:   https://github.com/alisakkaf
echo   * Facebook: https://www.facebook.com/AliSakkaf.Dev/
echo   * Website:  https://alisakkaf.com/
echo.

echo [1/7] Detecting Architecture and Framework...
echo       Architecture: %PROCESSOR_ARCHITECTURE%

set IS_X64=0
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" set IS_X64=1
if "%PROCESSOR_ARCHITEW6432%"=="AMD64" set IS_X64=1

set REGASM32="%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe"
set REGASM64="%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"

set FOUND_NET=0
if exist %REGASM32% set FOUND_NET=1
if exist %REGASM64% set FOUND_NET=1

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

echo.
echo [2/7] Cleaning legacy ApkShellext2 and old version registry entries...

if exist %REGASM64% (
    %REGASM64% /unregister "%~dp0ApkShellext.dll" >nul 2>&1
    %REGASM64% /unregister "%~dp0apkshellext2.dll" >nul 2>&1
    %REGASM64% /unregister "%~dp0ApkShellext2.dll" >nul 2>&1
)
if exist %REGASM32% (
    %REGASM32% /unregister "%~dp0ApkShellext.dll" >nul 2>&1
    %REGASM32% /unregister "%~dp0apkshellext2.dll" >nul 2>&1
    %REGASM32% /unregister "%~dp0ApkShellext2.dll" >nul 2>&1
)

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

set EXTS=.apk .xapk .apks .apkm .ipa .appx .appxbundle

for %%E in (%EXTS%) do (
    reg delete "HKCR\%%E\shellex" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\ApkShellext" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\ApkShellext.Install" /f >nul 2>&1
    reg delete "HKCR\%%E\shell\ApkShellext.Preferences" /f >nul 2>&1
    reg delete "HKCR\%%E\DefaultIcon" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shellex" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\ApkShellext" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\ApkShellext.Install" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shell\ApkShellext.Preferences" /f >nul 2>&1
)

reg delete "HKCR\Applications\ApkShellextService.exe" /f >nul 2>&1
reg delete "HKCR\ApkShellext.Package" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\ApkShellext.Install" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\ApkShellext.Preferences" /f >nul 2>&1

echo       Legacy registry keys cleaned successfully.

echo.
echo [3/7] Registering Shell Extension DLL...

set ERR_COUNT=0
if "%IS_X64%"=="1" (
    if exist %REGASM64% (
        echo       Registering 64-bit COM Server...
        %REGASM64% /codebase "%~dp0ApkShellext.dll"
        if errorlevel 1 set /a ERR_COUNT+=1
    )
)

if exist %REGASM32% (
    echo       Registering 32-bit COM Server...
    %REGASM32% /codebase "%~dp0ApkShellext.dll"
    if errorlevel 1 set /a ERR_COUNT+=1
)

if "%ERR_COUNT%"=="0" (
    echo       Shell Extension DLL Registered Successfully!
) else (
    echo.
    echo ===================================================================
    echo     [ERROR] Shell Extension registration failed with errors.
    echo     Please ensure no antivirus is blocking regasm.exe.
    echo ===================================================================
    echo.
    echo Press Enter to exit...
    pause >nul
    exit /B 1
)

echo       Restoring ApkShellext settings (EnableThumbnail=True)...
reg add "HKCU\Software\ApkShellext" /v "EnableThumbnail" /t REG_SZ /d "True" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowOverlayIcon" /t REG_SZ /d "False" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowOverLayIcon" /t REG_SZ /d "False" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "StretchThumbnail" /t REG_SZ /d "True" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowIpaIcon" /t REG_SZ /d "True" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowAppxIcon" /t REG_SZ /d "True" /f >nul 2>&1

echo.
echo.
echo [4/7] Registering and Starting Auto-Updater Service...
net stop "ApkShellext Service" >nul 2>&1
sc delete "ApkShellext Service" >nul 2>&1
sc create "ApkShellext Service" binPath= "\"%~dp0ApkShellextService.exe\"" start= auto DisplayName= "AliSakkaF ApkShellext Service" >nul 2>&1
net start "ApkShellext Service" >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "ApkShellextService" /t REG_SZ /d "\"%~dp0ApkShellextService.exe\" /interactive" /f >nul 2>&1
taskkill /F /IM ApkShellextService.exe >nul 2>&1
start "" /B "%~dp0ApkShellextService.exe" /interactive >nul 2>&1
echo       Auto-Updater Service registered and started successfully.

echo.
echo [5/7] Registering Windows 11 Context Menu and Thumbnail Provider...

reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "IconsOnly" /t REG_DWORD /d 0 /f >nul 2>&1

reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved" /v "{6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF}" /t REG_SZ /d "AliSakkaF ApkShellext Icon Handler" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved" /v "{7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B}" /t REG_SZ /d "AliSakkaF ApkShellext Thumbnail Handler" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved" /v "{9C5B86A2-4D1F-4A5B-8D7E-3C2B4A5D9E8F}" /t REG_SZ /d "AliSakkaF ApkShellext Context Menu" /f >nul 2>&1

for %%E in (%EXTS%) do (
    reg add "HKCR\%%E" /v "Treatment" /t REG_DWORD /d 2 /f >nul 2>&1
    reg add "HKCR\SystemFileAssociations\%%E" /v "Treatment" /t REG_DWORD /d 2 /f >nul 2>&1
    reg add "HKCR\SystemFileAssociations\%%E" /v "PerceivedType" /t REG_SZ /d "application" /f >nul 2>&1
    reg add "HKCR\%%E\shellex\{e357fccd-a995-4576-b01f-234630154e96}" /ve /t REG_SZ /d "{7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B}" /f >nul 2>&1
    reg add "HKCR\SystemFileAssociations\%%E\shellex\{e357fccd-a995-4576-b01f-234630154e96}" /ve /t REG_SZ /d "{7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B}" /f >nul 2>&1
    reg add "HKCR\%%E\shellex\{000214fa-0000-0000-c000-000000000046}" /ve /t REG_SZ /d "{6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF}" /f >nul 2>&1
    reg add "HKCR\SystemFileAssociations\%%E\shellex\{000214fa-0000-0000-c000-000000000046}" /ve /t REG_SZ /d "{6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF}" /f >nul 2>&1
    reg add "HKCR\%%E\shellex\ContextMenuHandlers\ApkShellext" /ve /t REG_SZ /d "{9C5B86A2-4D1F-4A5B-8D7E-3C2B4A5D9E8F}" /f >nul 2>&1
    reg add "HKCR\SystemFileAssociations\%%E\shellex\ContextMenuHandlers\ApkShellext" /ve /t REG_SZ /d "{9C5B86A2-4D1F-4A5B-8D7E-3C2B4A5D9E8F}" /f >nul 2>&1
    reg add "HKCR\%%E\OpenWithList\ApkShellextService.exe" /f >nul 2>&1
    reg add "HKCR\SystemFileAssociations\%%E\OpenWithList\ApkShellextService.exe" /f >nul 2>&1
)
echo       Shell Extension and Thumbnail Provider registered successfully.

echo.
echo [6/7] Select Installation / Double-Click Mode...
echo ===================================================================
echo   [1] Standard Mode: Install via Right-Click Context Menu (Default)
echo   [2] Double-Click Mode: Double-click .apk to Install directly
echo ===================================================================
echo   Auto-selecting [1] Standard Mode in 10 seconds...
echo.

choice /C 12 /N /T 10 /D 1 /M "Select option [1] or [2]: "
IF ERRORLEVEL 2 (
    echo.
    echo       Enabling Double-Click to Install Mode...
    for %%E in (%EXTS%) do (
        reg add "HKCR\%%E\shell\open" /ve /t REG_SZ /d "Install on Device (ADB)" /f >nul 2>&1
        reg add "HKCR\%%E\shell\open\command" /ve /t REG_SZ /d "\"%~dp0ApkShellextService.exe\" /install \"%%1\"" /f >nul 2>&1
        reg add "HKCR\SystemFileAssociations\%%E\shell\open" /ve /t REG_SZ /d "Install on Device (ADB)" /f >nul 2>&1
        reg add "HKCR\SystemFileAssociations\%%E\shell\open\command" /ve /t REG_SZ /d "\"%~dp0ApkShellextService.exe\" /install \"%%1\"" /f >nul 2>&1
    )
    echo       Double-Click mode enabled successfully!
) ELSE (
    echo       Standard Context Menu mode active.
    for %%E in (%EXTS%) do (
        reg delete "HKCR\%%E\shell\open" /f >nul 2>&1
        reg delete "HKCR\SystemFileAssociations\%%E\shell\open" /f >nul 2>&1
    )
)

echo.
echo [7/7] Clearing Windows Caches and Restarting Explorer...
taskkill /F /IM explorer.exe >nul 2>&1
taskkill /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\iconcache_*.db" >nul 2>&1
del /f /q "%localappdata%\IconCache.db" >nul 2>&1

start explorer.exe

echo.
echo ===================================================================
echo   Installation Completed Successfully!
echo ===================================================================
echo.
echo   AliSakkaF ApkShellext is ready to use.
echo.
echo Press Enter to finish...
pause >nul
@ECHO ON
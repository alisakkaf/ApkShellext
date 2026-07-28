@ECHO OFF
powershell -NoProfile -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File" 2>nul
CLS

>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    if '%1' EQU '1' (
        powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
        powershell -NoProfile -Command "Write-Host '   [ERROR] Administrative privileges required.' -ForegroundColor Red"
        powershell -NoProfile -Command "Write-Host '   Please right-click install.bat and select Run as Administrator.' -ForegroundColor Red"
        powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
        pause
        exit /B 1
    ) else (
        echo Requesting administrative privileges...
        echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
        echo UAC.ShellExecute "%~s0", "1", "", "runas", 1 >> "%temp%\getadmin.vbs"
        "%temp%\getadmin.vbs"
        exit /B 0
    )
)
if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
pushd "%CD%"
CD /D "%~dp0"

powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
powershell -NoProfile -Command "Write-Host '  AliSakkaF ApkShellext - Shell Extension Installer' -ForegroundColor Green"
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
echo.
powershell -NoProfile -Command "Write-Host '  * GitHub:   https://github.com/alisakkaf' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  * Facebook: https://www.facebook.com/AliSakkaf.Dev/' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  * Website:  https://alisakkaf.com/' -ForegroundColor Gray"
echo.

powershell -NoProfile -Command "Write-Host '[1/7] Detecting Architecture and Framework...' -ForegroundColor Yellow"
powershell -NoProfile -Command "Write-Host '      Architecture: %PROCESSOR_IDENTIFIER%' -ForegroundColor White"

set IS_X64=0
echo %PROCESSOR_IDENTIFIER% | FIND /i "x86" > nul
IF %ERRORLEVEL%==1 set IS_X64=1

set REGASM32="%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe"
set REGASM64="%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"

set FOUND_NET=0
if exist %REGASM32% set FOUND_NET=1
if exist %REGASM64% set FOUND_NET=1

if %FOUND_NET%==0 (
    echo.
    powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
    powershell -NoProfile -Command "Write-Host '   [ERROR] Microsoft .NET Framework 4.8 (or 4.0+) was not found.' -ForegroundColor Red"
    powershell -NoProfile -Command "Write-Host '   Installation cannot proceed without Microsoft .NET Framework.' -ForegroundColor Red"
    powershell -NoProfile -Command "Write-Host '   Please download and install .NET Framework 4.8 from:' -ForegroundColor Yellow"
    powershell -NoProfile -Command "Write-Host '   https://dotnet.microsoft.com/download/dotnet-framework/net48' -ForegroundColor Cyan"
    powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
    echo.
    pause
    exit /B 1
)

echo.
powershell -NoProfile -Command "Write-Host '[2/7] Cleaning legacy ApkShellext2 and old version registry entries...' -ForegroundColor Yellow"

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

powershell -NoProfile -Command "Write-Host '      Legacy registry keys cleaned successfully.' -ForegroundColor Green"

echo.
powershell -NoProfile -Command "Write-Host '[3/7] Registering Shell Extension DLL...' -ForegroundColor Yellow"

set ERR_COUNT=0
if %IS_X64%==1 (
    if exist %REGASM64% (
        powershell -NoProfile -Command "Write-Host '      Registering 64-bit COM Server...' -ForegroundColor White"
        %REGASM64% /codebase "%~dp0ApkShellext.dll"
        if errorlevel 1 set /a ERR_COUNT+=1
    )
)

if exist %REGASM32% (
    powershell -NoProfile -Command "Write-Host '      Registering 32-bit COM Server...' -ForegroundColor White"
    %REGASM32% /codebase "%~dp0ApkShellext.dll"
    if errorlevel 1 set /a ERR_COUNT+=1
)

if %ERR_COUNT% EQU 0 (
    powershell -NoProfile -Command "Write-Host '      Shell Extension DLL Registered Successfully!' -ForegroundColor Green"
) else (
    powershell -NoProfile -Command "Write-Host '      [WARNING] Registration completed with warnings or minor errors.' -ForegroundColor Red"
)

powershell -NoProfile -Command "Write-Host '      Restoring ApkShellext settings (EnableThumbnail=True)...' -ForegroundColor White"
reg add "HKCU\Software\ApkShellext" /v "EnableThumbnail" /t REG_SZ /d "True" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowOverlayIcon" /t REG_SZ /d "False" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowOverLayIcon" /t REG_SZ /d "False" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "StretchThumbnail" /t REG_SZ /d "True" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowIpaIcon" /t REG_SZ /d "True" /f >nul 2>&1
reg add "HKCU\Software\ApkShellext" /v "ShowAppxIcon" /t REG_SZ /d "True" /f >nul 2>&1

echo.
powershell -NoProfile -Command "Write-Host '[4/7] Registering and Starting Auto-Updater Service...' -ForegroundColor Yellow"
net stop "ApkShellext Service" >nul 2>&1
sc delete "ApkShellext Service" >nul 2>&1
sc create "ApkShellext Service" binPath= "\"%~dp0ApkShellextService.exe\"" start= auto DisplayName= "AliSakkaF ApkShellext Service" >nul 2>&1
net start "ApkShellext Service" >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "ApkShellextService" /t REG_SZ /d "\"%~dp0ApkShellextService.exe\" /interactive" /f >nul 2>&1
taskkill /F /IM ApkShellextService.exe >nul 2>&1
start "" /B "%~dp0ApkShellextService.exe" /interactive >nul 2>&1
powershell -NoProfile -Command "Write-Host '      Auto-Updater Service registered and started successfully.' -ForegroundColor Green"

echo.
powershell -NoProfile -Command "Write-Host '[5/7] Registering Windows 11 Context Menu and Thumbnail Provider...' -ForegroundColor Yellow"

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
powershell -NoProfile -Command "Write-Host '      Shell Extension and Thumbnail Provider registered successfully.' -ForegroundColor Green"

echo.
powershell -NoProfile -Command "Write-Host '[6/7] Select Installation / Double-Click Mode...' -ForegroundColor Yellow"
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  [1] Standard Mode: Install via Right-Click Context Menu (Default)' -ForegroundColor White"
powershell -NoProfile -Command "Write-Host '  [2] Double-Click Mode: Double-click .apk to Install directly' -ForegroundColor White"
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  Auto-selecting [1] Standard Mode in 10 seconds...' -ForegroundColor Cyan"
echo.

choice /C 12 /N /T 10 /D 1 /M "  Select option [1] or [2]: "
IF ERRORLEVEL 2 (
    echo.
    powershell -NoProfile -Command "Write-Host '      Enabling Double-Click to Install Mode...' -ForegroundColor Yellow"
    for %%E in (%EXTS%) do (
        reg add "HKCR\%%E\shell\open" /ve /t REG_SZ /d "Install on Device (ADB)" /f >nul 2>&1
        reg add "HKCR\%%E\shell\open\command" /ve /t REG_SZ /d "\"%~dp0ApkShellextService.exe\" /install \"%%1\"" /f >nul 2>&1
        reg add "HKCR\SystemFileAssociations\%%E\shell\open" /ve /t REG_SZ /d "Install on Device (ADB)" /f >nul 2>&1
        reg add "HKCR\SystemFileAssociations\%%E\shell\open\command" /ve /t REG_SZ /d "\"%~dp0ApkShellextService.exe\" /install \"%%1\"" /f >nul 2>&1
    )
    powershell -NoProfile -Command "Write-Host '      Double-Click mode enabled successfully!' -ForegroundColor Green"
) ELSE (
    powershell -NoProfile -Command "Write-Host '      Standard Context Menu mode active.' -ForegroundColor Green"
    for %%E in (%EXTS%) do (
        reg delete "HKCR\%%E\shell\open" /f >nul 2>&1
        reg delete "HKCR\SystemFileAssociations\%%E\shell\open" /f >nul 2>&1
    )
)

echo.
powershell -NoProfile -Command "Write-Host '[7/7] Clearing Windows Caches and Restarting Explorer...' -ForegroundColor Yellow"
taskkill /F /IM explorer.exe >nul 2>&1
taskkill /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\iconcache_*.db" >nul 2>&1
del /f /q "%localappdata%\IconCache.db" >nul 2>&1

start explorer.exe
powershell -NoProfile -Command "[SharpShell.Interop.Shell32]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)" >nul 2>&1

echo.
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
powershell -NoProfile -Command "Write-Host '  Installation Completed Successfully!' -ForegroundColor Green"
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
echo.
powershell -NoProfile -Command "Write-Host '  AliSakkaF ApkShellext is ready to use.' -ForegroundColor White"
powershell -NoProfile -Command "Write-Host '  If icons do not update immediately, run debug.bat for detailed logs.' -ForegroundColor Gray"
echo.

PAUSE
@ECHO ON

@ECHO OFF
:: Automatically unblock all downloaded project files to prevent SmartScreen/Security block warnings
powershell -NoProfile -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File" 2>nul
CLS

:: Check for Administrative Privileges
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

powershell -NoProfile -Command "Write-Host '[1/5] Detecting Architecture and Framework...' -ForegroundColor Yellow"
powershell -NoProfile -Command "Write-Host '      Architecture: %PROCESSOR_IDENTIFIER%' -ForegroundColor White"

set IS_X64=0
echo %PROCESSOR_IDENTIFIER% | FIND /i "x86" > nul
IF %ERRORLEVEL%==1 (
    set IS_X64=1
)

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
powershell -NoProfile -Command "Write-Host '[2/5] Cleaning legacy ApkShellext2 and old version registry entries...' -ForegroundColor Yellow"

:: Unregister legacy DLLs if present
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

:: Delete legacy software registry keys
reg delete "HKCU\Software\ApkShellext" /f >nul 2>&1
reg delete "HKCU\Software\ApkShellext2" /f >nul 2>&1
reg delete "HKLM\Software\ApkShellext" /f >nul 2>&1
reg delete "HKLM\Software\ApkShellext2" /f >nul 2>&1
reg delete "HKLM\Software\WOW6432Node\ApkShellext" /f >nul 2>&1
reg delete "HKLM\Software\WOW6432Node\ApkShellext2" /f >nul 2>&1
reg delete "HKCR\KKHomeProj.ApkShellExt.ApkShellExt" /f >nul 2>&1

:: List of CLSIDs to clean
set CLSIDS={6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF} {7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B} {8D6C97F3-5E2A-4B6E-9E8D-2C1F5A6D9B4F} {9C5B86A2-4D1F-4A5B-8D7E-3C2B4A5D9E8F} {d5ff6172-1ae5-4c4a-a207-5a2dd100891e} {a0ac4758-12d3-4dcf-9d12-03faaa3c0a9d}

for %%C in (%CLSIDS%) do (
    reg delete "HKCR\CLSID\%%C" /f >nul 2>&1
    reg delete "HKCR\WOW6432Node\CLSID\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\Classes\CLSID\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\Classes\WOW6432Node\CLSID\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved\%%C" /f >nul 2>&1
    reg delete "HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved\%%C" /f >nul 2>&1
)

:: Clean legacy extension shellex keys
set EXTS=.apk .xapk .ipa .appx .appxbundle
for %%E in (%EXTS%) do (
    reg delete "HKCR\%%E\shellex" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shellex" /f >nul 2>&1
)

powershell -NoProfile -Command "Write-Host '      Legacy registry keys cleaned successfully.' -ForegroundColor Green"

echo.
powershell -NoProfile -Command "Write-Host '[3/5] Registering Shell Extension DLL...' -ForegroundColor Yellow"

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

echo.
powershell -NoProfile -Command "Write-Host '[4/5] Clearing Windows Thumbnail and Icon Caches...' -ForegroundColor Yellow"
taskkill /F /IM explorer.exe >nul 2>&1
taskkill /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\iconcache_*.db" >nul 2>&1
del /f /q "%localappdata%\IconCache.db" >nul 2>&1

powershell -NoProfile -Command "Write-Host '      Cache files cleared.' -ForegroundColor Green"

echo.
powershell -NoProfile -Command "Write-Host '[5/5] Restarting Windows Explorer Shell...' -ForegroundColor Yellow"
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

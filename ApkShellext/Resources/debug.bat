@ECHO OFF
:: Automatically unblock all downloaded project files to prevent SmartScreen/Security block warnings
powershell -NoProfile -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File" 2>nul
CLS

set LOGFILE=%~dp0ApkShellext_Debug_Log.txt
echo =================================================================== > "%LOGFILE%"
echo   AliSakkaF ApkShellext - Debug & Diagnostics Log >> "%LOGFILE%"
echo   Generated at: %DATE% %TIME% >> "%LOGFILE%"
echo =================================================================== >> "%LOGFILE%"
echo. >> "%LOGFILE%"

:: Check for Administrative Privileges
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    if '%1' EQU '1' (
        powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
        powershell -NoProfile -Command "Write-Host '   [ERROR] Administrative privileges required.' -ForegroundColor Red"
        powershell -NoProfile -Command "Write-Host '   Please right-click debug.bat and select Run as Administrator.' -ForegroundColor Red"
        powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
        echo [ERROR] Administrative privileges required. >> "%LOGFILE%"
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
powershell -NoProfile -Command "Write-Host '  AliSakkaF ApkShellext - Shell Extension Diagnostic & Repair' -ForegroundColor Green"
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
echo.
powershell -NoProfile -Command "Write-Host '  * GitHub:   https://github.com/alisakkaf' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  * Facebook: https://www.facebook.com/AliSakkaf.Dev/' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  * Website:  https://alisakkaf.com/' -ForegroundColor Gray"
echo.

powershell -NoProfile -Command "Write-Host '[1/6] Diagnostic Check: System & Environment Information...' -ForegroundColor Yellow"
powershell -NoProfile -Command "Write-Host '      Directory: %~dp0' -ForegroundColor White"
powershell -NoProfile -Command "Write-Host '      Architecture: %PROCESSOR_IDENTIFIER%' -ForegroundColor White"

echo Directory: %~dp0 >> "%LOGFILE%"
echo Architecture: %PROCESSOR_IDENTIFIER% >> "%LOGFILE%"

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
    powershell -NoProfile -Command "Write-Host '   Please download and install Microsoft .NET Framework 4.8 from:' -ForegroundColor Yellow"
    powershell -NoProfile -Command "Write-Host '   https://dotnet.microsoft.com/download/dotnet-framework/net48' -ForegroundColor Cyan"
    powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Red"
    echo [ERROR] .NET Framework 4.8 not found >> "%LOGFILE%"
    echo.
    pause
    exit /B 1
)

:: Check DLL Files
if exist "%~dp0ApkShellext.dll" (
    powershell -NoProfile -Command "Write-Host '      [OK] Found ApkShellext.dll' -ForegroundColor Green"
    echo [OK] Found ApkShellext.dll >> "%LOGFILE%"
) else (
    powershell -NoProfile -Command "Write-Host '      [FAIL] Missing ApkShellext.dll' -ForegroundColor Red"
    echo [FAIL] Missing ApkShellext.dll >> "%LOGFILE%"
)

if exist "%~dp0libwebp_x64.dll" (
    powershell -NoProfile -Command "Write-Host '      [OK] Found libwebp_x64.dll' -ForegroundColor Green"
    echo [OK] Found libwebp_x64.dll >> "%LOGFILE%"
) else (
    powershell -NoProfile -Command "Write-Host '      [WARN] Missing libwebp_x64.dll' -ForegroundColor Yellow"
    echo [WARN] Missing libwebp_x64.dll >> "%LOGFILE%"
)

if exist "%~dp0Google.Protobuf.dll" (
    powershell -NoProfile -Command "Write-Host '      [OK] Found Google.Protobuf.dll' -ForegroundColor Green"
    echo [OK] Found Google.Protobuf.dll >> "%LOGFILE%"
) else (
    powershell -NoProfile -Command "Write-Host '      [WARN] Missing Google.Protobuf.dll' -ForegroundColor Yellow"
    echo [WARN] Missing Google.Protobuf.dll >> "%LOGFILE%"
)

echo.
powershell -NoProfile -Command "Write-Host '[2/6] Cleaning Legacy & Conflict Registrations...' -ForegroundColor Yellow"

:: Unregister legacy DLLs if present
if exist %REGASM64% (
    %REGASM64% /unregister "%~dp0ApkShellext.dll" >> "%LOGFILE%" 2>&1
    %REGASM64% /unregister "%~dp0apkshellext2.dll" >> "%LOGFILE%" 2>&1
    %REGASM64% /unregister "%~dp0ApkShellext2.dll" >> "%LOGFILE%" 2>&1
)
if exist %REGASM32% (
    %REGASM32% /unregister "%~dp0ApkShellext.dll" >> "%LOGFILE%" 2>&1
    %REGASM32% /unregister "%~dp0apkshellext2.dll" >> "%LOGFILE%" 2>&1
    %REGASM32% /unregister "%~dp0ApkShellext2.dll" >> "%LOGFILE%" 2>&1
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
set EXTS=.apk .xapk .apks .apkm .ipa .appx .appxbundle
for %%E in (%EXTS%) do (
    reg delete "HKCR\%%E\shellex" /f >nul 2>&1
    reg delete "HKCR\SystemFileAssociations\%%E\shellex" /f >nul 2>&1
)

powershell -NoProfile -Command "Write-Host '      Old registrations cleaned.' -ForegroundColor Green"
echo Legacy registrations cleaned >> "%LOGFILE%"

echo.
powershell -NoProfile -Command "Write-Host '[3/6] Registering Assemblies & Testing RegAsm Output...' -ForegroundColor Yellow"

set REG_SUCCESS=1
if %IS_X64%==1 (
    if exist %REGASM64% (
        powershell -NoProfile -Command "Write-Host '      Executing 64-bit RegAsm...' -ForegroundColor White"
        echo --- 64-bit RegAsm Output --- >> "%LOGFILE%"
        %REGASM64% /codebase "%~dp0ApkShellext.dll" >> "%LOGFILE%" 2>&1
        if errorlevel 1 (
            powershell -NoProfile -Command "Write-Host '      [FAIL] 64-bit RegAsm returned error code.' -ForegroundColor Red"
            set REG_SUCCESS=0
        ) else (
            powershell -NoProfile -Command "Write-Host '      [OK] 64-bit RegAsm executed successfully.' -ForegroundColor Green"
        )
    )
)

if exist %REGASM32% (
    powershell -NoProfile -Command "Write-Host '      Executing 32-bit RegAsm...' -ForegroundColor White"
    echo --- 32-bit RegAsm Output --- >> "%LOGFILE%"
    %REGASM32% /codebase "%~dp0ApkShellext.dll" >> "%LOGFILE%" 2>&1
    if errorlevel 1 (
        powershell -NoProfile -Command "Write-Host '      [FAIL] 32-bit RegAsm returned error code.' -ForegroundColor Red"
        set REG_SUCCESS=0
    ) else (
        powershell -NoProfile -Command "Write-Host '      [OK] 32-bit RegAsm executed successfully.' -ForegroundColor Green"
    )
)

echo.
powershell -NoProfile -Command "Write-Host '[4/6] Verifying Registry Key Registration...' -ForegroundColor Yellow"
reg query "HKCR\CLSID\{6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF}" >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Write-Host '      [FAIL] IconHandler CLSID is NOT registered in HKCR\CLSID!' -ForegroundColor Red"
    echo [FAIL] IconHandler CLSID not found in HKCR\CLSID >> "%LOGFILE%"
) else (
    powershell -NoProfile -Command "Write-Host '      [OK] IconHandler CLSID is verified in HKCR\CLSID.' -ForegroundColor Green"
    echo [OK] IconHandler CLSID verified in HKCR\CLSID >> "%LOGFILE%"
)

reg query "HKCR\CLSID\{7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B}" >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Write-Host '      [FAIL] ThumbnailHandler CLSID is NOT registered in HKCR\CLSID!' -ForegroundColor Red"
    echo [FAIL] ThumbnailHandler CLSID not found in HKCR\CLSID >> "%LOGFILE%"
) else (
    powershell -NoProfile -Command "Write-Host '      [OK] ThumbnailHandler CLSID is verified in HKCR\CLSID.' -ForegroundColor Green"
    echo [OK] ThumbnailHandler CLSID verified in HKCR\CLSID >> "%LOGFILE%"
)

echo.
powershell -NoProfile -Command "Write-Host '[5/6] Flushing Explorer & Thumbnail Database Caches...' -ForegroundColor Yellow"
taskkill /F /IM explorer.exe >nul 2>&1
taskkill /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
del /f /s /q "%localappdata%\Microsoft\Windows\Explorer\iconcache_*.db" >nul 2>&1
del /f /q "%localappdata%\IconCache.db" >nul 2>&1

powershell -NoProfile -Command "Write-Host '      Cache files cleared.' -ForegroundColor Green"

echo.
powershell -NoProfile -Command "Write-Host '[6/6] Restarting Windows Explorer Shell...' -ForegroundColor Yellow"
start explorer.exe
powershell -NoProfile -Command "[SharpShell.Interop.Shell32]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)" >nul 2>&1

echo.
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
if %REG_SUCCESS%==1 (
    powershell -NoProfile -Command "Write-Host '  Debug & Repair Completed Successfully!' -ForegroundColor Green"
) else (
    powershell -NoProfile -Command "Write-Host '  Debug Completed with Errors (Check Log File).' -ForegroundColor Red"
)
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
echo.
powershell -NoProfile -Command "Write-Host '  A detailed diagnostic log has been saved to:' -ForegroundColor White"
powershell -NoProfile -Command "Write-Host '  %LOGFILE%' -ForegroundColor Yellow"
echo.
powershell -NoProfile -Command "Write-Host '  If icons still fail to show, please copy the content of the log file' -ForegroundColor Gray"
powershell -NoProfile -Command "Write-Host '  and send it to the developer (https://github.com/alisakkaf).' -ForegroundColor Gray"
echo.

PAUSE
@ECHO ON

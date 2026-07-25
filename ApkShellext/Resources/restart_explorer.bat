@ECHO OFF
CLS
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
powershell -NoProfile -Command "Write-Host '  AliSakkaF ApkShellext - Restarting Windows Explorer Shell' -ForegroundColor Green"
powershell -NoProfile -Command "Write-Host '===================================================================' -ForegroundColor Cyan"
echo.

powershell -NoProfile -Command "Write-Host '  Terminating explorer.exe and dllhost.exe...' -ForegroundColor Yellow"
TASKKILL /F /IM explorer.exe >nul 2>&1
TASKKILL /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

powershell -NoProfile -Command "Write-Host '  Starting fresh Windows Explorer process...' -ForegroundColor Yellow"
START %WINDIR%\explorer.exe
powershell -NoProfile -Command "[SharpShell.Interop.Shell32]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)" >nul 2>&1

echo.
powershell -NoProfile -Command "Write-Host '  Windows Explorer restarted successfully!' -ForegroundColor Green"
echo.
PAUSE
@ECHO ON
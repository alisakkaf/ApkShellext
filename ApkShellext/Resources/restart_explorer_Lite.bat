@ECHO OFF
CLS
echo ===================================================================
echo   AliSakkaF ApkShellext - Restarting Windows Explorer Shell (Lite)
echo ===================================================================
echo.

echo   Terminating explorer.exe and dllhost.exe...
TASKKILL /F /IM explorer.exe >nul 2>&1
TASKKILL /F /IM dllhost.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

echo   Starting fresh Windows Explorer process...
START %WINDIR%\explorer.exe

echo.
echo   Windows Explorer restarted successfully!
echo.
echo Press Enter to finish.
pause
@ECHO ON

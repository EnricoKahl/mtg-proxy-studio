@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo Das .NET 8 SDK wurde nicht gefunden.
  echo.
  echo Fuehre zuerst "DOTNET-SDK-installieren.bat" aus oder installiere
  echo das .NET 8 SDK von Microsoft.
  pause
  exit /b 1
)

echo Stelle NuGet-Pakete wieder her und erstelle die portable EXE...
dotnet publish "%~dp0MTGProxyStudio\MTGProxyStudio.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%~dp0Fertige_EXE"
if errorlevel 1 (
  echo.
  echo Der Build ist fehlgeschlagen. Bitte die Fehlermeldung oben kopieren.
  pause
  exit /b 1
)

echo.
echo Fertig: %~dp0Fertige_EXE\MTGProxyStudio.exe
start "" "%~dp0Fertige_EXE"
pause

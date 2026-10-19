@echo off
setlocal
where winget >nul 2>nul
if errorlevel 1 (
  echo winget wurde nicht gefunden.
  echo Bitte installiere das .NET 8 SDK manuell von Microsoft.
  pause
  exit /b 1
)

echo Installiere das Microsoft .NET 8 SDK...
winget install --id Microsoft.DotNet.SDK.8 --exact --accept-package-agreements --accept-source-agreements
if errorlevel 1 (
  echo Installation fehlgeschlagen.
  pause
  exit /b 1
)

echo.
echo Installation abgeschlossen. Schliesse dieses Fenster und starte danach EXE-erstellen.bat.
pause

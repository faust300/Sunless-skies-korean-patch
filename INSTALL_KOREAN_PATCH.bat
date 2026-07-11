@echo off
chcp 65001 > nul
setlocal

echo Installing Sunless Skies Korean Patch...
echo Please close the game before continuing.
echo.

if exist "%~dp0SunlessSkiesKoreanInstaller.exe" (
  "%~dp0SunlessSkiesKoreanInstaller.exe"
) else (
  echo SunlessSkiesKoreanInstaller.exe was not found.
  echo Running the legacy PowerShell installer.
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
)

echo.
pause

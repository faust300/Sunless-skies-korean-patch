@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Update-KoreanPatch.ps1" %*
exit /b %errorlevel%

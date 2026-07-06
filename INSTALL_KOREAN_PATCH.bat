@echo off
chcp 65001 > nul
setlocal

echo Sunless Skies 한국어 패치를 설치합니다.
echo 게임이 실행 중이라면 먼저 종료해 주세요.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"

echo.
pause

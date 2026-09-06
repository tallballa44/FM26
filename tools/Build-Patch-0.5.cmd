@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Patch-0.5.ps1"
echo.
pause

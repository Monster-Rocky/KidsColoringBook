@echo off
setlocal
echo ========================================================
echo   Installing Kids Coloring Book Desktop Application...
echo ========================================================
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
echo.
pause

@echo off
REM Double-click entry point for Launch-Live.ps1 — no PowerShell execution-policy
REM prompt, no manual "Right click > Run with PowerShell" needed.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launch-Live.ps1"
pause

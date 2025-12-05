@echo off
REM CodeMedic Health Analysis Script (Batch)
REM Runs CodeMedic against the repository with the health parameter
REM Works on: Windows Command Prompt (cmd.exe)

setlocal enabledelayedexpansion

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0

REM Change to the repository root directory
cd /d "%SCRIPT_DIR%"

echo Running CodeMedic health analysis...
echo Repository: %SCRIPT_DIR%
echo.

dotnet run --project .\src\CodeMedic\CodeMedic.csproj -- health

endlocal


@echo off
REM Run CodeMedic with a configuration file

if "%1"=="" (
    set CONFIG_FILE=sample-config.yaml
) else (
    set CONFIG_FILE=%1
)

if not exist "%CONFIG_FILE%" (
    echo Configuration file not found: %CONFIG_FILE%
    exit /b 1
)

echo Running CodeMedic with configuration: %CONFIG_FILE%

dotnet run --project src\CodeMedic config %CONFIG_FILE%

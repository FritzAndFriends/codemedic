#!/usr/bin/env pwsh
# Run CodeMedic with a configuration file

param(
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = "sample-config.yaml"
)

if (-not (Test-Path $ConfigFile)) {
    Write-Host "Configuration file not found: $ConfigFile" -ForegroundColor Red
    exit 1
}

Write-Host "Running CodeMedic with configuration: $ConfigFile" -ForegroundColor Cyan

dotnet run --project src\CodeMedic config $ConfigFile

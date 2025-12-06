#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs CodeMedic vulnerability scan on the current repository
#>

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = $scriptDir

Write-Host "Running CodeMedic vulnerability scan..." -ForegroundColor Cyan
Write-Host "Repository: $projectRoot" -ForegroundColor Gray

& dotnet "$projectRoot\src\CodeMedic\bin\Release\net10.0\CodeMedic.dll" vulnerabilities @args

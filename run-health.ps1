# CodeMedic Health Analysis Script (PowerShell)
# Runs CodeMedic against the repository with the health parameter
# Works on: Windows PowerShell, PowerShell Core (cross-platform)

# Get the directory where this script is located
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path

# Change to the repository root directory
Push-Location $SCRIPT_DIR

try {
    Write-Host "Running CodeMedic health analysis..."
    Write-Host "Repository: $SCRIPT_DIR"
    Write-Host ""
    
    & dotnet run --project .\src\CodeMedic\CodeMedic.csproj -- health
}
finally {
    Pop-Location
}

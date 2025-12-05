#!/bin/bash
# CodeMedic Health Analysis Script (Bash)
# Runs CodeMedic against the repository with the health parameter
# Works on: Linux, macOS, WSL

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Running CodeMedic health analysis..."
echo "Repository: $SCRIPT_DIR"
echo ""

dotnet run --project ./src/CodeMedic/CodeMedic.csproj -- health


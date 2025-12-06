#!/bin/bash
# Runs CodeMedic vulnerability scan on the current repository

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "Running CodeMedic vulnerability scan..."
echo "Repository: $PROJECT_ROOT"

dotnet "$PROJECT_ROOT/src/CodeMedic/bin/Release/net10.0/CodeMedic.dll" vulnerabilities "$@"

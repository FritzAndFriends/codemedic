#!/usr/bin/env bash
# Run CodeMedic with a configuration file

CONFIG_FILE="${1:-sample-config.yaml}"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Configuration file not found: $CONFIG_FILE"
    exit 1
fi

echo "Running CodeMedic with configuration: $CONFIG_FILE"

dotnet run --project src/CodeMedic config "$CONFIG_FILE"

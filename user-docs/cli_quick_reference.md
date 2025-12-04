# CodeMedic CLI Quick Reference

## Commands

| Command | Aliases | Purpose |
|---------|---------|---------|
| `help` | `-h`, `--help` | Display help and available commands |
| `version` | `-v`, `--version` | Display application version |
| `health` | - | Display repository health dashboard *(coming soon)* |
| `bom` | - | Generate bill of materials report *(coming soon)* |

## Basic Usage

```bash
# Show help (any of these work)
codemedic
codemedic help
codemedic --help
codemedic -h

# Show version (any of these work)
codemedic version
codemedic --version
codemedic -v

# Repository health analysis (coming soon)
codemedic health
codemedic health --format json

# Bill of materials (coming soon)
codemedic bom
codemedic bom --format json
codemedic bom --format markdown
```

## Installation & Running

### Prerequisites
- .NET 10.0 runtime or SDK
- Windows, macOS, or Linux

### Build from Source
```bash
cd src/CodeMedic
dotnet build -c Release
```

### Run
```bash
# Via dotnet
dotnet run -- --help

# Direct binary (after build)
./bin/Debug/net10.0/CodeMedic.exe --help
```

## Output Examples

### Help Screen
- Rich formatted table showing all available commands
- Usage examples
- Quick reference for common tasks

### Version Info
- Application name and version
- Description of the tool

## Exit Codes
- `0` - Success
- `1` - Error (e.g., unknown command)

## Cross-Platform Support
All output is automatically formatted for:
- Windows (cmd.exe, PowerShell)
- macOS (Terminal, iTerm2)
- Linux (bash, zsh, etc.)

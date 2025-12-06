# CodeMedic CLI Quick Reference

## Commands

| Command | Purpose |
|---------|---------|
| `help` | Display help and available commands |
| `version` | Display application version |
| `health` | Display repository health dashboard |
| `bom` | Generate bill of materials report |
| `vulnerabilities` | Scan for known vulnerabilities in NuGet packages |

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

# Repository health analysis
codemedic health
codemedic health --format markdown > report.md

# Bill of materials
codemedic bom
codemedic bom --format json
codemedic bom --format markdown

# Vulnerability scanning
codemedic vulnerabilities
codemedic vulnerabilities --format markdown > vulnerabilities-report.md
codemedic vulnerabilities /path/to/repo
```

## Output Formats

All commands support `--format` option:
- `console` (default) - Rich formatted output for terminal
- `markdown` or `md` - Markdown format suitable for reports and documentation

```bash
# Console output (default)
codemedic health

# Markdown output
codemedic health --format markdown > report.md
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

### Run via Scripts

**Windows (PowerShell):**
```powershell
.\run-health.ps1
.\run-vulnerabilities.ps1
```

**macOS/Linux (Bash):**
```bash
./run-health.sh
./run-vulnerabilities.sh
```

### Run via dotnet

```bash
# Health dashboard
dotnet ./src/CodeMedic/bin/Release/net10.0/CodeMedic.dll health

# Vulnerability scan
dotnet ./src/CodeMedic/bin/Release/net10.0/CodeMedic.dll vulnerabilities

# Bill of materials
dotnet ./src/CodeMedic/bin/Release/net10.0/CodeMedic.dll bom
```

## Exit Codes
- `0` - Success
- `1` - Error (e.g., unknown command, scan failure)

## Cross-Platform Support
All output is automatically formatted for:
- Windows (cmd.exe, PowerShell)
- macOS (Terminal, iTerm2)
- Linux (bash, zsh, etc.)

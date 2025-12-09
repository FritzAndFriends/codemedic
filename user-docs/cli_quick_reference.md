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
codemedic health -p /path/to/repo
codemedic health --path /path/to/repo --format markdown > report.md

# Bill of materials
codemedic bom
codemedic bom -p /path/to/repo
codemedic bom --path /path/to/repo --format markdown

# Vulnerability scanning
codemedic vulnerabilities
codemedic vulnerabilities -p /path/to/repo
codemedic vulnerabilities --path /path/to/repo --format markdown > vulnerabilities-report.md
```

## Command Options

### Path Argument
All analysis commands support a path argument to specify which repository to analyze:

- `-p <path>` or `--path <path>` - Specify the path to the repository
- If not provided, uses the current directory

```bash
# Analyze current directory
codemedic health

# Analyze specific directory
codemedic health -p /path/to/repo
codemedic health --path /path/to/repo

# Relative paths work too
codemedic bom -p ../other-project
codemedic vulnerabilities --path .
```

### Output Formats

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

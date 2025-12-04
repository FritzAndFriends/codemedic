# CodeMedic CLI Skeleton - Initial Implementation

## Overview

The initial CLI skeleton for CodeMedic has been implemented with core infrastructure for handling commands, options, and formatted output. The system provides:

- **Help command** (`--help`, `-h`, `help`) - displays available commands and usage examples
- **Version command** (`--version`, `-v`, `version`) - displays application version
- **Default behavior** - running without arguments shows the help menu
- **Rich console output** - uses Spectre.Console for formatted tables, panels, and styled text

## Project Structure

```
CodeMedic/
├── Commands/
│   └── RootCommandHandler.cs      # Main CLI command router
├── Output/
│   └── ConsoleRenderer.cs         # Spectre.Console formatting utilities
├── Utilities/
│   └── VersionUtility.cs          # Version information retrieval
├── Program.cs                      # Application entry point
└── CodeMedic.csproj               # Project configuration
```

## Technology Stack

- **System.CommandLine 2.0.0** - Command-line parsing and routing
- **Spectre.Console 0.49.1** - Cross-platform rich terminal output
- **Nerdbank.GitVersioning 3.9.50** - Automatic versioning from git

## Components

### 1. RootCommandHandler

**File:** `Commands/RootCommandHandler.cs`

Handles argument parsing and routes to appropriate handlers:

- `ProcessArguments(string[] args)` - Main entry point that processes CLI arguments
  - Returns 0 on success, 1 on error
  - Handles: `--help`, `-h`, `help`, `--version`, `-v`, `version`
  - Shows error and help for unrecognized commands

### 2. ConsoleRenderer

**File:** `Output/ConsoleRenderer.cs`

Provides rich output formatting using Spectre.Console:

- `RenderBanner(string version)` - Displays the welcome banner with version
- `RenderHelp()` - Shows available commands in a formatted table
- `RenderVersion(string version)` - Displays version in a styled panel
- `RenderError(string message)` - Shows error messages with red formatting
- `RenderSuccess(string message)` - Shows success messages with green formatting
- `RenderInfo(string message)` - Shows info messages with blue formatting

### 3. VersionUtility

**File:** `Utilities/VersionUtility.cs`

Handles version information retrieval:

- `GetVersion()` - Retrieves the application version (injected by NBGv2)
- `GetApplicationInfo()` - Returns formatted version string

## Usage Examples

```bash
# Display help
codemedic
codemedic --help
codemedic -h
codemedic help

# Display version
codemedic --version
codemedic -v
codemedic version

# Future commands (not yet implemented)
codemedic health
codemedic bom
codemedic bom --format json
```

## Output Examples

### Help Display

```
───────────────────────────────────────────────── CodeMedic ───────────────────────────────────────────────
v0.1.0.0 - .NET Repository Health Analysis Tool

                        Available Commands
┌──────────────────────────┬─────────────────────────────────────┐
│ Command                  │ Description                         │
├──────────────────────────┼─────────────────────────────────────┤
│ health                   │ Display repository health dashboard │
│ bom                      │ Generate bill of materials report   │
│ version or -v, --version │ Display application version         │
│ help or -h, --help       │ Display this help message           │
└──────────────────────────┴─────────────────────────────────────┘

Usage:
  codemedic <command> [options]
  codemedic --help
  codemedic --version

Examples:
  codemedic health
  codemedic bom --format json
  codemedic --version
```

### Version Display

```
┌────────────────────┐
│                    │
│ CodeMedic v0.1.0.0 │
│                    │
└────────────────────┘

.NET Repository Health Analysis Tool
```

## Building and Testing

### Build the Project

```bash
cd src/CodeMedic
dotnet build
```

### Run Commands

```bash
# From the bin/Debug/net10.0 directory
./CodeMedic.exe --help
./CodeMedic.exe --version
./CodeMedic.exe

# Or using dotnet
dotnet run -- --help
dotnet run -- --version
```

## Key Design Decisions

1. **Simplified Initial Implementation** - Started with a straightforward argument parser instead of complex System.CommandLine setup, allowing for rapid iteration and extensibility

2. **Cross-Platform** - All output uses Spectre.Console which works consistently across Windows, macOS, and Linux

3. **Extensible Structure** - Clear separation of concerns:
   - `RootCommandHandler` - routing logic
   - `ConsoleRenderer` - presentation layer
   - `VersionUtility` - data layer
   - Easy to add new handlers and output formatters

4. **Version Management** - Uses Nerdbank.GitVersioning for automatic semantic versioning based on git history

## Next Steps for Extension

The skeleton is ready for the following additions:

1. **Health Command** - Implement the repository health dashboard aggregator
2. **BOM Command** - Implement the bill of materials report generator
3. **Plugin System** - Add plugin discovery and loading
4. **Options Parsing** - Add format options (JSON, Markdown) for reports
5. **Error Handling** - Enhanced error messages and exit codes

## Testing

Basic CLI tests can be performed manually:

```bash
# Test help
codemedic --help     # Should show help
codemedic -h         # Should show help
codemedic help       # Should show help

# Test version
codemedic --version  # Should show version panel
codemedic -v         # Should show version panel
codemedic version    # Should show version panel

# Test default
codemedic            # Should show help (no arguments)

# Test error
codemedic unknown    # Should show error + help
```

All commands should return exit code 0 on success, 1 on error (currently all success cases return 0).

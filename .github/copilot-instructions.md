---
description: Copilot instructions for CodeMedic - A .NET repository health analysis tool
applyTo: '**'
---

# CodeMedic Copilot Instructions

## Project Overview

**CodeMedic** is a .NET 10 console application that provides comprehensive health analysis and reporting for .NET repositories. The application scans and reviews .NET projects to deliver actionable insights about dependency health, architecture quality, code standards, and repository maintenance.

### Technology Stack
- **Framework**: .NET 10 (.NET 10.0)
- **Application Type**: Console/CLI Application
- **Language**: C#
- **Primary Delivery**: Cross-platform command-line interface

## Core Principles

### 1. Cross-Platform Console UI
- Ensure all UI components work consistently across **Windows, macOS, and Linux**
- Use cross-platform CLI libraries that abstract OS-specific concerns
- Test and validate output formatting on all target operating systems
- Prefer libraries like **Spectre.Console** for rich terminal output that works everywhere

### 2. Console-First Design
- Design all features with the command-line interface as the primary interaction model
- Provide clear, actionable command-line feedback and output
- Support piping and scripting patterns where applicable
- Implement consistent command structure and help documentation

### 3. Health Analysis Focus
CodeMedic delivers features that help developers:
- **Scan** .NET repositories for health indicators
- **Review** dependencies, architecture, code quality, and build health
- **Report** findings in human-readable and machine-readable formats

## Key Features

### Repository Health Dashboard
A unified, extensible command that orchestrates multiple subsystems and produces a holistic, actionable report. The dashboard aggregates:
- Overall health score and trends
- Code quality summary
- Architecture and layering analysis
- Dependency health status
- Build performance metrics
- Test coverage and health
- Security posture and configuration
- GitHub workflow activity
- Doctor's Orders (actionable recommendations)

### Bill of Materials (BOM)
A comprehensive, multi-layered inventory of:
- NuGet packages (direct and transitive dependencies)
- Framework and platform features
- External services and vendors
- Environmental requirements
- Risk factors and known vulnerabilities
- License compliance information

Outputs available in both human-readable and machine-readable formats (JSON, Markdown).

## Development Guidelines

### Architecture
- Follow **modular design patterns** to support extensibility
- Separate concerns: scanning engines, analysis processors, report generators
- Make the BOM and other analysis engines pluggable subsystems
- Design for future feature additions without breaking existing functionality

### Command-Line Libraries
- Use **Spectre.Console** for rich formatting, tables, and interactive prompts
- Implement **System.CommandLine** or similar for robust command parsing
- Consider **Figlet** for ASCII art headers and branding
- Use progress indicators and spinners for long-running operations
- Provide color-coded output that degrades gracefully on systems without color support

### Code Quality Standards
- Enable **Nullable reference types** (`<Nullable>enable</Nullable>`)
- Use **implicit usings** (`<ImplicitUsings>enable</ImplicitUsings>`)
- Follow C# naming conventions and best practices
- Activate TreatWarningsAsErrors so that we can follow good coding practices
- Write defensive code with proper error handling
- Write unit-testable code as much as possible
- Provide meaningful error messages to end users

### Versioning
- Use **Nerd Bank Git Versioning** (NBGv2) to manage project version numbers
- Version is automatically calculated from git commits and tags
- Configure versioning in `.version.json` at the repository root
- Version information is injected into assemblies automatically
- Semantic versioning is enforced through git workflow

### Output and Reporting
- Default to human-readable console output
- Support machine-readable formats (JSON) for automation and integration
- Include structured logging for debugging and troubleshooting
- Ensure output is accessible and clear regardless of terminal capabilities

## Code Organization

### Project Structure
```
CodeMedic/
├── Program.cs                 # Application entry point
├── Commands/                  # Command definitions
├── Engines/                   # Analysis engines (BOM, Health, etc.)
├── Models/                    # Data models and entities
├── Processors/                # Analysis and transformation logic
├── Reporters/                 # Output formatters and generators
├── Utilities/                 # Cross-cutting utilities
└── Plugins/                   # Plugin infrastructure
    └── PluginLoader.cs        # Plugin discovery and lifecycle management
```

### Naming Conventions
- Command classes: `{Feature}Command.cs` (e.g., `HealthCommand.cs`, `BomCommand.cs`)
- Engine classes: `{Feature}Engine.cs` (e.g., `BomEngine.cs`)
- Model classes: PascalCase (e.g., `Package.cs`, `HealthReport.cs`)
- Plugin interfaces: `I{Feature}Plugin.cs` (e.g., `IAnalysisEnginePlugin.cs`)

## Plugin Architecture

CodeMedic supports a plugin system that allows extending analysis capabilities, adding custom commands, and developing specialized reporters without modifying core code.

### Core Plugin Interfaces
Plugins implement well-defined abstractions:

- **`IAnalysisEnginePlugin`** - Contribute custom scanning and analysis engines
- **`IReporterPlugin`** - Add custom output formatters and report generators
- **`ICommandPlugin`** - Extend CLI with custom commands
- **`IProcessorPlugin`** - Add custom data processing and transformation logic

All plugin interfaces should be defined in a `CodeMedic.Abstractions` assembly that plugins reference.

### Plugin Package Structure
```
CodeMedic.Abstractions/
├── Plugins/
│   ├── IAnalysisEnginePlugin.cs
│   ├── IReporterPlugin.cs
│   ├── ICommandPlugin.cs
│   └── IProcessorPlugin.cs
├── Models/
│   └── PluginMetadata.cs       # Plugin identification and versioning
└── CodeMedic.Abstractions.csproj

Plugins/
├── CodeMedic.Plugin.Custom/     # Example custom plugin
├── CodeMedic.Plugin.Vendor/     # Example vendor-specific plugin
└── CodeMedic.Plugin.Security/   # Example security scanner plugin
```

### Plugin Discovery & Loading
- Plugins are discovered from designated directories (e.g., `~/.codemedic/plugins/`, `./plugins/`)
- The `PluginLoader` utility uses reflection to scan plugin assemblies
- Each plugin must export plugin metadata (name, version, capabilities)
- Plugins are loaded into the command system and integrated with the dashboard

### Plugin Development Guidelines
- Plugins must target .NET 10.0 and reference `CodeMedic.Abstractions`
- Implement one or more core plugin interfaces
- Provide meaningful descriptions and help text
- Follow the same code quality standards as core application
- Include unit tests within the plugin project
- Handle errors gracefully and provide user-friendly messages
- Ensure cross-platform compatibility

### Plugin Integration Points
- **Analysis Results**: Plugins can contribute data to the repository health dashboard
- **Command Registration**: Plugin commands automatically appear in CLI help and command discovery
- **Report Generation**: Custom reporters can format results in specialized formats
- **Data Processing**: Processors can transform or enrich scan data before reporting

### Plugin Distribution & Management
- Plugins are distributed as NuGet packages or standalone assemblies
- Plugins include a manifest file (JSON) describing metadata, dependencies, and capabilities
- Versioning follows semantic versioning; core application validates plugin compatibility
- Use **Nerd Bank Git Versioning** (NBGv2) for plugin version management when publishing NuGet packages
- Future: plugin registry/marketplace for discovering and installing community plugins

## Testing Guidelines

- Write unit tests for core analysis logic
- Write unit tests that live in a /test folder and use xUnit to verify functionality
- Test cross-platform scenarios where applicable
- Validate console output formatting
- Include integration tests for command execution
- Mock external dependencies (file system, network calls)

## Performance Considerations

- Optimize scanning and analysis for large repositories
- Implement parallel processing where beneficial
- Provide progress feedback for long-running operations
- Consider caching for repeated analysis operations

## Documentation

- Keep README.md focused and action-oriented
- Document all available commands and options in a /user-docs folder
  - This folder should be focused on feature description for end-users 
- Provide examples for common use cases
- Include troubleshooting guidance for cross-platform issues
- Add feature documentation in `/doc` directory for contributors to the project
- **DO NOT create meta-documentation**: Never create summary documents, completion reports, or index files that only document other documentation. Consolidate documentation into existing structure (README, /doc, /user-docs) instead.
- **Consolidate over proliferation**: When documentation would duplicate existing content, merge it instead of creating new files

## When Adding New Features

1. Verify cross-platform compatibility of any dependencies
2. Ensure console output is consistent across all platforms
3. Update help text and documentation
4. Add appropriate unit tests
5. Test on multiple operating systems before completion
6. Consider integration with the health dashboard or BOM

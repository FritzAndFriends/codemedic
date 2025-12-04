# CodeMedic Plugin Architecture

## Overview

CodeMedic supports a extensible plugin system that allows developers to:
- Create custom analysis engines that integrate with the repository health dashboard
- Develop specialized reporters for custom output formats
- Add new CLI commands without modifying core application code
- Build custom data processors for specialized transformations

This document provides comprehensive guidance on developing, packaging, and integrating plugins with CodeMedic.

---

## Core Concepts

### Plugin Types

CodeMedic supports four types of plugins:

#### 1. Analysis Engine Plugins (`IAnalysisEnginePlugin`)
Contribute custom scanning and analysis capabilities to CodeMedic.

**Responsibilities:**
- Scan repositories for specific health indicators
- Return structured analysis results
- Provide meaningful descriptions of findings

**Example Use Cases:**
- Custom security vulnerability scanner
- Architecture compliance checker
- Performance profiler
- Code duplication detector

#### 2. Reporter Plugins (`IReporterPlugin`)
Add custom output formatters and report generators.

**Responsibilities:**
- Accept analysis results in standard formats
- Transform and format data for specific output targets
- Support both human-readable and machine-readable formats

**Example Use Cases:**
- SARIF format reporter for security findings
- HTML report generator
- Markdown report formatter
- Integration with external reporting systems

#### 3. Command Plugins (`ICommandPlugin`)
Extend the CLI with custom commands.

**Responsibilities:**
- Define CLI command structure and options
- Execute command logic
- Return exit codes and output

**Example Use Cases:**
- `codemedic scan-vulnerabilities` - dedicated vulnerability scanner command
- `codemedic audit-licenses` - license compliance auditor
- `codemedic export-compliance` - compliance report exporter

#### 4. Processor Plugins (`IProcessorPlugin`)
Transform and enrich scan data before reporting.

**Responsibilities:**
- Accept raw analysis data
- Apply transformations, enrichment, or filtering
- Output processed data

**Example Use Cases:**
- Data aggregator combining results from multiple engines
- Risk scorer for dependencies
- Compliance rule evaluator
- Metric calculator

---

## Plugin Interface Definitions

### IAnalysisEnginePlugin

```csharp
namespace CodeMedic.Abstractions.Plugins;

public interface IAnalysisEnginePlugin : IPlugin
{
    /// <summary>
    /// Gets a short description of what this engine analyzes.
    /// </summary>
    string AnalysisDescription { get; }

    /// <summary>
    /// Scans the repository and returns analysis results.
    /// </summary>
    /// <param name="repositoryPath">Path to the root of the repository to analyze</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Analysis result data in JSON format</returns>
    Task<string> AnalyzeAsync(string repositoryPath, CancellationToken cancellationToken);
}
```

### IReporterPlugin

```csharp
namespace CodeMedic.Abstractions.Plugins;

public interface IReporterPlugin : IPlugin
{
    /// <summary>
    /// Gets the output format this reporter produces.
    /// </summary>
    string OutputFormat { get; }

    /// <summary>
    /// Generates a formatted report from analysis results.
    /// </summary>
    /// <param name="analysisResults">Analysis results in JSON format</param>
    /// <param name="outputPath">Path where the report should be written</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    Task GenerateReportAsync(string analysisResults, string outputPath, CancellationToken cancellationToken);
}
```

### ICommandPlugin

```csharp
namespace CodeMedic.Abstractions.Plugins;

public interface ICommandPlugin : IPlugin
{
    /// <summary>
    /// Gets the command name (e.g., "scan-security").
    /// </summary>
    string CommandName { get; }

    /// <summary>
    /// Gets a description of what this command does.
    /// </summary>
    string CommandDescription { get; }

    /// <summary>
    /// Executes the plugin command.
    /// </summary>
    /// <param name="args">Command-line arguments passed to this command</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken);
}
```

### IProcessorPlugin

```csharp
namespace CodeMedic.Abstractions.Plugins;

public interface IProcessorPlugin : IPlugin
{
    /// <summary>
    /// Gets a description of what this processor does.
    /// </summary>
    string ProcessorDescription { get; }

    /// <summary>
    /// Processes and transforms analysis data.
    /// </summary>
    /// <param name="inputData">Input data in JSON format</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Processed data in JSON format</returns>
    Task<string> ProcessAsync(string inputData, CancellationToken cancellationToken);
}
```

### IPlugin (Base Interface)

```csharp
namespace CodeMedic.Abstractions.Plugins;

public interface IPlugin
{
    /// <summary>
    /// Gets the plugin metadata.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Initializes the plugin.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up plugin resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupAsync(CancellationToken cancellationToken);
}
```

### PluginMetadata

```csharp
namespace CodeMedic.Abstractions.Plugins;

public class PluginMetadata
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Author { get; init; }
    public required string Description { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? DocumentationUrl { get; init; }
    public required string[] Capabilities { get; init; }
    public required string MinimumCodeMedicVersion { get; init; }
}
```

---

## Plugin Development Workflow

### Step 1: Create the Plugin Project

Create a new .NET 10 class library project:

```bash
dotnet new classlib -n CodeMedic.Plugin.MyAnalyzer -f net10.0
cd CodeMedic.Plugin.MyAnalyzer
```

### Step 2: Reference CodeMedic.Abstractions

Add a reference to the abstractions package in your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="CodeMedic.Abstractions" Version="1.0.0" />
</ItemGroup>
```

### Step 3: Implement a Plugin Interface

Example: Analysis Engine Plugin

```csharp
using CodeMedic.Abstractions.Plugins;

namespace CodeMedic.Plugin.MyAnalyzer;

public class MyAnalyzerPlugin : IAnalysisEnginePlugin
{
    public PluginMetadata Metadata { get; } = new()
    {
        Name = "My Custom Analyzer",
        Version = "1.0.0",
        Author = "Your Name",
        Description = "Analyzes code for custom metrics",
        RepositoryUrl = "https://github.com/yourusername/my-analyzer",
        Capabilities = new[] { "custom-analysis", "metrics" },
        MinimumCodeMedicVersion = "1.0.0"
    };

    public string AnalysisDescription => "Performs custom code analysis";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Initialize plugin resources (e.g., load configuration, validate environment)
        await Task.CompletedTask;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        // Clean up plugin resources
        await Task.CompletedTask;
    }

    public async Task<string> AnalyzeAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        // Perform analysis
        var results = new
        {
            engineName = Metadata.Name,
            timestamp = DateTime.UtcNow,
            findings = new[]
            {
                new { severity = "info", message = "Example finding" }
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(results);
    }
}
```

### Step 4: Create a Plugin Manifest

Create a `plugin-manifest.json` file in your project root:

```json
{
  "name": "My Custom Analyzer",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Analyzes code for custom metrics",
  "repositoryUrl": "https://github.com/yourusername/my-analyzer",
  "documentationUrl": "https://github.com/yourusername/my-analyzer/wiki",
  "capabilities": [
    "custom-analysis",
    "metrics"
  ],
  "minimumCodeMedicVersion": "1.0.0",
  "dependencies": []
}
```

### Step 5: Package the Plugin

Build your plugin as a NuGet package:

```bash
dotnet pack -c Release
```

Or create a standalone assembly package with the manifest included.

### Step 6: Install the Plugin

Place the plugin DLL in a CodeMedic plugins directory:
- `~/.codemedic/plugins/` (user plugins)
- `./plugins/` (local plugins)
- System plugins directory (configurable)

---

## Plugin Discovery & Loading

### Discovery Process

1. **Scan Plugin Directories**: The `PluginLoader` scans designated directories for plugin assemblies
2. **Reflect on Assemblies**: Uses reflection to find types implementing plugin interfaces
3. **Load Metadata**: Extracts `PluginMetadata` from each plugin
4. **Validate Compatibility**: Checks version compatibility with core application
5. **Instantiate Plugins**: Creates instances of discovered plugins
6. **Register with System**: Plugins are registered with command system and analysis engine registry

### Plugin Loader Example

```csharp
using System.Reflection;
using CodeMedic.Abstractions.Plugins;

namespace CodeMedic.Utilities;

public class PluginLoader
{
    private readonly string[] _pluginDirectories;

    public PluginLoader(params string[] pluginDirectories)
    {
        _pluginDirectories = pluginDirectories;
    }

    public async Task<IEnumerable<IPlugin>> DiscoverPluginsAsync(CancellationToken cancellationToken)
    {
        var plugins = new List<IPlugin>();

        foreach (var directory in _pluginDirectories)
        {
            if (!Directory.Exists(directory))
                continue;

            var dllFiles = Directory.GetFiles(directory, "*.dll");
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var pluginType in pluginTypes)
                    {
                        if (Activator.CreateInstance(pluginType) is IPlugin plugin)
                        {
                            await plugin.InitializeAsync(cancellationToken);
                            plugins.Add(plugin);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load plugin from {dllFile}: {ex.Message}");
                }
            }
        }

        return plugins;
    }
}
```

---

## Integration Points

### Dashboard Integration

Plugins can contribute data to the repository health dashboard:

```csharp
// Analysis engines contribute findings
var engineResults = await analysisPlugin.AnalyzeAsync(repoPath, ct);
dashboard.AddSection("Custom Analysis", engineResults);

// Processors enrich and transform data
var enrichedData = await processor.ProcessAsync(rawData, ct);

// Reporters format final output
await reporter.GenerateReportAsync(finalData, outputPath, ct);
```

### Command Registration

Plugin commands are automatically discovered and registered:

```bash
codemedic --help
# Output includes:
# Commands:
#   health        Show repository health dashboard
#   bom           Generate bill of materials
#   my-command    [Custom plugin command description]
```

### Data Flow

```
Repository → Analysis Engines → Processors → Dashboard/Reports → Output
                ↑ Plugin                ↑ Plugin         ↑ Plugin
```

---

## Best Practices

### 1. Error Handling
- Handle and report errors gracefully
- Provide meaningful error messages to users
- Use try-catch blocks to prevent crashes
- Log errors at appropriate levels

### 2. Performance
- Implement cancellation token support
- Show progress for long-running operations
- Optimize for large repositories
- Cache results when appropriate

### 3. Cross-Platform Compatibility
- Test on Windows, macOS, and Linux
- Use cross-platform APIs
- Handle path differences appropriately
- Ensure console output works everywhere

### 4. Code Quality
- Follow C# naming conventions
- Enable nullable reference types
- Use implicit usings
- Write unit tests
- Document public APIs

### 5. Security
- Validate all inputs
- Don't execute untrusted code
- Sanitize output before displaying
- Handle sensitive data carefully

### 6. Documentation
- Include README in plugin package
- Document all public methods
- Provide usage examples
- Include troubleshooting guide

---

## Testing Plugins

### Unit Testing

Create unit tests within your plugin project:

```csharp
using Xunit;
using CodeMedic.Plugin.MyAnalyzer;

namespace CodeMedic.Plugin.MyAnalyzer.Tests;

public class MyAnalyzerPluginTests
{
    [Fact]
    public async Task AnalyzeAsync_WithValidRepository_ReturnsResults()
    {
        // Arrange
        var plugin = new MyAnalyzerPlugin();
        await plugin.InitializeAsync(CancellationToken.None);
        var repoPath = "/path/to/test/repo";

        // Act
        var result = await plugin.AnalyzeAsync(repoPath, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("findings", result);

        // Cleanup
        await plugin.CleanupAsync(CancellationToken.None);
    }
}
```

### Integration Testing

Test plugin integration with CodeMedic:

```bash
# Copy plugin DLL to plugins directory
cp bin/Release/net10.0/CodeMedic.Plugin.MyAnalyzer.dll ~/.codemedic/plugins/

# Run CodeMedic with the plugin loaded
codemedic health --include-plugins
```

---

## Distribution

### NuGet Package

```bash
# Create package
dotnet pack -c Release

# Publish to nuget.org
dotnet nuget push bin/Release/CodeMedic.Plugin.MyAnalyzer.1.0.0.nupkg --api-key YOUR_API_KEY
```

### Standalone Distribution

1. Build the plugin assembly
2. Create a zip file with:
   - Plugin DLL
   - `plugin-manifest.json`
   - `README.md`
   - Dependencies (if any)
3. Distribute via GitHub Releases, plugin registry, or package manager

### Plugin Registry (Future)

CodeMedic will support a central plugin registry for discovering and installing community plugins:

```bash
codemedic plugin install my-analyzer
codemedic plugin list
codemedic plugin update
codemedic plugin remove my-analyzer
```

---

## Versioning & Compatibility

### Semantic Versioning

Plugins use semantic versioning (MAJOR.MINOR.PATCH):

- **MAJOR**: Breaking changes to plugin interface or capabilities
- **MINOR**: New features added in backward-compatible manner
- **PATCH**: Bug fixes and improvements

### Compatibility Checking

- Plugins must declare minimum CodeMedic version in metadata
- CoreMedic validates plugin compatibility before loading
- Incompatible plugins are skipped with a warning

---

## Examples

### Example 1: Security Scanner Plugin

```csharp
public class SecurityScannerPlugin : IAnalysisEnginePlugin
{
    public PluginMetadata Metadata { get; } = new()
    {
        Name = "Security Scanner",
        Version = "1.0.0",
        Author = "Security Team",
        Description = "Scans dependencies for known vulnerabilities",
        Capabilities = new[] { "vulnerability-scanning" },
        MinimumCodeMedicVersion = "1.0.0"
    };

    public string AnalysisDescription => "Vulnerability analysis";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Initialize vulnerability database
        await Task.CompletedTask;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken) => await Task.CompletedTask;

    public async Task<string> AnalyzeAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        // Scan for vulnerabilities
        var vulnerabilities = await ScanForVulnerabilitiesAsync(repositoryPath, cancellationToken);
        return System.Text.Json.JsonSerializer.Serialize(vulnerabilities);
    }

    private Task<object> ScanForVulnerabilitiesAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        // Implementation
        throw new NotImplementedException();
    }
}
```

### Example 2: HTML Reporter Plugin

```csharp
public class HtmlReporterPlugin : IReporterPlugin
{
    public PluginMetadata Metadata { get; } = new()
    {
        Name = "HTML Reporter",
        Version = "1.0.0",
        Author = "Reports Team",
        Description = "Generates HTML reports from analysis results",
        Capabilities = new[] { "html-reporting" },
        MinimumCodeMedicVersion = "1.0.0"
    };

    public string OutputFormat => "html";

    public async Task InitializeAsync(CancellationToken cancellationToken) => await Task.CompletedTask;
    public async Task CleanupAsync(CancellationToken cancellationToken) => await Task.CompletedTask;

    public async Task GenerateReportAsync(string analysisResults, string outputPath, CancellationToken cancellationToken)
    {
        var html = GenerateHtml(analysisResults);
        await File.WriteAllTextAsync(outputPath, html, cancellationToken);
    }

    private string GenerateHtml(string analysisResults)
    {
        // Implementation
        throw new NotImplementedException();
    }
}
```

---

## Troubleshooting

### Plugin Not Loading

1. **Check plugin directory**: Ensure DLL is in correct plugins directory
2. **Verify dependencies**: Ensure all plugin dependencies are installed
3. **Check compatibility**: Verify plugin targets .NET 10.0
4. **Check manifest**: Ensure `plugin-manifest.json` is valid JSON

### Plugin Crashes

1. **Enable debug logging**: Run CodeMedic with `--debug` flag
2. **Check error messages**: Review plugin error output
3. **Test in isolation**: Run plugin tests to verify functionality
4. **Check dependencies**: Ensure all external dependencies are available

### Performance Issues

1. **Profile plugin**: Use .NET profiling tools to identify bottlenecks
2. **Implement cancellation**: Add cancellation token support
3. **Add progress reporting**: Show progress for long-running operations
4. **Cache results**: Implement caching where appropriate

---

## Contributing Plugins

We welcome community contributions! To contribute a plugin:

1. Fork the CodeMedic repository
2. Create a plugin following this guide
3. Add comprehensive tests
4. Document your plugin
5. Submit a pull request
6. Maintain compatibility with newer CodeMedic versions

---

## Support & Resources

- **Documentation**: https://github.com/csharpfritz/codemedic/wiki
- **Issue Tracker**: https://github.com/csharpfritz/codemedic/issues
- **Community Plugins**: https://github.com/csharpfritz/codemedic/discussions
- **Plugin Template**: https://github.com/csharpfritz/codemedic-plugin-template

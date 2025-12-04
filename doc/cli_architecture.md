# CLI Architecture and Extension Guide

## Overview

The CodeMedic CLI skeleton provides a foundation for building extensible command-line operations. The architecture emphasizes clean separation of concerns, making it easy to add new commands without modifying core infrastructure.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Program.cs                              │
│              (Application Entry Point)                       │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│               RootCommandHandler                             │
│                                                              │
│  ProcessArguments(string[] args)                            │
│  - Parses arguments                                         │
│  - Routes to handlers                                       │
│  - Returns exit codes                                       │
└────────────────────────┬────────────────────────────────────┘
         │               │               │
         ▼               ▼               ▼
    ┌────────────┐ ┌──────────┐  ┌──────────────┐
    │ Help       │ │ Version  │  │ Future       │
    │ Handler    │ │ Handler  │  │ Commands     │
    └────┬───────┘ └────┬─────┘  └──────┬───────┘
         │              │               │
         └──────────────┼───────────────┘
                        │
                        ▼
         ┌──────────────────────────────┐
         │    ConsoleRenderer           │
         │                              │
         │  - RenderBanner()            │
         │  - RenderHelp()              │
         │  - RenderVersion()           │
         │  - RenderError()             │
         │  - RenderSuccess()           │
         │  - RenderInfo()              │
         └──────────────┬───────────────┘
                        │
                        ▼
         ┌──────────────────────────────┐
         │   Spectre.Console            │
         │   (Cross-platform Output)    │
         └──────────────────────────────┘
```

## File Organization

```
CodeMedic/
│
├── Program.cs                      # Entry point, delegates to handler
│
├── Commands/
│   ├── RootCommandHandler.cs       # Command routing logic
│   └── [Future: HealthCommand.cs]
│   └── [Future: BomCommand.cs]
│
├── Output/
│   └── ConsoleRenderer.cs          # All output formatting
│
├── Utilities/
│   └── VersionUtility.cs           # Version retrieval
│
└── Options/
    └── [Reserved for future options parsing]
```

## Design Principles

### 1. Single Responsibility Principle
- **RootCommandHandler**: Only routes commands
- **ConsoleRenderer**: Only formats output
- **VersionUtility**: Only retrieves version info

Each class has one reason to change.

### 2. Open/Closed Principle
- Open for extension: Easy to add new commands
- Closed for modification: Core handler logic unchanged

### 3. Dependency Inversion
- Commands depend on abstractions (ConsoleRenderer)
- Output details abstracted away from command logic

## Extending with New Commands

### Step 1: Create Command Handler

**File:** `Commands/HealthCommand.cs`

```csharp
namespace CodeMedic.Commands;

/// <summary>
/// Handles the 'health' command to display repository health dashboard.
/// </summary>
public class HealthCommand
{
    public static int Execute(string[] args)
    {
        // Parse command-specific arguments
        var format = args.Contains("--format") 
            ? args[args.IndexOf("--format") + 1] 
            : "text";
        
        // Execute analysis
        var healthReport = AnalyzeRepositoryHealth();
        
        // Render output based on format
        if (format == "json")
        {
            ConsoleRenderer.RenderJson(healthReport);
        }
        else
        {
            ConsoleRenderer.RenderHealthDashboard(healthReport);
        }
        
        return 0;
    }
}
```

### Step 2: Add Command to Router

**File:** `Commands/RootCommandHandler.cs`

Modify `ProcessArguments()`:

```csharp
public static int ProcessArguments(string[] args)
{
    var version = VersionUtility.GetVersion();

    // ... existing code ...

    // Add new command routing
    if (args.Contains("health"))
    {
        return HealthCommand.Execute(args);
    }

    if (args.Contains("bom"))
    {
        return BomCommand.Execute(args);
    }

    // ... rest of code ...
}
```

### Step 3: Add Renderer Methods

**File:** `Output/ConsoleRenderer.cs`

```csharp
public static void RenderHealthDashboard(HealthReport report)
{
    var table = new Table { /* ... */ };
    // Format and display report
    AnsiConsole.Write(table);
}

public static void RenderJson(object data)
{
    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
    AnsiConsole.MarkupLine(json);
}
```

## Command-Specific Options Pattern

For commands with multiple options, use this pattern:

```csharp
public class CommandOptions
{
    public string Format { get; set; } = "text";
    public bool Verbose { get; set; }
    public string OutputFile { get; set; }
    
    public static CommandOptions ParseArgs(string[] args)
    {
        var options = new CommandOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                    options.Format = args[++i];
                    break;
                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "-o":
                case "--output":
                    options.OutputFile = args[++i];
                    break;
            }
        }
        
        return options;
    }
}
```

## Error Handling Pattern

```csharp
try
{
    // Execute command logic
    var result = PerformAnalysis();
    ConsoleRenderer.RenderSuccess("Analysis completed");
    return 0;
}
catch (IOException ex)
{
    ConsoleRenderer.RenderError($"File error: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    ConsoleRenderer.RenderError($"Unexpected error: {ex.Message}");
    if (verbose) Console.WriteLine(ex.StackTrace);
    return 1;
}
```

## Output Format Guidelines

### Success Output
```csharp
ConsoleRenderer.RenderSuccess("Operation completed successfully");
```

### Error Output
```csharp
ConsoleRenderer.RenderError("Invalid configuration file");
```

### Information Output
```csharp
ConsoleRenderer.RenderInfo("Processing repository (this may take a moment)");
```

### Data Display
Use Spectre.Console tables for structured data:

```csharp
var table = new Table();
table.AddColumn("Property");
table.AddColumn("Value");
table.AddRow("[yellow]Status[/]", "[green]Healthy[/]");
table.AddRow("[yellow]Score[/]", "[cyan]8.5/10[/]");
AnsiConsole.Write(table);
```

## Cross-Platform Considerations

1. **File Paths**: Use `Path.Combine()` instead of hardcoded separators
2. **Colors**: Spectre.Console handles platform differences automatically
3. **Line Endings**: `Environment.NewLine` for proper line breaks
4. **Environment Variables**: Use `Environment.GetEnvironmentVariable()`

## Testing Commands

### Unit Testing Pattern

```csharp
[TestClass]
public class HealthCommandTests
{
    [TestMethod]
    public void Execute_WithValidRepo_ReturnsZero()
    {
        // Arrange
        var args = new[] { "health" };
        
        // Act
        var result = HealthCommand.Execute(args);
        
        // Assert
        Assert.AreEqual(0, result);
    }
    
    [TestMethod]
    public void Execute_WithInvalidPath_ReturnsOne()
    {
        // Arrange
        var args = new[] { "health", "--path", "/invalid/path" };
        
        // Act
        var result = HealthCommand.Execute(args);
        
        // Assert
        Assert.AreEqual(1, result);
    }
}
```

### Integration Testing Pattern

```bash
# Run in actual terminal
codemedic health
codemedic health --format json
codemedic bom --output report.json
```

## Performance Optimization

For long-running operations, use progress indicators:

```csharp
AnsiConsole.Progress()
    .Start(ctx =>
    {
        var task = ctx.AddTask("[green]Analyzing repository[/]");
        
        for (int i = 0; i < 100; i++)
        {
            // Do work
            task.Increment(1);
        }
    });
```

## Version Management

The version is automatically managed by Nerd Bank Git Versioning (NBGv2). It's injected during build based on git history and tags. To update the version:

```bash
# Install NBGV tool (one-time)
dotnet tool install -g nbgv

# Set version
nbgv set-version 1.0.0

# View current version
nbgv get-version

# Commit and tag
git add version.json
git commit -m "chore: bump version to 1.0.0"
git tag v1.0.0
git push origin main --tags
```

Version info is automatically injected into the assembly during build:

```csharp
var version = VersionUtility.GetVersion(); // Returns version from NBGv2
```

## Dependency Management

Currently minimal dependencies:
- **System.CommandLine 2.0.0** - Available but not yet actively used
- **Spectre.Console 0.49.1** - All output formatting
- **Nerdbank.GitVersioning 3.9.50** - Version injection

Future: Consider using System.CommandLine for more complex option parsing.

## Contributing Guidelines

When adding new commands:

1. ✅ Keep command handlers in `Commands/` folder
2. ✅ Add output methods to `ConsoleRenderer` (not in command handler)
3. ✅ Use consistent error handling patterns
4. ✅ Return proper exit codes (0 = success, 1 = error)
5. ✅ Add XML documentation comments
6. ✅ Test on Windows, macOS, and Linux
7. ✅ Update help text in `RenderHelp()`
8. ✅ Add tests in `/test` folder

## Future Enhancements

- [ ] Plugin system for extending commands
- [ ] Command-line option validation framework
- [ ] Output format plugins (XML, YAML)
- [ ] Configuration file support
- [ ] Async command execution for long-running operations
- [ ] Bash/PowerShell completion support

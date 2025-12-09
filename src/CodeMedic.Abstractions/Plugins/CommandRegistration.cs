using System.Collections.Generic;

namespace CodeMedic.Abstractions.Plugins;

/// <summary>
/// Represents a command that can be registered with the CLI.
/// </summary>
public class CommandRegistration
{
    /// <summary>
    /// Gets or sets the command name (e.g., "health", "bom").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the command description for help text.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the command handler that will be executed.
    /// </summary>
    public required Func<string[], IRenderer, Task<int>> Handler { get; init; }

    /// <summary>
    /// Gets or sets example usage strings for help text.
    /// </summary>
    public string[]? Examples { get; init; }

    /// <summary>
    /// Gets or sets the command arguments specification.
    /// </summary>
    public CommandArgument[]? Arguments { get; init; }
}

/// <summary>
/// Represents a command-line argument specification.
/// </summary>
 public class CommandArgument
{
    /// <summary>
    /// Gets or sets the short name of the argument (e.g., "p" for "-p").
    /// </summary>
    public string? ShortName { get; init; }

    /// <summary>
    /// Gets or sets the long name of the argument (e.g., "path" for "--path").
    /// </summary>
    public string? LongName { get; init; }

    /// <summary>
    /// Gets or sets the description of what this argument does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets whether this argument is required.
    /// </summary>
    public bool IsRequired { get; init; } = false;

    /// <summary>
    /// Gets or sets whether this argument takes a value.
    /// </summary>
    public bool HasValue { get; init; } = true;

    /// <summary>
    /// Gets or sets the default value for this argument.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets or sets the value type name for help display (e.g., "path", "format", "count").
    /// </summary>
    public string? ValueName { get; init; }
}

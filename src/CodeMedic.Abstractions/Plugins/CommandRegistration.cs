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
    public required Func<HandlerPayload, Task<int>> Handler { get; init; }

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
/// <param name="Description">The description of what this argument does.</param>
/// <param name="ShortName">The short name of the argument (e.g., "p" for "-p").</param>
/// <param name="LongName">The long name of the argument (e.g., "path" for "--path").</param>
/// <param name="IsRequired">Whether this argument is required.</param>
/// <param name="HasValue">Whether this argument takes a value.</param>
/// <param name="DefaultValue">The default value for this argument.</param>
/// <param name="ValueName">The value type name for help display (e.g., "path", "format", "count").</param>
public record CommandArgument(
    string Description,
    string? ShortName = null,
    string? LongName = null,
    bool IsRequired = false,
    bool HasValue = true,
    string? DefaultValue = null,
    string? ValueName = null);

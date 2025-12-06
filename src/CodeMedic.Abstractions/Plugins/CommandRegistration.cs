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
}

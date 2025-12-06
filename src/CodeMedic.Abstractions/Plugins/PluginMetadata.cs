namespace CodeMedic.Abstractions.Plugins;

/// <summary>
/// Metadata describing a plugin's identity, version, and capabilities.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for the plugin.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the display name of the plugin.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets a brief description of what the plugin does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the plugin author or maintainer.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets or sets any additional tags or categories for the plugin.
    /// </summary>
    public string[]? Tags { get; init; }
}

namespace CodeMedic.Abstractions.Plugins;

/// <summary>
/// Base interface for all CodeMedic plugins.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the plugin metadata describing its identity and capabilities.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Initializes the plugin with any necessary configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async initialization.</param>
    /// <returns>A task that completes when initialization is done.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

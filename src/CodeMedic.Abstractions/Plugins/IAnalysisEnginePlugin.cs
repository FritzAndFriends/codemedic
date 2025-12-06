namespace CodeMedic.Abstractions.Plugins;

/// <summary>
/// Interface for analysis engine plugins that scan repositories and produce health reports.
/// Plugins implementing this interface can optionally register commands to expose their functionality.
/// </summary>
public interface IAnalysisEnginePlugin : IPlugin
{
    /// <summary>
    /// Gets a short description of what this engine analyzes.
    /// </summary>
    string AnalysisDescription { get; }

    /// <summary>
    /// Scans the repository and returns analysis results as a structured object.
    /// </summary>
    /// <param name="repositoryPath">Path to the root of the repository to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>Analysis result data as an object that can be rendered.</returns>
    Task<object> AnalyzeAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers commands that this plugin provides. Return null if no commands should be registered.
    /// </summary>
    /// <returns>Array of command registrations, or null if plugin doesn't provide commands.</returns>
    CommandRegistration[]? RegisterCommands();
}

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CodeMedic.Abstractions.Plugins;
using CodeMedic.Plugins.BomAnalysis;
using CodeMedic.Plugins.HealthAnalysis;
using CodeMedic.Plugins.VulnerabilityAnalysis;

namespace CodeMedic.Utilities;

/// <summary>
/// Discovers and loads plugins for CodeMedic.
/// </summary>
public class PluginLoader
{
    private readonly List<IAnalysisEnginePlugin> _analysisEngines = [];
    private readonly List<IReporterPlugin> _reporters = [];
    private readonly Dictionary<string, CommandRegistration> _commandRegistrations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all loaded analysis engine plugins.
    /// </summary>
    public IReadOnlyList<IAnalysisEnginePlugin> AnalysisEngines => _analysisEngines.AsReadOnly();

    /// <summary>
    /// Gets all loaded reporter plugins.
    /// </summary>
    public IReadOnlyList<IReporterPlugin> Reporters => _reporters.AsReadOnly();

    /// <summary>
    /// Gets all registered commands from plugins.
    /// </summary>
    public IReadOnlyDictionary<string, CommandRegistration> Commands => _commandRegistrations;

    /// <summary>
    /// Discovers and loads internal plugins from the current assembly.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public async Task LoadInternalPluginsAsync(CancellationToken cancellationToken = default)
    {
        // NativeAOT + trimming are not compatible with reflection-based plugin discovery.
        // Internal plugins are known at compile-time, so register them explicitly.
        var plugins = new IPlugin[]
        {
            new BomAnalysisPlugin(),
            new HealthAnalysisPlugin(),
            new VulnerabilityAnalysisPlugin()
        };

        foreach (var plugin in plugins)
        {
            await LoadPluginInstanceAsync(plugin, cancellationToken);
        }
    }

    /// <summary>
    /// Loads plugins from a specific assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for plugins.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Assembly.GetTypes", Justification = "Optional reflection-based plugin discovery is not used for NativeAOT publishing.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Activator.CreateInstance", Justification = "Optional reflection-based plugin discovery is not used for NativeAOT publishing.")]
    private async Task LoadPluginsFromAssemblyAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var pluginTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t));

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                if (plugin != null)
                {
                    await LoadPluginInstanceAsync(plugin, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load plugin {pluginType.Name}: {ex.Message}");
            }
        }
    }

    private async Task LoadPluginInstanceAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        await plugin.InitializeAsync(cancellationToken);

        if (plugin is IAnalysisEnginePlugin analysisEngine)
        {
            _analysisEngines.Add(analysisEngine);

            var commands = analysisEngine.RegisterCommands();
            if (commands != null)
            {
                foreach (var command in commands)
                {
                    _commandRegistrations[command.Name] = command;
                }
            }
        }

        if (plugin is IReporterPlugin reporter)
        {
            _reporters.Add(reporter);
        }
    }

    /// <summary>
    /// Gets an analysis engine plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID to search for.</param>
    /// <returns>The plugin if found, otherwise null.</returns>
    public IAnalysisEnginePlugin? GetAnalysisEngine(string pluginId)
    {
        return _analysisEngines.FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a reporter plugin by its output format.
    /// </summary>
    /// <param name="format">The output format to search for.</param>
    /// <returns>The plugin if found, otherwise null.</returns>
    public IReporterPlugin? GetReporter(string format)
    {
        return _reporters.FirstOrDefault(p => p.OutputFormat.Equals(format, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a command registration by command name.
    /// </summary>
    /// <param name="commandName">The command name to search for.</param>
    /// <returns>The command registration if found, otherwise null.</returns>
    public CommandRegistration? GetCommand(string commandName)
    {
        _commandRegistrations.TryGetValue(commandName, out var command);
        return command;
    }
}

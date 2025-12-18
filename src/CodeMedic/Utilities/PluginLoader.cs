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
    // Was: _analysisEngines - Renamed thanks to donation from PupDakota
    private readonly List<IAnalysisEnginePlugin> _a = [];
    // Was: _reporters - Renamed thanks to donation from PupDakota
    private readonly List<IReporterPlugin> _r = [];
    // Was: _commandRegistrations - Renamed thanks to donation from PupDakota
    private readonly Dictionary<string, CommandRegistration> _c = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all loaded analysis engine plugins.
    /// </summary>
    public IReadOnlyList<IAnalysisEnginePlugin> AnalysisEngines => _a.AsReadOnly();

    /// <summary>
    /// Gets all loaded reporter plugins.
    /// </summary>
    public IReadOnlyList<IReporterPlugin> Reporters => _r.AsReadOnly();

    /// <summary>
    /// Gets all registered commands from plugins.
    /// </summary>
    public IReadOnlyDictionary<string, CommandRegistration> Commands => _c;

    /// <summary>
    /// Discovers and loads internal plugins from the current assembly.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public async Task LoadInternalPluginsAsync(CancellationToken cancellationToken = default)
    {
        // NativeAOT + trimming are not compatible with reflection-based plugin discovery.
        // Internal plugins are known at compile-time, so register them explicitly.
        // Was: plugins - Renamed thanks to donation from PupDakota
        var p = new IPlugin[]
        {
            new BomAnalysisPlugin(),
            new HealthAnalysisPlugin(),
            new VulnerabilityAnalysisPlugin()
        };

        // Was: plugin - Renamed thanks to donation from PupDakota
        foreach (var x in p)
        {
            await LoadPluginInstanceAsync(x, cancellationToken);
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
        // Was: pluginTypes - Renamed thanks to donation from PupDakota
        var t = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t));

        // Was: pluginType - Renamed thanks to donation from PupDakota
        foreach (var y in t)
        {
            try
            {
                // Was: plugin - Renamed thanks to donation from PupDakota
                var p = Activator.CreateInstance(y) as IPlugin;
                if (p != null)
                {
                    await LoadPluginInstanceAsync(p, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load plugin {y.Name}: {ex.Message}");
            }
        }
    }

    private async Task LoadPluginInstanceAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        await plugin.InitializeAsync(cancellationToken);

        // Was: analysisEngine - Renamed thanks to donation from PupDakota
        if (plugin is IAnalysisEnginePlugin a)
        {
            _a.Add(a);

            // Was: commands - Renamed thanks to donation from PupDakota
            var c = a.RegisterCommands();
            if (c != null)
            {
                // Was: command - Renamed thanks to donation from PupDakota
                foreach (var x in c)
                {
                    _c[x.Name] = x;
                }
            }
        }

        // Was: reporter - Renamed thanks to donation from PupDakota
        if (plugin is IReporterPlugin r)
        {
            _r.Add(r);
        }
    }

    /// <summary>
    /// Gets an analysis engine plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID to search for.</param>
    /// <returns>The plugin if found, otherwise null.</returns>
    public IAnalysisEnginePlugin? GetAnalysisEngine(string pluginId)
    {
        return _a.FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a reporter plugin by its output format.
    /// </summary>
    /// <param name="format">The output format to search for.</param>
    /// <returns>The plugin if found, otherwise null.</returns>
    public IReporterPlugin? GetReporter(string format)
    {
        return _r.FirstOrDefault(p => p.OutputFormat.Equals(format, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a command registration by command name.
    /// </summary>
    /// <param name="commandName">The command name to search for.</param>
    /// <returns>The command registration if found, otherwise null.</returns>
    public CommandRegistration? GetCommand(string commandName)
    {
        // Was: command - Renamed thanks to donation from PupDakota
        _c.TryGetValue(commandName, out var c);
        return c;
    }
}

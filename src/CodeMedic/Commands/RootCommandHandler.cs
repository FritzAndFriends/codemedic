using CodeMedic.Output;
using CodeMedic.Utilities;
using Spectre.Console;

namespace CodeMedic.Commands;

/// <summary>
/// Root command handler for the CodeMedic CLI application.
/// Manages the main command structure and default behaviors.
/// </summary>
public class RootCommandHandler
{
    private static PluginLoader? _pluginLoader;

		/// <summary>
		/// Gets a console renderer for providing feedback to the user.
		/// </summary>
    public static readonly ConsoleRenderer Console = new ConsoleRenderer();

    /// <summary>
    /// Processes command-line arguments and executes appropriate handler.
    /// </summary>
    public static async Task<int> ProcessArguments(string[] args)
    {
        var version = VersionUtility.GetVersion();
        var console = new ConsoleRenderer();

        // Load plugins first
        _pluginLoader = new PluginLoader();
        await _pluginLoader.LoadInternalPluginsAsync();

        // No arguments or help requested
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("help"))
        {
            console.RenderBanner(version);
            RenderHelp();
            return 0;
        }

        // Version requested
        if (args.Contains("--version") || args.Contains("-v") || args.Contains("version"))
        {
            ConsoleRenderer.RenderVersion(version);
            RenderPluginInfo();
            return 0;
        }

        // Check if a plugin registered this command
        var commandName = args[0];
        var commandRegistration = _pluginLoader.GetCommand(commandName);
        
        if (commandRegistration != null)
        {
            // Parse --format argument (default: console)
            string format = "console";
            var commandArgsList = args.Skip(1).ToList();
            for (int i = 0; i < commandArgsList.Count; i++)
            {
                if (commandArgsList[i] == "--format" && i + 1 < commandArgsList.Count)
                {
                    format = commandArgsList[i + 1].ToLower();
                    commandArgsList.RemoveAt(i + 1);
                    commandArgsList.RemoveAt(i);
                    break;
                }
            }
            IRenderer renderer = format switch
            {
                "markdown" or "md" => new MarkdownRenderer(),
                _ => new ConsoleRenderer()
            };
            return await commandRegistration.Handler(commandArgsList.ToArray(), renderer);
        }

        // Unknown command
        console.RenderError($"Unknown command: {args[0]}");
        RenderHelp();
        return 1;
    }

    /// <summary>
    /// Renders the help text with available commands (including plugin commands).
    /// </summary>
    private static void RenderHelp()
    {
        var table = new Table
        {
            Border = TableBorder.Rounded,
            Title = new TableTitle("[bold]Available Commands[/]")
        };

        table.AddColumn("Command");
        table.AddColumn("Description");

        // Add plugin-registered commands
        if (_pluginLoader != null)
        {
            foreach (var command in _pluginLoader.Commands.Values.OrderBy(c => c.Name))
            {
                table.AddRow($"[cyan]{command.Name}[/]", command.Description);
            }
        }

        // Add built-in commands
        table.AddRow("[cyan]version[/] or [cyan]-v[/], [cyan]--version[/]", "Display application version");
        table.AddRow("[cyan]help[/] or [cyan]-h[/], [cyan]--help[/]", "Display this help message");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Usage:[/]");
        AnsiConsole.MarkupLine("  [green]codemedic[/] [cyan]<command>[/] [yellow][[options]][/]");
        AnsiConsole.MarkupLine("  [green]codemedic[/] [cyan]--help[/]");
        AnsiConsole.MarkupLine("  [green]codemedic[/] [cyan]--version[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Options:[/]");
        AnsiConsole.MarkupLine("  [yellow]--format <format>[/]  Output format: [cyan]console[/] (default), [cyan]markdown[/] (or [cyan]md[/])");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Examples:[/]");
        
        // Display examples from plugins
        if (_pluginLoader != null)
        {
            foreach (var command in _pluginLoader.Commands.Values.OrderBy(c => c.Name))
            {
                if (command.Examples != null)
                {
                    foreach (var example in command.Examples)
                    {
                        AnsiConsole.MarkupLine($"  [green]{example}[/]");
                    }
                }
            }
        }

        AnsiConsole.MarkupLine("  [green]codemedic --version[/]");
    }

    /// <summary>
    /// Renders information about loaded plugins.
    /// </summary>
    private static void RenderPluginInfo()
    {
        if (_pluginLoader == null)
        {
            return;
        }

        var analysisEngines = _pluginLoader.AnalysisEngines;
        var reporters = _pluginLoader.Reporters;
        var totalPlugins = analysisEngines.Count + reporters.Count;

        if (totalPlugins == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Loaded Plugins:[/] {totalPlugins}");
        AnsiConsole.WriteLine();

        // Analysis Engine Plugins
        if (analysisEngines.Count > 0)
        {
            var pluginTable = new Table
            {
                Border = TableBorder.Rounded,
                Title = new TableTitle("[bold cyan]Analysis Engines[/]")
            };

            pluginTable.AddColumn("Name");
            pluginTable.AddColumn("Version");
            pluginTable.AddColumn("Description");
            pluginTable.AddColumn("Commands");

            foreach (var plugin in analysisEngines.OrderBy(p => p.Metadata.Name))
            {
                // Get commands registered by this specific plugin
                var pluginCommands = plugin.RegisterCommands();
                var commandList = pluginCommands != null && pluginCommands.Length > 0
                    ? string.Join(", ", pluginCommands.Select(c => $"[cyan]{c.Name}[/]"))
                    : "[dim]none[/]";

                pluginTable.AddRow(
                    plugin.Metadata.Name,
                    plugin.Metadata.Version,
                    plugin.Metadata.Description,
                    commandList
                );
            }

            AnsiConsole.Write(pluginTable);
        }

        // Reporter Plugins (if any)
        if (reporters.Count > 0)
        {
            AnsiConsole.WriteLine();
            var reporterTable = new Table
            {
                Border = TableBorder.Rounded,
                Title = new TableTitle("[bold yellow]Reporters[/]")
            };

            reporterTable.AddColumn("Name");
            reporterTable.AddColumn("Version");
            reporterTable.AddColumn("Format");
            reporterTable.AddColumn("Description");

            foreach (var reporter in reporters.OrderBy(r => r.Metadata.Name))
            {
                reporterTable.AddRow(
                    reporter.Metadata.Name,
                    reporter.Metadata.Version,
                    $"[cyan]{reporter.OutputFormat}[/]",
                    reporter.Metadata.Description
                );
            }

            AnsiConsole.Write(reporterTable);
        }
    }
}

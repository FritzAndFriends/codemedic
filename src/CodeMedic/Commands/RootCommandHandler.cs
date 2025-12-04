using CodeMedic.Abstractions;
using CodeMedic.Output;
using CodeMedic.Utilities;

namespace CodeMedic.Commands;

/// <summary>
/// Root command handler for the CodeMedic CLI application.
/// Manages the main command structure and default behaviors.
/// </summary>
public class RootCommandHandler
{
    /// <summary>
    /// Processes command-line arguments and executes appropriate handler.
    /// </summary>
    public static async Task<int> ProcessArguments(string[] args)
    {
        var version = VersionUtility.GetVersion();
				var console = new ConsoleRenderer();

        // No arguments or help requested
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("help"))
        {
            console.RenderBanner(version);
            ConsoleRenderer.RenderHelp();
            return 0;
        }

        // Version requested
        if (args.Contains("--version") || args.Contains("-v") || args.Contains("version"))
        {
            ConsoleRenderer.RenderVersion(version);
            return 0;
        }

        // Health command
        if (args[0] == "health")
        {
            string? targetPath = null;
            string format = "console"; // default format

            // Parse arguments
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--format" && i + 1 < args.Length)
                {
                    format = args[i + 1].ToLower();
                    i++; // skip the format value
                }
                else if (!args[i].StartsWith("--"))
                {
                    targetPath = args[i];
                }
            }

            // Create appropriate renderer
            IRenderer renderer = format switch
            {
                "markdown" or "md" => new MarkdownRenderer(),
                "console" => console,
                _ => console
            };

            var command = new HealthCommand(renderer, targetPath);
            return await command.ExecuteAsync();
        }

        // Unknown command
        console.RenderError($"Unknown command: {args[0]}");
        ConsoleRenderer.RenderHelp();
        return 1;
    }
}

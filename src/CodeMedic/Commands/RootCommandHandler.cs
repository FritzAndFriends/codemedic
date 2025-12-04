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
    public static int ProcessArguments(string[] args)
    {
        var version = VersionUtility.GetVersion();

        // No arguments or help requested
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("help"))
        {
            ConsoleRenderer.RenderBanner(version);
            ConsoleRenderer.RenderHelp();
            return 0;
        }

        // Version requested
        if (args.Contains("--version") || args.Contains("-v") || args.Contains("version"))
        {
            ConsoleRenderer.RenderVersion(version);
            return 0;
        }

        // Unknown command
        ConsoleRenderer.RenderError($"Unknown command: {args[0]}");
        ConsoleRenderer.RenderHelp();
        return 1;
    }
}

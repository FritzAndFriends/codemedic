using Spectre.Console;

namespace CodeMedic.Output;

/// <summary>
/// Provides utilities for rendering output using Spectre.Console.
/// </summary>
public static class ConsoleRenderer
{
    /// <summary>
    /// Renders the application banner with title and version.
    /// </summary>
    public static void RenderBanner(string version)
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
            // Ignore clear failures (e.g., when output is piped)
        }

        var rule = new Rule("[bold cyan]CodeMedic[/]");
        AnsiConsole.Write(rule);

        AnsiConsole.MarkupLine($"[dim]v{version} - .NET Repository Health Analysis Tool[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders the help text with available commands.
    /// </summary>
    public static void RenderHelp()
    {
        var table = new Table
        {
            Border = TableBorder.Rounded,
            Title = new TableTitle("[bold]Available Commands[/]")
        };

        table.AddColumn("Command");
        table.AddColumn("Description");

        table.AddRow("[cyan]health[/]", "Display repository health dashboard");
        table.AddRow("[cyan]bom[/]", "Generate bill of materials report");
        table.AddRow("[cyan]version[/] or [cyan]-v[/], [cyan]--version[/]", "Display application version");
        table.AddRow("[cyan]help[/] or [cyan]-h[/], [cyan]--help[/]", "Display this help message");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Usage:[/]");
        AnsiConsole.MarkupLine("  [green]codemedic[/] [cyan]<command>[/] [yellow][[options]][/]");
        AnsiConsole.MarkupLine("  [green]codemedic[/] [cyan]--help[/]");
        AnsiConsole.MarkupLine("  [green]codemedic[/] [cyan]--version[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Examples:[/]");
        AnsiConsole.MarkupLine("  [green]codemedic health[/]");
        AnsiConsole.MarkupLine("  [green]codemedic bom --format json[/]");
        AnsiConsole.MarkupLine("  [green]codemedic --version[/]");
    }

    /// <summary>
    /// Renders a version information panel.
    /// </summary>
    public static void RenderVersion(string version)
    {
        var panel = new Panel($"[bold cyan]CodeMedic v{version}[/]")
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim].NET Repository Health Analysis Tool[/]");
    }

    /// <summary>
    /// Renders an error message.
    /// </summary>
    public static void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[red bold]✗ Error:[/] {message}");
    }

    /// <summary>
    /// Renders a success message.
    /// </summary>
    public static void RenderSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green bold]✓ Success:[/] {message}");
    }

    /// <summary>
    /// Renders an informational message.
    /// </summary>
    public static void RenderInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue bold]ℹ Info:[/] {message}");
    }
}

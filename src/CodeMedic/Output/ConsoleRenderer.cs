using Spectre.Console;
using CodeMedic.Abstractions;
using CodeMedic.Utilities;
using CodeMedic.Models.Report;

namespace CodeMedic.Output;

/// <summary>
/// Provides utilities for rendering output using Spectre.Console.
/// </summary>
public class ConsoleRenderer : IRenderer
{
    /// <summary>
    /// Renders the application banner with title and version.
    /// </summary>
    public void RenderBanner(string subtitle = "")
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

        var version = VersionUtility.GetVersion();
        AnsiConsole.MarkupLine($"[dim]v{version} - .NET Repository Health Analysis Tool[/]");
				 if (!string.IsNullOrWhiteSpace(subtitle))
				{
						AnsiConsole.MarkupLine($"[dim]{subtitle}[/]");
				}
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

        AnsiConsole.MarkupLine("[dim]Options:[/]");
        AnsiConsole.MarkupLine("  [yellow]--format <format>[/]  Output format: [cyan]console[/] (default), [cyan]markdown[/] (or [cyan]md[/])");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Examples:[/]");
        AnsiConsole.MarkupLine("  [green]codemedic health[/]");
        AnsiConsole.MarkupLine("  [green]codemedic health --format markdown[/]");
        AnsiConsole.MarkupLine("  [green]codemedic health --format md > report.md[/]");
        AnsiConsole.MarkupLine("  [green]codemedic bom[/]");
        AnsiConsole.MarkupLine("  [green]codemedic bom --format markdown[/]");
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
    public void RenderError(string message)
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
    public void RenderInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue bold]ℹ Info:[/] {message}");
    }

    /// <summary>
    /// Renders a section header.
    /// </summary>
    public void RenderSectionHeader(string title)
    {
        var rule = new Rule($"[bold yellow]{title}[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders a spinner with a label during an async operation.
    /// </summary>
    public async Task RenderWaitAsync(string message, Func<Task> operation)
    {
        await AnsiConsole.Status()
            .StartAsync(message, async ctx =>
            {
                await operation();
            });
    }

    /// <summary>
    /// Renders a report document to the console.
    /// </summary>
    public void RenderReport(object report)
    {
        if (report is not ReportDocument document)
        {
            RenderError($"Unsupported report type: {report?.GetType().Name ?? "null"}");
            return;
        }

        // Render each section
        foreach (var section in document.Sections)
        {
            RenderSection(section);
        }
    }

    /// <summary>
    /// Renders a report section.
    /// </summary>
    private void RenderSection(ReportSection section)
    {
        // Render section title based on level
        if (!string.IsNullOrWhiteSpace(section.Title))
        {
            if (section.Level == 1)
            {
                var rule = new Rule($"[bold yellow]{section.Title}[/]");
                AnsiConsole.Write(rule);
                AnsiConsole.WriteLine();
            }
            else if (section.Level == 2)
            {
                AnsiConsole.MarkupLine($"[cyan bold]{section.Title}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold]{section.Title}[/]");
            }
        }

        // Render each element in the section
        foreach (var element in section.Elements)
        {
            RenderElement(element);
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders a report element.
    /// </summary>
    private void RenderElement(IReportElement element)
    {
        switch (element)
        {
            case ReportParagraph paragraph:
                RenderParagraph(paragraph);
                break;
            case ReportTable table:
                RenderTable(table);
                break;
            case ReportKeyValueList kvList:
                RenderKeyValueList(kvList);
                break;
            case ReportList list:
                RenderList(list);
                break;
            case ReportSection section:
                RenderSection(section);
                break;
            default:
                AnsiConsole.MarkupLine($"[dim]Unsupported element type: {element.GetType().Name}[/]");
                break;
        }
    }

    /// <summary>
    /// Renders a paragraph.
    /// </summary>
    private void RenderParagraph(ReportParagraph paragraph)
    {
        var markup = ApplyTextStyle(paragraph.Text, paragraph.Style);
        AnsiConsole.MarkupLine(markup);
    }

    /// <summary>
    /// Renders a table.
    /// </summary>
    private void RenderTable(ReportTable reportTable)
    {
        var table = new Table
        {
            Border = TableBorder.Rounded,
            Expand = true
        };

        if (!string.IsNullOrWhiteSpace(reportTable.Title))
        {
            table.Title = new TableTitle($"[bold]{reportTable.Title}[/]");
        }

        // Add columns (wrap long-text columns; keep short columns compact)
        var noWrapHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Version",
            "Latest",
            "Type",
            "License",
            "Source",
            "Comm",
            "Severity",
            "Score",
            "Count",
            "Status"
        };

        foreach (var header in reportTable.Headers)
        {
            var column = new TableColumn(header);
            if (noWrapHeaders.Contains(header))
            {
                column.NoWrap();
            }

            table.AddColumn(column);
        }

        // Add rows
        foreach (var row in reportTable.Rows)
        {
            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders a key-value list.
    /// </summary>
    private void RenderKeyValueList(ReportKeyValueList kvList)
    {
        if (!string.IsNullOrWhiteSpace(kvList.Title))
        {
            AnsiConsole.MarkupLine($"[bold]{kvList.Title}[/]");
        }

        foreach (var item in kvList.Items)
        {
            var valueMarkup = ApplyTextStyle(item.Value, item.ValueStyle);
            AnsiConsole.MarkupLine($"[dim]{item.Key}:[/] {valueMarkup}");
        }
    }

    /// <summary>
    /// Renders a list.
    /// </summary>
    private void RenderList(ReportList list)
    {
        if (!string.IsNullOrWhiteSpace(list.Title))
        {
            AnsiConsole.MarkupLine($"[dim]{list.Title}[/]");
        }

        for (int i = 0; i < list.Items.Count; i++)
        {
            var bullet = list.IsOrdered ? $"{i + 1}." : "•";
            AnsiConsole.MarkupLine($"  {bullet} {list.Items[i]}");
        }
    }

    /// <summary>
    /// Applies text style markup to a string.
    /// </summary>
    private string ApplyTextStyle(string text, TextStyle style)
    {
        return style switch
        {
            TextStyle.Bold => $"[bold]{text}[/]",
            TextStyle.Italic => $"[italic]{text}[/]",
            TextStyle.Code => $"[cyan]{text}[/]",
            TextStyle.Success => $"[green]{text}[/]",
            TextStyle.Warning => $"[yellow]{text}[/]",
            TextStyle.Error => $"[red]{text}[/]",
            TextStyle.Info => $"[blue]{text}[/]",
            TextStyle.Dim => $"[dim]{text}[/]",
            _ => text
        };
    }

    /// <summary>
    /// Renders a footer message (not implemented for console output).
    /// </summary>
	public void RenderFooter(string footer)
	{
		// do nothing
	}

}

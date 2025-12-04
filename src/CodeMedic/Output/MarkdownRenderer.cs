using System.Text;
using CodeMedic.Abstractions;
using CodeMedic.Models.Report;

namespace CodeMedic.Output;

/// <summary>
/// Renders output in Markdown format.
/// </summary>
public class MarkdownRenderer : IRenderer
{
    private readonly StringBuilder _output = new();
    private readonly TextWriter _writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownRenderer"/> class.
    /// </summary>
    /// <param name="writer">The text writer to output to. Defaults to Console.Out.</param>
    public MarkdownRenderer(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
    }

    /// <summary>
    /// Renders the application banner with title and version.
    /// </summary>
    public void RenderBanner(string subtitle = "")
    {
        _output.AppendLine("# CodeMedic");
        _output.AppendLine();
        _output.AppendLine("*.NET Repository Health Analysis Tool*");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            _output.AppendLine();
            _output.AppendLine($"*{subtitle}*");
        }
        _output.AppendLine();
    }

    /// <summary>
    /// Renders an error message.
    /// </summary>
    public void RenderError(string message)
    {
        _output.AppendLine($"**❌ Error:** {message}");
        _output.AppendLine();
    }

    /// <summary>
    /// Renders an informational message.
    /// </summary>
    public void RenderInfo(string message)
    {
        _output.AppendLine($"**ℹ️ Info:** {message}");
        _output.AppendLine();
    }

    /// <summary>
    /// Renders a section header.
    /// </summary>
    public void RenderSectionHeader(string title)
    {
        _output.AppendLine($"## {title}");
        _output.AppendLine();
    }

    /// <summary>
    /// Renders a spinner with a label during an async operation.
    /// </summary>
    public async Task RenderWaitAsync(string message, Func<Task> operation)
    {
        // For markdown, we just execute the operation without visual feedback
        await operation();
    }

    /// <summary>
    /// Renders a report document to markdown.
    /// </summary>
    public void RenderReport(object report)
    {
        if (report is not ReportDocument document)
        {
            RenderError($"Unsupported report type: {report?.GetType().Name ?? "null"}");
            return;
        }

        // Render metadata if present
        if (document.Metadata.Count > 0)
        {
            _output.AppendLine("---");
            foreach (var kvp in document.Metadata)
            {
                _output.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            _output.AppendLine("---");
            _output.AppendLine();
        }

        // Render each section
        foreach (var section in document.Sections)
        {
            RenderSection(section);
        }

        // Flush output
        Flush();
    }

    /// <summary>
    /// Renders a report section.
    /// </summary>
    private void RenderSection(ReportSection section)
    {
        // Render section title based on level
        if (!string.IsNullOrWhiteSpace(section.Title))
        {
            var headerPrefix = new string('#', section.Level + 1); // +1 because banner is H1
            _output.AppendLine($"{headerPrefix} {section.Title}");
            _output.AppendLine();
        }

        // Render each element in the section
        foreach (var element in section.Elements)
        {
            RenderElement(element);
        }
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
                _output.AppendLine($"*Unsupported element type: {element.GetType().Name}*");
                _output.AppendLine();
                break;
        }
    }

    /// <summary>
    /// Renders a paragraph.
    /// </summary>
    private void RenderParagraph(ReportParagraph paragraph)
    {
        var text = ApplyTextStyle(paragraph.Text, paragraph.Style);
        _output.AppendLine(text);
        _output.AppendLine();
    }

    /// <summary>
    /// Renders a table.
    /// </summary>
    private void RenderTable(ReportTable reportTable)
    {
        if (!string.IsNullOrWhiteSpace(reportTable.Title))
        {
            _output.AppendLine($"**{reportTable.Title}**");
            _output.AppendLine();
        }

        if (reportTable.Headers.Count == 0 || reportTable.Rows.Count == 0)
        {
            return;
        }

        // Table header
        _output.Append("| ");
        _output.Append(string.Join(" | ", reportTable.Headers));
        _output.AppendLine(" |");

        // Separator row
        _output.Append("| ");
        _output.Append(string.Join(" | ", reportTable.Headers.Select(_ => "---")));
        _output.AppendLine(" |");

        // Table rows
        foreach (var row in reportTable.Rows)
        {
            _output.Append("| ");
            _output.Append(string.Join(" | ", row.Select(EscapeMarkdown)));
            _output.AppendLine(" |");
        }

        _output.AppendLine();
    }

    /// <summary>
    /// Renders a key-value list.
    /// </summary>
    private void RenderKeyValueList(ReportKeyValueList kvList)
    {
        if (!string.IsNullOrWhiteSpace(kvList.Title))
        {
            _output.AppendLine($"**{kvList.Title}**");
            _output.AppendLine();
        }

        foreach (var item in kvList.Items)
        {
            var value = ApplyTextStyle(item.Value, item.ValueStyle);
            _output.AppendLine($"- **{item.Key}:** {value}");
        }

        _output.AppendLine();
    }

    /// <summary>
    /// Renders a list.
    /// </summary>
    private void RenderList(ReportList list)
    {
        if (!string.IsNullOrWhiteSpace(list.Title))
        {
            _output.AppendLine($"**{list.Title}**");
            _output.AppendLine();
        }

        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.IsOrdered)
            {
                _output.AppendLine($"{i + 1}. {list.Items[i]}");
            }
            else
            {
                _output.AppendLine($"- {list.Items[i]}");
            }
        }

        _output.AppendLine();
    }

    /// <summary>
    /// Applies text style formatting to markdown.
    /// </summary>
    private string ApplyTextStyle(string text, TextStyle style)
    {
        return style switch
        {
            TextStyle.Bold => $"**{text}**",
            TextStyle.Italic => $"*{text}*",
            TextStyle.Code => $"`{text}`",
            TextStyle.Success => $"✅ {text}",
            TextStyle.Warning => $"⚠️ {text}",
            TextStyle.Error => $"❌ {text}",
            TextStyle.Info => $"ℹ️ {text}",
            TextStyle.Dim => $"*{text}*",
            _ => text
        };
    }

    /// <summary>
    /// Escapes special markdown characters in table cells.
    /// </summary>
    private string EscapeMarkdown(string text)
    {
        return text.Replace("|", "\\|").Replace("\n", " ");
    }

    /// <summary>
    /// Renders a footer message.
    /// </summary>
    public void RenderFooter(string footer)
    {
        if (!string.IsNullOrWhiteSpace(footer))
        {
            _output.AppendLine("---");
            _output.AppendLine();
            _output.AppendLine(footer);
            _output.AppendLine();
        }
    }

    /// <summary>
    /// Flushes the accumulated output to the writer.
    /// </summary>
    private void Flush()
    {
        _writer.Write(_output.ToString());
        _writer.Flush();
        _output.Clear();
    }
}

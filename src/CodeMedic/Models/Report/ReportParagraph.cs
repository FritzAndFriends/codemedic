namespace CodeMedic.Models.Report;

/// <summary>
/// Represents a paragraph of text in a report.
/// </summary>
public class ReportParagraph : IReportElement
{
    /// <summary>
    /// Gets or sets the paragraph text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text style/emphasis.
    /// </summary>
    public TextStyle Style { get; set; } = TextStyle.Normal;

    public ReportParagraph() { }

    public ReportParagraph(string text, TextStyle style = TextStyle.Normal)
    {
        Text = text;
        Style = style;
    }
}

/// <summary>
/// Text style options for report content.
/// </summary>
public enum TextStyle
{
    Normal,
    Bold,
    Italic,
    Code,
    Success,
    Warning,
    Error,
    Info,
    Dim
}

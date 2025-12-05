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

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportParagraph"/> class.
    /// </summary>
    public ReportParagraph() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportParagraph"/> class with text and style.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <param name="style">The text style to apply.</param>
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
    /// <summary>Normal text style.</summary>
    Normal,

    /// <summary>Bold text style.</summary>
    Bold,

    /// <summary>Italic text style.</summary>
    Italic,

    /// <summary>Code/monospace text style.</summary>
    Code,

    /// <summary>Success indicator style (typically green).</summary>
    Success,

    /// <summary>Warning indicator style (typically yellow).</summary>
    Warning,

    /// <summary>Error indicator style (typically red).</summary>
    Error,

    /// <summary>Information style (typically blue).</summary>
    Info,

    /// <summary>Dim/subdued text style.</summary>
    Dim
}

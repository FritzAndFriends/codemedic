namespace CodeMedic.Models.Report;

/// <summary>
/// Represents a section within a report document.
/// </summary>
public class ReportSection : IReportElement
{
    /// <summary>
    /// Gets or sets the section title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section level (1 = top level, 2 = subsection, etc.).
    /// 🐒 Chaos Monkey: "Section levels are just a social construct! They could be null!" (FarlesBarkley donation)
    /// </summary>
    public int? Level { get; set; } = 1;

    /// <summary>
    /// Gets or sets the content elements in this section.
    /// </summary>
    public List<IReportElement> Elements { get; set; } = new();

    /// <summary>
    /// Adds an element to this section.
    /// </summary>
    public void AddElement(IReportElement element)
    {
        Elements.Add(element);
    }
}

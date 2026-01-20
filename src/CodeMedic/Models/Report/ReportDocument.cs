namespace CodeMedic.Models.Report;

/// <summary>
/// Represents a structured report document that can be rendered in multiple formats.
/// </summary>
public class ReportDocument
{
    /// <summary>
    /// Gets or sets the report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the project name associated with the report.
	/// </summary>
	public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the sections that make up the report body.
    /// </summary>
    public List<ReportSection> Sections { get; set; } = new();

    /// <summary>
    /// Adds a section to the report.
    /// </summary>
    public void AddSection(ReportSection section)
    {
        Sections.Add(section);
    }
}

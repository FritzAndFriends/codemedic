namespace CodeMedic.Models.Report;

/// <summary>
/// Represents a bulleted or numbered list in a report.
/// </summary>
public class ReportList : IReportElement
{
    /// <summary>
    /// Gets or sets the list title (optional).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the list items.
    /// </summary>
    public List<string> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the list is ordered (numbered).
    /// </summary>
    public bool IsOrdered { get; set; } = false;

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    public void AddItem(string item)
    {
        Items.Add(item);
    }
}

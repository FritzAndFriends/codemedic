namespace CodeMedic.Models.Report;

/// <summary>
/// Represents a table in a report.
/// </summary>
public class ReportTable : IReportElement
{
    /// <summary>
    /// Gets or sets the table title (optional).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the column headers.
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the table rows (each row is a list of cell values).
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    public void AddRow(params string[] cells)
    {
        Rows.Add(new List<string>(cells));
    }
}

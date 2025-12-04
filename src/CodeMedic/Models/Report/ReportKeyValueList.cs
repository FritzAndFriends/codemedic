namespace CodeMedic.Models.Report;

/// <summary>
/// Represents a list of key-value pairs in a report.
/// </summary>
public class ReportKeyValueList : IReportElement
{
    /// <summary>
    /// Gets or sets the title of the key-value list (optional).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the items in the list.
    /// </summary>
    public List<KeyValueItem> Items { get; set; } = new();

    /// <summary>
    /// Adds a key-value item to the list.
    /// </summary>
    public void Add(string key, string value, TextStyle valueStyle = TextStyle.Normal)
    {
        Items.Add(new KeyValueItem { Key = key, Value = value, ValueStyle = valueStyle });
    }
}

/// <summary>
/// Represents a single key-value pair.
/// </summary>
public class KeyValueItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public TextStyle ValueStyle { get; set; } = TextStyle.Normal;
}

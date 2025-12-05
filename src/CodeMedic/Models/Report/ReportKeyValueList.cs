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
    /// <summary>
    /// Gets or sets the key of the pair.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the pair.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text style to apply to the value.
    /// </summary>
    public TextStyle ValueStyle { get; set; } = TextStyle.Normal;
}

namespace CodeMedic.Models;

/// <summary>
/// Represents a project-to-project reference within a repository.
/// </summary>
public class ProjectReference
{
    /// <summary>
    /// Gets or sets the name of the referenced project.
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the referenced project file.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a private asset (not exposed to dependent projects).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the reference.
    /// </summary>
    public string? Metadata { get; set; }
}

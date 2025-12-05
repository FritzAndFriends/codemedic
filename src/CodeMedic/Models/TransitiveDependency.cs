namespace CodeMedic.Models;

/// <summary>
/// Represents a transitive (indirect) NuGet package dependency.
/// Transitive dependencies are packages that are included as a side effect of 
/// adding a direct dependency that itself depends on other packages.
/// </summary>
public class TransitiveDependency
{
    /// <summary>
    /// Gets or sets the name of the transitive package.
    /// </summary>
    public required string PackageName { get; set; }

    /// <summary>
    /// Gets or sets the version of the transitive package.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the name of the direct dependency that introduced this transitive dependency.
    /// </summary>
    public string? SourcePackage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this transitive dependency is marked as private (PrivateAssets="All").
    /// Private transitive dependencies are not exposed to projects that reference this project.
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Gets or sets the depth in the dependency chain (1 = direct dependency of source package).
    /// </summary>
    public int Depth { get; set; }
}

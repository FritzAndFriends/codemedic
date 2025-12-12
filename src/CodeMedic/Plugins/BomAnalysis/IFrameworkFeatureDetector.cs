namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Interface for framework feature detectors that identify specific patterns or technologies in use.
/// </summary>
public interface IFrameworkFeatureDetector
{
    /// <summary>
    /// Gets the category name for this detector (e.g., "Web Framework Features", "Data Access").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the display order for this category in reports (lower numbers appear first).
    /// </summary>
    int DisplayOrder { get; }

    /// <summary>
    /// Detects features based on the provided package information.
    /// </summary>
    /// <param name="packages">Collection of all packages used across projects.</param>
    /// <returns>Collection of detected framework features.</returns>
    IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages);
}

/// <summary>
/// Represents a detected framework feature.
/// </summary>
public class FrameworkFeature
{
    /// <summary>
    /// Gets or sets the feature name (e.g., "ASP.NET Core MVC", "Entity Framework Core").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the package that provides this feature.
    /// </summary>
    public required string Package { get; init; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the projects using this feature.
    /// </summary>
    public required List<string> Projects { get; init; }

    /// <summary>
    /// Gets or sets optional additional information about the feature.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets optional documentation link.
    /// </summary>
    public string? DocumentationUrl { get; init; }
}

/// <summary>
/// Package information exposed to feature detectors.
/// </summary>
public class PackageInfo
{
    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// Gets or sets whether this is a direct dependency.
    /// </summary>
    public required bool IsDirect { get; init; }
    
    /// <summary>
    /// Gets or sets the list of projects using this package.
    /// </summary>
    public required List<string> Projects { get; init; }
}

namespace CodeMedic.Models;

/// <summary>
/// Represents information about a .NET project discovered during scanning.
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// Gets or sets the project file path (absolute).
    /// </summary>
    public required string ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the project name (filename without extension).
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the relative path from the repository root.
    /// </summary>
    public required string RelativePath { get; set; }

    /// <summary>
    /// Gets or sets the target framework(s) (e.g., "net10.0", "net8.0;net10.0").
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the project output type (e.g., "Exe", "Library", "Web").
    /// </summary>
    public string? OutputType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether nullable reference types are enabled.
    /// </summary>
    public bool NullableEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether implicit usings are enabled.
    /// </summary>
    public bool ImplicitUsingsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the language version (e.g., "12", "13").
    /// </summary>
    public string? LanguageVersion { get; set; }

    /// <summary>
    /// Gets or sets the list of direct NuGet package dependencies.
    /// </summary>
    public List<Package> PackageDependencies { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of project references.
    /// </summary>
    public int ProjectReferenceCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the project generates documentation.
    /// </summary>
    public bool GeneratesDocumentation { get; set; }

    /// <summary>
    /// Gets or sets any parsing errors encountered while reading the project.
    /// </summary>
    public List<string> ParseErrors { get; set; } = [];

    /// <summary>
    /// Gets the display name for the project.
    /// </summary>
    public string DisplayName => $"{ProjectName} ({RelativePath})";
}

/// <summary>
/// Represents a NuGet package dependency with its name and version.
/// </summary>
/// <param name="Name">The name of the package.</param>
/// <param name="Version">The version of the package.</param>
public record struct Package(string Name, string Version);

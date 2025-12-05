using System.Xml.Linq;
using CodeMedic.Models;
using CodeMedic.Models.Report;

namespace CodeMedic.Engines;

/// <summary>
/// Scans a directory tree for .NET projects and collects initial health information.
/// </summary>
public class RepositoryScanner
{
    private readonly string _rootPath;
    private readonly List<ProjectInfo> _projects = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryScanner"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory to scan. Defaults to current directory if null or empty.</param>
    public RepositoryScanner(string? rootPath = null)
    {
        _rootPath = string.IsNullOrWhiteSpace(rootPath) 
            ? Directory.GetCurrentDirectory() 
            : Path.GetFullPath(rootPath);
    }

    /// <summary>
    /// Scans the repository for all .NET projects.
    /// </summary>
    /// <returns>A list of discovered projects.</returns>
    public async Task<List<ProjectInfo>> ScanAsync()
    {
        _projects.Clear();

        try
        {
            var projectFiles = Directory.EnumerateFiles(
                _rootPath,
                "*.csproj",
                SearchOption.AllDirectories);

            foreach (var projectFile in projectFiles)
            {
                await ParseProjectAsync(projectFile);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - we want partial results if possible
            Console.Error.WriteLine($"Error scanning repository: {ex.Message}");
        }

        return _projects;
    }

    /// <summary>
    /// Gets the count of discovered projects.
    /// </summary>
    public int ProjectCount => _projects.Count;

    /// <summary>
    /// Gets all discovered projects.
    /// </summary>
    public IReadOnlyList<ProjectInfo> Projects => _projects.AsReadOnly();

    /// <summary>
    /// Generates a report document from the scanned projects.
    /// </summary>
    /// <returns>A structured report document ready for rendering.</returns>
    public ReportDocument GenerateReport()
    {
        var report = new ReportDocument
        {
            Title = "Repository Health Dashboard"
        };

        report.Metadata["ScanTime"] = DateTime.UtcNow.ToString("u");
        report.Metadata["RootPath"] = _rootPath;

        var totalProjects = _projects.Count;
        var totalPackages = _projects.Sum(p => p.PackageDependencies.Count);
        var projectsWithNullable = _projects.Count(p => p.NullableEnabled);
        var projectsWithImplicitUsings = _projects.Count(p => p.ImplicitUsingsEnabled);
        var projectsWithDocumentation = _projects.Count(p => p.GeneratesDocumentation);
        var projectsWithErrors = _projects.Where(p => p.ParseErrors.Count > 0).ToList();

        // Summary section
        var summarySection = new ReportSection
        {
            Title = "Summary",
            Level = 1
        };

        summarySection.AddElement(new ReportParagraph(
            $"Found {totalProjects} project(s)",
            totalProjects > 0 ? TextStyle.Bold : TextStyle.Warning
        ));

        if (totalProjects > 0)
        {
            var summaryKvList = new ReportKeyValueList();
            // this is redundant
						// summaryKvList.Add("Total Projects", totalProjects.ToString());
            summaryKvList.Add("Total NuGet Packages", totalPackages.ToString());
            summaryKvList.Add("Projects without Nullable", (totalProjects - projectsWithNullable).ToString(),
                (totalProjects - projectsWithNullable) > 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects without Implicit Usings", (totalProjects - projectsWithImplicitUsings).ToString(),
                (totalProjects - projectsWithImplicitUsings) > 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects missing Documentation", (totalProjects - projectsWithDocumentation).ToString(),
                (totalProjects - projectsWithDocumentation) > 0 ? TextStyle.Success : TextStyle.Warning);
            summarySection.AddElement(summaryKvList);
        }

        report.AddSection(summarySection);

        // Projects table section
        if (totalProjects > 0)
        {
            var projectsSection = new ReportSection
            {
                Title = "Projects",
                Level = 1
            };

            var projectsTable = new ReportTable
            {
                Title = "Projects Summary"
            };

            projectsTable.Headers.AddRange(new[]
            {
                "Project Name",
                "Path",
                "Framework",
                "Output Type",
                "Packages",
                "Settings"
            });

            foreach (var project in _projects)
            {
                var settings = new List<string>();
                if (project.NullableEnabled) settings.Add("✓N");
                if (project.ImplicitUsingsEnabled) settings.Add("✓U");
                if (project.GeneratesDocumentation) settings.Add("✓D");

                projectsTable.AddRow(
                    project.ProjectName,
                    project.RelativePath,
                    project.TargetFramework ?? "unknown",
                    project.OutputType ?? "unknown",
                    project.PackageDependencies.Count.ToString(),
                    settings.Count > 0 ? string.Join(" ", settings) : "-"
                );
            }

            projectsSection.AddElement(projectsTable);

            var legend = new ReportParagraph("Legend: N=Nullable, U=ImplicitUsings, D=Documentation", TextStyle.Dim);
            projectsSection.AddElement(legend);

            report.AddSection(projectsSection);

            // Project details section
            var detailsSection = new ReportSection
            {
                Title = "Project Details",
                Level = 1
            };

            foreach (var project in _projects)
            {
                var projectSubSection = new ReportSection
                {
                    Title = project.ProjectName,
                    Level = 2
                };

                var detailsKvList = new ReportKeyValueList();
                detailsKvList.Add("Path", project.RelativePath);
                detailsKvList.Add("Output Type", project.OutputType ?? "unknown");
                detailsKvList.Add("Target Framework", project.TargetFramework ?? "unknown");
                detailsKvList.Add("Language Version", project.LanguageVersion ?? "default");
                detailsKvList.Add("Nullable Enabled", project.NullableEnabled ? "✓" : "✗",
                    project.NullableEnabled ? TextStyle.Success : TextStyle.Warning);
                detailsKvList.Add("Implicit Usings", project.ImplicitUsingsEnabled ? "✓" : "✗",
                    project.ImplicitUsingsEnabled ? TextStyle.Success : TextStyle.Warning);
                detailsKvList.Add("Documentation", project.GeneratesDocumentation ? "✓" : "✗",
                    project.GeneratesDocumentation ? TextStyle.Success : TextStyle.Warning);

                projectSubSection.AddElement(detailsKvList);

                if (project.PackageDependencies.Count > 0)
                {
                    var packagesList = new ReportList
                    {
                        Title = $"NuGet Packages ({project.PackageDependencies.Count})"
                    };

                    foreach (var pkg in project.PackageDependencies.Take(5))
                    {
                        packagesList.AddItem($"{pkg.Name} ({pkg.Version})");
                    }

                    if (project.PackageDependencies.Count > 5)
                    {
                        packagesList.AddItem($"... and {project.PackageDependencies.Count - 5} more");
                    }

                    projectSubSection.AddElement(packagesList);
                }

                if (project.ProjectReferenceCount > 0)
                {
                    projectSubSection.AddElement(new ReportParagraph(
                        $"Project References: {project.ProjectReferenceCount}",
                        TextStyle.Info
                    ));
                }

                detailsSection.Elements.Add(projectSubSection);
            }

            report.AddSection(detailsSection);
        }
        else
        {
            var noProjectsSection = new ReportSection
            {
                Title = "Notice",
                Level = 1
            };
            noProjectsSection.AddElement(new ReportParagraph(
                "⚠ No .NET projects found in the repository.",
                TextStyle.Warning
            ));
            report.AddSection(noProjectsSection);
        }

        // Parse errors section
        if (projectsWithErrors.Count > 0)
        {
            var errorsSection = new ReportSection
            {
                Title = "Parse Errors",
                Level = 1
            };

            foreach (var project in projectsWithErrors)
            {
                var errorList = new ReportList
                {
                    Title = project.ProjectName
                };

                foreach (var error in project.ParseErrors)
                {
                    errorList.AddItem(error);
                }

                errorsSection.AddElement(errorList);
            }

            report.AddSection(errorsSection);
        }

        return report;
    }

    private async Task ParseProjectAsync(string projectFilePath)
    {
        try
        {
            var projectInfo = new ProjectInfo
            {
                ProjectPath = projectFilePath,
                ProjectName = Path.GetFileNameWithoutExtension(projectFilePath),
                RelativePath = Path.GetRelativePath(_rootPath, projectFilePath)
            };

            // Parse the project file XML
            var doc = XDocument.Load(projectFilePath);
            var ns = doc.Root?.Name.NamespaceName ?? "";
            var root = doc.Root;

            if (root == null)
            {
                projectInfo.ParseErrors.Add("Project file has no root element");
                _projects.Add(projectInfo);
                return;
            }

            // Extract PropertyGroup settings
            var propertyGroup = root.Descendants(XName.Get("PropertyGroup", ns)).FirstOrDefault();
            if (propertyGroup != null)
            {
                projectInfo.TargetFramework = propertyGroup.Element(XName.Get("TargetFramework", ns))?.Value;
                projectInfo.OutputType = propertyGroup.Element(XName.Get("OutputType", ns))?.Value;

								// If output type is not specified, default to Library
								if (string.IsNullOrWhiteSpace(projectInfo.OutputType))
								{
									projectInfo.OutputType = "Library";
								}

                var nullableElement = propertyGroup.Element(XName.Get("Nullable", ns));
                projectInfo.NullableEnabled = nullableElement?.Value?.ToLower() == "enable";

                var implicitUsingsElement = propertyGroup.Element(XName.Get("ImplicitUsings", ns));
                projectInfo.ImplicitUsingsEnabled = implicitUsingsElement?.Value?.ToLower() == "enable";

                projectInfo.LanguageVersion = propertyGroup.Element(XName.Get("LangVersion", ns))?.Value;

                var docElement = propertyGroup.Element(XName.Get("GenerateDocumentationFile", ns));
                projectInfo.GeneratesDocumentation = docElement?.Value?.ToLower() == "true";
            }

            // Count package references
            var packageReferences = root.Descendants(XName.Get("PackageReference", ns)).ToList();
            projectInfo.PackageDependencies = packageReferences
                .Select(pr => new Package(
										pr.Attribute("Include")?.Value ?? "unknown",
										pr.Attribute("Version")?.Value ?? "unknown"))
                .ToList();

            // Count project references
            projectInfo.ProjectReferenceCount = root.Descendants(XName.Get("ProjectReference", ns)).Count();

            _projects.Add(projectInfo);
        }
        catch (Exception ex)
        {
            var projectInfo = new ProjectInfo
            {
                ProjectPath = projectFilePath,
                ProjectName = Path.GetFileNameWithoutExtension(projectFilePath),
                RelativePath = Path.GetRelativePath(_rootPath, projectFilePath),
                ParseErrors = [ex.Message]
            };

            _projects.Add(projectInfo);
        }
    }
}

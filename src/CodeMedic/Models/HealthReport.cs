using CodeMedic.Models.Report;

namespace CodeMedic.Models;

/// <summary>
/// Represents a repository health report containing structured data for rendering.
/// </summary>
public class HealthReport
{
    /// <summary>
    /// Gets or sets the root path that was scanned.
    /// </summary>
    public required string RootPath { get; set; }

    /// <summary>
    /// Gets or sets the total number of projects found.
    /// </summary>
    public int TotalProjects { get; set; }

    /// <summary>
    /// Gets or sets the list of projects discovered.
    /// </summary>
    public List<ProjectInfo> Projects { get; set; } = [];

    /// <summary>
    /// Gets the projects that had parse errors.
    /// </summary>
    public IEnumerable<ProjectInfo> ProjectsWithErrors => Projects.Where(p => p.ParseErrors.Count > 0);

    /// <summary>
    /// Gets a value indicating whether any parse errors were encountered.
    /// </summary>
    public bool HasErrors => ProjectsWithErrors.Any();

    /// <summary>
    /// Gets the total count of NuGet packages across all projects.
    /// </summary>
    public int TotalPackages => Projects.Sum(p => p.PackageDependencies.Count);

    /// <summary>
    /// Gets the count of projects with nullable enabled.
    /// </summary>
    public int ProjectsWithNullableEnabled => Projects.Count(p => p.NullableEnabled);

    /// <summary>
    /// Gets the count of projects with implicit usings enabled.
    /// </summary>
    public int ProjectsWithImplicitUsings => Projects.Count(p => p.ImplicitUsingsEnabled);

    /// <summary>
    /// Gets the count of projects that generate documentation.
    /// </summary>
    public int ProjectsWithDocumentation => Projects.Count(p => p.GeneratesDocumentation);

    /// <summary>
    /// Gets the scan timestamp.
    /// </summary>
    public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Converts the health report into a structured report document.
    /// </summary>
    public ReportDocument ToReportDocument()
    {
        var report = new ReportDocument
        {
            Title = "Repository Health Dashboard"
        };

        report.Metadata["ScanTime"] = ScanTimestamp.ToString("u");
        report.Metadata["RootPath"] = RootPath;

        // Summary section
        var summarySection = new ReportSection
        {
            Title = "Summary",
            Level = 1
        };

        summarySection.AddElement(new ReportParagraph(
            $"Found {TotalProjects} project(s)",
            TotalProjects > 0 ? TextStyle.Bold : TextStyle.Warning
        ));

        if (TotalProjects > 0)
        {
            var summaryKvList = new ReportKeyValueList();
            summaryKvList.Add("Total Projects", TotalProjects.ToString());
            summaryKvList.Add("Total NuGet Packages", TotalPackages.ToString());
            summaryKvList.Add("Projects with Nullable", ProjectsWithNullableEnabled.ToString(), 
                ProjectsWithNullableEnabled > 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects with Implicit Usings", ProjectsWithImplicitUsings.ToString(),
                ProjectsWithImplicitUsings > 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects with Documentation", ProjectsWithDocumentation.ToString(),
                ProjectsWithDocumentation > 0 ? TextStyle.Success : TextStyle.Warning);
            summarySection.AddElement(summaryKvList);
        }

        report.AddSection(summarySection);

        // Projects table section
        if (TotalProjects > 0)
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

            foreach (var project in Projects)
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

            foreach (var project in Projects)
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

                // Display project references
                if (project.ProjectReferences.Count > 0)
                {
                    var projectRefsList = new ReportList
                    {
                        Title = $"Project References ({project.ProjectReferences.Count})"
                    };

                    foreach (var projRef in project.ProjectReferences)
                    {
                        var refLabel = $"{projRef.ProjectName}";
                        if (projRef.IsPrivate)
                        {
                            refLabel += " [Private]";
                        }
                        projectRefsList.AddItem(refLabel);
                    }

                    projectSubSection.AddElement(projectRefsList);
                }

                // Display transitive dependencies
                if (project.TransitiveDependencies.Count > 0)
                {
                    var transitiveDeps = new ReportList
                    {
                        Title = $"Transitive Dependencies ({project.TransitiveDependencies.Count})"
                    };

                    foreach (var transDep in project.TransitiveDependencies.Take(5))
                    {
                        var depLabel = $"{transDep.PackageName} ({transDep.Version})";
                        if (transDep.IsPrivate)
                        {
                            depLabel += " [Private]";
                        }
                        transitiveDeps.AddItem(depLabel);
                    }

                    if (project.TransitiveDependencies.Count > 5)
                    {
                        transitiveDeps.AddItem($"... and {project.TransitiveDependencies.Count - 5} more");
                    }

                    projectSubSection.AddElement(transitiveDeps);
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
        if (HasErrors)
        {
            var errorsSection = new ReportSection
            {
                Title = "Parse Errors",
                Level = 1
            };

            foreach (var project in ProjectsWithErrors)
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
}


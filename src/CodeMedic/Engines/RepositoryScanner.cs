using System.Xml.Linq;
using System.Text.Json;
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
            // First, restore packages to ensure lock/assets files are generated
            await RestorePackagesAsync();

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
    /// Restores NuGet packages for the repository to generate lock/assets files.
    /// </summary>
    private async Task RestorePackagesAsync()
    {
        try
        {
            Console.Error.WriteLine("Restoring NuGet packages...");
            
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{_rootPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0)
                {
                    Console.Error.WriteLine("Package restore completed successfully.");
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    Console.Error.WriteLine($"Package restore completed with warnings/errors: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not restore packages - {ex.Message}. Proceeding with scan...");
            // Don't throw - we'll work with whatever assets files already exist
        }
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
        var totalLinesOfCode = _projects.Sum(p => p.TotalLinesOfCode);
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
            summaryKvList.Add("Total Lines of Code", totalLinesOfCode.ToString());
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
                "Lines of Code",
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
                    project.TotalLinesOfCode.ToString(),
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
                detailsKvList.Add("Lines of Code", project.TotalLinesOfCode.ToString());
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

            // Count lines of code in C# files
            projectInfo.TotalLinesOfCode = CountLinesOfCode(projectFilePath);

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

            // Extract project references with metadata
            var projectReferenceElements = root.Descendants(XName.Get("ProjectReference", ns)).ToList();
            projectInfo.ProjectReferences = projectReferenceElements
                .Select(prElement => new ProjectReference
                {
                    ProjectName = Path.GetFileNameWithoutExtension(prElement.Attribute("Include")?.Value ?? "unknown"),
                    Path = prElement.Attribute("Include")?.Value ?? "unknown",
                    IsPrivate = prElement.Attribute("PrivateAssets")?.Value?.ToLower() == "all",
                    Metadata = prElement.Attribute("Condition")?.Value
                })
                .ToList();

            // Extract transitive dependencies from lock or assets file
            projectInfo.TransitiveDependencies = ExtractTransitiveDependencies(projectFilePath, projectInfo.PackageDependencies, projectInfo.ProjectReferences);

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

    /// <summary>
    /// Extracts transitive dependencies from packages.lock.json or project.assets.json file.
    /// Transitive dependencies are packages that are pulled in by direct dependencies.
    /// Project references are excluded from the results as they are not NuGet packages.
    /// </summary>
    private List<TransitiveDependency> ExtractTransitiveDependencies(string projectFilePath, List<Package> directDependencies, List<ProjectReference> projectReferences)
    {
        var transitiveDeps = new List<TransitiveDependency>();
        var projectDir = Path.GetDirectoryName(projectFilePath) ?? "";
        var projectRefNames = projectReferences.Select(pr => pr.ProjectName.ToLower()).ToHashSet();

        // First, try to read packages.lock.json (if lock file is enabled)
        var lockFilePath = Path.Combine(projectDir, "packages.lock.json");
        if (File.Exists(lockFilePath))
        {
            transitiveDeps.AddRange(ExtractFromLockFile(lockFilePath, directDependencies, projectRefNames));
            return transitiveDeps;
        }

        // Fall back to project.assets.json in the obj folder
        var assetsFilePath = Path.Combine(projectDir, "obj", "project.assets.json");
        if (File.Exists(assetsFilePath))
        {
            transitiveDeps.AddRange(ExtractFromAssetsFile(assetsFilePath, directDependencies, projectRefNames));
        }

        return transitiveDeps;
    }

    /// <summary>
    /// Extracts transitive dependencies from packages.lock.json.
    /// </summary>
    private List<TransitiveDependency> ExtractFromLockFile(string lockFilePath, List<Package> directDependencies, HashSet<string> projectReferenceNames)
    {
        var transitiveDeps = new List<TransitiveDependency>();
        var directPackageNames = directDependencies.Select(d => d.Name.ToLower()).ToHashSet();

        try
        {
            var json = File.ReadAllText(lockFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // packages.lock.json has a structure like: { "dependencies": { "net10.0": { "packageName": {...} } } }
            if (root.TryGetProperty("dependencies", out var dependencies))
            {
                foreach (var framework in dependencies.EnumerateObject())
                {
                    foreach (var package in framework.Value.EnumerateObject())
                    {
                        var packageName = package.Name;
                        
                        // Skip direct dependencies - we only want transitive ones
                        if (directPackageNames.Contains(packageName.ToLower()))
                        {
                            continue;
                        }

                        // Skip project references - they are not NuGet packages
                        if (projectReferenceNames.Contains(packageName.ToLower()))
                        {
                            continue;
                        }

                        if (package.Value.TryGetProperty("resolved", out var version))
                        {
                            var transDep = new TransitiveDependency
                            {
                                PackageName = packageName,
                                Version = version.GetString() ?? "unknown",
                                SourcePackage = FindSourcePackage(package.Value, directDependencies),
                                IsPrivate = false,
                                Depth = 1
                            };
                            transitiveDeps.Add(transDep);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading packages.lock.json: {ex.Message}");
        }

        return transitiveDeps;
    }

    /// <summary>
    /// Extracts transitive dependencies from project.assets.json.
    /// </summary>
    private List<TransitiveDependency> ExtractFromAssetsFile(string assetsFilePath, List<Package> directDependencies, HashSet<string> projectReferenceNames)
    {
        var transitiveDeps = new List<TransitiveDependency>();
        var directPackageNames = directDependencies.Select(d => d.Name.ToLower()).ToHashSet();

        try
        {
            var json = File.ReadAllText(assetsFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // project.assets.json has a "libraries" section with all resolved packages
            if (root.TryGetProperty("libraries", out var libraries))
            {
                foreach (var library in libraries.EnumerateObject())
                {
                    var libraryName = library.Name;
                    
                    // Parse "packagename/version" format
                    var parts = libraryName.Split('/');
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    var packageName = parts[0];
                    var version = parts[1];

                    // Skip direct dependencies - we only want transitive ones
                    if (directPackageNames.Contains(packageName.ToLower()))
                    {
                        continue;
                    }

                    // Skip project references - they are not NuGet packages
                    if (projectReferenceNames.Contains(packageName.ToLower()))
                    {
                        continue;
                    }

                    var transDep = new TransitiveDependency
                    {
                        PackageName = packageName,
                        Version = version,
                        SourcePackage = FindSourcePackageFromAssets(packageName, root, directDependencies),
                        IsPrivate = false,
                        Depth = 1
                    };
                    transitiveDeps.Add(transDep);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading project.assets.json: {ex.Message}");
        }

        return transitiveDeps;
    }

    /// <summary>
    /// Attempts to find which direct dependency introduced a transitive dependency.
    /// </summary>
    private string? FindSourcePackage(JsonElement packageElement, List<Package> directDependencies)
    {
        if (packageElement.TryGetProperty("dependencies", out var dependencies))
        {
            foreach (var dep in dependencies.EnumerateObject())
            {
                if (directDependencies.Any(d => d.Name.Equals(dep.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return dep.Name;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to find which direct dependency introduced a transitive dependency from project.assets.json.
    /// </summary>
    private string? FindSourcePackageFromAssets(string transitiveName, JsonElement root, List<Package> directDependencies)
    {
        try
        {
            if (!root.TryGetProperty("targets", out var targets))
            {
                return null;
            }

            foreach (var target in targets.EnumerateObject())
            {
                foreach (var packageRef in target.Value.EnumerateObject())
                {
                    var packageName = packageRef.Name.Split('/')[0];

                    // Check if this is a direct dependency
                    if (!directDependencies.Any(d => d.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    // Check if this package lists our transitive as a dependency
                    if (packageRef.Value.TryGetProperty("dependencies", out var deps))
                    {
                        foreach (var dep in deps.EnumerateObject())
                        {
                            if (dep.Name.Equals(transitiveName, StringComparison.OrdinalIgnoreCase))
                            {
                                return packageName;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Silently fail
        }

        return null;
    }

    /// <summary>
    /// Counts total lines of code in all C# files included in a project, excluding blank lines and comments.
    /// </summary>
    private int CountLinesOfCode(string projectFilePath)
    {
        try
        {
            var projectDir = Path.GetDirectoryName(projectFilePath) ?? "";
            var csFiles = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith(".") && 
                           !f.Contains("\\.vs\\") && 
                           !f.Contains("\\bin\\") &&
                           !f.Contains("\\obj\\") &&
                           !Path.GetFileName(f).EndsWith(".g.cs"))
                .ToList();

            if (csFiles.Count == 0)
            {
                return 0;
            }

            // Use parallel processing to read and count lines in multiple files simultaneously
            int totalLines = 0;
            object lockObj = new object();

            Parallel.ForEach(csFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, csFile =>
            {
                try
                {
                    var lines = File.ReadAllLines(csFile);
                    int fileLineCount = CountCodeLines(lines);

                    Interlocked.Add(ref totalLines, fileLineCount);
                }
                catch
                {
                    // Skip files that can't be read
                }
            });

            return totalLines;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Counts code lines in an array of source lines, excluding blank lines and comments (both single-line and block).
    /// </summary>
    private int CountCodeLines(string[] lines)
    {
        int codeLines = 0;
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            // Check for block comment end
            if (inBlockComment)
            {
                if (trimmed.Contains("*/"))
                {
                    inBlockComment = false;
                }
                continue;
            }

            // Check for block comment start
            if (trimmed.StartsWith("/*"))
            {
                inBlockComment = true;
                // Check if it ends on the same line
                if (trimmed.Contains("*/"))
                {
                    inBlockComment = false;
                }
                continue;
            }

            // Count line if it's not blank and not a single-line comment
            if (!string.IsNullOrWhiteSpace(line) && !trimmed.StartsWith("//"))
            {
                codeLines++;
            }
        }

        return codeLines;
    }
}

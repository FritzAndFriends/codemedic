using System.Xml.Linq;
using CodeMedic.Engines;
using CodeMedic.Models;
using CodeMedic.Models.Report;

namespace CodeMedic.Plugins.HealthAnalysis;

/// <summary>
/// Scans a directory tree for .NET projects and collects initial health information.
/// </summary>
public class RepositoryScanner
{
    private readonly string _rootPath;
    private readonly NuGetInspector _nugetInspector;
    private readonly VulnerabilityScanner _vulnerabilityScanner;
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
        _nugetInspector = new NuGetInspector(_rootPath);
        _vulnerabilityScanner = new VulnerabilityScanner(_rootPath);
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
            await _nugetInspector.RestorePackagesAsync();
            _nugetInspector.RefreshCentralPackageVersionFiles();

            var projectFiles = Directory.EnumerateFiles(
                _rootPath,
                "*.csproj",
                SearchOption.AllDirectories);

            foreach (var projectFile in projectFiles)
            {
                await ParseProjectAsync(projectFile);
            }

            // Scan for vulnerabilities after all projects are parsed
            await CollectVulnerabilitiesAsync();
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
    /// <param name="limitPackageLists">When true, truncate large package lists (for console output); when false, include all entries.</param>
    /// <returns>A structured report document ready for rendering.</returns>
    public ReportDocument GenerateReport(bool limitPackageLists = true)
    {
        var report = new ReportDocument
        {
            Title = "Repository Health Dashboard"
        };

        report.Metadata["ScanTime"] = DateTime.UtcNow.ToString("u");
        report.Metadata["RootPath"] = _rootPath;

        var totalProjects = _projects.Count;
        var testProjectCount = _projects.Count(p => p.IsTestProject);
        var nonTestProjects = totalProjects - testProjectCount;
        var totalPackages = _projects.Sum(p => p.PackageDependencies.Count);
        var totalLinesOfCode = _projects.Sum(p => p.TotalLinesOfCode);
        var testLinesOfCode = _projects.Where(p => p.IsTestProject).Sum(p => p.TotalLinesOfCode);
        var projectsWithNullable = _projects.Count(p => p.NullableEnabled);
        var projectsWithImplicitUsings = _projects.Count(p => p.ImplicitUsingsEnabled);
        var projectsWithDocumentation = _projects.Count(p => p.GeneratesDocumentation);
        var projectsWithErrors = _projects.Where(p => p.ParseErrors.Count > 0).ToList();
        var versionMismatches = FindPackageVersionMismatches();

        // Collect vulnerabilities early for summary metrics
        var allVulnerabilities = _projects
            .Where(p => p.Metadata.ContainsKey("Vulnerabilities"))
            .SelectMany(p => (List<PackageVulnerability>)p.Metadata["Vulnerabilities"])
            .Distinct()
            .ToList();

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
            summaryKvList.Add("Production Projects", nonTestProjects.ToString());
            summaryKvList.Add("Test Projects", testProjectCount.ToString(), 
                testProjectCount > 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Total Lines of Code", totalLinesOfCode.ToString());
            if (testProjectCount > 0)
            {
                summaryKvList.Add("Test Lines of Code", testLinesOfCode.ToString());
                var testCoverageRatio = totalLinesOfCode > 0 ? (double)testLinesOfCode / totalLinesOfCode : 0;
                summaryKvList.Add("Test/Code Ratio", $"{testCoverageRatio:P1}",
                    testCoverageRatio >= 0.3 ? TextStyle.Success : TextStyle.Warning);
            }
            summaryKvList.Add("Total NuGet Packages", totalPackages.ToString());
            summaryKvList.Add("Known Vulnerabilities", allVulnerabilities.Count.ToString(),
                allVulnerabilities.Count == 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects without Nullable", (totalProjects - projectsWithNullable).ToString(),
                (totalProjects - projectsWithNullable) == 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects without Implicit Usings", (totalProjects - projectsWithImplicitUsings).ToString(),
                (totalProjects - projectsWithImplicitUsings) == 0 ? TextStyle.Success : TextStyle.Warning);
            summaryKvList.Add("Projects missing Documentation", (totalProjects - projectsWithDocumentation).ToString(),
                (totalProjects - projectsWithDocumentation) == 0 ? TextStyle.Success : TextStyle.Warning);
            summarySection.AddElement(summaryKvList);
        }

        report.AddSection(summarySection);

        if (versionMismatches.Count > 0)
        {
            var mismatchSection = new ReportSection
            {
                Title = "Package Version Mismatches",
                Level = 1
            };

            mismatchSection.AddElement(new ReportParagraph(
                "Align package versions across projects to avoid restore/runtime drift.",
                TextStyle.Warning));

            var mismatchList = new ReportList
            {
                Title = "Packages with differing versions"
            };

            foreach (var mismatch in versionMismatches.OrderBy(m => m.PackageName, StringComparer.OrdinalIgnoreCase))
            {
                var versionDetails = string.Join(", ", mismatch.ProjectVersions
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key}={kv.Value}"));

                mismatchList.AddItem($"{mismatch.PackageName}: {versionDetails}");
            }

            mismatchSection.AddElement(mismatchList);
            report.AddSection(mismatchSection);
        }

        // Vulnerabilities section
        if (allVulnerabilities.Count > 0)
        {
            var vulnSection = new ReportSection
            {
                Title = "Known Vulnerabilities",
                Level = 1
            };

            vulnSection.AddElement(new ReportParagraph(
                $"Found {allVulnerabilities.Count} package(s) with known vulnerabilities. Review and update affected packages.",
                TextStyle.Warning));

            // Group by severity
            var bySeverity = allVulnerabilities.GroupBy(v => v.Severity).OrderByDescending(g => GetSeverityOrder(g.Key));

            foreach (var severityGroup in bySeverity)
            {
                var sevTable = new ReportTable
                {
                    Title = $"Vulnerabilities - {severityGroup.Key}"
                };

                sevTable.Headers.AddRange(new[]
                {
                    "Package",
                    "Version",
                    "CVE ID",
                    "Description",
                    "Fixed In",
                    "Published"
                });

                foreach (var vuln in severityGroup.OrderBy(v => v.PackageName))
                {
                    sevTable.AddRow(
                        vuln.PackageName,
                        vuln.AffectedVersion,
                        vuln.VulnerabilityId,
                        vuln.Description,
                        vuln.FixedInVersion ?? "Unknown",
                        vuln.PublishedDate?.ToString("yyyy-MM-dd") ?? "Unknown"
                    );
                }

                vulnSection.AddElement(sevTable);
            }

            report.AddSection(vulnSection);
        }
        else
        {
            var noVulnSection = new ReportSection
            {
                Title = "Security Status",
                Level = 1
            };

            noVulnSection.AddElement(new ReportParagraph(
                "✓ No known vulnerabilities detected in any packages!",
                TextStyle.Success
            ));

            report.AddSection(noVulnSection);
        }

        // Projects table section
        if (totalProjects > 0)
        {
            var projectsSection = new ReportSection
            {
                Title = "Projects",
                Level = 1
            };

            // Group projects by type
            var productionProjects = _projects.Where(p => !p.IsTestProject).OrderBy(p => p.ProjectName).ToList();
            var testProjects = _projects.Where(p => p.IsTestProject).OrderBy(p => p.ProjectName).ToList();

            // Production projects table
            if (productionProjects.Count > 0)
            {
                var productionTable = new ReportTable
                {
                    Title = "Production Projects"
                };

                productionTable.Headers.AddRange(new[]
                {
                    "Project Name",
                    "Path",
                    "Framework",
                    "Output Type",
                    "Lines of Code",
                    "Packages",
                    "Settings"
                });

                foreach (var project in productionProjects)
                {
                    var settings = new List<string>();
                    if (project.NullableEnabled) settings.Add("✓N");
                    if (project.ImplicitUsingsEnabled) settings.Add("✓U");
                    if (project.GeneratesDocumentation) settings.Add("✓D");

                    productionTable.AddRow(
                        project.ProjectName,
                        project.RelativePath,
                        project.TargetFramework ?? "unknown",
                        project.OutputType ?? "unknown",
                        project.TotalLinesOfCode.ToString(),
                        project.PackageDependencies.Count.ToString(),
                        settings.Count > 0 ? string.Join(" ", settings) : "-"
                    );
                }

                projectsSection.AddElement(productionTable);
            }

            // Test projects table (only if test projects exist)
            if (testProjects.Count > 0)
            {
                var testTable = new ReportTable
                {
                    Title = "Test Projects"
                };

                testTable.Headers.AddRange(new[]
                {
                    "Project Name",
                    "Path",
                    "Framework",
                    "Output Type",
                    "Lines of Code",
                    "Packages",
                    "Settings"
                });

                foreach (var project in testProjects)
                {
                    var settings = new List<string>();
                    if (project.NullableEnabled) settings.Add("✓N");
                    if (project.ImplicitUsingsEnabled) settings.Add("✓U");
                    if (project.GeneratesDocumentation) settings.Add("✓D");

                    testTable.AddRow(
                        project.ProjectName,
                        project.RelativePath,
                        project.TargetFramework ?? "unknown",
                        project.OutputType ?? "unknown",
                        project.TotalLinesOfCode.ToString(),
                        project.PackageDependencies.Count.ToString(),
                        settings.Count > 0 ? string.Join(" ", settings) : "-"
                    );
                }

                projectsSection.AddElement(testTable);
            }

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
                detailsKvList.Add("Project Type", project.IsTestProject ? "Test" : "Production",
                    project.IsTestProject ? TextStyle.Success : TextStyle.Normal);
                detailsKvList.Add("Lines of Code", project.TotalLinesOfCode.ToString());
                detailsKvList.Add("Output Type", project.OutputType ?? "unknown");
                detailsKvList.Add("Target Framework", project.TargetFramework ?? "unknown");
                detailsKvList.Add("C# Language Version", project.LanguageVersion ?? "default");
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

                    var packagesToRender = limitPackageLists
                        ? project.PackageDependencies.Take(5)
                        : project.PackageDependencies;

                    foreach (var pkg in packagesToRender)
                    {
                        packagesList.AddItem($"{pkg.Name} ({pkg.Version})");
                    }

                    if (limitPackageLists && project.PackageDependencies.Count > 5)
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

                    var transitiveDepsToRender = limitPackageLists
                        ? project.TransitiveDependencies.Take(5)
                        : project.TransitiveDependencies;

                    foreach (var transDep in transitiveDepsToRender)
                    {
                        var depLabel = $"{transDep.PackageName} ({transDep.Version})";
                        if (transDep.IsPrivate)
                        {
                            depLabel += " [Private]";
                        }
                        transitiveDeps.AddItem(depLabel);
                    }

                    if (limitPackageLists && project.TransitiveDependencies.Count > 5)
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

            var projectDir = Path.GetDirectoryName(projectFilePath) ?? _rootPath;

            // Count lines of code in C# files
            projectInfo.TotalLinesOfCode = CountLinesOfCode(projectFilePath);

            // Parse the project file XML
            var doc = XDocument.Load(projectFilePath);
            var xmlNamespace = doc.Root?.Name.Namespace ?? XNamespace.None;
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

                var langVersion = propertyGroup.Element(XName.Get("LangVersion", ns))?.Value;
                if (!string.IsNullOrWhiteSpace(langVersion))
                {
                    projectInfo.LanguageVersion = langVersion;
                }
                else
                {
                    // Infer default C# language version from TargetFramework
                    projectInfo.LanguageVersion = InferDefaultCSharpVersion(projectInfo.TargetFramework);
                }

                var docElement = propertyGroup.Element(XName.Get("GenerateDocumentationFile", ns));
                projectInfo.GeneratesDocumentation = docElement?.Value?.ToLower() == "true";

                var isPackableElement = propertyGroup.Element(XName.Get("IsPackable", ns));
                var isPackable = isPackableElement?.Value?.ToLower() != "false";
                projectInfo.IsTestProject = !isPackable;
            }

            // Count package references
            projectInfo.PackageDependencies = _nugetInspector.ReadPackageReferences(root, xmlNamespace, projectDir);

            // Confirm test project by checking for test framework packages if IsPackable wasn't explicit
            if (!projectInfo.IsTestProject)
            {
                var testFrameworkPackages = new[] { "xunit", "nunit", "mstest", "microsoft.net.test.sdk", "coverlet" };
                projectInfo.IsTestProject = projectInfo.PackageDependencies.Any(pkg => 
                    testFrameworkPackages.Any(tfp => pkg.Name.Contains(tfp, StringComparison.OrdinalIgnoreCase)));
            }

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
            projectInfo.TransitiveDependencies = _nugetInspector.ExtractTransitiveDependencies(projectFilePath, projectInfo.PackageDependencies, projectInfo.ProjectReferences);

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
    /// Infers the default C# language version for a given target framework, based on Microsoft documentation.
    /// </summary>
    /// <param name="targetFramework">The target framework moniker (e.g., net6.0, net7.0, net8.0, net10.0).</param>
    /// <returns>The default C# language version as a string.</returns>
    private static string InferDefaultCSharpVersion(string? targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            return "unknown";

        // Normalize to lower for comparison
        var tfm = targetFramework.ToLowerInvariant();

        // Mapping based on https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version#defaults
        // and .NET 10 preview announcements
        if (tfm.StartsWith("net10.0")) return "14"; 
        if (tfm.StartsWith("net9.0")) return "13";
        if (tfm.StartsWith("net8.0")) return "12";
        if (tfm.StartsWith("net7.0")) return "11";
        if (tfm.StartsWith("net6.0")) return "10";
        if (tfm.StartsWith("net5.0")) return "9";
        if (tfm.StartsWith("netcoreapp3.1")) return "8";
        if (tfm.StartsWith("netcoreapp3.0")) return "8";
        if (tfm.StartsWith("netcoreapp2.1")) return "7.3";
        if (tfm.StartsWith("netcoreapp2.0")) return "7.1";
        if (tfm.StartsWith("netcoreapp1.")) return "7.0";
        if (tfm.StartsWith("netstandard2.1")) return "8";
        if (tfm.StartsWith("netstandard2.0")) return "7.3";
        if (tfm.StartsWith("netstandard1.")) return "7";
        if (tfm.StartsWith("net4.8")) return "7.3";
        if (tfm.StartsWith("net4.7")) return "7";
        if (tfm.StartsWith("net4.6")) return "6";
        if (tfm.StartsWith("net4.5")) return "5";
        if (tfm.StartsWith("net4.0")) return "4";
        if (tfm.StartsWith("net3.5")) return "3";
        if (tfm.StartsWith("net2.0")) return "2";

        return "unknown";
    }

    private List<PackageVersionMismatch> FindPackageVersionMismatches()
    {
        var packageVersions = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in _projects)
        {
            foreach (var package in project.PackageDependencies)
            {
                if (string.IsNullOrWhiteSpace(package.Name) || package.Name.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(package.Version) || package.Version.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!packageVersions.TryGetValue(package.Name, out var versionsByProject))
                {
                    versionsByProject = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    packageVersions[package.Name] = versionsByProject;
                }

                versionsByProject[project.ProjectName] = package.Version;
            }
        }

        var mismatches = new List<PackageVersionMismatch>();

        foreach (var kvp in packageVersions)
        {
            var distinctVersions = kvp.Value.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctVersions.Count > 1)
            {
                mismatches.Add(new PackageVersionMismatch(kvp.Key, kvp.Value));
            }
        }

        return mismatches;
    }

    /// <summary>
    /// Collects vulnerability information for all packages across all projects.
    /// </summary>
    private async Task CollectVulnerabilitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var project in _projects)
            {
                if (project.PackageDependencies.Count == 0)
                {
                    continue;
                }

                foreach (var package in project.PackageDependencies)
                {
                    var vulnerabilities = await _vulnerabilityScanner.ScanPackageAsync(
                        package.Name,
                        package.Version,
                        cancellationToken);

                    if (vulnerabilities.Count > 0)
                    {
                        if (!project.Metadata.ContainsKey("Vulnerabilities"))
                        {
                            project.Metadata["Vulnerabilities"] = new List<PackageVulnerability>();
                        }

                        var vulnList = (List<PackageVulnerability>)project.Metadata["Vulnerabilities"];
                        vulnList.AddRange(vulnerabilities);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Vulnerability scanning failed: {ex.Message}");
        }
    }

    private sealed record PackageVersionMismatch(string PackageName, Dictionary<string, string> ProjectVersions);

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

    /// <summary>
    /// Gets the severity ordering value for vulnerability grouping (higher values = more severe).
    /// </summary>
    private static int GetSeverityOrder(string severity) => severity.ToLower() switch
    {
        "critical" => 4,
        "high" => 3,
        "moderate" => 2,
        "low" => 1,
        _ => 0
    };
}

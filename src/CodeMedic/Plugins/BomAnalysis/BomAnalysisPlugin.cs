using System.Xml.Linq;
using CodeMedic.Abstractions;
using CodeMedic.Abstractions.Plugins;
using CodeMedic.Engines;
using CodeMedic.Models.Report;
using CodeMedic.Output;
using CodeMedic.Utilities;

namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Internal plugin that provides Bill of Materials (BOM) analysis for .NET repositories.
/// </summary>
public class BomAnalysisPlugin : IAnalysisEnginePlugin
{
    private NuGetInspector? _inspector;

    /// <inheritdoc/>
    public PluginMetadata Metadata => new()
    {
        Id = "codemedic.bom",
        Name = "Bill of Materials Analyzer",
        Version = VersionUtility.GetVersion(),
        Description = "Generates comprehensive Bill of Materials including NuGet packages, frameworks, services, and vendors",
        Author = "CodeMedic Team",
        Tags = ["bom", "dependencies", "inventory", "packages"]
    };

    /// <inheritdoc/>
    public string AnalysisDescription => "Comprehensive dependency and service inventory (BOM)";

    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // No initialization required
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<object> AnalyzeAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        _inspector = new NuGetInspector(repositoryPath);
        
        // Restore packages to ensure we have all dependency information
        await _inspector.RestorePackagesAsync();
        _inspector.RefreshCentralPackageVersionFiles();

        // Generate BOM report
        var bomReport = GenerateBomReport(repositoryPath);
        return bomReport;
    }

    /// <inheritdoc/>
    public CommandRegistration[]? RegisterCommands()
    {
        return
        [
            new CommandRegistration
            {
                Name = "bom",
                Description = "Generate bill of materials report",
                Handler = ExecuteBomCommandAsync,
                Examples =
                [
                    "codemedic bom",
                    "codemedic bom --format markdown",
                    "codemedic bom --format md > bom.md"
                ]
            }
        ];
    }

    private async Task<int> ExecuteBomCommandAsync(string[] args, IRenderer renderer)
    {
        try
        {
            // Parse arguments (target path only)
            string? targetPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--"))
                {
                    targetPath = args[i];
                }
            }

            // Render banner and header
            renderer.RenderBanner();
            renderer.RenderSectionHeader("Bill of Materials (BOM)");

            // Run analysis
            var repositoryPath = targetPath ?? Directory.GetCurrentDirectory();
            object reportDocument;

            await renderer.RenderWaitAsync($"Running {AnalysisDescription}...", async () =>
            {
                reportDocument = await AnalyzeAsync(repositoryPath);
            });

            reportDocument = await AnalyzeAsync(repositoryPath);

            // Render report
            renderer.RenderReport(reportDocument);

            return 0;
        }
        catch (Exception ex)
        {
            CodeMedic.Commands.RootCommandHandler.Console.RenderError($"Failed to generate BOM: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Generates a structured BOM report.
    /// </summary>
    private ReportDocument GenerateBomReport(string repositoryPath)
    {
        var report = new ReportDocument
        {
            Title = "Bill of Materials (BOM)"
        };

        report.Metadata["ScanTime"] = DateTime.UtcNow.ToString("u");
        report.Metadata["RootPath"] = repositoryPath;

        // Summary section
        var summarySection = new ReportSection
        {
            Title = "BOM Summary",
            Level = 1
        };

        summarySection.AddElement(new ReportParagraph(
            "Generating comprehensive Bill of Materials...",
            TextStyle.Normal
        ));

        report.AddSection(summarySection);

        // NuGet Packages section
        AddNuGetPackagesSection(report, repositoryPath);

        // Frameworks & Platform Features section
        AddFrameworksSection(report);

        // External Services & Vendors section (placeholder)
        AddExternalServicesSection(report);

        return report;
    }

    /// <summary>
    /// Adds the NuGet packages section to the BOM report.
    /// </summary>
    private void AddNuGetPackagesSection(ReportDocument report, string repositoryPath)
    {
        var packagesSection = new ReportSection
        {
            Title = "NuGet Package Dependencies",
            Level = 1
        };

        // Find all project files
        var projectFiles = Directory.EnumerateFiles(
            repositoryPath,
            "*.csproj",
            SearchOption.AllDirectories).ToList();

        if (projectFiles.Count == 0)
        {
            packagesSection.AddElement(new ReportParagraph(
                "No .NET projects found in repository.",
                TextStyle.Warning
            ));
            report.AddSection(packagesSection);
            return;
        }

        var allPackages = new Dictionary<string, PackageInfo>();

        // Parse each project file to extract packages
        foreach (var projectFile in projectFiles)
        {
            try
            {
                var doc = XDocument.Load(projectFile);
                var root = doc.Root;
                if (root == null) continue;

                var ns = root.GetDefaultNamespace();
                var projectDir = Path.GetDirectoryName(projectFile) ?? repositoryPath;

                // Get direct package references
                var packages = _inspector!.ReadPackageReferences(root, ns, projectDir);

                foreach (var package in packages)
                {
                    var key = $"{package.Name}@{package.Version}";
                    if (!allPackages.ContainsKey(key))
                    {
                        allPackages[key] = new PackageInfo
                        {
                            Name = package.Name,
                            Version = package.Version,
                            IsDirect = true,
                            Projects = []
                        };
                    }
                    allPackages[key].Projects.Add(Path.GetFileNameWithoutExtension(projectFile));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Could not parse {projectFile}: {ex.Message}");
            }
        }

        if (allPackages.Count == 0)
        {
            packagesSection.AddElement(new ReportParagraph(
                "No NuGet packages found in projects.",
                TextStyle.Warning
            ));
            report.AddSection(packagesSection);
            return;
        }

        // Create packages table
        var packagesTable = new ReportTable
        {
            Title = "All Packages"
        };

        packagesTable.Headers.AddRange(["Package", "Version", "Type", "Used In"]);

        foreach (var package in allPackages.Values.OrderBy(p => p.Name))
        {
            packagesTable.AddRow(
                package.Name,
                package.Version,
                package.IsDirect ? "Direct" : "Transitive",
                string.Join(", ", package.Projects.Distinct())
            );
        }

        var summaryKvList = new ReportKeyValueList();
        summaryKvList.Add("Total Unique Packages", allPackages.Count.ToString());
        summaryKvList.Add("Direct Dependencies", allPackages.Values.Count(p => p.IsDirect).ToString());
        summaryKvList.Add("Transitive Dependencies", allPackages.Values.Count(p => !p.IsDirect).ToString());

        packagesSection.AddElement(summaryKvList);
        packagesSection.AddElement(packagesTable);

        report.AddSection(packagesSection);
    }

    /// <summary>
    /// Adds the frameworks section (placeholder for future implementation).
    /// </summary>
    private void AddFrameworksSection(ReportDocument report)
    {
        var frameworksSection = new ReportSection
        {
            Title = "Framework & Platform Features",
            Level = 1
        };

        frameworksSection.AddElement(new ReportParagraph(
            "Framework feature detection coming soon...",
            TextStyle.Dim
        ));

        report.AddSection(frameworksSection);
    }

    /// <summary>
    /// Adds the external services section (placeholder for future implementation).
    /// </summary>
    private void AddExternalServicesSection(ReportDocument report)
    {
        var servicesSection = new ReportSection
        {
            Title = "External Services & Vendors",
            Level = 1
        };

        servicesSection.AddElement(new ReportParagraph(
            "External service detection coming soon...",
            TextStyle.Dim
        ));

        report.AddSection(servicesSection);
    }

    /// <summary>
    /// Helper class to track package information across projects.
    /// </summary>
    private class PackageInfo
    {
        public required string Name { get; init; }
        public required string Version { get; init; }
        public required bool IsDirect { get; init; }
        public required List<string> Projects { get; init; }
    }
}

using System.Diagnostics;
using System.Xml.Linq;
using System.Text.Json;
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
        var bomReport = await GenerateBomReportAsync(repositoryPath);
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
                Arguments =
                [
                    new CommandArgument(
                        Description: "Path to the repository to analyze",
                        ShortName: "p",
                        LongName: "path",
                        HasValue: true,
                        ValueName: "path",
                        DefaultValue: "current directory")
                ],
                Examples =
                [
                    "codemedic bom",
                    "codemedic bom -p /path/to/repo",
                    "codemedic bom --path /path/to/repo --format markdown",
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
    private async Task<ReportDocument> GenerateBomReportAsync(string repositoryPath)
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
        await AddNuGetPackagesSectionAsync(report, repositoryPath);

        // Frameworks & Platform Features section
        AddFrameworksSection(report);

        // External Services & Vendors section (placeholder)
        AddExternalServicesSection(report);

        return report;
    }

    /// <summary>
    /// Adds the NuGet packages section to the BOM report.
    /// </summary>
    private async Task AddNuGetPackagesSectionAsync(ReportDocument report, string repositoryPath)
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

        // Fetch license information for all packages
        await FetchLicenseInformationAsync(allPackages.Values);

        // Create packages table
        var packagesTable = new ReportTable
        {
            Title = "All Packages"
        };

        packagesTable.Headers.AddRange(["Package", "Version", "Type", "License", "Source Type", "Commercial", "Used In"]);

        foreach (var package in allPackages.Values.OrderBy(p => p.Name))
        {
            packagesTable.AddRow(
                package.Name,
                package.Version,
                package.IsDirect ? "Direct" : "Transitive",
                package.License ?? "Unknown",
                package.SourceType,
                package.Commercial,
                string.Join(", ", package.Projects.Distinct())
            );
        }

        var summaryKvList = new ReportKeyValueList();
        summaryKvList.Add("Total Unique Packages", allPackages.Count.ToString());
        summaryKvList.Add("Direct Dependencies", allPackages.Values.Count(p => p.IsDirect).ToString());
        summaryKvList.Add("Transitive Dependencies", allPackages.Values.Count(p => !p.IsDirect).ToString());

        packagesSection.AddElement(summaryKvList);
        packagesSection.AddElement(packagesTable);

        // Add footer with license information link
        packagesSection.AddElement(new ReportParagraph(
            "For more information about open source licenses, visit https://choosealicense.com/licenses/",
            TextStyle.Dim
        ));

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
    /// Fetches license information for packages from local .nuspec files in the NuGet global packages cache.
    /// </summary>
    private async Task FetchLicenseInformationAsync(IEnumerable<PackageInfo> packages)
    {
        // Get the NuGet global packages folder
        var globalPackagesPath = await GetNuGetGlobalPackagesFolderAsync();
        if (string.IsNullOrEmpty(globalPackagesPath))
        {
            Console.Error.WriteLine("Warning: Could not determine NuGet global packages folder location.");
            return;
        }

        var tasks = packages.Select(async package =>
        {
            try
            {
                await FetchLicenseForPackageAsync(globalPackagesPath, package);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire operation
                Console.Error.WriteLine($"Warning: Could not fetch license for {package.Name}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Fetches license information for a specific package from its local .nuspec file.
    /// </summary>
    private async Task FetchLicenseForPackageAsync(string globalPackagesPath, PackageInfo package)
    {
        try
        {
            // Construct path to the local .nuspec file
            // NuGet packages are stored in: {globalPackages}/{packageId}/{version}/{packageId}.nuspec
            var packageFolder = Path.Combine(globalPackagesPath, package.Name.ToLowerInvariant(), package.Version.ToLowerInvariant());
            var nuspecPath = Path.Combine(packageFolder, $"{package.Name.ToLowerInvariant()}.nuspec");
            
            if (!File.Exists(nuspecPath))
            {
                // Try alternative naming (some packages might use original casing)
                nuspecPath = Path.Combine(packageFolder, $"{package.Name}.nuspec");
                if (!File.Exists(nuspecPath))
                {
                    return; // Skip if we can't find the nuspec file
                }
            }

            var nuspecContent = await File.ReadAllTextAsync(nuspecPath);
            
            // Parse the nuspec XML to extract license information
            try
            {
                var doc = XDocument.Parse(nuspecContent);
                var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                
                // Try to get license information from metadata
                var metadata = doc.Root?.Element(ns + "metadata");
                if (metadata != null)
                {
                    // Check for license element first (newer format)
                    var licenseElement = metadata.Element(ns + "license");
                    if (licenseElement != null)
                    {
                        var licenseType = licenseElement.Attribute("type")?.Value;
                        if (licenseType == "expression")
                        {
                            package.License = licenseElement.Value?.Trim();
                        }
                        else if (licenseType == "file")
                        {
                            package.License = "See package contents";
                        }
                    }
                    else
                    {
                        // Fall back to licenseUrl (older format)
                        var licenseUrl = metadata.Element(ns + "licenseUrl")?.Value?.Trim();
                        if (!string.IsNullOrWhiteSpace(licenseUrl))
                        {
                            package.LicenseUrl = licenseUrl;
                            // Try to extract license type from common URL patterns
                            if (licenseUrl.Contains("mit", StringComparison.OrdinalIgnoreCase))
                            {
                                package.License = "MIT";
                            }
                            else if (licenseUrl.Contains("apache", StringComparison.OrdinalIgnoreCase))
                            {
                                package.License = "Apache-2.0";
                            }
                            else if (licenseUrl.Contains("bsd", StringComparison.OrdinalIgnoreCase))
                            {
                                package.License = "BSD";
                            }
                            else if (licenseUrl.Contains("gpl", StringComparison.OrdinalIgnoreCase))
                            {
                                package.License = "GPL";
                            }
                            else
                            {
                                package.License = "See URL";
                            }
                        }
                    }

                    // Determine source type and commercial status based on license and other metadata
                    DetermineSourceTypeAndCommercialStatus(package, metadata, ns);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Could not parse nuspec for {package.Name}: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Error reading license for {package.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the NuGet global packages folder path by executing 'dotnet nuget locals global-packages --list'.
    /// </summary>
    private async Task<string?> GetNuGetGlobalPackagesFolderAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "nuget locals global-packages --list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    // Parse output like "global-packages: C:\Users\user\.nuget\packages\"
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("global-packages:", StringComparison.OrdinalIgnoreCase))
                        {
                            var path = trimmedLine.Substring("global-packages:".Length).Trim();
                            if (Directory.Exists(path))
                            {
                                return path;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not determine NuGet global packages folder: {ex.Message}");
        }

        // Fallback to default location
        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        return Directory.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>
    /// Determines the source type (Open Source/Closed Source) and commercial status of a package.
    /// </summary>
    private static void DetermineSourceTypeAndCommercialStatus(PackageInfo package, XElement metadata, XNamespace ns)
    {
        var license = package.License?.ToLowerInvariant();
        var licenseUrl = package.LicenseUrl?.ToLowerInvariant();
        var projectUrl = metadata.Element(ns + "projectUrl")?.Value?.ToLowerInvariant();
        var repositoryUrl = metadata.Element(ns + "repository")?.Attribute("url")?.Value?.ToLowerInvariant();
        var packageId = package.Name.ToLowerInvariant();
        var authors = metadata.Element(ns + "authors")?.Value?.ToLowerInvariant();
        var owners = metadata.Element(ns + "owners")?.Value?.ToLowerInvariant();

        // Determine if it's open source based on multiple indicators
        var isOpenSource = false;

        // Open source license indicators
        var openSourceLicenses = new[] {
            "mit", "apache", "bsd", "gpl", "lgpl", "mpl", "isc", "unlicense",
            "cc0", "zlib", "ms-pl", "ms-rl", "eclipse", "cddl", "artistic"
        };

        if (!string.IsNullOrEmpty(license))
        {
            isOpenSource = openSourceLicenses.Any(oss => license.Contains(oss));
        }
        
        if (!isOpenSource && !string.IsNullOrEmpty(licenseUrl))
        {
            isOpenSource = openSourceLicenses.Any(oss => licenseUrl.Contains(oss)) ||
                          licenseUrl.Contains("github.com") ||
                          licenseUrl.Contains("opensource.org");
        }

        // Check repository URLs for open source indicators
        if (!isOpenSource)
        {
            var urls = new[] { projectUrl, repositoryUrl }.Where(url => !string.IsNullOrEmpty(url));
            isOpenSource = urls.Any(url => 
                url!.Contains("github.com") ||
                url.Contains("gitlab.com") ||
                url.Contains("bitbucket.org") ||
                url.Contains("codeplex.com") ||
                url.Contains("sourceforge.net"));
        }

        // Determine commercial status
        // Microsoft packages are generally free but from a commercial entity
        var isMicrosoft = packageId.StartsWith("microsoft.") ||
                         packageId.StartsWith("system.") ||
                         !string.IsNullOrEmpty(authors) && authors.Contains("microsoft") ||
                         !string.IsNullOrEmpty(owners) && owners.Contains("microsoft");

        // Other commercial indicators
        var commercialIndicators = new[] {
            "commercial", "proprietary", "enterprise", "professional", "premium",
            "telerik", "devexpress", "syncfusion", "infragistics", "componentone"
        };

        var hasCommercialIndicators = commercialIndicators.Any(indicator => 
            (!string.IsNullOrEmpty(license) && license.Contains(indicator)) ||
            (!string.IsNullOrEmpty(authors) && authors.Contains(indicator)) ||
            (!string.IsNullOrEmpty(packageId) && packageId.Contains(indicator)));

        // License-based commercial detection
        var commercialLicenses = new[] { "proprietary", "commercial", "eula" };
        var hasCommercialLicense = !string.IsNullOrEmpty(license) && 
                                  commercialLicenses.Any(cl => license.Contains(cl));

        // Set source type
        if (isOpenSource)
        {
            package.SourceType = "Open Source";
        }
        else if (hasCommercialLicense || hasCommercialIndicators)
        {
            package.SourceType = "Closed Source";
        }
        else if (isMicrosoft)
        {
            package.SourceType = "Closed Source"; // Microsoft packages are typically closed source even if free
        }
        else
        {
            package.SourceType = "Unknown";
        }

        // Set commercial status
        if (hasCommercialLicense || hasCommercialIndicators)
        {
            package.Commercial = "Yes";
        }
        else if (isOpenSource || isMicrosoft)
        {
            package.Commercial = "No";
        }
        else
        {
            package.Commercial = "Unknown";
        }
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
        public string? License { get; set; }
        public string? LicenseUrl { get; set; }
        public string SourceType { get; set; } = "Unknown";
        public string Commercial { get; set; } = "Unknown";
    }
}

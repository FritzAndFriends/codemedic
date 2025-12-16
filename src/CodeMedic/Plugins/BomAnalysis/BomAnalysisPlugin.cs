using System.Diagnostics;
using System.Xml.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeMedic.Abstractions;
using CodeMedic.Abstractions.Plugins;
using CodeMedic.Engines;
using CodeMedic.Models;
using CodeMedic.Models.Report;
using CodeMedic.Output;
using CodeMedic.Utilities;
using CodeMedic.Commands;

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
            RootCommandHandler.Console.RenderError($"Failed to generate BOM: {ex.Message}");
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

        // NuGet packages with framework feature detection needs access to allPackages
        var allPackages = await AddNuGetPackagesSectionAsyncAndReturnPackages(report, repositoryPath);

        // Frameworks & Platform Features section
        AddFrameworksSection(report, repositoryPath, allPackages);

        // External Services & Vendors section (placeholder)
        AddExternalServicesSection(report);

        return report;
    }

    /// <summary>
    /// Adds the NuGet packages section to the BOM report and returns the package dictionary for framework detection.
    /// </summary>
    private async Task<Dictionary<string, PackageInfo>> AddNuGetPackagesSectionAsyncAndReturnPackages(ReportDocument report, string repositoryPath)
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
            return new Dictionary<string, PackageInfo>();
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
                var projectName = Path.GetFileNameWithoutExtension(projectFile);

                // Read project references to filter them out
                var projectReferenceElements = root.Descendants(ns + "ProjectReference").ToList();
                var projectReferences = projectReferenceElements
                    .Select(prElement => new ProjectReference
                    {
                        ProjectName = Path.GetFileNameWithoutExtension(prElement.Attribute("Include")?.Value ?? "unknown"),
                        Path = prElement.Attribute("Include")?.Value ?? "unknown",
                        IsPrivate = prElement.Attribute("PrivateAssets")?.Value?.ToLower() == "all",
                        Metadata = prElement.Attribute("Condition")?.Value
                    })
                    .ToList();

                var projectRefNames = projectReferences.Select(pr => pr.ProjectName.ToLower()).ToHashSet();

                // Get direct package references and filter out project references
                var directPackages = _inspector!.ReadPackageReferences(root, ns, projectDir)
                    .Where(package => !projectRefNames.Contains(package.Name.ToLower()))
                    .ToList();

                foreach (var package in directPackages)
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
                    allPackages[key].Projects.Add(projectName);
                }

                // Get transitive dependencies using the same method as health analysis, now with proper project reference filtering
                var transitivePackages = _inspector.ExtractTransitiveDependencies(projectFile, directPackages.ToList(), projectReferences);

                foreach (var transitive in transitivePackages)
                {
                    var key = $"{transitive.PackageName}@{transitive.Version}";
                    if (!allPackages.ContainsKey(key))
                    {
                        allPackages[key] = new PackageInfo
                        {
                            Name = transitive.PackageName,
                            Version = transitive.Version,
                            IsDirect = false,
                            Projects = []
                        };
                    }
                    allPackages[key].Projects.Add(projectName);
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
            return allPackages;
        }

        // Fetch license information for all packages
        await FetchLicenseInformationAsync(allPackages.Values);

        // Fetch latest version information for all packages
        await FetchLatestVersionInformationAsync(allPackages.Values);

        // Fetch latest license information to detect changes
        await FetchLatestLicenseInformationAsync(allPackages.Values);

        // Create packages table
        var packagesTable = new ReportTable
        {
            Title = "All Packages"
        };

        packagesTable.Headers.AddRange(["Package", "Version", "Latest", "Type", "License", "Source", "Comm", "Used In"]);

        foreach (var package in allPackages.Values.OrderBy(p => p.Name))
        {
            var latestVersionDisplay = package.LatestVersion ?? "Unknown";
            if (package.HasNewerVersion)
            {
                latestVersionDisplay = $"^ {package.LatestVersion}";
            }
            else if (!string.IsNullOrEmpty(package.LatestVersion) &&
                     string.Equals(package.Version, package.LatestVersion, StringComparison.OrdinalIgnoreCase))
            {
                latestVersionDisplay = "Current";
            }

            // Truncate package names if too long to improve table formatting
            var displayName = package.Name.Length > 25 ? package.Name.Substring(0, 22) + "..." : package.Name;

            // Shorten source type and commercial status for better formatting
            var sourceType = package.SourceType == "Open Source" ? "Open" :
                            package.SourceType == "Closed Source" ? "Closed" :
                            package.SourceType;

            var commercial = package.Commercial == "Unknown" ? "?" :
                           package.Commercial == "Yes" ? "Y" : "N";

            // Truncate license if too long
            var license = package.License?.Length > 12 ? package.License.Substring(0, 9) + "..." : package.License ?? "Unknown";

            packagesTable.AddRow(
                displayName,
                package.Version,
                latestVersionDisplay,
                package.IsDirect ? "Direct" : "Trans",
                license,
                sourceType,
                commercial,
                string.Join(", ", package.Projects.Distinct())
            );
        }

        var summaryKvList = new ReportKeyValueList();
        summaryKvList.Add("Total Unique Packages", allPackages.Count.ToString());
        summaryKvList.Add("Direct Dependencies", allPackages.Values.Count(p => p.IsDirect).ToString());
        summaryKvList.Add("Transitive Dependencies", allPackages.Values.Count(p => !p.IsDirect).ToString());
        summaryKvList.Add("Packages with Updates", allPackages.Values.Count(p => p.HasNewerVersion).ToString());
        summaryKvList.Add("License Changes Detected", allPackages.Values.Count(p => p.HasLicenseChange).ToString());

        packagesSection.AddElement(summaryKvList);
        packagesSection.AddElement(packagesTable);

        // Add license change warnings if any
        var packagesWithLicenseChanges = allPackages.Values.Where(p => p.HasLicenseChange).ToList();
        if (packagesWithLicenseChanges.Count > 0)
        {
            var warningSection = new ReportSection
            {
                Title = "License Change Warnings",
                Level = 2
            };

            warningSection.AddElement(new ReportParagraph(
                "The following packages have different licenses in their latest versions:",
                TextStyle.Warning
            ));

            var licenseChangeTable = new ReportTable
            {
                Title = "Packages with License Changes"
            };

            licenseChangeTable.Headers.AddRange(["Package", "Current Version", "Current License", "Latest Version", "Latest License"]);

            foreach (var package in packagesWithLicenseChanges.OrderBy(p => p.Name))
            {
                licenseChangeTable.AddRow(
                    package.Name,
                    package.Version,
                    package.License ?? "Unknown",
                    package.LatestVersion ?? "Unknown",
                    package.LatestLicense ?? "Unknown"
                );
            }

            warningSection.AddElement(licenseChangeTable);
            packagesSection.AddElement(warningSection);
        }

        // Add footer with license information link
        packagesSection.AddElement(new ReportParagraph(
            "For more information about open source licenses, visit https://choosealicense.com/licenses/",
            TextStyle.Dim
        ));
        packagesSection.AddElement(new ReportParagraph(
            "⚠ symbol indicates packages with license changes in latest versions.",
            TextStyle.Dim
        ));

        report.AddSection(packagesSection);

        return allPackages;
    }

    /// <summary>
    /// Adds the frameworks section with project configuration and detected framework features.
    /// </summary>
    private void AddFrameworksSection(ReportDocument report, string rootPath, Dictionary<string, PackageInfo> allPackages)
    {
        var frameworksSection = new ReportSection
        {
            Title = "Framework & Platform Features",
            Level = 1
        };

        // Add project configuration table
        var frameworkAnalysis = new FrameworkAnalysis().AnalyzeFrameworkForProjects(rootPath);
        frameworksSection.AddElement(frameworkAnalysis);

        // Convert internal PackageInfo to framework detector PackageInfo
        var detectorPackages = allPackages.Values.Select(p => new CodeMedic.Plugins.BomAnalysis.PackageInfo
        {
            Name = p.Name,
            Version = p.Version,
            IsDirect = p.IsDirect,
            Projects = new List<string>(p.Projects)
        }).ToList();

        // Run framework feature detection
        var detector = new FrameworkFeatureDetectorEngine();
        var featureSections = detector.AnalyzeFeatures(detectorPackages);

        // Add each feature category section
        foreach (var featureSection in featureSections)
        {
            frameworksSection.AddElement(featureSection);
        }

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
        var tasks = packages.Select(async package =>
        {
            try
            {
                var (license, licenseUrl) = await _inspector!.FetchLicenseFromLocalCacheAsync(package.Name, package.Version);
                package.License = license;
                package.LicenseUrl = licenseUrl;

                if (!string.IsNullOrEmpty(license))
                {
                    // Get additional metadata from local nuspec to determine source type and commercial status
                    var globalPackagesPath = await _inspector.GetNuGetGlobalPackagesFolderAsync();
                    if (!string.IsNullOrEmpty(globalPackagesPath))
                    {
                        var packageFolder = Path.Combine(globalPackagesPath, package.Name.ToLowerInvariant(), package.Version.ToLowerInvariant());
                        var nuspecPath = Path.Combine(packageFolder, $"{package.Name.ToLowerInvariant()}.nuspec");

                        if (!File.Exists(nuspecPath))
                        {
                            nuspecPath = Path.Combine(packageFolder, $"{package.Name}.nuspec");
                        }

                        if (File.Exists(nuspecPath))
                        {
                            var nuspecContent = await File.ReadAllTextAsync(nuspecPath);
                            var doc = XDocument.Parse(nuspecContent);
                            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                            var metadata = doc.Root?.Element(ns + "metadata");

                            if (metadata != null)
                            {
                                var projectUrl = metadata.Element(ns + "projectUrl")?.Value;
                                var repositoryUrl = metadata.Element(ns + "repository")?.Attribute("url")?.Value;
                                var authors = metadata.Element(ns + "authors")?.Value;
                                var owners = metadata.Element(ns + "owners")?.Value;

                                var (sourceType, commercial) = NuGetInspector.DetermineSourceTypeAndCommercialStatus(
                                    package.Name, license, licenseUrl, projectUrl, repositoryUrl, authors, owners);

                                package.SourceType = sourceType;
                                package.Commercial = commercial;
                            }
                        }
                    }
                }
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
    /// Fetches latest version information for packages using the NuGet API.
    /// </summary>
    private async Task FetchLatestVersionInformationAsync(IEnumerable<PackageInfo> packages)
    {
        // Limit concurrent operations to avoid overwhelming the NuGet service
        const int maxConcurrency = 5;
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = packages.Select(async package =>
        {
            await semaphore.WaitAsync();
            try
            {
                var latestVersion = await _inspector!.FetchLatestVersionAsync(package.Name, package.Version);
                if (!string.IsNullOrEmpty(latestVersion))
                {
                    package.LatestVersion = latestVersion;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire operation
                Console.Error.WriteLine($"Warning: Could not fetch latest version for {package.Name}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Fetches latest license information for packages using NuGet API to detect license changes.
    /// </summary>
    private async Task FetchLatestLicenseInformationAsync(IEnumerable<PackageInfo> packages)
    {
        // Limit concurrent operations to avoid overwhelming the NuGet service
        const int maxConcurrency = 3;
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = packages.Select(async package =>
        {
            await semaphore.WaitAsync();
            try
            {
                // Skip if we don't have a latest version to check
                if (!string.IsNullOrEmpty(package.LatestVersion))
                {
                    var (license, licenseUrl) = await _inspector!.FetchLicenseFromApiAsync(package.Name, package.LatestVersion);
                    package.LatestLicense = license;
                    package.LatestLicenseUrl = licenseUrl;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire operation
                Console.Error.WriteLine($"Warning: Could not fetch latest license for {package.Name}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
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
        public string? LatestLicense { get; set; }
        public string? LatestLicenseUrl { get; set; }
        public string SourceType { get; set; } = "Unknown";
        public string Commercial { get; set; } = "Unknown";
        public string? LatestVersion { get; set; }
        public bool HasNewerVersion => !string.IsNullOrEmpty(LatestVersion) &&
                                      !string.Equals(Version, LatestVersion, StringComparison.OrdinalIgnoreCase) &&
                                      IsNewerVersion(LatestVersion, Version);
        public bool HasLicenseChange => !string.IsNullOrEmpty(License) &&
                                       !string.IsNullOrEmpty(LatestLicense) &&
                                       !NormalizeLicense(License).Equals(NormalizeLicense(LatestLicense), StringComparison.OrdinalIgnoreCase);

        private static bool IsNewerVersion(string? latestVersion, string currentVersion)
        {
            if (string.IsNullOrEmpty(latestVersion)) return false;

            // Simple semantic version comparison - parse major.minor.patch
            if (TryParseVersion(currentVersion, out var currentParts) &&
                TryParseVersion(latestVersion, out var latestParts))
            {
                for (int i = 0; i < Math.Min(currentParts.Length, latestParts.Length); i++)
                {
                    if (latestParts[i] > currentParts[i]) return true;
                    if (latestParts[i] < currentParts[i]) return false;
                }
                // If all compared parts are equal, check if latest has more parts
                return latestParts.Length > currentParts.Length;
            }

            // Fallback to string comparison if parsing fails
            return string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }

        private static bool TryParseVersion(string version, out int[] parts)
        {
            parts = Array.Empty<int>();
            if (string.IsNullOrEmpty(version)) return false;

            // Remove pre-release suffixes like "-alpha", "-beta", etc.
            var cleanVersion = Regex.Replace(version, @"[-+].*$", "");
            var versionParts = cleanVersion.Split('.');

            parts = new int[versionParts.Length];
            for (int i = 0; i < versionParts.Length; i++)
            {
                if (!int.TryParse(versionParts[i], out parts[i]))
                    return false;
            }
            return true;
        }

        private static string NormalizeLicense(string license)
        {
            if (string.IsNullOrEmpty(license)) return string.Empty;

            // Normalize common license variations for comparison
            var normalized = license.Trim().ToLowerInvariant();

            // Handle common variations
            var licenseMapping = new Dictionary<string, string>
            {
                { "mit", "mit" },
                { "mit license", "mit" },
                { "the mit license", "mit" },
                { "apache-2.0", "apache-2.0" },
                { "apache 2.0", "apache-2.0" },
                { "apache license 2.0", "apache-2.0" },
                { "bsd-3-clause", "bsd-3-clause" },
                { "bsd 3-clause", "bsd-3-clause" },
                { "bsd", "bsd" },
                { "gpl-3.0", "gpl-3.0" },
                { "gpl v3", "gpl-3.0" },
                { "see url", "see url" },
                { "see package contents", "see package contents" }
            };

            return licenseMapping.TryGetValue(normalized, out var mappedLicense) ? mappedLicense : normalized;
        }
    }
}

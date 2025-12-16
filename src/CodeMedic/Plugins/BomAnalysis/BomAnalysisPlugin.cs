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
    /// Fetches latest version information for packages using 'dotnet nuget search'.
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
                await FetchLatestVersionForPackageAsync(package);
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
    /// Fetches the latest version for a specific package using the NuGet API.
    /// </summary>
    private async Task FetchLatestVersionForPackageAsync(PackageInfo package)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeMedic/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Set reasonable timeout

            // Use the NuGet V3 API to get package information
            var apiUrl = $"https://api.nuget.org/v3-flatcontainer/{package.Name.ToLowerInvariant()}/index.json";

            var response = await httpClient.GetStringAsync(apiUrl);

            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (!doc.RootElement.TryGetProperty("versions", out var versionsElement) ||
                versionsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var versions = new List<string>();
            foreach (var element in versionsElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    var value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        versions.Add(value);
                    }
                }
            }

            if (versions.Count > 0)
            {
                var latestStable = versions.Where(v => !IsPreReleaseVersion(v)).LastOrDefault();
                package.LatestVersion = latestStable ?? versions.Last();
            }
        }
        catch (HttpRequestException ex)
        {
            // Package might not exist on nuget.org or network issue
            // Only log 404s for debugging, skip others as they're common for private packages
            if (ex.Message.Contains("404"))
            {
                Console.Error.WriteLine($"Debug: Package {package.Name} not found on nuget.org");
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout - skip silently
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse version data for {package.Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Log other unexpected errors but don't fail - this is supplementary information
            Console.Error.WriteLine($"Warning: Could not fetch latest version for {package.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if a version string represents a pre-release version.
    /// </summary>
    private static bool IsPreReleaseVersion(string version)
    {
        return version.Contains('-') || version.Contains('+');
    }

    /// <summary>
    /// Response model for NuGet V3 API version query.
    /// </summary>
    private class NuGetVersionResponse
    {
        public string[]? Versions { get; set; }
    }

    /// <summary>
    /// Response model for NuGet V3 API package metadata query.
    /// </summary>
    private class NuGetPackageResponse
    {
        public string? LicenseExpression { get; set; }
        public string? LicenseUrl { get; set; }
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
                await FetchLatestLicenseForPackageAsync(package);
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
    /// Fetches the latest license for a specific package using the NuGet V3 API.
    /// </summary>
    private async Task FetchLatestLicenseForPackageAsync(PackageInfo package)
    {
        // Skip if we don't have a latest version to check
        if (string.IsNullOrEmpty(package.LatestVersion))
            return;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeMedic/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(15); // Slightly longer timeout for metadata

            // Use the NuGet V3 API to get package metadata for the latest version
            var apiUrl = $"https://api.nuget.org/v3-flatcontainer/{package.Name.ToLowerInvariant()}/{package.LatestVersion.ToLowerInvariant()}/{package.Name.ToLowerInvariant()}.nuspec";

            var response = await httpClient.GetStringAsync(apiUrl);

            // Parse the nuspec XML to extract license information
            try
            {
                var doc = XDocument.Parse(response);
                var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

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
                            package.LatestLicense = licenseElement.Value?.Trim();
                        }
                        else if (licenseType == "file")
                        {
                            package.LatestLicense = "See package contents";
                        }
                    }
                    else
                    {
                        // Fall back to licenseUrl (older format)
                        var licenseUrl = metadata.Element(ns + "licenseUrl")?.Value?.Trim();
                        if (!string.IsNullOrWhiteSpace(licenseUrl))
                        {
                            package.LatestLicenseUrl = licenseUrl;
                            // Extract license type from URL patterns (same logic as local license detection)
                            if (licenseUrl.Contains("mit", StringComparison.OrdinalIgnoreCase))
                            {
                                package.LatestLicense = "MIT";
                            }
                            else if (licenseUrl.Contains("apache", StringComparison.OrdinalIgnoreCase))
                            {
                                package.LatestLicense = "Apache-2.0";
                            }
                            else if (licenseUrl.Contains("bsd", StringComparison.OrdinalIgnoreCase))
                            {
                                package.LatestLicense = "BSD";
                            }
                            else if (licenseUrl.Contains("gpl", StringComparison.OrdinalIgnoreCase))
                            {
                                package.LatestLicense = "GPL";
                            }
                            else
                            {
                                package.LatestLicense = "See URL";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Could not parse latest nuspec for {package.Name}: {ex.Message}");
            }
        }
        catch (HttpRequestException ex)
        {
            // Package might not exist on nuget.org or network issue
            if (ex.Message.Contains("404"))
            {
                Console.Error.WriteLine($"Debug: Latest version nuspec for {package.Name} not found on nuget.org");
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout - skip silently
        }
        catch (Exception ex)
        {
            // Log other unexpected errors but don't fail
            Console.Error.WriteLine($"Warning: Could not fetch latest license for {package.Name}: {ex.Message}");
        }
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

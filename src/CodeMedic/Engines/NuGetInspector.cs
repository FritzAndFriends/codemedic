using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using CodeMedic.Models;

namespace CodeMedic.Engines;

/// <summary>
/// Handles NuGet package restore, discovery, and inspection helpers used during repository scanning.
/// </summary>
public class NuGetInspector
{
    private readonly string _rootPath;
    private readonly string _normalizedRootPath;
    private readonly IFileSystem _fs;
    private readonly Dictionary<string, Dictionary<string, string>> _centralPackageVersionCache = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _centralPackageVersionFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance for inspecting NuGet data under the provided root path.
    /// </summary>
    /// <param name="rootPath">Root directory of the repository being scanned.</param>
    /// <param name="fileSystem">File system abstraction for I/O operations; defaults to physical file system if not provided.</param>
    public NuGetInspector(string rootPath, IFileSystem? fileSystem = null)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _normalizedRootPath = Path.TrimEndingDirectorySeparator(_rootPath);
        _fs = fileSystem ?? new PhysicalFileSystem();
        RefreshCentralPackageVersionFiles();
    }

    /// <summary>
    /// Restores NuGet packages for the repository to generate lock/assets files.
    /// </summary>
    public async Task RestorePackagesAsync()
    {
        try
        {
            Console.Error.WriteLine("Restoring NuGet packages...");

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{_rootPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
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
        }
    }

    /// <summary>
    /// Enumerates Directory.Packages.props files and resets cached central version data.
    /// </summary>
    public void RefreshCentralPackageVersionFiles()
    {
        try
        {
            _centralPackageVersionCache.Clear();
            _centralPackageVersionFiles = _fs
                .EnumerateFiles(_rootPath, "Directory.Packages.props", SearchOption.AllDirectories)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not enumerate central package management files: {ex.Message}");
            _centralPackageVersionFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Reads direct PackageReference entries, resolving versions via central package management when needed.
    /// </summary>
    /// <param name="projectRoot">Root XML element of the project file.</param>
    /// <param name="xmlNamespace">XML namespace of the project file.</param>
    /// <param name="projectDirectory">Directory containing the project file.</param>
    /// <returns>List of resolved direct package dependencies.</returns>
    public List<Package> ReadPackageReferences(XElement projectRoot, XNamespace xmlNamespace, string projectDirectory)
    {
        var packageReferences = projectRoot.Descendants(xmlNamespace + "PackageReference").ToList();
        var packageDependencies = new List<Package>();

        foreach (var pr in packageReferences)
        {
            var packageName = pr.Attribute("Include")?.Value
                             ?? pr.Attribute("Update")?.Value
                             ?? "unknown";

            var version = pr.Attribute("Version")?.Value
                          ?? pr.Element(xmlNamespace + "Version")?.Value;

            if (string.IsNullOrWhiteSpace(version))
            {
                version = ResolveCentralPackageVersion(packageName, projectDirectory) ?? "unknown";
            }

            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageName = "unknown";
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                version = "unknown";
            }

            packageDependencies.Add(new Package(packageName, version));
        }

        return packageDependencies;
    }

    /// <summary>
    /// Extracts transitive dependencies from packages.lock.json or project.assets.json file.
    /// Transitive dependencies are packages that are pulled in by direct dependencies.
    /// Project references are excluded from the results as they are not NuGet packages.
    /// </summary>
    public List<TransitiveDependency> ExtractTransitiveDependencies(string projectFilePath, List<Package> directDependencies, List<ProjectReference> projectReferences)
    {
        var transitiveDeps = new List<TransitiveDependency>();
        var projectDir = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
        var projectRefNames = projectReferences.Select(pr => pr.ProjectName.ToLower()).ToHashSet();

        var lockFilePath = Path.Combine(projectDir, "packages.lock.json");
        if (_fs.FileExists(lockFilePath))
        {
            transitiveDeps.AddRange(ExtractFromLockFile(lockFilePath, directDependencies, projectRefNames));
            return transitiveDeps;
        }

        var assetsFilePath = Path.Combine(projectDir, "obj", "project.assets.json");
        if (_fs.FileExists(assetsFilePath))
        {
            transitiveDeps.AddRange(ExtractFromAssetsFile(assetsFilePath, directDependencies, projectRefNames));
        }

        return transitiveDeps;
    }

    private string? ResolveCentralPackageVersion(string packageName, string projectDirectory)
    {
        if (_centralPackageVersionFiles.Count == 0)
        {
            return null;
        }

        try
        {
            var current = new DirectoryInfo(Path.GetFullPath(projectDirectory));

            while (current != null)
            {
                var currentPath = Path.TrimEndingDirectorySeparator(current.FullName);
                var propsPath = Path.Combine(current.FullName, "Directory.Packages.props");

                if (_centralPackageVersionFiles.Contains(propsPath))
                {
                    var versions = GetCentralPackageVersions(propsPath);

                    if (versions.TryGetValue(packageName, out var resolvedVersion))
                    {
                        return resolvedVersion;
                    }
                }

                if (string.Equals(currentPath, _normalizedRootPath, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = current.Parent;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not resolve central package version for {packageName}: {ex.Message}");
        }

        return null;
    }

    private Dictionary<string, string> GetCentralPackageVersions(string propsPath)
    {
        if (_centralPackageVersionCache.TryGetValue(propsPath, out var cached))
        {
            return cached;
        }

        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var stream = _fs.OpenRead(propsPath);
            var doc = XDocument.Load(stream);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            var packageVersionElements = doc.Descendants(ns + "PackageVersion");

            foreach (var pkg in packageVersionElements)
            {
                var name = pkg.Attribute("Include")?.Value ?? pkg.Attribute("Update")?.Value;
                var version = pkg.Attribute("Version")?.Value ?? pkg.Element(ns + "Version")?.Value;

                if (string.IsNullOrWhiteSpace(version))
                {
                    version = pkg.Attribute("VersionOverride")?.Value ?? pkg.Element(ns + "VersionOverride")?.Value;
                }

                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
                {
                    versions[name] = version;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not read central package versions from {propsPath}: {ex.Message}");
        }

        _centralPackageVersionCache[propsPath] = versions;
        return versions;
    }

    private List<TransitiveDependency> ExtractFromLockFile(string lockFilePath, List<Package> directDependencies, HashSet<string> projectReferenceNames)
    {
        var transitiveDeps = new List<TransitiveDependency>();
        var directPackageNames = directDependencies.Select(d => d.Name.ToLower()).ToHashSet();

        try
        {
            using var stream = _fs.OpenRead(lockFilePath);
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            if (root.TryGetProperty("dependencies", out var dependencies))
            {
                foreach (var framework in dependencies.EnumerateObject())
                {
                    foreach (var package in framework.Value.EnumerateObject())
                    {
                        var packageName = package.Name;

                        if (directPackageNames.Contains(packageName.ToLower()))
                        {
                            continue;
                        }

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

    private List<TransitiveDependency> ExtractFromAssetsFile(string assetsFilePath, List<Package> directDependencies, HashSet<string> projectReferenceNames)
    {
        var transitiveDeps = new List<TransitiveDependency>();
        var directPackageNames = directDependencies.Select(d => d.Name.ToLower()).ToHashSet();

        try
        {
            using var stream = _fs.OpenRead(assetsFilePath);
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            if (root.TryGetProperty("libraries", out var libraries))
            {
                foreach (var library in libraries.EnumerateObject())
                {
                    var libraryName = library.Name;
                    var parts = libraryName.Split('/');
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    var packageName = parts[0];
                    var version = parts[1];

                    if (directPackageNames.Contains(packageName.ToLower()))
                    {
                        continue;
                    }

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

                    if (!directDependencies.Any(d => d.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

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
    /// Gets the NuGet global packages folder path by executing 'dotnet nuget locals global-packages --list'.
    /// </summary>
    public async Task<string?> GetNuGetGlobalPackagesFolderAsync()
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
    /// Fetches the latest version for a specific package using the NuGet API.
    /// </summary>
    public async Task<string?> FetchLatestVersionAsync(string packageName, string currentVersion)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeMedic/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var apiUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/index.json";
            var response = await httpClient.GetStringAsync(apiUrl);

            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!doc.RootElement.TryGetProperty("versions", out var versionsElement) ||
                versionsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
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
                return latestStable ?? versions.Last();
            }
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.Contains("404"))
            {
                Console.Error.WriteLine($"Debug: Package {packageName} not found on nuget.org");
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout - skip silently
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse version data for {packageName}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not fetch latest version for {packageName}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Fetches the published date of the latest version (including prerelease) for a package from NuGet.org.
    /// Returns null if the package is not found or if the date cannot be determined.
    /// </summary>
    public async Task<DateTime?> FetchLatestVersionPublishedDateAsync(string packageName)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeMedic/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            // Use the NuGet V3 registration API to get package metadata
            var apiUrl = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";
            var response = await httpClient.GetStringAsync(apiUrl);

            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // The root-level commitTimeStamp represents the most recent catalog update,
            // which corresponds to the publish date of the latest version
            if (doc.RootElement.TryGetProperty("commitTimeStamp", out var commitTimeStamp) &&
                commitTimeStamp.ValueKind == JsonValueKind.String)
            {
                var timestampStr = commitTimeStamp.GetString();
                if (!string.IsNullOrWhiteSpace(timestampStr) &&
                    DateTime.TryParse(timestampStr, out var publishedDate))
                {
                    return publishedDate;
                }
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.Contains("404"))
            {
                Console.Error.WriteLine($"Debug: Package {packageName} not found on nuget.org");
            }
            else
            {
                Console.Error.WriteLine($"Warning: HTTP error fetching publish date for {packageName}: {ex.Message}");
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout - skip silently
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse publish date data for {packageName}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not fetch publish date for {packageName}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Determines if a version string represents a pre-release version.
    /// </summary>
    public static bool IsPreReleaseVersion(string version)
    {
        return version.Contains('-') || version.Contains('+');
    }

    /// <summary>
    /// Fetches license information from a local .nuspec file in the NuGet global packages cache.
    /// Returns a tuple of (License, LicenseUrl).
    /// </summary>
    public async Task<(string? License, string? LicenseUrl)> FetchLicenseFromLocalCacheAsync(string packageName, string version)
    {
        var globalPackagesPath = await GetNuGetGlobalPackagesFolderAsync();
        if (string.IsNullOrEmpty(globalPackagesPath))
        {
            return (null, null);
        }

        try
        {
            var packageFolder = Path.Combine(globalPackagesPath, packageName.ToLowerInvariant(), version.ToLowerInvariant());
            var nuspecPath = Path.Combine(packageFolder, $"{packageName.ToLowerInvariant()}.nuspec");

            if (!File.Exists(nuspecPath))
            {
                nuspecPath = Path.Combine(packageFolder, $"{packageName}.nuspec");
                if (!File.Exists(nuspecPath))
                {
                    return (null, null);
                }
            }

            var nuspecContent = await File.ReadAllTextAsync(nuspecPath);
            var doc = XDocument.Parse(nuspecContent);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var metadata = doc.Root?.Element(ns + "metadata");
            if (metadata == null)
            {
                return (null, null);
            }

            // Check for license element first (newer format)
            var licenseElement = metadata.Element(ns + "license");
            if (licenseElement != null)
            {
                var licenseType = licenseElement.Attribute("type")?.Value;
                if (licenseType == "expression")
                {
                    return (licenseElement.Value?.Trim(), null);
                }
                else if (licenseType == "file")
                {
                    return ("See package contents", null);
                }
            }

            // Fall back to licenseUrl (older format)
            var licenseUrl = metadata.Element(ns + "licenseUrl")?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(licenseUrl))
            {
                var license = ExtractLicenseFromUrl(licenseUrl);
                return (license, licenseUrl);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Error reading license for {packageName}: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Fetches license information from the NuGet API for a specific version.
    /// Returns a tuple of (License, LicenseUrl).
    /// </summary>
    public async Task<(string? License, string? LicenseUrl)> FetchLicenseFromApiAsync(string packageName, string version)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeMedic/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            var apiUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/{version.ToLowerInvariant()}/{packageName.ToLowerInvariant()}.nuspec";
            var response = await httpClient.GetStringAsync(apiUrl);

            var doc = XDocument.Parse(response);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var metadata = doc.Root?.Element(ns + "metadata");
            if (metadata == null)
            {
                return (null, null);
            }

            // Check for license element first (newer format)
            var licenseElement = metadata.Element(ns + "license");
            if (licenseElement != null)
            {
                var licenseType = licenseElement.Attribute("type")?.Value;
                if (licenseType == "expression")
                {
                    return (licenseElement.Value?.Trim(), null);
                }
                else if (licenseType == "file")
                {
                    return ("See package contents", null);
                }
            }

            // Fall back to licenseUrl (older format)
            var licenseUrl = metadata.Element(ns + "licenseUrl")?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(licenseUrl))
            {
                var license = ExtractLicenseFromUrl(licenseUrl);
                return (license, licenseUrl);
            }

            return (null, null);
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.Contains("404"))
            {
                Console.Error.WriteLine($"Debug: Nuspec for {packageName} version {version} not found on nuget.org");
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout - skip silently
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not fetch license for {packageName}: {ex.Message}");
        }

        return (null, null);
    }

    /// <summary>
    /// Extracts a license identifier from a license URL using common patterns.
    /// </summary>
    private static string ExtractLicenseFromUrl(string licenseUrl)
    {
        if (licenseUrl.Contains("mit", StringComparison.OrdinalIgnoreCase))
        {
            return "MIT";
        }
        else if (licenseUrl.Contains("apache", StringComparison.OrdinalIgnoreCase))
        {
            return "Apache-2.0";
        }
        else if (licenseUrl.Contains("bsd", StringComparison.OrdinalIgnoreCase))
        {
            return "BSD";
        }
        else if (licenseUrl.Contains("gpl", StringComparison.OrdinalIgnoreCase))
        {
            return "GPL";
        }
        else
        {
            return "See URL";
        }
    }

    /// <summary>
    /// Analyzes package metadata to determine source type and commercial status.
    /// Returns a tuple of (SourceType, Commercial).
    /// </summary>
    public static (string SourceType, string Commercial) DetermineSourceTypeAndCommercialStatus(
        string packageName,
        string? license,
        string? licenseUrl,
        string? projectUrl,
        string? repositoryUrl,
        string? authors,
        string? owners)
    {
        var packageId = packageName.ToLowerInvariant();
        var lowerLicense = license?.ToLowerInvariant();
        var lowerLicenseUrl = licenseUrl?.ToLowerInvariant();
        var lowerProjectUrl = projectUrl?.ToLowerInvariant();
        var lowerRepositoryUrl = repositoryUrl?.ToLowerInvariant();
        var lowerAuthors = authors?.ToLowerInvariant();
        var lowerOwners = owners?.ToLowerInvariant();

        // Determine if it's open source
        var isOpenSource = false;

        var openSourceLicenses = new[] {
            "mit", "apache", "bsd", "gpl", "lgpl", "mpl", "isc", "unlicense",
            "cc0", "zlib", "ms-pl", "ms-rl", "eclipse", "cddl", "artistic"
        };

        if (!string.IsNullOrEmpty(lowerLicense))
        {
            isOpenSource = openSourceLicenses.Any(oss => lowerLicense.Contains(oss));
        }

        if (!isOpenSource && !string.IsNullOrEmpty(lowerLicenseUrl))
        {
            isOpenSource = openSourceLicenses.Any(oss => lowerLicenseUrl.Contains(oss)) ||
                          lowerLicenseUrl.Contains("github.com") ||
                          lowerLicenseUrl.Contains("opensource.org");
        }

        // Check repository URLs
        if (!isOpenSource)
        {
            var urls = new[] { lowerProjectUrl, lowerRepositoryUrl }.Where(url => !string.IsNullOrEmpty(url));
            isOpenSource = urls.Any(url =>
                url!.Contains("github.com") ||
                url.Contains("gitlab.com") ||
                url.Contains("bitbucket.org") ||
                url.Contains("codeplex.com") ||
                url.Contains("sourceforge.net"));
        }

        // Determine commercial status
        var isMicrosoft = packageId.StartsWith("microsoft.") ||
                         packageId.StartsWith("system.") ||
                         !string.IsNullOrEmpty(lowerAuthors) && lowerAuthors.Contains("microsoft") ||
                         !string.IsNullOrEmpty(lowerOwners) && lowerOwners.Contains("microsoft");

        var commercialIndicators = new[] {
            "commercial", "proprietary", "enterprise", "professional", "premium",
            "telerik", "devexpress", "syncfusion", "infragistics", "componentone"
        };

        var hasCommercialIndicators = commercialIndicators.Any(indicator =>
            (!string.IsNullOrEmpty(lowerLicense) && lowerLicense.Contains(indicator)) ||
            (!string.IsNullOrEmpty(lowerAuthors) && lowerAuthors.Contains(indicator)) ||
            (packageId.Contains(indicator)));

        var commercialLicenses = new[] { "proprietary", "commercial", "eula" };
        var hasCommercialLicense = !string.IsNullOrEmpty(lowerLicense) &&
                                  commercialLicenses.Any(cl => lowerLicense.Contains(cl));

        // Determine source type
        string sourceType;
        if (isOpenSource)
        {
            sourceType = "Open Source";
        }
        else if (hasCommercialLicense || hasCommercialIndicators)
        {
            sourceType = "Closed Source";
        }
        else if (isMicrosoft)
        {
            sourceType = "Closed Source";
        }
        else
        {
            sourceType = "Unknown";
        }

        // Determine commercial status
        string commercial;
        if (hasCommercialLicense || hasCommercialIndicators)
        {
            commercial = "Yes";
        }
        else if (isOpenSource || isMicrosoft)
        {
            commercial = "No";
        }
        else
        {
            commercial = "Unknown";
        }

        return (sourceType, commercial);
    }
}

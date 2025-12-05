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
}

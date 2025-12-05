# NuGet Scanning Architecture

## Overview

The NuGet scanning subsystem is responsible for discovering, parsing, and analyzing NuGet package dependencies across .NET projects in a repository. It handles both direct package references and transitive (indirect) dependencies, with special support for central package management.

## Architecture

### Components

```
RepositoryScanner
    │
    ├── NuGetInspector
    │   │
    │   ├── RestorePackagesAsync()          # Restore packages for lock/assets files
    │   ├── RefreshCentralPackageVersionFiles()
    │   ├── ReadPackageReferences()         # Parse direct PackageReference entries
    │   │
    │   └── ExtractTransitiveDependencies() # Extract indirect dependencies
    │       │
    │       ├── ExtractFromLockFile()       # From packages.lock.json
    │       └── ExtractFromAssetsFile()     # From project.assets.json
    │
    └── PackageVersionMismatchDetection     # Cross-project version alignment
```

### Key Classes

#### **RepositoryScanner**
The main scanning orchestrator that:
- Discovers all `.csproj` files in the repository
- Delegates NuGet operations to `NuGetInspector`
- Aggregates results from all projects
- Generates comprehensive health reports
- Detects package version mismatches across projects

**Key Methods:**
- `ScanAsync()` - Main scanning entry point
- `GenerateReport()` - Creates structured report document
- `FindPackageVersionMismatches()` - Identifies inconsistent package versions

#### **NuGetInspector**
Specialized handler for all NuGet-related operations:
- Package restore and lock file generation
- Direct package reference resolution
- Central package management (Directory.Packages.props) support
- Transitive dependency extraction

**Key Methods:**
- `RestorePackagesAsync()` - Executes `dotnet restore` to generate lock/assets files
- `RefreshCentralPackageVersionFiles()` - Discovers Directory.Packages.props files
- `ReadPackageReferences()` - Parses and resolves direct dependencies
- `ExtractTransitiveDependencies()` - Extracts indirect dependencies

### Data Flow

```
1. Repository Scan
   └─> Discover all .csproj files
   
2. Package Restore
   └─> NuGetInspector.RestorePackagesAsync()
       └─> Generates: packages.lock.json or obj/project.assets.json
       
3. Central Package Discovery
   └─> NuGetInspector.RefreshCentralPackageVersionFiles()
       └─> Finds all Directory.Packages.props files
       └─> Caches version definitions
       
4. Parse Project Files
   └─> For each .csproj:
       
       a. Read PropertyGroup settings
          └─> Target framework, output type, nullable, etc.
       
       b. Read PackageReference entries
          └─> NuGetInspector.ReadPackageReferences()
              └─> Resolves missing versions from Directory.Packages.props
              └─> Builds Package(Name, Version) list
       
       c. Read ProjectReference entries
          └─> Internal project-to-project references
       
       d. Extract transitive dependencies
          └─> NuGetInspector.ExtractTransitiveDependencies()
              └─> Reads packages.lock.json (preferred)
                  OR obj/project.assets.json (fallback)
              └─> Filters out direct deps and project refs
              └─> Builds TransitiveDependency list
       
5. Aggregate and Detect Mismatches
   └─> FindPackageVersionMismatches()
       └─> Groups packages by name across projects
       └─> Identifies versions with conflicts
       
6. Generate Report
   └─> ReportDocument with sections:
       - Summary statistics
       - Package version mismatches (if any)
       - Project listing and details
       - Parse errors (if any)
```

## Central Package Management Support

CodeMedic supports the MSBuild [central package management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) feature.

### How It Works

When `Directory.Packages.props` exists at the repository root or in project directories:

1. **Discovery Phase**
   - `RefreshCentralPackageVersionFiles()` recursively finds all `Directory.Packages.props` files
   - Paths are cached for fast lookups

2. **Resolution Phase**
   - When a project references a package without a version attribute:
     ```xml
     <PackageReference Include="Spectre.Console" />
     ```
   - `ReadPackageReferences()` walks up from the project directory to the repo root
   - Looks for `Directory.Packages.props` in each directory
   - Parses `<PackageVersion>` entries:
     ```xml
     <PackageVersion Include="Spectre.Console" Version="0.49.0" />
     ```

3. **Version Resolution**
   - Version resolved from central file replaces missing inline version
   - Supports `Update` attribute for version overrides
   - Falls back to "unknown" if version cannot be resolved

### Example Structure

```
Repository Root/
├── Directory.Packages.props        # Central definitions
│   └── <PackageVersion Include="Serilog" Version="3.0.0" />
│
└── src/
    ├── Project1/
    │   └── Project1.csproj
    │       └── <PackageReference Include="Serilog" />  ← Resolved to 3.0.0
    │
    └── Project2/
        └── Project2.csproj
            └── <PackageReference Include="Serilog" />  ← Resolved to 3.0.0
```

## Package Version Mismatch Detection

CodeMedic identifies when different projects use different versions of the same package and recommends alignment.

### Algorithm

1. Aggregate all packages from all projects
2. Group by package name (case-insensitive)
3. For each package:
   - Collect all distinct versions used
   - If more than one distinct version exists:
     - Add to mismatches list
     - Record which projects use which versions

### Report Output

When mismatches are detected, the report includes a dedicated section:

```
Package Version Mismatches
──────────────────────────
Align package versions across projects to avoid restore/runtime drift.

Packages with differing versions:
  • Newtonsoft.Json: Project1=12.0.3, Project2=13.0.1
  • Serilog: Project1=2.12.0, Project2=3.0.0
```

## Transitive Dependencies

CodeMedic distinguishes between direct and transitive dependencies:

- **Direct dependencies** come from `<PackageReference>` in project files
- **Transitive dependencies** are pulled in by direct dependencies (from lock/assets files)

### Source Tracking

For transitive dependencies, the system attempts to track which direct dependency introduced them:
- Walks the NuGet graph from lock/assets file
- Links transitive package back to originating direct dependency
- Stored in `TransitiveDependency.SourcePackage` property

### Private Assets Handling

When a dependency is marked as private (`PrivateAssets="All"`):
- Not exposed to projects that reference the current project
- Still tracked and reported
- Marked with `IsPrivate` flag

## File System Abstraction

To improve testability, `NuGetInspector` uses an abstraction layer for file operations:

### INuGetFileSystem Interface

```csharp
public interface INuGetFileSystem
{
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    bool FileExists(string path);
    Stream OpenRead(string path);
}
```

### Implementation Options

- **PhysicalNuGetFileSystem** - Default implementation using actual file system
- **Mock implementations** - For unit testing (inject custom implementations)

### Usage Example

```csharp
// Production usage (physical file system)
var inspector = new NuGetInspector(rootPath);

// Testing usage (mock file system)
var mockFs = new MockNuGetFileSystem();
var inspector = new NuGetInspector(rootPath, mockFs);
```

## Error Handling

The NuGet scanning subsystem is designed to be resilient:

- **Package restore failures** are logged but don't stop scanning
- **Missing lock/assets files** result in no transitive dependencies (acceptable)
- **Central package file parse errors** are caught and logged
- **Individual project failures** are recorded in `ProjectInfo.ParseErrors`
- **Partial results** are preferred over complete failure

## Performance Considerations

### Optimizations

1. **Parallel Line Counting**
   - C# file counting uses `Parallel.ForEach` for multi-core utilization
   - Respects system processor count for optimal performance

2. **Caching**
   - Central package versions cached after first read
   - Prevents redundant XML parsing

3. **Early Returns**
   - Transitive extraction checks `packages.lock.json` first (faster)
   - Falls back to `project.assets.json` only if needed

4. **Streaming**
   - Uses streams for XML and JSON parsing (lower memory overhead)
   - Replaces string-based parsing

### Time Complexity

- **Per-project scanning** - O(n) where n = number of C# files
- **Lock file parsing** - O(m) where m = number of dependencies
- **Central package lookup** - O(log d) where d = directory depth
- **Overall** - Linear with repository size

## Testing Strategy

The file system abstraction enables comprehensive unit testing:

```csharp
[Fact]
public void ReadPackageReferences_ResolvesFromCentralPackageFile()
{
    var mockFs = new MockNuGetFileSystem();
    mockFs.AddFile("Directory.Packages.props", 
        @"<PackageVersion Include=""TestPkg"" Version=""1.0.0"" />");
    
    var inspector = new NuGetInspector(rootPath, mockFs);
    var packages = inspector.ReadPackageReferences(projectRoot, ns, projectDir);
    
    Assert.Contains(packages, p => p.Name == "TestPkg" && p.Version == "1.0.0");
}
```

## Future Enhancements

1. **Transitive Version Conflict Detection**
   - Warn when transitive dependencies have conflicting versions
   - Suggest explicit pinning via PackageVersion entries

2. **Dependency Graph Visualization**
   - Generate mermaid diagrams showing dependency chains
   - Export as JSON for tooling integration

3. **Vulnerability Scanning**
   - Integrate with NuGet security advisories
   - Flag known vulnerabilities in dependencies

4. **License Analysis**
   - Extract and report package licenses
   - Detect license compliance issues

5. **Package Age and Maintenance**
   - Check when packages were last updated
   - Flag abandoned or dormant packages

## Related Documentation

- [Repository Health Dashboard](feature_repository-health-dashboard.md) - High-level overview
- [Bill of Materials](feature_bill-of-materials.md) - Comprehensive dependency inventory
- [CLI Architecture](cli_architecture.md) - Command structure and extensibility

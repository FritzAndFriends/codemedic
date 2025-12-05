using System.Text;
using System.Xml.Linq;
using CodeMedic.Abstractions;
using CodeMedic.Engines;
using CodeMedic.Models;
using Moq;

namespace Test.CodeMedic.Engines;

/// <summary>
/// Unit tests for NuGetInspector using Given-When-Then syntax.
/// </summary>
public class NuGetInspectorTests
{
    /// <summary>
    /// Gets a cross-platform root path for testing.
    /// Uses /tmp/TestRepo on Unix and C:\TestRepo on Windows.
    /// </summary>
    private static string TestRootPath => OperatingSystem.IsWindows() 
        ? @"C:\TestRepo" 
        : "/tmp/TestRepo";

    /// <summary>
    /// Gets a cross-platform project directory path for testing.
    /// </summary>
    private static string TestProjectDirectory => Path.Combine(TestRootPath, "src", "MyProject");

    /// <summary>
    /// Gets a cross-platform project file path for testing.
    /// </summary>
    private static string TestProjectFilePath => Path.Combine(TestProjectDirectory, "MyProject.csproj");

    /// <summary>
    /// Gets a cross-platform Directory.Packages.props path for testing.
    /// </summary>
    private static string TestPropsPath => Path.Combine(TestRootPath, "Directory.Packages.props");

    #region Constructor Tests

    [Fact]
    public void Constructor_GivenValidRootPath_WhenCreatingInspector_ThenInitializesSuccessfully()
    {
        // Given
        var rootPath = TestRootPath;
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        // When
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // Then
        Assert.NotNull(inspector);
        mockFileSystem.Verify(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories), Times.Once);
    }

    [Fact]
    public void Constructor_GivenNullFileSystem_WhenCreatingInspector_ThenUsesPhysicalFileSystem()
    {
        // Given
        var rootPath = Path.GetTempPath();

        // When
        var inspector = new NuGetInspector(rootPath, null);

        // Then
        Assert.NotNull(inspector);
    }

    #endregion

    #region RefreshCentralPackageVersionFiles Tests

    [Fact]
    public void RefreshCentralPackageVersionFiles_GivenDirectoryPackagesPropsExists_WhenRefreshing_ThenFindsFiles()
    {
        // Given
        var rootPath = TestRootPath;
        var mockFileSystem = new Mock<IFileSystem>();
        var propsFiles = new[] { TestPropsPath };
        
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(propsFiles);

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        inspector.RefreshCentralPackageVersionFiles();

        // Then
        mockFileSystem.Verify(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories), Times.Exactly(2)); // Once in constructor, once in refresh
    }

    [Fact]
    public void RefreshCentralPackageVersionFiles_GivenFileSystemThrowsException_WhenRefreshing_ThenHandlesGracefully()
    {
        // Given
        var rootPath = TestRootPath;
        var mockFileSystem = new Mock<IFileSystem>();
        var callCount = 0;
        
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(() =>
            {
                callCount++;
                if (callCount > 1)
                {
                    throw new UnauthorizedAccessException("Access denied");
                }
                return Array.Empty<string>();
            });

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var exception = Record.Exception(() => inspector.RefreshCentralPackageVersionFiles());

        // Then
        Assert.Null(exception);
    }

    #endregion

    #region ReadPackageReferences Tests

    [Fact]
    public void ReadPackageReferences_GivenProjectWithDirectVersions_WhenReading_ThenReturnsPackagesWithVersions()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""Moq"" Version=""4.20.72"" />
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Equal(2, packages.Count);
        Assert.Contains(packages, p => p.Name == "Newtonsoft.Json" && p.Version == "13.0.3");
        Assert.Contains(packages, p => p.Name == "Moq" && p.Version == "4.20.72");
    }

    [Fact]
    public void ReadPackageReferences_GivenPackageWithVersionElement_WhenReading_ThenReadsVersionFromElement()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Spectre.Console"">
      <Version>0.49.1</Version>
    </PackageReference>
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Single(packages);
        Assert.Equal("Spectre.Console", packages[0].Name);
        Assert.Equal("0.49.1", packages[0].Version);
    }

    [Fact]
    public void ReadPackageReferences_GivenPackageWithUpdateAttribute_WhenReading_ThenUsesUpdateAttribute()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Update=""xunit"" Version=""2.9.3"" />
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Single(packages);
        Assert.Equal("xunit", packages[0].Name);
        Assert.Equal("2.9.3", packages[0].Version);
    }

    [Fact]
    public void ReadPackageReferences_GivenPackageWithCentralVersionManagement_WhenReading_ThenResolvesCentralVersion()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var propsPath = TestPropsPath;
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(new[] { propsPath });

        var propsXml = @"
<Project>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";

        mockFileSystem.Setup(fs => fs.FileExists(propsPath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(propsPath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(propsXml)));

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Single(packages);
        Assert.Equal("Newtonsoft.Json", packages[0].Name);
        Assert.Equal("13.0.3", packages[0].Version);
    }

    [Fact]
    public void ReadPackageReferences_GivenPackageWithMissingVersion_WhenNoCentralVersion_ThenReturnsUnknownVersion()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""SomePackage"" />
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Single(packages);
        Assert.Equal("SomePackage", packages[0].Name);
        Assert.Equal("unknown", packages[0].Version);
    }

    [Fact]
    public void ReadPackageReferences_GivenEmptyProject_WhenReading_ThenReturnsEmptyList()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Empty(packages);
    }

    #endregion

    #region ExtractTransitiveDependencies Tests

    [Fact]
    public void ExtractTransitiveDependencies_GivenLockFileExists_WhenExtracting_ThenReturnsTransitiveDependencies()
    {
        // Given
        var rootPath = TestRootPath;
        var projectFilePath = TestProjectFilePath;
        var lockFilePath = Path.Combine(TestProjectDirectory, "packages.lock.json");
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var lockFileJson = @"{
  ""version"": 1,
  ""dependencies"": {
    ""net10.0"": {
      ""Newtonsoft.Json"": {
        ""type"": ""Direct"",
        ""requested"": ""[13.0.3, )"",
        ""resolved"": ""13.0.3""
      },
      ""System.Text.Json"": {
        ""type"": ""Transitive"",
        ""resolved"": ""8.0.0"",
        ""dependencies"": {
          ""Newtonsoft.Json"": ""13.0.3""
        }
      }
    }
  }
}";

        mockFileSystem.Setup(fs => fs.FileExists(lockFilePath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(lockFilePath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(lockFileJson)));

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);
        var directDependencies = new List<Package> { new("Newtonsoft.Json", "13.0.3") };
        var projectReferences = new List<ProjectReference>();

        // When
        var transitiveDeps = inspector.ExtractTransitiveDependencies(
            projectFilePath,
            directDependencies,
            projectReferences);

        // Then
        Assert.Single(transitiveDeps);
        Assert.Equal("System.Text.Json", transitiveDeps[0].PackageName);
        Assert.Equal("8.0.0", transitiveDeps[0].Version);
        Assert.Equal("Newtonsoft.Json", transitiveDeps[0].SourcePackage);
        Assert.Equal(1, transitiveDeps[0].Depth);
    }

    [Fact]
    public void ExtractTransitiveDependencies_GivenAssetsFileExists_WhenNoLockFile_ThenReadsFromAssetsFile()
    {
        // Given
        var rootPath = TestRootPath;
        var projectFilePath = TestProjectFilePath;
        var lockFilePath = Path.Combine(TestProjectDirectory, "packages.lock.json");
        var assetsFilePath = Path.Combine(TestProjectDirectory, "obj", "project.assets.json");
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var assetsFileJson = @"{
  ""version"": 3,
  ""targets"": {
    ""net10.0"": {
      ""Moq/4.20.72"": {
        ""type"": ""package"",
        ""dependencies"": {
          ""Castle.Core"": ""5.1.1""
        }
      },
      ""Castle.Core/5.1.1"": {
        ""type"": ""package""
      }
    }
  },
  ""libraries"": {
    ""Moq/4.20.72"": {
      ""type"": ""package""
    },
    ""Castle.Core/5.1.1"": {
      ""type"": ""package""
    }
  }
}";

        mockFileSystem.Setup(fs => fs.FileExists(lockFilePath)).Returns(false);
        mockFileSystem.Setup(fs => fs.FileExists(assetsFilePath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(assetsFilePath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(assetsFileJson)));

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);
        var directDependencies = new List<Package> { new("Moq", "4.20.72") };
        var projectReferences = new List<ProjectReference>();

        // When
        var transitiveDeps = inspector.ExtractTransitiveDependencies(
            projectFilePath,
            directDependencies,
            projectReferences);

        // Then
        Assert.Single(transitiveDeps);
        Assert.Equal("Castle.Core", transitiveDeps[0].PackageName);
        Assert.Equal("5.1.1", transitiveDeps[0].Version);
        Assert.Equal("Moq", transitiveDeps[0].SourcePackage);
    }

    [Fact]
    public void ExtractTransitiveDependencies_GivenProjectReferences_WhenExtracting_ThenExcludesProjectReferences()
    {
        // Given
        var rootPath = TestRootPath;
        var projectFilePath = TestProjectFilePath;
        var lockFilePath = Path.Combine(TestProjectDirectory, "packages.lock.json");
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var lockFileJson = @"{
  ""version"": 1,
  ""dependencies"": {
    ""net10.0"": {
      ""MyOtherProject"": {
        ""type"": ""Project"",
        ""resolved"": ""1.0.0""
      },
      ""ActualPackage"": {
        ""type"": ""Transitive"",
        ""resolved"": ""2.0.0""
      }
    }
  }
}";

        mockFileSystem.Setup(fs => fs.FileExists(lockFilePath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(lockFilePath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(lockFileJson)));

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);
        var directDependencies = new List<Package>();
        var projectReferences = new List<ProjectReference>
        {
            new() { ProjectName = "MyOtherProject", Path = "../MyOtherProject/MyOtherProject.csproj" }
        };

        // When
        var transitiveDeps = inspector.ExtractTransitiveDependencies(
            projectFilePath,
            directDependencies,
            projectReferences);

        // Then
        Assert.Single(transitiveDeps);
        Assert.Equal("ActualPackage", transitiveDeps[0].PackageName);
    }

    [Fact]
    public void ExtractTransitiveDependencies_GivenNoLockOrAssetsFile_WhenExtracting_ThenReturnsEmptyList()
    {
        // Given
        var rootPath = TestRootPath;
        var projectFilePath = TestProjectFilePath;
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);
        var directDependencies = new List<Package> { new("Moq", "4.20.72") };
        var projectReferences = new List<ProjectReference>();

        // When
        var transitiveDeps = inspector.ExtractTransitiveDependencies(
            projectFilePath,
            directDependencies,
            projectReferences);

        // Then
        Assert.Empty(transitiveDeps);
    }

    [Fact]
    public void ExtractTransitiveDependencies_GivenDirectDependencies_WhenExtracting_ThenExcludesDirectDependencies()
    {
        // Given
        var rootPath = TestRootPath;
        var projectFilePath = TestProjectFilePath;
        var lockFilePath = Path.Combine(TestProjectDirectory, "packages.lock.json");
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());

        var lockFileJson = @"{
  ""version"": 1,
  ""dependencies"": {
    ""net10.0"": {
      ""Moq"": {
        ""type"": ""Direct"",
        ""resolved"": ""4.20.72""
      },
      ""Castle.Core"": {
        ""type"": ""Transitive"",
        ""resolved"": ""5.1.1""
      }
    }
  }
}";

        mockFileSystem.Setup(fs => fs.FileExists(lockFilePath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(lockFilePath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(lockFileJson)));

        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);
        var directDependencies = new List<Package> { new("Moq", "4.20.72") };
        var projectReferences = new List<ProjectReference>();

        // When
        var transitiveDeps = inspector.ExtractTransitiveDependencies(
            projectFilePath,
            directDependencies,
            projectReferences);

        // Then
        Assert.Single(transitiveDeps);
        Assert.Equal("Castle.Core", transitiveDeps[0].PackageName);
        Assert.DoesNotContain(transitiveDeps, t => t.PackageName == "Moq");
    }

    #endregion

    #region Central Package Management Tests

    [Fact]
    public void ReadPackageReferences_GivenNestedDirectoryPackagesProps_WhenReading_ThenUsesClosestPropsFile()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var rootPropsPath = Path.Combine(rootPath, "Directory.Packages.props");
        var srcPropsPath = Path.Combine(rootPath, "src", "Directory.Packages.props");
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(new[] { rootPropsPath, srcPropsPath });

        var rootPropsXml = @"
<Project>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";

        var srcPropsXml = @"
<Project>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";

        mockFileSystem.Setup(fs => fs.FileExists(rootPropsPath)).Returns(true);
        mockFileSystem.Setup(fs => fs.FileExists(srcPropsPath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(rootPropsPath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(rootPropsXml)));
        mockFileSystem.Setup(fs => fs.OpenRead(srcPropsPath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(srcPropsXml)));

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Single(packages);
        Assert.Equal("Newtonsoft.Json", packages[0].Name);
        Assert.Equal("13.0.3", packages[0].Version); // Should use the closer src version
    }

    [Fact]
    public void ReadPackageReferences_GivenVersionOverride_WhenReading_ThenUsesVersionOverride()
    {
        // Given
        var rootPath = TestRootPath;
        var projectDirectory = TestProjectDirectory;
        var propsPath = TestPropsPath;
        
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "Directory.Packages.props",
            SearchOption.AllDirectories))
            .Returns(new[] { propsPath });

        var propsXml = @"
<Project>
  <ItemGroup>
    <PackageVersion Include=""TestPackage"" VersionOverride=""2.0.0"" />
  </ItemGroup>
</Project>";

        mockFileSystem.Setup(fs => fs.FileExists(propsPath)).Returns(true);
        mockFileSystem.Setup(fs => fs.OpenRead(propsPath))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(propsXml)));

        var projectXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
</Project>";

        var doc = XDocument.Parse(projectXml);
        var ns = doc.Root!.Name.Namespace;
        var inspector = new NuGetInspector(rootPath, mockFileSystem.Object);

        // When
        var packages = inspector.ReadPackageReferences(doc.Root, ns, projectDirectory);

        // Then
        Assert.Single(packages);
        Assert.Equal("TestPackage", packages[0].Name);
        Assert.Equal("2.0.0", packages[0].Version);
    }

    #endregion
}

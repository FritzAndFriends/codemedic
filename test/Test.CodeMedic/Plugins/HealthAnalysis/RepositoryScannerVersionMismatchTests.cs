using CodeMedic.Models;
using CodeMedic.Plugins.HealthAnalysis;

namespace Test.CodeMedic.Plugins.HealthAnalysis;

public class RepositoryScannerVersionMismatchTests
{
    [Fact]
    public void ComputePackageVersionMismatches_WhenOnlyTransitiveMismatchesExist_ThenReportsMismatch()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                ProjectPath = "C:/repo/A/A.csproj",
                ProjectName = "A",
                RelativePath = "A/A.csproj",
                TransitiveDependencies =
                [
                    new TransitiveDependency { PackageName = "Newtonsoft.Json", Version = "13.0.1" }
                ]
            },
            new()
            {
                ProjectPath = "C:/repo/B/B.csproj",
                ProjectName = "B",
                RelativePath = "B/B.csproj",
                TransitiveDependencies =
                [
                    new TransitiveDependency { PackageName = "Newtonsoft.Json", Version = "13.0.3" }
                ]
            }
        };

        var mismatches = RepositoryScanner.ComputePackageVersionMismatches(projects);

        Assert.Single(mismatches);
        Assert.Equal("Newtonsoft.Json", mismatches[0].PackageName, ignoreCase: true);

        var perProject = mismatches[0].ProjectVersions;
        Assert.Equal(2, perProject.Count);
        Assert.Contains("13.0.1", perProject["A"]);
        Assert.Contains("13.0.3", perProject["B"]);
    }

    [Fact]
    public void ComputePackageVersionMismatches_WhenAllProjectsUseSameTransitiveVersion_ThenNoMismatch()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                ProjectPath = "C:/repo/A/A.csproj",
                ProjectName = "A",
                RelativePath = "A/A.csproj",
                TransitiveDependencies =
                [
                    new TransitiveDependency { PackageName = "Serilog", Version = "4.0.0" }
                ]
            },
            new()
            {
                ProjectPath = "C:/repo/B/B.csproj",
                ProjectName = "B",
                RelativePath = "B/B.csproj",
                TransitiveDependencies =
                [
                    new TransitiveDependency { PackageName = "Serilog", Version = "4.0.0" }
                ]
            }
        };

        var mismatches = RepositoryScanner.ComputePackageVersionMismatches(projects);

        Assert.Empty(mismatches);
    }

    [Fact]
    public void ComputePackageVersionMismatches_WhenAProjectHasMultipleResolvedVersions_ThenShowsAllVersionsForThatProject()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                ProjectPath = "C:/repo/A/A.csproj",
                ProjectName = "A",
                RelativePath = "A/A.csproj",
                TransitiveDependencies =
                [
                    new TransitiveDependency { PackageName = "Example.Package", Version = "1.0.0" },
                    new TransitiveDependency { PackageName = "Example.Package", Version = "2.0.0" }
                ]
            },
            new()
            {
                ProjectPath = "C:/repo/B/B.csproj",
                ProjectName = "B",
                RelativePath = "B/B.csproj",
                TransitiveDependencies =
                [
                    new TransitiveDependency { PackageName = "Example.Package", Version = "1.0.0" }
                ]
            }
        };

        var mismatches = RepositoryScanner.ComputePackageVersionMismatches(projects);

        Assert.Single(mismatches);
        Assert.Equal("Example.Package", mismatches[0].PackageName, ignoreCase: true);

        Assert.Equal(2, mismatches[0].ProjectVersions["A"].Count);
        Assert.Contains("1.0.0", mismatches[0].ProjectVersions["A"]);
        Assert.Contains("2.0.0", mismatches[0].ProjectVersions["A"]);
        Assert.Single(mismatches[0].ProjectVersions["B"]);
    }
}

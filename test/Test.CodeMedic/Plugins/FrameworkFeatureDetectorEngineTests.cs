using CodeMedic.Plugins.BomAnalysis;
using Xunit;

namespace Test.CodeMedic.Plugins;

/// <summary>
/// Unit tests for the FrameworkFeatureDetectorEngine.
/// </summary>
public class FrameworkFeatureDetectorEngineTests
{
    [Fact]
    public void AnalyzeFeatures_WithNoPackages_ReturnsEmptySections()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>();

        // Act
        var sections = engine.AnalyzeFeatures(packages);

        // Assert
        Assert.Empty(sections);
    }

    [Fact]
    public void AnalyzeFeatures_WithTestingPackages_DetectsTestingFrameworks()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "xunit",
                Version = "2.9.3",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            },
            new PackageInfo
            {
                Name = "Moq",
                Version = "4.20.72",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var sections = engine.AnalyzeFeatures(packages);

        // Assert
        Assert.Single(sections);
        var testingSection = sections[0];
        Assert.Contains("Testing Frameworks", testingSection.Title);
        Assert.Contains("2 detected", testingSection.Title);
    }

    [Fact]
    public void AnalyzeFeatures_WithWebPackages_DetectsWebFrameworks()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.Mvc",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            },
            new PackageInfo
            {
                Name = "Swashbuckle.AspNetCore",
                Version = "6.5.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            }
        };

        // Act
        var sections = engine.AnalyzeFeatures(packages);

        // Assert
        Assert.Single(sections);
        var webSection = sections[0];
        Assert.Contains("Web Framework Features", webSection.Title);
        Assert.Contains("2 detected", webSection.Title);
    }

    [Fact]
    public void AnalyzeFeatures_WithMultipleCategories_ReturnsMultipleSections()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.Mvc",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            },
            new PackageInfo
            {
                Name = "Microsoft.EntityFrameworkCore",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            },
            new PackageInfo
            {
                Name = "xunit",
                Version = "2.9.3",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var sections = engine.AnalyzeFeatures(packages);

        // Assert
        Assert.Equal(3, sections.Count);
        Assert.Contains(sections, s => s.Title.Contains("Web Framework Features"));
        Assert.Contains(sections, s => s.Title.Contains("Data Access"));
        Assert.Contains(sections, s => s.Title.Contains("Testing Frameworks"));
    }

    [Fact]
    public void AnalyzeFeatures_SectionsAreOrderedByDisplayOrder()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "xunit",
                Version = "2.9.3",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            },
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.Mvc",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            }
        };

        // Act
        var sections = engine.AnalyzeFeatures(packages);

        // Assert
        Assert.Equal(2, sections.Count);
        // Web Framework Features should come before Testing Frameworks (DisplayOrder 1 vs 6)
        Assert.Contains("Web Framework Features", sections[0].Title);
        Assert.Contains("Testing Frameworks", sections[1].Title);
    }

    [Fact]
    public void GetFeatureSummary_WithNoPackages_ReturnsEmptyDictionary()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>();

        // Act
        var summary = engine.GetFeatureSummary(packages);

        // Assert
        Assert.Empty(summary);
    }

    [Fact]
    public void GetFeatureSummary_WithPackages_ReturnsCorrectCounts()
    {
        // Arrange
        var engine = new FrameworkFeatureDetectorEngine();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.Mvc",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            },
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.SignalR",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            },
            new PackageInfo
            {
                Name = "xunit",
                Version = "2.9.3",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var summary = engine.GetFeatureSummary(packages);

        // Assert
        Assert.Equal(2, summary.Count);
        Assert.Equal(2, summary["Web Framework Features"]);
        Assert.Equal(1, summary["Testing Frameworks"]);
    }
}

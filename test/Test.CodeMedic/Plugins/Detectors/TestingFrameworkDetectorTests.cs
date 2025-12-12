using CodeMedic.Plugins.BomAnalysis;
using Xunit;

namespace Test.CodeMedic.Plugins.Detectors;

/// <summary>
/// Unit tests for the TestingFrameworkDetector.
/// </summary>
public class TestingFrameworkDetectorTests
{
    [Fact]
    public void DetectFeatures_WithXUnit_DetectsXUnit()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "xunit",
                Version = "2.9.3",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("xUnit", features[0].Name);
        Assert.Equal("Unit testing framework", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithMoq_DetectsMoq()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Moq",
                Version = "4.20.72",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("Moq", features[0].Name);
        Assert.Equal("Mocking framework", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithFluentAssertions_DetectsFluentAssertions()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "FluentAssertions",
                Version = "7.0.0",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("FluentAssertions", features[0].Name);
        Assert.Equal("Assertion library", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithNUnit_DetectsNUnit()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "NUnit",
                Version = "4.0.0",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("NUnit", features[0].Name);
    }

    [Fact]
    public void DetectFeatures_WithMSTest_DetectsMSTest()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "MSTest.TestFramework",
                Version = "3.0.0",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("MSTest", features[0].Name);
    }

    [Fact]
    public void DetectFeatures_WithBogus_DetectsBogus()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Bogus",
                Version = "35.0.0",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("Bogus", features[0].Name);
        Assert.Equal("Fake data generator for testing", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithCompleteTestStack_DetectsAll()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();
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
            },
            new PackageInfo
            {
                Name = "FluentAssertions",
                Version = "7.0.0",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Equal(3, features.Count);
        Assert.Contains(features, f => f.Name == "xUnit");
        Assert.Contains(features, f => f.Name == "Moq");
        Assert.Contains(features, f => f.Name == "FluentAssertions");
    }

    [Fact]
    public void Category_ReturnsCorrectValue()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();

        // Act & Assert
        Assert.Equal("Testing Frameworks", detector.Category);
    }

    [Fact]
    public void DisplayOrder_ReturnsCorrectValue()
    {
        // Arrange
        var detector = new TestingFrameworkDetector();

        // Act & Assert
        Assert.Equal(6, detector.DisplayOrder);
    }
}

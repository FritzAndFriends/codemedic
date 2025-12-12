using CodeMedic.Plugins.BomAnalysis;
using Xunit;

namespace Test.CodeMedic.Plugins.Detectors;

/// <summary>
/// Unit tests for the WebFrameworkDetector.
/// </summary>
public class WebFrameworkDetectorTests
{
    [Fact]
    public void DetectFeatures_WithNoPackages_ReturnsEmpty()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>();

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Empty(features);
    }

    [Fact]
    public void DetectFeatures_WithAspNetCoreMvc_DetectsMvc()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.Mvc",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("ASP.NET Core MVC", features[0].Name);
        Assert.Equal("Microsoft.AspNetCore.Mvc", features[0].Package);
        Assert.Equal("10.0.0", features[0].Version);
        Assert.Contains("WebProject", features[0].Projects);
    }

    [Fact]
    public void DetectFeatures_WithSignalR_DetectsSignalR()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.SignalR",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "ChatApp" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("SignalR", features[0].Name);
        Assert.Equal("Microsoft.AspNetCore.SignalR", features[0].Package);
    }

    [Fact]
    public void DetectFeatures_WithGrpc_DetectsGrpc()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Grpc.AspNetCore",
                Version = "2.60.0",
                IsDirect = true,
                Projects = new List<string> { "GrpcService" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("gRPC", features[0].Name);
    }

    [Fact]
    public void DetectFeatures_WithSwashbuckle_DetectsSwagger()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Swashbuckle.AspNetCore",
                Version = "6.5.0",
                IsDirect = true,
                Projects = new List<string> { "ApiProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("Swagger/OpenAPI", features[0].Name);
        Assert.Equal("API documentation generation", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithBlazorServer_DetectsBlazor()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.AspNetCore.Components.Server",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "BlazorApp" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("Blazor Server", features[0].Name);
    }

    [Fact]
    public void DetectFeatures_WithMultipleWebFeatures_DetectsAll()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
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
                Name = "Swashbuckle.AspNetCore",
                Version = "6.5.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Equal(3, features.Count);
        Assert.Contains(features, f => f.Name == "ASP.NET Core MVC");
        Assert.Contains(features, f => f.Name == "SignalR");
        Assert.Contains(features, f => f.Name == "Swagger/OpenAPI");
    }

    [Fact]
    public void DetectFeatures_IsCaseInsensitive()
    {
        // Arrange
        var detector = new WebFrameworkDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "microsoft.aspnetcore.mvc", // lowercase
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "WebProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("ASP.NET Core MVC", features[0].Name);
    }

    [Fact]
    public void Category_ReturnsCorrectValue()
    {
        // Arrange
        var detector = new WebFrameworkDetector();

        // Act & Assert
        Assert.Equal("Web Framework Features", detector.Category);
    }

    [Fact]
    public void DisplayOrder_ReturnsCorrectValue()
    {
        // Arrange
        var detector = new WebFrameworkDetector();

        // Act & Assert
        Assert.Equal(1, detector.DisplayOrder);
    }
}

using CodeMedic.Plugins.BomAnalysis;
using Xunit;

namespace Test.CodeMedic.Plugins.Detectors;

/// <summary>
/// Unit tests for the DataAccessDetector.
/// </summary>
public class DataAccessDetectorTests
{
    [Fact]
    public void DetectFeatures_WithEntityFrameworkCore_DetectsEfCore()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.EntityFrameworkCore",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("Entity Framework Core", features[0].Name);
        Assert.Equal("Object-relational mapping framework", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithSqlServerProvider_DetectsSqlServer()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.EntityFrameworkCore.SqlServer",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("EF Core - SQL Server", features[0].Name);
        Assert.Equal("SQL Server database provider", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithPostgreSql_DetectsPostgreSql()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Npgsql.EntityFrameworkCore.PostgreSQL",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("EF Core - PostgreSQL", features[0].Name);
    }

    [Fact]
    public void DetectFeatures_WithDapper_DetectsDapper()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Dapper",
                Version = "2.1.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("Dapper", features[0].Name);
        Assert.Equal("Lightweight micro-ORM", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithMongoDb_DetectsMongoDb()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "MongoDB.Driver",
                Version = "3.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("MongoDB Driver", features[0].Name);
        Assert.Equal("NoSQL document database", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithInMemoryProvider_DetectsInMemory()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.EntityFrameworkCore.InMemory",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "TestProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Single(features);
        Assert.Equal("EF Core - InMemory", features[0].Name);
        Assert.Contains("testing", features[0].Description);
    }

    [Fact]
    public void DetectFeatures_WithMultipleDataAccessTechnologies_DetectsAll()
    {
        // Arrange
        var detector = new DataAccessDetector();
        var packages = new List<PackageInfo>
        {
            new PackageInfo
            {
                Name = "Microsoft.EntityFrameworkCore",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            },
            new PackageInfo
            {
                Name = "Microsoft.EntityFrameworkCore.SqlServer",
                Version = "10.0.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            },
            new PackageInfo
            {
                Name = "Dapper",
                Version = "2.1.0",
                IsDirect = true,
                Projects = new List<string> { "DataProject" }
            }
        };

        // Act
        var features = detector.DetectFeatures(packages).ToList();

        // Assert
        Assert.Equal(3, features.Count);
        Assert.Contains(features, f => f.Name == "Entity Framework Core");
        Assert.Contains(features, f => f.Name == "EF Core - SQL Server");
        Assert.Contains(features, f => f.Name == "Dapper");
    }

    [Fact]
    public void Category_ReturnsCorrectValue()
    {
        // Arrange
        var detector = new DataAccessDetector();

        // Act & Assert
        Assert.Equal("Data Access", detector.Category);
    }

    [Fact]
    public void DisplayOrder_ReturnsCorrectValue()
    {
        // Arrange
        var detector = new DataAccessDetector();

        // Act & Assert
        Assert.Equal(2, detector.DisplayOrder);
    }
}

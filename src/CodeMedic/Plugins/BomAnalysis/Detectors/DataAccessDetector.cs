namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Detects data access frameworks and database providers.
/// </summary>
public class DataAccessDetector : IFrameworkFeatureDetector
{
    /// <inheritdoc/>
    public string Category => "Data Access";
    
    /// <inheritdoc/>
    public int DisplayOrder => 2;

    /// <inheritdoc/>
    public IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages)
    {
        var features = new List<FrameworkFeature>();
        var packageList = packages.ToList();

        // Entity Framework Core
        var efCorePackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        if (efCorePackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Entity Framework Core",
                Package = efCorePackage.Name,
                Version = efCorePackage.Version,
                Projects = efCorePackage.Projects,
                Description = "Object-relational mapping framework"
            });
        }

        // EF Core - SQL Server
        var efSqlServerPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase));
        if (efSqlServerPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "EF Core - SQL Server",
                Package = efSqlServerPackage.Name,
                Version = efSqlServerPackage.Version,
                Projects = efSqlServerPackage.Projects,
                Description = "SQL Server database provider"
            });
        }

        // EF Core - PostgreSQL
        var efPostgresPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase));
        if (efPostgresPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "EF Core - PostgreSQL",
                Package = efPostgresPackage.Name,
                Version = efPostgresPackage.Version,
                Projects = efPostgresPackage.Projects,
                Description = "PostgreSQL database provider"
            });
        }

        // EF Core - SQLite
        var efSqlitePackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.EntityFrameworkCore.Sqlite", StringComparison.OrdinalIgnoreCase));
        if (efSqlitePackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "EF Core - SQLite",
                Package = efSqlitePackage.Name,
                Version = efSqlitePackage.Version,
                Projects = efSqlitePackage.Projects,
                Description = "SQLite database provider"
            });
        }

        // EF Core - InMemory (testing)
        var efInMemoryPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.EntityFrameworkCore.InMemory", StringComparison.OrdinalIgnoreCase));
        if (efInMemoryPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "EF Core - InMemory",
                Package = efInMemoryPackage.Name,
                Version = efInMemoryPackage.Version,
                Projects = efInMemoryPackage.Projects,
                Description = "In-memory database provider (testing)"
            });
        }

        // EF Core - Cosmos DB
        var efCosmosPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.EntityFrameworkCore.Cosmos", StringComparison.OrdinalIgnoreCase));
        if (efCosmosPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "EF Core - Cosmos DB",
                Package = efCosmosPackage.Name,
                Version = efCosmosPackage.Version,
                Projects = efCosmosPackage.Projects,
                Description = "Azure Cosmos DB provider"
            });
        }

        // Dapper
        var dapperPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Dapper", StringComparison.OrdinalIgnoreCase));
        if (dapperPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Dapper",
                Package = dapperPackage.Name,
                Version = dapperPackage.Version,
                Projects = dapperPackage.Projects,
                Description = "Lightweight micro-ORM"
            });
        }

        // MongoDB
        var mongoPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("MongoDB.Driver", StringComparison.OrdinalIgnoreCase));
        if (mongoPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "MongoDB Driver",
                Package = mongoPackage.Name,
                Version = mongoPackage.Version,
                Projects = mongoPackage.Projects,
                Description = "NoSQL document database"
            });
        }

        return features;
    }
}

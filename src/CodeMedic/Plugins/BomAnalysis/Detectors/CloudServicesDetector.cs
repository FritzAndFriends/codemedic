namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Detects cloud service SDKs and infrastructure packages.
/// </summary>
public class CloudServicesDetector : IFrameworkFeatureDetector
{
    /// <inheritdoc/>
    public string Category => "Cloud Services";
    
    /// <inheritdoc/>
    public int DisplayOrder => 4;

    /// <inheritdoc/>
    public IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages)
    {
        var features = new List<FrameworkFeature>();
        var packageList = packages.ToList();

        // Azure Blob Storage
        var blobPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Azure.Storage.Blobs", StringComparison.OrdinalIgnoreCase));
        if (blobPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Azure Blob Storage",
                Package = blobPackage.Name,
                Version = blobPackage.Version,
                Projects = blobPackage.Projects,
                Description = "Azure cloud object storage"
            });
        }

        // Azure Service Bus
        var serviceBusPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Azure.Messaging.ServiceBus", StringComparison.OrdinalIgnoreCase));
        if (serviceBusPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Azure Service Bus",
                Package = serviceBusPackage.Name,
                Version = serviceBusPackage.Version,
                Projects = serviceBusPackage.Projects,
                Description = "Azure messaging service"
            });
        }

        // Azure Key Vault
        var keyVaultPackages = packageList.Where(p => 
            p.Name.StartsWith("Azure.Security.KeyVault", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var kvPackage in keyVaultPackages)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Azure Key Vault",
                Package = kvPackage.Name,
                Version = kvPackage.Version,
                Projects = kvPackage.Projects,
                Description = "Azure secrets management"
            });
        }

        // Azure Cosmos DB
        var cosmosPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.Azure.Cosmos", StringComparison.OrdinalIgnoreCase));
        if (cosmosPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Azure Cosmos DB",
                Package = cosmosPackage.Name,
                Version = cosmosPackage.Version,
                Projects = cosmosPackage.Projects,
                Description = "Azure NoSQL database"
            });
        }

        // AWS SDK packages
        var awsPackages = packageList.Where(p => 
            p.Name.StartsWith("AWSSDK.", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var awsPackage in awsPackages)
        {
            var serviceName = awsPackage.Name.Substring("AWSSDK.".Length);
            features.Add(new FrameworkFeature
            {
                Name = $"AWS {serviceName}",
                Package = awsPackage.Name,
                Version = awsPackage.Version,
                Projects = awsPackage.Projects,
                Description = "Amazon Web Services SDK"
            });
        }

        // Redis
        var redisPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("StackExchange.Redis", StringComparison.OrdinalIgnoreCase));
        if (redisPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Redis",
                Package = redisPackage.Name,
                Version = redisPackage.Version,
                Projects = redisPackage.Projects,
                Description = "In-memory data structure store"
            });
        }

        // RabbitMQ
        var rabbitMqPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("RabbitMQ.Client", StringComparison.OrdinalIgnoreCase));
        if (rabbitMqPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "RabbitMQ",
                Package = rabbitMqPackage.Name,
                Version = rabbitMqPackage.Version,
                Projects = rabbitMqPackage.Projects,
                Description = "Message broker"
            });
        }

        // Kafka
        var kafkaPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Confluent.Kafka", StringComparison.OrdinalIgnoreCase));
        if (kafkaPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Apache Kafka",
                Package = kafkaPackage.Name,
                Version = kafkaPackage.Version,
                Projects = kafkaPackage.Projects,
                Description = "Distributed event streaming platform"
            });
        }

        return features;
    }
}

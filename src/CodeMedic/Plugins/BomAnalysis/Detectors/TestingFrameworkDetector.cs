namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Detects testing frameworks and testing utilities.
/// </summary>
public class TestingFrameworkDetector : IFrameworkFeatureDetector
{
    /// <inheritdoc/>
    public string Category => "Testing Frameworks";
    
    /// <inheritdoc/>
    public int DisplayOrder => 6;

    /// <inheritdoc/>
    public IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages)
    {
        var features = new List<FrameworkFeature>();
        var packageList = packages.ToList();

        // xUnit
        var xunitPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("xunit", StringComparison.OrdinalIgnoreCase));
        if (xunitPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "xUnit",
                Package = xunitPackage.Name,
                Version = xunitPackage.Version,
                Projects = xunitPackage.Projects,
                Description = "Unit testing framework"
            });
        }

        // NUnit
        var nunitPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("NUnit", StringComparison.OrdinalIgnoreCase));
        if (nunitPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "NUnit",
                Package = nunitPackage.Name,
                Version = nunitPackage.Version,
                Projects = nunitPackage.Projects,
                Description = "Unit testing framework"
            });
        }

        // MSTest
        var msTestPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("MSTest.TestFramework", StringComparison.OrdinalIgnoreCase));
        if (msTestPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "MSTest",
                Package = msTestPackage.Name,
                Version = msTestPackage.Version,
                Projects = msTestPackage.Projects,
                Description = "Microsoft unit testing framework"
            });
        }

        // Moq
        var moqPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Moq", StringComparison.OrdinalIgnoreCase));
        if (moqPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Moq",
                Package = moqPackage.Name,
                Version = moqPackage.Version,
                Projects = moqPackage.Projects,
                Description = "Mocking framework"
            });
        }

        // FluentAssertions
        var fluentPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("FluentAssertions", StringComparison.OrdinalIgnoreCase));
        if (fluentPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "FluentAssertions",
                Package = fluentPackage.Name,
                Version = fluentPackage.Version,
                Projects = fluentPackage.Projects,
                Description = "Assertion library"
            });
        }

        // Bogus (fake data generation)
        var bogusPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Bogus", StringComparison.OrdinalIgnoreCase));
        if (bogusPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Bogus",
                Package = bogusPackage.Name,
                Version = bogusPackage.Version,
                Projects = bogusPackage.Projects,
                Description = "Fake data generator for testing"
            });
        }

        return features;
    }
}

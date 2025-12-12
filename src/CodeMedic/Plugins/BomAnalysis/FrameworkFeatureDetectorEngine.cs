using CodeMedic.Models.Report;

namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Engine that coordinates multiple framework feature detectors to analyze package usage.
/// </summary>
public class FrameworkFeatureDetectorEngine
{
    private readonly List<IFrameworkFeatureDetector> _detectors;

    /// <summary>
    /// Initializes a new instance with all available feature detectors.
    /// </summary>
    public FrameworkFeatureDetectorEngine()
    {
        // Register all detectors here - new detectors can be added easily
        _detectors = new List<IFrameworkFeatureDetector>
        {
            new WebFrameworkDetector(),
            new DataAccessDetector(),
            new AuthenticationDetector(),
            new CloudServicesDetector(),
            new LoggingDetector(),
            new TestingFrameworkDetector()
        };
    }

    /// <summary>
    /// Analyzes packages and generates report sections for detected framework features.
    /// </summary>
    /// <param name="packages">All packages from the repository.</param>
    /// <returns>List of report sections, one per category that has detected features.</returns>
    public List<ReportSection> AnalyzeFeatures(IEnumerable<PackageInfo> packages)
    {
        var sections = new List<ReportSection>();
        var packageList = packages.ToList();

        // Run each detector and create sections for categories with detected features
        foreach (var detector in _detectors.OrderBy(d => d.DisplayOrder))
        {
            var features = detector.DetectFeatures(packageList).ToList();
            
            if (features.Count == 0)
                continue;

            var section = CreateFeatureSection(detector.Category, features);
            sections.Add(section);
        }

        return sections;
    }

    /// <summary>
    /// Creates a report section for a category of detected features.
    /// </summary>
    private static ReportSection CreateFeatureSection(string category, List<FrameworkFeature> features)
    {
        var section = new ReportSection
        {
            Title = $"{category} ({features.Count} detected)",
            Level = 2
        };

        var table = new ReportTable();
        table.Headers = new List<string> { "Feature", "Package", "Version", "Used In" };

        foreach (var feature in features.OrderBy(f => f.Name))
        {
            table.AddRow(
                feature.Name,
                feature.Package,
                feature.Version,
                string.Join(", ", feature.Projects.Distinct())
            );
        }

        section.AddElement(table);

        return section;
    }

    /// <summary>
    /// Gets a summary of all detected features across all categories.
    /// </summary>
    public Dictionary<string, int> GetFeatureSummary(IEnumerable<PackageInfo> packages)
    {
        var summary = new Dictionary<string, int>();
        var packageList = packages.ToList();

        foreach (var detector in _detectors)
        {
            var featureCount = detector.DetectFeatures(packageList).Count();
            if (featureCount > 0)
            {
                summary[detector.Category] = featureCount;
            }
        }

        return summary;
    }
}

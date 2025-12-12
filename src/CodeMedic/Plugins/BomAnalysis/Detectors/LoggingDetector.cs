namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Detects logging and monitoring frameworks.
/// </summary>
public class LoggingDetector : IFrameworkFeatureDetector
{
    /// <inheritdoc/>
    public string Category => "Logging & Monitoring";
    
    /// <inheritdoc/>
    public int DisplayOrder => 5;

    /// <inheritdoc/>
    public IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages)
    {
        var features = new List<FrameworkFeature>();
        var packageList = packages.ToList();

        // Serilog
        var serilogPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("Serilog.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals("Serilog", StringComparison.OrdinalIgnoreCase));
        if (serilogPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Serilog",
                Package = serilogPackage.Name,
                Version = serilogPackage.Version,
                Projects = serilogPackage.Projects,
                Description = "Structured logging framework"
            });
        }

        // NLog
        var nlogPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("NLog.Web.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals("NLog", StringComparison.OrdinalIgnoreCase));
        if (nlogPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "NLog",
                Package = nlogPackage.Name,
                Version = nlogPackage.Version,
                Projects = nlogPackage.Projects,
                Description = "Flexible logging framework"
            });
        }

        // Application Insights
        var appInsightsPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("Microsoft.ApplicationInsights.AspNetCore", StringComparison.OrdinalIgnoreCase));
        if (appInsightsPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Application Insights",
                Package = appInsightsPackage.Name,
                Version = appInsightsPackage.Version,
                Projects = appInsightsPackage.Projects,
                Description = "Azure application monitoring"
            });
        }

        // OpenTelemetry
        var otelPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase));
        if (otelPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "OpenTelemetry",
                Package = otelPackage.Name,
                Version = otelPackage.Version,
                Projects = otelPackage.Projects,
                Description = "Observability framework"
            });
        }

        // Seq (Serilog sink)
        var seqPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Serilog.Sinks.Seq", StringComparison.OrdinalIgnoreCase));
        if (seqPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Seq",
                Package = seqPackage.Name,
                Version = seqPackage.Version,
                Projects = seqPackage.Projects,
                Description = "Structured log server"
            });
        }

        return features;
    }
}

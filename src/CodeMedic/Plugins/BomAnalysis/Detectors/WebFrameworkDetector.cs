namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Detects ASP.NET Core web framework features.
/// </summary>
public class WebFrameworkDetector : IFrameworkFeatureDetector
{
    /// <inheritdoc/>
    public string Category => "Web Framework Features";
    
    /// <inheritdoc/>
    public int DisplayOrder => 1;

    /// <inheritdoc/>
    public IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages)
    {
        var features = new List<FrameworkFeature>();
        var packageList = packages.ToList();

        // ASP.NET Core MVC
        var mvcPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase));
        if (mvcPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "ASP.NET Core MVC",
                Package = mvcPackage.Name,
                Version = mvcPackage.Version,
                Projects = mvcPackage.Projects,
                Description = "Model-View-Controller web framework",
                DocumentationUrl = "https://docs.microsoft.com/aspnet/core/mvc"
            });
        }

        // Razor Pages
        var razorPagesPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Mvc.RazorPages", StringComparison.OrdinalIgnoreCase));
        if (razorPagesPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Razor Pages",
                Package = razorPagesPackage.Name,
                Version = razorPagesPackage.Version,
                Projects = razorPagesPackage.Projects,
                Description = "Page-based web UI framework"
            });
        }

        // Blazor Server
        var blazorServerPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Components.Server", StringComparison.OrdinalIgnoreCase));
        if (blazorServerPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Blazor Server",
                Package = blazorServerPackage.Name,
                Version = blazorServerPackage.Version,
                Projects = blazorServerPackage.Projects,
                Description = "Server-side Blazor components"
            });
        }

        // Blazor WebAssembly
        var blazorWasmPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Components.WebAssembly", StringComparison.OrdinalIgnoreCase));
        if (blazorWasmPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Blazor WebAssembly",
                Package = blazorWasmPackage.Name,
                Version = blazorWasmPackage.Version,
                Projects = blazorWasmPackage.Projects,
                Description = "Client-side Blazor with WebAssembly"
            });
        }

        // SignalR
        var signalRPackages = packageList.Where(p => 
            p.Name.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var signalRPackage in signalRPackages)
        {
            features.Add(new FrameworkFeature
            {
                Name = "SignalR",
                Package = signalRPackage.Name,
                Version = signalRPackage.Version,
                Projects = signalRPackage.Projects,
                Description = "Real-time web functionality"
            });
        }

        // gRPC
        var grpcPackages = packageList.Where(p => 
            p.Name.StartsWith("Grpc.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
            p.Name.StartsWith("Grpc.Net.Client", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var grpcPackage in grpcPackages)
        {
            features.Add(new FrameworkFeature
            {
                Name = "gRPC",
                Package = grpcPackage.Name,
                Version = grpcPackage.Version,
                Projects = grpcPackage.Projects,
                Description = "High-performance RPC framework"
            });
        }

        // Health Checks
        var healthChecksPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.Extensions.Diagnostics.HealthChecks", StringComparison.OrdinalIgnoreCase));
        if (healthChecksPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Health Checks",
                Package = healthChecksPackage.Name,
                Version = healthChecksPackage.Version,
                Projects = healthChecksPackage.Projects,
                Description = "Application health monitoring"
            });
        }

        // Swagger/OpenAPI
        var swaggerPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("Swashbuckle.AspNetCore", StringComparison.OrdinalIgnoreCase));
        if (swaggerPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Swagger/OpenAPI",
                Package = swaggerPackage.Name,
                Version = swaggerPackage.Version,
                Projects = swaggerPackage.Projects,
                Description = "API documentation generation"
            });
        }

        // NSwag (alternative to Swashbuckle)
        var nswagPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("NSwag.AspNetCore", StringComparison.OrdinalIgnoreCase));
        if (nswagPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "NSwag OpenAPI",
                Package = nswagPackage.Name,
                Version = nswagPackage.Version,
                Projects = nswagPackage.Projects,
                Description = "API documentation and client generation"
            });
        }

        // API Versioning
        var versioningPackages = packageList.Where(p => 
            p.Name.StartsWith("Asp.Versioning", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var versioningPackage in versioningPackages)
        {
            features.Add(new FrameworkFeature
            {
                Name = "API Versioning",
                Package = versioningPackage.Name,
                Version = versioningPackage.Version,
                Projects = versioningPackage.Projects,
                Description = "REST API versioning support"
            });
        }

        return features;
    }
}

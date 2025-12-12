namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Detects authentication and authorization frameworks.
/// </summary>
public class AuthenticationDetector : IFrameworkFeatureDetector
{
    /// <inheritdoc/>
    public string Category => "Authentication & Security";
    
    /// <inheritdoc/>
    public int DisplayOrder => 3;

    /// <inheritdoc/>
    public IEnumerable<FrameworkFeature> DetectFeatures(IEnumerable<PackageInfo> packages)
    {
        var features = new List<FrameworkFeature>();
        var packageList = packages.ToList();

        // ASP.NET Core Identity
        var identityPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Identity", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals("Microsoft.AspNetCore.Identity.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        if (identityPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "ASP.NET Core Identity",
                Package = identityPackage.Name,
                Version = identityPackage.Version,
                Projects = identityPackage.Projects,
                Description = "Membership system with login functionality"
            });
        }

        // JWT Bearer
        var jwtPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Authentication.JwtBearer", StringComparison.OrdinalIgnoreCase));
        if (jwtPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "JWT Bearer Authentication",
                Package = jwtPackage.Name,
                Version = jwtPackage.Version,
                Projects = jwtPackage.Projects,
                Description = "Token-based authentication"
            });
        }

        // OpenID Connect
        var oidcPackage = packageList.FirstOrDefault(p => 
            p.Name.Equals("Microsoft.AspNetCore.Authentication.OpenIdConnect", StringComparison.OrdinalIgnoreCase));
        if (oidcPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "OpenID Connect",
                Package = oidcPackage.Name,
                Version = oidcPackage.Version,
                Projects = oidcPackage.Projects,
                Description = "External authentication provider"
            });
        }

        // Microsoft Identity Web (Azure AD)
        var msIdentityPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("Microsoft.Identity.Web", StringComparison.OrdinalIgnoreCase));
        if (msIdentityPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Microsoft Identity (Azure AD)",
                Package = msIdentityPackage.Name,
                Version = msIdentityPackage.Version,
                Projects = msIdentityPackage.Projects,
                Description = "Azure Active Directory integration"
            });
        }

        // IdentityServer/Duende
        var identityServerPackage = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("IdentityServer4", StringComparison.OrdinalIgnoreCase) ||
            p.Name.StartsWith("Duende.IdentityServer", StringComparison.OrdinalIgnoreCase));
        if (identityServerPackage != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "IdentityServer",
                Package = identityServerPackage.Name,
                Version = identityServerPackage.Version,
                Projects = identityServerPackage.Projects,
                Description = "OAuth 2.0 and OpenID Connect framework"
            });
        }

        // Auth0
        var auth0Package = packageList.FirstOrDefault(p => 
            p.Name.StartsWith("Auth0.AspNetCore.Authentication", StringComparison.OrdinalIgnoreCase));
        if (auth0Package != null)
        {
            features.Add(new FrameworkFeature
            {
                Name = "Auth0",
                Package = auth0Package.Name,
                Version = auth0Package.Version,
                Projects = auth0Package.Projects,
                Description = "Auth0 identity platform integration"
            });
        }

        return features;
    }
}

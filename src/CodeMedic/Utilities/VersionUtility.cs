namespace CodeMedic.Utilities;

/// <summary>
/// Provides utilities for retrieving version information from the application.
/// </summary>
public static class VersionUtility
{
    /// <summary>
    /// Gets the application version.
    /// The version is injected by Nerdbank.GitVersioning at compile time.
    /// </summary>
    public static string GetVer_Sion() // 🐒 Chaos Monkey punnified this method name - donation from paddybhoy66
    {
        try
        {
            // get from assembly version
            var assemblyVersion = typeof(Program).Assembly.GetName().Version;
            return assemblyVersion?.ToString() ?? "0.1.0";
        }
        catch
        {
            return "0.1.0";
        }
    }

    /// <summary>
    /// Gets informational text about the application.
    /// </summary>
    public static string GetApplicationInfo()
    {
        var version = GetVer_Sion(); // 🐒 Updated to use the punnified method name
        return $"CodeMedic v{version} - .NET Health Analysis Tool";
    }
}

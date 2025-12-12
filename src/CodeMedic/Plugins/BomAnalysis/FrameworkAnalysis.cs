using CodeMedic.Models.Report;

namespace CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Analyze the .NET framework and SDK used in a project.
/// </summary>
public class FrameworkAnalysis
{
	
	/// <summary>
	/// Analyzes the .NET framework and SDK used in projects within the specified root path.
	/// </summary>
	/// <param name="rootPath">The root directory path to search for projects.</param>
	/// <returns>A report section containing the framework analysis results.</returns>
	public ReportSection AnalyzeFrameworkForProjects(string rootPath)
	{
		// Placeholder for actual framework analysis logic
		var section = new ReportSection
		{
			Title = "Framework Analysis",
			Level = 2
		};

		// Get all of the CSPROJ files in the root path and child directories
		var csprojFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

		// Create table for project framework information
		var table = new ReportTable
		{
			Headers = new List<string> { "Project", "Target Framework", "SDK", "Features" }
		};

		foreach (var csprojFile in csprojFiles)
		{
			// Load the CSPROJ file as XML
			var csprojXml = System.Xml.Linq.XDocument.Load(csprojFile);

			// Extract the TargetFramework element and SDK used
			var targetFrameworkElement = csprojXml.Descendants("TargetFramework").FirstOrDefault();
			var targetFrameworksElement = csprojXml.Descendants("TargetFrameworks").FirstOrDefault();
			var sdkAttribute = csprojXml.Root?.Attribute("Sdk")?.Value;

			// Check for multi-targeting (TargetFrameworks plural)
			string targetFramework;
			bool isMultiTargeting = false;
			if (targetFrameworksElement != null)
			{
				targetFramework = targetFrameworksElement.Value;
				isMultiTargeting = targetFramework.Contains(';');
			}
			else
			{
				targetFramework = targetFrameworkElement?.Value ?? "Unknown";
			}

			// Detect ASP.NET Core features
			var aspNetFeatures = new List<string>();
			var aspNetCoreHostingModel = csprojXml.Descendants("AspNetCoreHostingModel").FirstOrDefault()?.Value;
			var preserveCompilationContext = csprojXml.Descendants("PreserveCompilationContext").FirstOrDefault()?.Value;
			var mvcRazorCompileOnPublish = csprojXml.Descendants("MvcRazorCompileOnPublish").FirstOrDefault()?.Value;

			if (!string.IsNullOrEmpty(aspNetCoreHostingModel))
			{
				aspNetFeatures.Add($"Hosting:{aspNetCoreHostingModel}");
			}
			if (preserveCompilationContext?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
			{
				aspNetFeatures.Add("RuntimeCompilation");
			}
			if (mvcRazorCompileOnPublish?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
			{
				aspNetFeatures.Add("RazorPrecompile");
			}

			// Detect testing frameworks by looking for PackageReference elements
			var testingFeatures = new List<string>();
			var packageReferences = csprojXml.Descendants("PackageReference")
				.Select(pr => pr.Attribute("Include")?.Value)
				.Where(name => !string.IsNullOrEmpty(name))
				.ToList();

			if (packageReferences.Any(p => p!.StartsWith("xunit", StringComparison.OrdinalIgnoreCase)))
			{
				testingFeatures.Add("xUnit");
			}
			if (packageReferences.Any(p => p!.StartsWith("NUnit", StringComparison.OrdinalIgnoreCase)))
			{
				testingFeatures.Add("NUnit");
			}
			if (packageReferences.Any(p => p!.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase)))
			{
				testingFeatures.Add("MSTest");
			}
			if (packageReferences.Any(p => p!.Equals("Moq", StringComparison.OrdinalIgnoreCase)))
			{
				testingFeatures.Add("Moq");
			}
			if (packageReferences.Any(p => p!.Equals("FluentAssertions", StringComparison.OrdinalIgnoreCase)))
			{
				testingFeatures.Add("FluentAssertions");
			}

			// Build features string
			var features = new List<string>();
			if (isMultiTargeting)
			{
				features.Add("Multi-targeting");
			}
			features.AddRange(aspNetFeatures);
			features.AddRange(testingFeatures);

			// Omit SDK if it's the standard Microsoft.NET.Sdk
			var sdkDisplay = sdkAttribute == "Microsoft.NET.Sdk" ? "" : sdkAttribute ?? "";

			// Add row to table
			table.AddRow(
				Path.GetFileName(csprojFile),
				targetFramework,
				sdkDisplay,
				features.Any() ? string.Join(", ", features) : ""
			);
		}

		section.AddElement(table);

		return section;
	}

}


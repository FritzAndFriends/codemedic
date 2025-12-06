using CodeMedic.Abstractions;
using CodeMedic.Abstractions.Plugins;
using CodeMedic.Models.Report;
using CodeMedic.Output;
using CodeMedic.Utilities;

namespace CodeMedic.Plugins.HealthAnalysis;

/// <summary>
/// Internal plugin that provides repository health analysis.
/// </summary>
public class HealthAnalysisPlugin : IAnalysisEnginePlugin
{
    private RepositoryScanner? _scanner;

    /// <inheritdoc/>
    public PluginMetadata Metadata => new()
    {
        Id = "codemedic.health",
        Name = "Repository Health Analyzer",
        Version = VersionUtility.GetVersion(),
        Description = "Analyzes .NET repository health, including projects, dependencies, and code quality indicators",
        Author = "CodeMedic Team",
        Tags = ["health", "analysis", "repository", "dotnet"]
    };

    /// <inheritdoc/>
    public string AnalysisDescription => "Repository health and code quality analysis";

    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // No initialization required
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<object> AnalyzeAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        _scanner = new RepositoryScanner(repositoryPath);
        await _scanner.ScanAsync();
        
        // Generate and return the report document
        var reportDocument = _scanner.GenerateReport();
        return reportDocument;
    }

    /// <inheritdoc/>
    public CommandRegistration[]? RegisterCommands()
    {
        return
        [
            new CommandRegistration
            {
                Name = "health",
                Description = "Display repository health dashboard",
                Handler = ExecuteHealthCommandAsync,
                Examples =
                [
                    "codemedic health",
                    "codemedic health --format markdown",
                    "codemedic health --format md > report.md"
                ]
            }
        ];
    }

    private async Task<int> ExecuteHealthCommandAsync(string[] args)
    {
        try
        {
            // Parse arguments
            string? targetPath = null;
            string format = "console";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--format" && i + 1 < args.Length)
                {
                    format = args[i + 1].ToLower();
                    i++;
                }
                else if (!args[i].StartsWith("--"))
                {
                    targetPath = args[i];
                }
            }

            // Create renderer
            IRenderer renderer = format switch
            {
                "markdown" or "md" => new MarkdownRenderer(),
                _ => new ConsoleRenderer()
            };

            // Render banner and header
            renderer.RenderBanner();
            renderer.RenderSectionHeader("Repository Health Dashboard");

            // Run analysis
            var repositoryPath = targetPath ?? Directory.GetCurrentDirectory();
            object reportDocument;

            await renderer.RenderWaitAsync($"Running {AnalysisDescription}...", async () =>
            {
                reportDocument = await AnalyzeAsync(repositoryPath);
            });

            reportDocument = await AnalyzeAsync(repositoryPath);

            // Render report
            renderer.RenderReport(reportDocument);

            return 0;
        }
        catch (Exception ex)
        {
            var renderer = new ConsoleRenderer();
            renderer.RenderError($"Failed to analyze repository: {ex.Message}");
            return 1;
        }
    }
}

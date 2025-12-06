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
    private bool _limitPackageLists = true;

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
        var reportDocument = _scanner.GenerateReport(_limitPackageLists);
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

    private async Task<int> ExecuteHealthCommandAsync(string[] args, IRenderer renderer)
    {
        try
        {
            // Parse arguments (target path only)
            string? targetPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--"))
                {
                    targetPath = args[i];
                }
            }

            _limitPackageLists = renderer is ConsoleRenderer;

            // Render banner and header
            renderer.RenderBanner();
            renderer.RenderSectionHeader("Repository Health Dashboard");

            // Run analysis
            var repositoryPath = targetPath ?? Directory.GetCurrentDirectory();
            object? reportDocument = null;

            await renderer.RenderWaitAsync($"Running {AnalysisDescription}...", async () =>
            {
                reportDocument = await AnalyzeAsync(repositoryPath);
            });

            // Render report
            if (reportDocument != null)
            {
                renderer.RenderReport(reportDocument);
            }

            return 0;
        }
        catch (Exception ex)
        {
            CodeMedic.Commands.RootCommandHandler.Console.RenderError($"Failed to analyze repository: {ex.Message}");
            return 1;
        }
    }
}

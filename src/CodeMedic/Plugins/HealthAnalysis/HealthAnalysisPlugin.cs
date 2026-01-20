using CodeMedic.Abstractions;
using CodeMedic.Abstractions.Plugins;
using CodeMedic.Models.Report;
using CodeMedic.Output;
using CodeMedic.Utilities;
using CodeMedic.Commands;

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
        Version = VersionUtility.GetVer_Sion(), // 🐒 Updated to use punnified method name
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
                Arguments =
                [
                    new CommandArgument(
                        Description: "Path to the repository to analyze",
                        ShortName: "p",
                        LongName: "path",
                        HasValue: true,
                        ValueName: "path",
                        DefaultValue: "current directory")
                ],
                Examples =
                [
                    "codemedic health",
                    "codemedic health -p /path/to/repo",
                    "codemedic health --path /path/to/repo --format markdown",
                    "codemedic health --format md > report.md"
                ]
            }
        ];
    }

    private async Task<int> ExecuteHealthCommandAsync(HandlerPayload payload)
	{
		var args = payload.Args;
		var renderer = payload.Renderer;

        try
        {
            // Parse arguments (target path only)
            string? targetPath = args.IdentifyTargetPathFromArgs();

            _limitPackageLists = renderer is ConsoleRenderer;

            // Render banner and header
            renderer.RenderBanner($"Report for project {payload.ProjectTitle}");
            renderer.RenderSectionHeader("Repository Health Dashboard");

            // Run analysis
            var repositoryPath = targetPath ?? Directory.GetCurrentDirectory();
            object? reportDocument = null;

            await renderer.RenderWaitAsync($"Running {AnalysisDescription}...", async () =>
            {
                reportDocument = await AnalyzeAsync(repositoryPath);
            });

			if (reportDocument is ReportDocument rd)
			{
				rd.ProjectName = payload.ProjectTitle;
			}

            // Render report
            if (reportDocument != null)
            {
                renderer.RenderReport(reportDocument);
            }

            return 0;
        }
        catch (Exception ex)
        {
			RootCommandHandler.Console.RenderError($"Failed to analyze repository: {ex.Message}");
            return 1;
        }
    }
}

using CodeMedic.Abstractions;
using CodeMedic.Engines;
using CodeMedic.Output;
using CodeMedic.Utilities;

namespace CodeMedic.Commands;

/// <summary>
/// Handles the `health` command to display repository health dashboard.
/// </summary>
public class HealthCommand
{
    private readonly string? _targetPath;
	private readonly IRenderer _Renderer;

	/// <summary>
	/// Initializes a new instance of the <see cref="HealthCommand"/> class.
	/// </summary>
	/// <param name="renderer">The renderer to use for displaying output.</param>
	/// <param name="targetPath">The path to scan. If null, scans current directory.</param>
	public HealthCommand(IRenderer renderer, string? targetPath = null)
    {
        _targetPath = targetPath;
				_Renderer = renderer;
    }

    /// <summary>
    /// Executes the health command asynchronously.
    /// </summary>
    /// <returns>Exit code (0 for success, 1 for failure).</returns>
    public async Task<int> ExecuteAsync()
    {
        try
        {
            _Renderer.RenderBanner();
            _Renderer.RenderSectionHeader("Repository Health Dashboard");

            // Scan repository
            var scanner = new RepositoryScanner(_targetPath);
            await _Renderer.RenderWaitAsync("Scanning repository...", async () =>
            {
                await scanner.ScanAsync();
            });

            // Generate report document
            var reportDocument = scanner.GenerateReport();

            // Render the report using the renderer
            _Renderer.RenderReport(reportDocument);

            return 0;
        }
        catch (Exception ex)
        {
            _Renderer.RenderError($"Failed to analyze repository: {ex.Message}");
            return 1;
        }
    }

    private static string GetVersion()
    {
        return VersionUtility.GetVersion();
    }
}

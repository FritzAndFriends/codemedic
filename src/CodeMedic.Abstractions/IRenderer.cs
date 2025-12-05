namespace CodeMedic.Abstractions;


/// <summary>
/// An interface that defines common rendering capabilities.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders a banner with an optional subtitle at the top of the console output.
    /// </summary>
    /// <param name="subtitle">Optional subtitle text to display in the banner.</param>
    void RenderBanner(string subtitle = "");
    /// <summary>
    /// Renders an error message with error-specific formatting.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    void RenderError(string message);

    /// <summary>
    /// Renders an informational message with info-specific formatting.
    /// </summary>
    /// <param name="message">The informational message to display.</param>
    void RenderInfo(string message);
    /// <summary>
    /// Renders a section header to organize content into logical sections.
    /// </summary>
    /// <param name="title">The title of the section.</param>
    void RenderSectionHeader(string title);

	    /// <summary>
    /// Renders a footer message at the bottom of the console output.
    /// </summary>
    /// <param name="footer">The footer text to display.</param>
    void RenderFooter(string footer);
    /// <summary>
    /// Renders a wait message while executing an asynchronous operation.
    /// </summary>
    /// <param name="message">The message to display while waiting.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>A task that completes when the operation completes.</returns>
    Task RenderWaitAsync(string message, Func<Task> operation);
    
    /// <summary>
    /// Renders a report object. The renderer decides how to format it appropriately.
    /// </summary>
    /// <param name="report">The report to render (e.g., HealthReport, BomReport).</param>
    void RenderReport(object report);
		
}

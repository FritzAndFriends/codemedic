namespace CodeMedic.Abstractions;


/// <summary>
/// An interface that defines common rendering capabilities.
/// </summary>
public interface IRenderer
{
    void RenderBanner(string subtitle = "");
    void RenderError(string message);

    void RenderInfo(string message);
    void RenderSectionHeader(string title);

		void RenderFooter(string footer);
    Task RenderWaitAsync(string message, Func<Task> operation);
    
    /// <summary>
    /// Renders a report object. The renderer decides how to format it appropriately.
    /// </summary>
    /// <param name="report">The report to render (e.g., HealthReport, BomReport).</param>
    void RenderReport(object report);
		
}

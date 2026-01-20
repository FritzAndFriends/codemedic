namespace CodeMedic.Abstractions.Plugins;

/// <summary>
/// Payload for command handler execution.
/// </summary>
/// <param name="Args">Command-line arguments passed to the handler.</param>
/// <param name="ProjectTitle">Title of the project being operated on</param>
/// <param name="Renderer">Renderer instance for output formatting.</param>
public record struct HandlerPayload(
	string[] Args,
	string ProjectTitle,
	IRenderer Renderer);

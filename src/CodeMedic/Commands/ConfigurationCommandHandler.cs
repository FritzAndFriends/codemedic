using CodeMedic.Utilities;
using CodeMedic.Commands;
using CodeMedic.Output;

namespace CodeMedic.Commands;

/// <summary>
/// Handles configuration-related commands.
/// </summary>
public class ConfigurationCommandHandler
{
	private PluginLoader _PluginLoader;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigurationCommandHandler"/> class.
	/// </summary>
	/// <param name="pluginLoader">The plugin loader instance used to manage plugins.</param>
	public ConfigurationCommandHandler(PluginLoader pluginLoader)
	{
		_PluginLoader = pluginLoader;
	}

	/// <summary>
	/// Handles the configuration file command.
	/// </summary>
	/// <param name="configFilePath">The path to the configuration file.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the exit code.</returns>
	internal async Task<int> HandleConfigurationFileAsync(string configFilePath)
	{

		// Check if the configuration file exists
		if (!File.Exists(configFilePath))
		{
			RootCommandHandler.Console.RenderError($"Configuration file not found: {configFilePath}");
			return 1; // Return a non-zero exit code to indicate failure
		}

		// Load the specified configuration file - we need to identify the file type and load accordingly
		CodeMedicRunConfiguration config;
		try {
		config = LoadConfigurationFromFile(configFilePath);
		if (config == null)
		{
			RootCommandHandler.Console.RenderError($"Failed to load configuration from file: {configFilePath}");
			return 1; // Return a non-zero exit code to indicate failure
		}
		} catch (Exception ex)
		{
			RootCommandHandler.Console.RenderError($"Error loading configuration file: {ex.Message}");
			return 1; // Return a non-zero exit code to indicate failure
		}

		await RunCommandsForConfigurationAsync(config);

		return 0; // Return zero to indicate success

	}

	private async Task RunCommandsForConfigurationAsync(CodeMedicRunConfiguration config)
	{

		// For each repository in the configuration, run the specified commands
		foreach (var repoConfig in config.Repositories)
		{
			RootCommandHandler.Console.RenderInfo($"Processing repository: {repoConfig.Name} at {repoConfig.Path}");

			foreach (var commandName in repoConfig.Commands)
			{

				// Check that the command is registered with the plugin loader
				if (!_PluginLoader.Commands.ContainsKey(commandName))
				{
					RootCommandHandler.Console.RenderError($"  Command not registered: {commandName}");
					continue;
				}

				RootCommandHandler.Console.RenderInfo($"  Running command: {commandName}");

				// Load the command plugin
				var commandPlugin = _PluginLoader.GetCommand(commandName);
				if (commandPlugin == null)
				{
					RootCommandHandler.Console.RenderError($"    Command plugin not found: {commandName}");
					continue;
				}

				// Get the Formatter plugin for output formatting - we only support markdown for now
				// Ensure output directory exists
				Directory.CreateDirectory(config.Global.OutputDirectory);

				var reportPath = Path.Combine(config.Global.OutputDirectory, $"{repoConfig.Name}_{commandName}.md");

				// Execute the command with proper disposal of the StreamWriter
				using (var writer = new StreamWriter(reportPath))
				{
					var formatter = new MarkdownRenderer(writer);

					// Build arguments array to pass the repository path to the command
					var commandArgs = new[] { "--path", repoConfig.Path };

					await commandPlugin.Handler(commandArgs, formatter);
				}

			}
			// For now, just simulate with a delay
			await Task.Delay(500);

			RootCommandHandler.Console.RenderInfo($"Completed processing repository: {repoConfig.Name}");
		}

	}

	private CodeMedicRunConfiguration LoadConfigurationFromFile(string configFilePath)
	{
		// detect if the file is json or yaml based on extension
		var extension = Path.GetExtension(configFilePath).ToLower();
		var fileContents = File.ReadAllText(configFilePath);
		if (extension == ".json")
		{
			var config = System.Text.Json.JsonSerializer.Deserialize<CodeMedicRunConfiguration>(fileContents);
			if (config == null)
			{
				throw new InvalidOperationException("Failed to deserialize JSON configuration file.");
			}
			return config;
		}
		else if (extension == ".yaml" || extension == ".yml")
		{
			// Configure YAML deserializer - we use explicit YamlMember aliases on properties
			var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
				.Build();
			var config = deserializer.Deserialize<CodeMedicRunConfiguration>(fileContents);
			if (config == null)
			{
				throw new InvalidOperationException("Failed to deserialize YAML configuration file.");
			}
			return config;
		}
		else
		{
			throw new InvalidOperationException("Unsupported configuration file format. Only JSON and YAML are supported.");
		}
	}

}

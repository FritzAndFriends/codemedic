using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace CodeMedic.Commands;

/// <summary>
/// Represents the configuration settings for running an instance of CodeMedic.
/// </summary>
public class CodeMedicRunConfiguration
{
	/// <summary>
	/// Global properties for the CodeMedic configuration.
	/// </summary>
	[JsonPropertyName("global")]
	[YamlMember(Alias = "global")]
	public GlobalProperties Global { get; set; } = new GlobalProperties();

	/// <summary>
	/// The repositories to analyze.
	/// </summary>
	[JsonPropertyName("repositories")]
	[YamlMember(Alias = "repositories")]
	public RepositoryConfiguration[] Repositories { get; set; } = Array.Empty<RepositoryConfiguration>();

	// TODO: Add command configuration section called "commands" to define settings for each command

	/// <summary>
	/// Global properties for the CodeMedic configuration.
	/// </summary>
	public class GlobalProperties
	{

		/// <summary>
		/// The output format for the results.  Supports "markdown"
		/// </summary>
		[JsonPropertyName("format")]
		[YamlMember(Alias = "format")]
		public string Format { get; set; } = "markdown";

		/// <summary>
		/// The output directory for the results.
		/// </summary>
		[JsonPropertyName("output-dir")]
		[YamlMember(Alias = "output-dir")]
		public string OutputDirectory { get; set; } = ".";


	}

	/// <summary>
	/// The definition of a repository to analyze.
	/// </summary>
	public class RepositoryConfiguration
	{
		/// <summary>
		/// The relative path to the repository to analyze.
		/// </summary>
		[JsonPropertyName("path")]
		[YamlMember(Alias = "path")]
		public required string Path { get; set; } = string.Empty;

		/// <summary>
		/// The name of the repository to analyze.
		/// </summary>
		[JsonPropertyName("name")]
		[YamlMember(Alias = "name")]
		public required string Name { get; set; } = string.Empty;

		/// <summary>
		/// The commands to run against the repository.
		/// </summary>
		[JsonPropertyName("commands")]
		[YamlMember(Alias = "commands")]
		public string[] Commands { get; set; } = Array.Empty<string>();

	}

}

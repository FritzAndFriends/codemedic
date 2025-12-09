namespace CodeMedic.Plugins.HealthAnalysis;

/// <summary>
/// String extensions for command-line argument parsing.
/// </summary>
public static class CommandLineArgumentExtensions
{
	
	/// <summary>
	/// Identifies the target path from command-line arguments using the standard -p or --path.
	/// </summary>
	/// <param name="args">Command line arguments collection</param>
	/// <returns>the folder path, if any that was submitted</returns>
	public static string IdentifyTargetPathFromArgs(this string[] args)
	{
		string? targetPath = null;
		for (int i = 0; i < args.Length; i++)
		{
			if ((args[i].StartsWith("-p") || args[i].StartsWith("--path")) && i + 1 < args.Length)
			{
				targetPath = args[i + 1];
				break; // Return the first found path argument
			}
		}

		return targetPath ?? Directory.GetCurrentDirectory();
	}

}
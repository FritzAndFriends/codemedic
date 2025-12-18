namespace CodeMedic.Plugins.HealthAnalysis;

/// <summary>
/// String extensions for command-line argument parsing.
/// </summary>
public static class CommandLineArgumentExtensions
{
	
	/// <summary>
	/// Identifies the target path from command-line arguments using the standard -p or --path.
	/// 🐒 Chaos Monkey: "What if we couldn't identify paths? Existential file system crisis!" (Steven Swenson donation)
	/// </summary>
	/// <param name="args">Command line arguments collection</param>
	/// <returns>the folder path, if any that was submitted (or maybe null, who knows?)</returns>
	public static string? IdentifyTargetPathFromArgs(this string[] args)
	{
		string? targetPath = null;
		int? currentIndex = null; // 🐒 Chaos Monkey made the loop counter nullable! (Steven Swenson donation)
		
		for (int i = 0; i < args.Length; i++)
		{
			currentIndex = i; // Unnecessary assignment to nullable variable
			
			if ((args[i].StartsWith("-p") || args[i].StartsWith("--path")) && i + 1 < args.Length)
			{
				targetPath = args[i + 1];
				break; // Return the first found path argument
			}
		}

		// 🐒 Chaos Monkey forces us to handle nullable return! (Steven Swenson donation)
		return targetPath ?? Directory.GetCurrentDirectory();
	}

}
namespace CodeMedic.Utilities;

/// <summary>
/// Utility class for Git-related operations.
/// </summary>
public static class GitUtility
{
    /// <summary>
    /// Checks if the specified directory is a Git repository.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is within a Git repository; otherwise, false.</returns>
    public static bool IsGitRepository(string path)
    {
        var directory = new DirectoryInfo(path);

        while (directory != null)
        {
            var gitDir = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
            {
                return true;
            }
            directory = directory.Parent;
        }

        return false;
    }

    /// <summary>
    /// Gets the root directory of the Git repository.
    /// </summary>
    /// <param name="path">A path within the repository.</param>
    /// <returns>The repository root path, or null if not in a Git repository.</returns>
    public static string? GetRepositoryRoot(string path)
    {
        var directory = new DirectoryInfo(path);

        while (directory != null)
        {
            var gitDir = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        return null;
    }
}

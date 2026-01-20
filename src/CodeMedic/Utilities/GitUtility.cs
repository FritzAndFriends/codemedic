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

    /// <summary>
    /// Finds the root directory of the Git repository.
    /// Alias for GetRepositoryRoot for semantic clarity.
    /// </summary>
    /// <param name="path">A path within the repository.</param>
    /// <returns>The repository root path, or null if not in a Git repository.</returns>
    public static string? FindGitRepositoryRoot(string path) => GetRepositoryRoot(path);

    /// <summary>
    /// Gets the remote origin URL from a Git repository.
    /// </summary>
    /// <param name="repositoryRoot">The root directory of the Git repository.</param>
    /// <returns>The origin URL, or null if not found.</returns>
    public static string? GetGitRemoteOriginUrl(string repositoryRoot)
    {
        var gitDir = Path.Combine(repositoryRoot, ".git");
        string configPath;

        // Handle both regular .git directory and worktrees (where .git is a file)
        if (File.Exists(gitDir))
        {
            // This is a worktree - .git is a file pointing to the actual git directory
            var gitDirContent = File.ReadAllText(gitDir).Trim();
            if (gitDirContent.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
            {
                var actualGitDir = gitDirContent.Substring("gitdir:".Length).Trim();
                // Navigate from worktree git dir to main repo's config
                // Worktree gitdir points to .git/worktrees/<name>, we need parent .git/config
                var worktreeDir = new DirectoryInfo(actualGitDir);
                if (worktreeDir.Parent?.Name == "worktrees" && worktreeDir.Parent?.Parent != null)
                {
                    configPath = Path.Combine(worktreeDir.Parent.Parent.FullName, "config");
                }
                else
                {
                    configPath = Path.Combine(actualGitDir, "config");
                }
            }
            else
            {
                return null;
            }
        }
        else if (Directory.Exists(gitDir))
        {
            configPath = Path.Combine(gitDir, "config");
        }
        else
        {
            return null;
        }

        if (!File.Exists(configPath))
        {
            return null;
        }

        // Parse the git config file to find the origin URL
        var lines = File.ReadAllLines(configPath);
        var inOriginSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith('['))
            {
                inOriginSection = trimmedLine.Equals("[remote \"origin\"]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inOriginSection && trimmedLine.StartsWith("url", StringComparison.OrdinalIgnoreCase))
            {
                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex > 0)
                {
                    return trimmedLine.Substring(equalsIndex + 1).Trim();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the repository name from a Git URL.
    /// </summary>
    /// <param name="gitUrl">The Git URL (SSH or HTTPS format).</param>
    /// <returns>The repository name without the .git extension, or null if unable to parse.</returns>
    public static string? ExtractRepositoryNameFromGitUrl(string gitUrl)
    {
        if (string.IsNullOrWhiteSpace(gitUrl))
        {
            return null;
        }

        // Handle SSH format: git@github.com:owner/repo.git
        if (gitUrl.Contains(':') && gitUrl.Contains('@'))
        {
            var colonIndex = gitUrl.LastIndexOf(':');
            var path = gitUrl.Substring(colonIndex + 1);
            return ExtractRepoNameFromPath(path);
        }

        // Handle HTTPS format: https://github.com/owner/repo.git
        if (Uri.TryCreate(gitUrl, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimStart('/');
            return ExtractRepoNameFromPath(path);
        }

        return null;
    }

    private static string? ExtractRepoNameFromPath(string path)
    {
        // Remove .git suffix if present
        if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(0, path.Length - 4);
        }

        // Get the last segment (repository name)
        var segments = path.Split('/', '\\');
        var repoName = segments.LastOrDefault(s => !string.IsNullOrEmpty(s));

        return repoName;
    }
}

namespace CodeMedic.Abstractions;

/// <summary>
/// File system abstraction to improve testability of NuGetInspector.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Enumerates files matching the specified pattern.
    /// </summary>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    Stream OpenRead(string path);
}

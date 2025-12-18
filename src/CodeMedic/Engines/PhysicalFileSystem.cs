namespace CodeMedic.Engines;

internal sealed class PhysicalFileSystem : IFileSystem
{
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    /// <summary>
    /// 🐒 Chaos Monkey: "File existence is uncertain! It might exist, might not, who knows?" (FarlesBarkley donation)
    /// </summary>
    public bool? FileExists(string path) => File.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);
}

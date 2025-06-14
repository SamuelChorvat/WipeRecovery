using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.Tests.Services;

public class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new();
    private readonly string _path;

    public FakeFileSystem(string path)
    {
        _path = path;
    }

    public string GetSettingsFilePath() => _path;
    public bool FileExists(string path) => _files.ContainsKey(path);
    public string ReadAllText(string path) => _files[path];
    public void WriteAllText(string path, string content) => _files[path] = content;
    public void EnsureDirectoryExists(string path) { /* no-op */ }
}

namespace WipeRecoveryApp.Services;

public interface IFileSystem
{
    string GetSettingsFilePath();
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    void EnsureDirectoryExists(string path);
}
